using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Data;
using VueLibV4.Web.Core;

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
    public DynDataApiController(DynamicCrudService svc, ProjectDbResolver projects)
    {
        _svc = svc;
        _projects = projects;
    }

    private SqlSugar.SqlSugarClient Resolve(string project) => _projects.Resolve(project);

    // ---------------- 元数据 ----------------

    [HttpGet("tables")]
    public ApiResult Tables(string project = null)
    {
        try
        {
            using var db = Resolve(project);
            return ApiResult.Ok(_svc.Tables(db));
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    [HttpGet("columns")]
    public ApiResult Columns(string table, string project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        try
        {
            using var db = Resolve(project);
            return ApiResult.Ok(_svc.Columns(db, table));
        }
        catch (Exception ex) { return ApiResult.Fail(ex.GetBaseException().Message); }
    }

    [HttpGet("meta")]
    public ApiResult Meta(string table, string project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
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
    public ApiResult Search([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
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
    public ApiResult Get(string table, string id, string project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
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
    public ApiResult Insert([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var data = req["data"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (data == null) return ApiResult.Fail("缺少 data（契约：body 必须是 {table,data:{...}}）");
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            return ApiResult.Ok(_svc.Insert(db, table, data), "新增成功");
        }
        catch (Exception ex) { return ApiResult.Fail("新增失败：" + ex.GetBaseException().Message); }
    }

    [HttpPost("update")]
    public ApiResult Update([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var data = req["data"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (data == null) return ApiResult.Fail("缺少 data（契约：body 必须是 {table,data:{...}}）");
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            return ApiResult.Ok(_svc.Update(db, table, data), "更新成功");
        }
        catch (Exception ex) { return ApiResult.Fail("更新失败：" + ex.GetBaseException().Message); }
    }

    /// <summary>保存（有主键→更新，无主键→新增）。Block 写操作的标准端点。</summary>
    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var data = req["data"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (data == null) return ApiResult.Fail("缺少 data（契约：body 必须是 {table,data:{...}}）");
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            var pks = _svc.PrimaryKeys(db, table);
            var hasPk = pks.Count > 0
                && pks.All(p => data[p] != null && data[p].Type != JTokenType.Null && !string.IsNullOrEmpty(data[p].ToString()));
            return hasPk
                ? ApiResult.Ok(_svc.Update(db, table, data), "保存成功")
                : ApiResult.Ok(_svc.Insert(db, table, data), "保存成功");
        }
        catch (Exception ex) { return ApiResult.Fail("保存失败：" + ex.GetBaseException().Message); }
    }

    /// <summary>删除：body={table,keys:{Id:...},project?}</summary>
    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var keys = req["keys"] as JObject;
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");
        if (keys == null || keys.Count == 0)
            return ApiResult.Fail("缺少 keys（契约：body 必须是 {table,keys:{Id:...}}）");
        try
        {
            using var db = Resolve(req["project"]?.ToString());
            return ApiResult.Ok(_svc.Delete(db, table, keys), "删除成功");
        }
        catch (Exception ex) { return ApiResult.Fail("删除失败：" + ex.GetBaseException().Message); }
    }
}
