using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Data;
using VueLibV4.Web.Core;
using VueLibV4.Web.Services;

namespace VueLibV4.Web.Controllers;

/// <summary>
/// 动态数据唯一端点：路由 /api/dyndata（平台/业务/项目库全走这里）。
/// 数据域只由 body.project 这一个坐标决定：
///   project 缺省                 → 默认业务库 BusinessDb
///   project="__platform__"       → 平台元数据库 PlatformDb（见 ProjectDbResolver.PlatformProjectKey）
///   project=DynProject.Code/Id   → 该项目独立库（无连接串时回退 BusinessDb）
/// 请求契约：search POST {table,page?,size?,filter?,sort?,project?}；
///           save/insert/update POST {table,data,project?}；delete POST {table,keys,project?}；
///           get GET ?table=&id=&project=。
/// </summary>
[Route("api/dyndata")]
[ApiController]
public class DynDataApiController : ControllerBase
{
    private readonly DynamicCrudService _svc;
    private readonly ProjectDbResolver _projects;
    private readonly IPermissionService _perm;
    public DynDataApiController(DynamicCrudService svc, ProjectDbResolver projects, IPermissionService perm)
    {
        _svc = svc;
        _projects = projects;
        _perm = perm;
    }

    private SqlSugar.SqlSugarClient Resolve(string project) => _projects.Resolve(project);

    /// <summary>表级权限校验，未绑定资源 = 放行</summary>
    private async Task<bool> TableAllowedAsync(string table, string level)
        => await _perm.CheckTableAsync(table, level);

    // ---------------- 元数据 ----------------

