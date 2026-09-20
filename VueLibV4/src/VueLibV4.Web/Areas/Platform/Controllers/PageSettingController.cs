using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 三屏页面设置（PageSetting）：筛选区/列表区/详情区的组件配置树 CRUD。
/// 路由：api/platform/pagesetting
/// </summary>
[Area("Platform")]
[Route("api/platform/pagesetting")]
[ApiController]
public class PageSettingController : ControllerBase
{
    private readonly DbFactory _dbs;
    public PageSettingController(DbFactory dbs) { _dbs = dbs; }

    /// <summary>全部（可按 type=Filter/List/Detail、projectId 过滤）</summary>
    [HttpGet("all")]
    public ApiResult All(string type = null, string projectId = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<PageSetting>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortNo)
            .OrderBy(p => p.Id, OrderByType.Asc);
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(p => p.SettingType == type);
        if (!string.IsNullOrWhiteSpace(projectId) && int.TryParse(projectId, out var pid))
            q = q.Where(p => p.ProjectId == pid);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        if (int.TryParse(id, out var iid))
            return ApiResult.Ok(db.Queryable<PageSetting>().First(p => p.Id == iid));
        return ApiResult.Ok(db.Queryable<PageSetting>().First(p => p.Code == id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] PageSetting data)
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
    public ApiResult Delete([FromBody] Newtonsoft.Json.Linq.JObject keys)
    {
        using var db = _dbs.PlatformDb();
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        db.Deleteable<PageSetting>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }
}
