using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页：真正的动态页面（强类型 Model + 强类型服务，外键 int Id）。
/// 选择一个 DynTemplate，结合用户配置参数（ConfigJson）实例化 PageJson，即可运行。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynwebpage")]
[ApiController]
public class DynWebPageController : ControllerBase
{
    private readonly IDynWebPageService _svc;
    private readonly IDynProjectService _projects;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;

    public DynWebPageController(
        IDynWebPageService svc,
        IDynProjectService projects,
        IDynTemplateService templates,
        IPageSettingService settings)
    {
        _svc = svc;
        _projects = projects;
        _templates = templates;
        _settings = settings;
    }

    [HttpGet("all")]
    public ApiResult All(string projectId = null)
    {
        // 兼容：projectId 可为数字 Id 或项目 Code
        if (!string.IsNullOrEmpty(projectId) && !int.TryParse(projectId, out _))
        {
            var proj = _projects.List(x => x.Code == projectId).FirstOrDefault();
            if (proj == null) return ApiResult.Ok(new List<DynWebPage>());
            projectId = proj.Id.ToString();
        }
        int? pid = int.TryParse(projectId, out var v) ? v : null;

        var rows = _svc.Query(p => p.IsActive && (pid == null || p.ProjectId == pid))
            .OrderBy(p => p.CreateTime, OrderByType.Desc)
            .ToList();
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    /// <summary>
    /// 运行时渲染数据：解析 PageJson；
    /// 若页面未实例化（PageJson 为空）则回退到模板 TemplateJson —— 选择模板+配置即完成页面。
    /// </summary>
    [HttpGet("render")]
    public ApiResult Render(string id, string code = null)
    {
        var row = string.IsNullOrEmpty(id)
            ? _svc.Query(p => p.Code == code).First()
            : FirstByIdOrCode(id);
        if (row == null) return ApiResult.Fail("页面不存在");
        var result = new JObject { ["id"] = row.Id, ["name"] = row.Name, ["code"] = row.Code };

        JObject config = null;
        if (!string.IsNullOrWhiteSpace(row.PageJson))
        {
            try { config = JObject.Parse(row.PageJson); } catch { config = null; }
        }
        if (config == null && row.TemplateId > 0)
        {
            var tpl = _templates.GetById(row.TemplateId.Value);
            if (tpl != null)
            {
                try { config = JObject.Parse(tpl.TemplateJson ?? "{}"); } catch { config = null; }
            }
        }

        result["config"] = config ?? new JObject();
        try { result["configjson"] = JObject.Parse(row.ConfigJson ?? "{}"); }
        catch { result["configjson"] = new JObject(); }

        // M4 三屏固定模板：返回模板 Code、运行 URL 与筛选/列表/详情三份配置树
        string templateCode = null;
        if (row.TemplateId != null)
        {
            var tpl2 = _templates.GetById(row.TemplateId.Value);
            templateCode = tpl2?.Code;
        }
        result["templateCode"] = templateCode;
        result["url"] = row.Url;
        result["filterConfig"] = LoadSettingConfig(row.FilterPageSettingId);
        result["listConfig"] = LoadSettingConfig(row.ListPageSettingId);
        result["detailConfig"] = LoadSettingConfig(row.DetailPageSettingId);
        return ApiResult.Ok(result);
    }

    private JObject LoadSettingConfig(int? settingId)
    {
        if (settingId == null) return null;
        var s = _settings.GetById(settingId.Value);
        if (s == null || string.IsNullOrWhiteSpace(s.ConfigJson)) return null;
        try { return JObject.Parse(s.ConfigJson); } catch { return null; }
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynWebPage data)
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

    private DynWebPage FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(p => p.Code == key).First();
    }
}
