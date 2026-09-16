using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Business.Controllers;

/// <summary>
/// 免模型动态数据控制器（核心）：
/// 只要知道表名，即可完成 增删改查、分页、过滤、排序。
/// 表属于某个 DynProject（业务库），通过项目 Code/Id 解析连接串。
/// 数据列全 nvarchar，无需实体类。
/// </summary>
[Area("Business")]
[Route("api/business/dyndata")]
[ApiController]
public class DynDataController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynDataController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    /// <summary>解析项目库：project 可为 DynProject.Code（推荐）或数字 Id，缺省用 BusinessDb</summary>
    private SqlSugarClient ResolveDb(string project)
    {
        if (string.IsNullOrWhiteSpace(project))
            return _dbs.BusinessDb();
        using var pdb = _dbs.PlatformDb();
        JObject proj = long.TryParse(project, out var pid)
            ? _svc.First(pdb, "DynProject", "[Id]=@id", new { id = pid })
            : _svc.First(pdb, "DynProject", "[Code]=@code", new { code = project });
        if (proj == null) return _dbs.BusinessDb();
        var cs = proj["ConnectionString"]?.ToString();
        return string.IsNullOrWhiteSpace(cs) ? _dbs.BusinessDb() : _dbs.ProjectDb(cs);
    }

    // ---------------- 元数据 ----------------

    [HttpGet("tables")]
    public ApiResult Tables(string project = null)
    {
        using var db = ResolveDb(project);
        return ApiResult.Ok(_svc.Tables(db));
    }

    [HttpGet("columns")]
    public ApiResult Columns(string table, string project = null)
    {
        using var db = ResolveDb(project);
        return ApiResult.Ok(_svc.Columns(db, table));
    }

    /// <summary>表 + 列 + 主键 一次取回（供 DynCrudPage / 设计器数据绑定）</summary>
    [HttpGet("meta")]
    public ApiResult Meta(string table, string project = null)
    {
        using var db = ResolveDb(project);
        var cols = _svc.Columns(db, table);
        return ApiResult.Ok(new
        {
            table,
            columns = cols,
            primaryKeys = _svc.PrimaryKeys(db, table)
        });
    }

    // ---------------- 查询 ----------------

    [HttpGet("page")]
    public ApiResult Page(string table, int page = 1, int size = 20, string filter = null, string project = null)
    {
        using var db = ResolveDb(project);
        JObject f = null;
        if (!string.IsNullOrWhiteSpace(filter))
        {
            try { f = JObject.Parse(filter); } catch { return ApiResult.Fail("filter 不是合法 JSON"); }
        }
        return ApiResult.Ok(_svc.Page(db, table, page, size, f));
    }

    [HttpPost("search")]
    public ApiResult Search([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var project = req["project"]?.ToString();
        var page = req["page"]?.Value<int>() ?? 1;
        var size = req["size"]?.Value<int>() ?? 20;
        var filter = req["filter"] as JObject;
        using var db = ResolveDb(project);
        return ApiResult.Ok(_svc.Page(db, table, page, size, filter));
    }

    [HttpGet("get")]
    public ApiResult Get(string table, string id, string project = null)
    {
        using var db = ResolveDb(project);
        var pks = _svc.PrimaryKeys(db, table);
        if (pks.Count == 0) return ApiResult.Fail("表没有主键");
        return ApiResult.Ok(_svc.First(db, table, $"[{pks[0]}]=@v", new { v = id }));
    }

    // ---------------- 增删改 ----------------

    [HttpPost("insert")]
    public ApiResult Insert([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var project = req["project"]?.ToString();
        var data = req["data"] as JObject;
        using var db = ResolveDb(project);
        return ApiResult.Ok(_svc.Insert(db, table, data), "新增成功");
    }

    [HttpPost("update")]
    public ApiResult Update([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var project = req["project"]?.ToString();
        var data = req["data"] as JObject;
        using var db = ResolveDb(project);
        return ApiResult.Ok(_svc.Update(db, table, data), "更新成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var project = req["project"]?.ToString();
        var keys = req["keys"] as JObject;
        using var db = ResolveDb(project);
        return ApiResult.Ok(_svc.Delete(db, table, keys), "删除成功");
    }

    /// <summary>保存（有主键→更新，无主键→新增）</summary>
    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject req)
    {
        var table = req["table"]?.ToString();
        var project = req["project"]?.ToString();
        var data = req["data"] as JObject;
        using var db = ResolveDb(project);
        var pks = _svc.PrimaryKeys(db, table);
        var hasPk = pks.Count > 0 && pks.All(p => data?[p] != null && data[p].Type != JTokenType.Null && !string.IsNullOrEmpty(data[p].ToString()));
        return hasPk
            ? ApiResult.Ok(_svc.Update(db, table, data), "保存成功")
            : ApiResult.Ok(_svc.Insert(db, table, data), "保存成功");
    }
}
