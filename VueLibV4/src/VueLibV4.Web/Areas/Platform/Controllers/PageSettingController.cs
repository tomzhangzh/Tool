using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;
using VueLibV4.Web.Services;

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
    private readonly IViewGenerateService _viewGen;
    public PageSettingController(IPageSettingService svc, IViewGenerateService viewGen)
    {
        _svc = svc;
        _viewGen = viewGen;
    }

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

    /// <summary>
    /// 按 ConfigJson 生成可手改的 cshtml 视图，并把虚拟路径回写 PageSetting.GeneratedViewPath；
    /// 之后该设置的 Front 渲染直接加载该文件。body：{ Id: 110, path: "可选自定义输出路径" }
    /// </summary>
    [HttpPost("generateview")]
    public async Task<ApiResult> GenerateView([FromBody] JObject body)
    {
        var id = body["Id"]?.Value<int?>() ?? body["id"]?.Value<int?>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        var setting = _svc.GetById(id);
        if (setting == null) return ApiResult.Fail($"PageSetting 不存在：Id={id}");

        var path = body["path"]?.ToString();
        ViewGenerateResult r;
        try
        {
            r = await _viewGen.GenerateAsync(id, string.IsNullOrWhiteSpace(path) ? null : path);
        }
        catch (InvalidOperationException ex)
        {
            // 业务前置条件不满足（如 List 型 columns 配置不是组件树）
            return ApiResult.Fail(ex.Message);
        }

        setting.GeneratedViewPath = r.ViewPath;
        _svc.Update(setting);

        return ApiResult.Ok(new
        {
            r.ViewPath,
            r.PhysicalPath,
            r.TagCount,
            r.SettingType
        }, $"视图已生成并切换为文件渲染：{r.ViewPath}（重新生成会覆盖）");
    }
}
