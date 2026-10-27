using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面详情控制器：Detail 弹窗片段（V1/V2）与独立详情/表单页。
/// 访问示例：
///   /Platform/Detail/Modal?id=1&amp;rowId=2
///   /Platform/Detail/ModalV2?id=1&amp;rowId=2&amp;execId=xxx
///   /Platform/Detail/Form?settingId=3&amp;table=xxx&amp;bizId=1
/// </summary>
[Area("Platform")]
public class PageDetailController : Controller
{
    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;

    public PageDetailController(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
    }

    /// <summary>Detail 弹窗片段：供宿主页面 fetchPartial 拉取并注入弹窗。
    /// 读取 DynWebPage（detail-modal 模板实例），透传 rowId/project 入参，返回无 Layout 片段。</summary>
    [HttpGet("/Platform/Detail/Modal")]
    public IActionResult DetailModal(long id, long? rowId = null, string project = null, string prefill = null)
    {
        var page = _webPages.GetById((int)id);
        if (page == null) return Content("弹窗页面实例不存在：" + id);
        // project 优先取请求参数，缺省用页面实例自身的 ProjectId
        ViewBag.RowId = rowId?.ToString() ?? "";
        ViewBag.ProjectId = string.IsNullOrEmpty(project) ? (page.ProjectId?.ToString() ?? "") : project;
        // 主子表"+子"预填：query 传入的 JSON 字符串（如 {"ParentId":12}），原样透传给片段解析
        ViewBag.Prefill = prefill ?? "";
        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        var viewPath = string.IsNullOrWhiteSpace(template?.ViewPath)
            ? "~/Views/DynTemplates/DetailModal.cshtml"
            : template.ViewPath;
        return View(viewPath, page);
    }

    /// <summary>Detail 弹窗片段 V2（DynEventBus 版）：供 V2 模板 fetchPartial 拉取。</summary>
    [HttpGet("/Platform/Detail/ModalV2")]
    public IActionResult DetailModalV2(long id, long? rowId = null, string project = null, string execId = null)
    {
        var page = _webPages.GetById((int)id);
        if (page == null) return Content("弹窗页面实例不存在：" + id);
        ViewBag.RowId = rowId?.ToString() ?? "";
        ViewBag.ProjectId = string.IsNullOrEmpty(project) ? (page.ProjectId?.ToString() ?? "") : project;
        ViewBag.ExecId = execId ?? "";
        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        var viewPath = string.IsNullOrWhiteSpace(template?.ViewPath)
            ? "~/Views/DynTemplates/DetailModalV2.cshtml"
            : template.ViewPath;
        return View(viewPath, page);
    }

    /// <summary>
    /// 详情/表单独立页面：被列表页 open 动作以 fragment 弹窗拉取，也可独立访问。
    /// 只加载 Detail 屏的 PageSetting（ConfigJson=表单UI / DefaultJson=表单model骨架），
    /// 实现 Filter/List/Detail 三屏各自独立、按需取用，列表页不再预加载 Detail。
    /// </summary>
    [HttpGet("/Platform/Detail/Form")]
    public IActionResult DynDetail(long settingId, long? bizId, string table)
    {
        var s = settingId > 0 ? _settings.GetById((int)settingId) : null;
        if (s == null) return Content("Detail 页面配置不存在：" + settingId);
        var m = new DetailTemplateModel
        {
            SettingId = (int)settingId,
            Table = table ?? s.TableName ?? string.Empty,
            BizId = bizId ?? 0,
            PageTitle = s.Name,
            DetailDefaultJson = s.DefaultJson,
            RenderMode = s.RenderMode,
            PartialPath = s.PartialPath,
            WinTitle = Request.Query["winTitle"].FirstOrDefault(),
            WinWidth = Request.Query["winWidth"].FirstOrDefault(),
            WinHeight = Request.Query["winHeight"].FirstOrDefault(),
            WinMax = string.Equals(Request.Query["winMax"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase),
            Project = string.IsNullOrWhiteSpace(Request.Query["project"])
                ? DynPageViewHelper.PlatformProject
                : Request.Query["project"].FirstOrDefault(),
            PrefillJson = Request.Query["prefill"].FirstOrDefault()
        };
        if (!string.IsNullOrWhiteSpace(s.ConfigJson))
        {
            try { m.DetailConfig = JObject.Parse(s.ConfigJson); } catch { }
        }
        ViewData["Title"] = s.Name + " - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View("~/Views/DynTemplates/DetailTemplate.cshtml", m);
    }
}
