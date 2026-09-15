using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>
/// 组件元数据：设计器组件库的数据来源。
/// 驱动：组件面板分组、拖拽准入规则（allowDrop/acceptAll）、属性面板自动渲染（PropsMeta）。
/// </summary>
[Area("Platform")]
[Route("api/platform/componentmeta")]
[ApiController]
public class ComponentMetaController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public ComponentMetaController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var rows = string.IsNullOrEmpty(category)
            ? _svc.Query(db, "ComponentMeta", orderBy: "[Category] asc,[Id] asc")
            : _svc.Query(db, "ComponentMeta", "[Category]=@c", new { c = category }, "[Id] asc");
        return ApiResult.Ok(rows);
    }

    /// <summary>按分类分组，供设计器左侧组件库渲染</summary>
    [HttpGet("grouped")]
    public ApiResult Grouped()
    {
        using var db = _dbs.PlatformDb();
        var rows = _svc.Query(db, "ComponentMeta", "[IsActive]=@a", new { a = "1" }, "[Category] asc,[Id] asc");
        var groups = rows.GroupBy(r => r["Category"]?.ToString() ?? "未分类")
            .Select(g => new { category = g.Key, components = g.ToList() });
        return ApiResult.Ok(groups);
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "ComponentMeta", id)); }

    /// <summary>按组件名取元数据（运行时/校验用）</summary>
    [HttpGet("byName")]
    public ApiResult ByName(string name)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.First(db, "ComponentMeta", "[ComponentName]=@n", new { n = name }));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "ComponentMeta", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "ComponentMeta", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "ComponentMeta", keys), "删除成功");
    }
}