    [HttpGet("tables")]
    public async Task<ApiResult> Tables(string project = null)
    {
        // 库级表清单不绑定具体资源，登录即可访问；表级权限在 columns/meta/数据端点校验
        await Task.CompletedTask;
        try
        {
            using var db = Resolve(project);
            return ApiResult.Ok(_svc.Tables(db));
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    [HttpGet("columns")]
    public async Task<ApiResult> Columns(string table, string project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (!await TableAllowedAsync(table, "Read")) return ApiResult.Fail($"没有权限查询 {table} 结构", 403);
        try
        {
            using var db = Resolve(project);
            return ApiResult.Ok(_svc.Columns(db, table));
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    [HttpGet("meta")]
    public async Task<ApiResult> Meta(string table, string project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (!await TableAllowedAsync(table, "Read")) return ApiResult.Fail($"没有权限查询 {table} 结构", 403);
        try
        {
            using var db = Resolve(project);
            return ApiResult.Ok(new
            {
                table,
                columns = _svc.Columns(db, table),
                primaryKeys = _svc.PrimaryKeys(db, table)
            });
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    // ---------------- 查询 ----------------

    /// <summary>分页查询：body={table,page,size,filter?,sort?,project?}；筛选告警随 data.warnings 回流。</summary>
    [HttpPost("search")]
    public async Task<ApiResult> Search([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (!await TableAllowedAsync(table, "Read")) return ApiResult.Fail($"没有权限查询 {table}", 403);
        var project = req["project"]?.ToString();
        var page = req["page"]?.Value<int>() ?? 1;
        var size = req["size"]?.Value<int>() ?? 20;
        var filter = req["filter"] as JObject;

        // 前端积木（TreeApp 等）统一传顶层 sort:{field,order}，转成 Page() 认识的 __orderby。
        // 列名与 asc/desc 由 Page() 内的白名单正则再校验一遍。
        var sort = req["sort"] as JObject;
        if (sort != null)
        {
            var field = sort["field"]?.ToString();
            var order = (sort["order"]?.ToString() ?? "asc").ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(field))
            {
                filter ??= new JObject();
                filter["__orderby"] = field + (order.StartsWith("desc") ? " desc" : " asc");
            }
        }

        try
        {
            using var db = Resolve(project);
            return ApiResult.Ok(_svc.Page(db, table, page, size, filter));
        }
        catch (Exception ex) { return ApiResult.Fail("查询失败：" + ex.GetBaseException().Message); }
    }

    /// <summary>按主键取单行（query：table/id/project）</summary>
    [HttpGet("get")]
    public async Task<ApiResult> Get(string table, string id, string project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (!await TableAllowedAsync(table, "Read")) return ApiResult.Fail($"没有权限读取 {table}", 403);
        try
        {
            using var db = Resolve(project);
            var pks = _svc.PrimaryKeys(db, table);
            if (pks.Count == 0) return ApiResult.Fail("表没有主键");
            return ApiResult.Ok(_svc.First(db, table, $"{DynamicCrudService.QuoteIdent(pks[0])}=@v", new { v = id }));
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    // ---------------- 增删改（统一信封，异常永远 JSON，不冒 HTML 异常页） ----------------

    [HttpPost("insert")]
    public async Task<ApiResult> Insert([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var data = req["data"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (data == null) return ApiResult.Fail("缺少 data（契约：body 必须是 {table,data:{...}}）");
        if (!await TableAllowedAsync(table, "Edit")) return ApiResult.Fail($"没有权限新增 {table}", 403);
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            return ApiResult.Ok(_svc.Insert(db, table, data), "新增成功");
        }
        catch (Exception ex) { return ApiResult.Fail("新增失败：" + ex.GetBaseException().Message); }
    }

    [HttpPost("update")]
    public async Task<ApiResult> Update([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var data = req["data"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (data == null) return ApiResult.Fail("缺少 data（契约：body 必须是 {table,data:{...}}）");
        if (!await TableAllowedAsync(table, "Edit")) return ApiResult.Fail($"没有权限修改 {table}", 403);
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            return ApiResult.Ok(_svc.Update(db, table, data), "更新成功");
        }
        catch (Exception ex) { return ApiResult.Fail("更新失败：" + ex.GetBaseException().Message); }
    }

    /// <summary>保存（有主键→更新，无主键→新增）。Block 写操作的标准端点。</summary>
    [HttpPost("save")]
    public async Task<ApiResult> Save([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var data = req["data"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (data == null) return ApiResult.Fail("缺少 data（契约：body 必须是 {table,data:{...}}）");
        if (!await TableAllowedAsync(table, "Edit")) return ApiResult.Fail($"没有权限保存 {table}", 403);
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            var pks = _svc.PrimaryKeys(db, table);
            // 主键缺失判据：null/JSON null/空字符串/数值 0。
            // 前端新增约定常传 Id:0（自增表从 1 起），0 必须按新增处理，否则 UPDATE ... WHERE Id=0
            // 影响 0 行却返回"保存成功"，造成新增静默丢失。
            var hasPk = pks.Count > 0 && pks.All(p => IsUsablePkValue(data[p]));
            return hasPk
                ? ApiResult.Ok(_svc.Update(db, table, data), "保存成功")
                : ApiResult.Ok(_svc.Insert(db, table, data), "保存成功");
        }
        catch (Exception ex) { return ApiResult.Fail("保存失败：" + ex.GetBaseException().Message); }
    }

    /// <summary>删除：body={table,keys:{Id:...},project?}</summary>
    [HttpPost("delete")]
    public async Task<ApiResult> Delete([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var keys = req["keys"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (keys == null || keys.Count == 0)
            return ApiResult.Fail("缺少 keys（契约：body 必须是 {table,keys:{Id:...}}）");
        if (!await TableAllowedAsync(table, "Delete")) return ApiResult.Fail($"没有权限删除 {table}", 403);
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            return ApiResult.Ok(_svc.Delete(db, table, keys), "删除成功");
        }
        catch (Exception ex) { return ApiResult.Fail("删除失败：" + ex.GetBaseException().Message); }
    }

    /// <summary>
    /// 批量操作（事务）：body={table,project?,ops:[{op:insert|update|delete|save, data?, keys?}]}。
    /// 任一步失败全部回滚。ops 里 op=save 自动判断 insert/update（按主键是否存在）。
    /// 权限：insert/update/save 需 Edit；op=delete 必须额外具备 Delete 权限，在执行前逐表校验。
    /// 返回每步结果数组。
    /// </summary>
    [HttpPost("batch")]
    public async Task<ApiResult> Batch([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var ops = req["ops"] as JArray;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (ops == null || ops.Count == 0) return ApiResult.Fail("缺少 ops 数组");
        if (!await TableAllowedAsync(table, "Edit")) return ApiResult.Fail($"没有权限批量操作 {table}", 403);
        // 只要批次里含删除步骤，就必须具备 Delete 权限，杜绝"只有 Edit 也能通过 batch 删数据"
        var hasDeleteOp = ops.Any(o => string.Equals((o as JObject)?["op"]?.ToString(), "delete", StringComparison.OrdinalIgnoreCase));
        if (hasDeleteOp && !await TableAllowedAsync(table, "Delete"))
            return ApiResult.Fail($"没有权限删除 {table}，批量操作被拒绝", 403);
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            var results = new List<object>();
            var pks = _svc.PrimaryKeys(db, table);

            try
            {
                db.Ado.BeginTran();
                for (var i = 0; i < ops.Count; i++)
                {
                    var op = ops[i] as JObject;
                    var opName = op?["op"]?.ToString()?.ToLower();
                    object result;
                    switch (opName)
                    {
                        case "insert":
                            result = _svc.Insert(db, table, op["data"] as JObject);
                            break;
                        case "update":
                            result = _svc.Update(db, table, op["data"] as JObject);
                            break;
                        case "delete":
                            result = _svc.Delete(db, table, op["keys"] as JObject);
                            break;
                        case "save":
                            var data = op["data"] as JObject;
                            var opHasPk = pks.Count > 0 && pks.All(p => IsUsablePkValue(data?[p]));
                            result = opHasPk
                                ? _svc.Update(db, table, data)
                                : _svc.Insert(db, table, data);
                            break;
                        default:
                            throw new Exception($"第 {i + 1} 步 op 无效: {opName}（支持 insert/update/delete/save）");
                    }
                    results.Add(new { step = i + 1, op = opName, result });
                }
                db.Ado.CommitTran();
                return ApiResult.Ok(new { total = ops.Count, results }, $"批量操作成功（{ops.Count} 步）");
            }
            catch (Exception ex)
            {
                db.Ado.RollbackTran();
                return ApiResult.Fail("批量操作已回滚：" + ex.GetBaseException().Message);
            }
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    /// <summary>
    /// 主键值是否可用于 UPDATE 定位：null/JSON null/空白字符串/数值 0 一律视为"无主键"（走新增）。
    /// 自增表从 1 起，前端新增约定的 Id:0 不能进 WHERE Id=0（影响 0 行却假报成功）。
    /// </summary>
    private static bool IsUsablePkValue(JToken v)
    {
        if (v == null || v.Type == JTokenType.Null || v.Type == JTokenType.Undefined) return false;
        if (v.Type == JTokenType.Integer) return v.Value<long>() != 0;
        if (v.Type == JTokenType.Float) return v.Value<double>() != 0;
        if (v.Type == JTokenType.String) return !string.IsNullOrWhiteSpace(v.Value<string>());
        return true;
    }
}
