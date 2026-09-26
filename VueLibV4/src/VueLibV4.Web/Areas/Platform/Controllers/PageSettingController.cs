using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

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
    private readonly IPageSettingService _svc;
    public PageSettingController(IPageSettingService svc) { _svc = svc; }

    /// <summary>全部（可按 type=Filter/List/Detail、projectId 过滤）</summary>
    [HttpGet("all")]
    public ApiResult All(string type = null, string projectId = null)
    {
        int? pid = (!string.IsNullOrWhiteSpace(projectId) && int.TryParse(projectId, out var v)) ? v : null;
        var q = _svc.Query(p => p.IsActive
                && (string.IsNullOrWhiteSpace(type) || p.SettingType == type)
                && (pid == null || p.ProjectId == pid))
            .OrderBy(p => p.SortNo)
            .OrderBy(p => p.Id, OrderByType.Asc);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        if (int.TryParse(id, out var iid))
            return ApiResult.Ok(_svc.GetById(iid));
        return ApiResult.Ok(_svc.Query(p => p.Code == id).First());
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] PageSetting data)
    {
        if (data.Id <= 0)
        {
            _svc.Insert(data);
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        _svc.Update(data);
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        _svc.DeleteById(id);
        return ApiResult.Ok(true, "删除成功");
    }
}
