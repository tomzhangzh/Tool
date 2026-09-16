using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 组件元数据：设计器组件库的数据来源（强类型 Model）。
/// 驱动：组件面板分组、拖拽准入规则（allowDrop/acceptAll）、属性面板自动渲染（PropsMeta）。
/// </summary>
[Area("Platform")]
[Route("api/platform/componentmeta")]
[ApiController]
public class ComponentMetaController : ControllerBase
{
    private readonly DbFactory _dbs;
    public ComponentMetaController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<ComponentMeta>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .OrderBy(c => c.Id);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(c => c.Category == category);
        return ApiResult.Ok(q.ToList());
    }

    /// <summary>按分类分组，供设计器左侧组件库渲染</summary>
    [HttpGet("grouped")]
    public ApiResult Grouped()
    {
        using var db = _dbs.PlatformDb();
        var rows = db.Queryable<ComponentMeta>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .OrderBy(c => c.Id)
            .ToList();
        var groups = rows.GroupBy(r => r.Category ?? "未分类")
            .Select(g => new { category = g.Key, components = g.ToList() });
        return ApiResult.Ok(groups);
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        var row = FirstByIdOrCode(db, id);
        return ApiResult.Ok(row);
    }

    /// <summary>按组件名取元数据（运行时/校验用）</summary>
    [HttpGet("byName")]
    public ApiResult ByName(string name)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(db.Queryable<ComponentMeta>().First(c => c.ComponentName == name));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] ComponentMeta data)
    {
        using var db = _dbs.PlatformDb();
        if (data.Id <= 0)
        {
            db.Insertable(data).ExecuteCommand();
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        db.Updateable(data).ExecuteCommand();
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        db.Deleteable<ComponentMeta>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private ComponentMeta FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<ComponentMeta>().First(c => c.Id == id);
        return db.Queryable<ComponentMeta>().First(c => c.ComponentName == key);
    }
}
