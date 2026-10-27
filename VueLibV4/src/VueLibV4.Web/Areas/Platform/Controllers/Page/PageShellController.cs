using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面外壳控制器：工作台桌面 / 设计器 / 动态页面运行时外壳。
/// 访问示例：
///   /Platform/Shell/Desktop
///   /Platform/Shell/Designer
///   /Platform/Shell/PageRender?code=student-manage   动态页面
///   /Platform/Shell/Runtime?id=1                     模板引擎运行时（外壳视图）
/// </summary>
[Area("Platform")]
public class PageShellController : Controller
{
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;
    private readonly IConfiguration _config;

    public PageShellController(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings,
        IConfiguration config)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
        _config = config;
    }

    /// <summary>工作台桌面（DesktopSolution + DesktopShortcut）</summary>
    [HttpGet("/Platform/Shell/Desktop")]
    public IActionResult Desktop()
    {
        ViewData["Title"] = "VueLibV4 企业级低代码平台";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "Desktop"));
    }

    /// <summary>
    /// 页面设计器（三栏：组件库 / 画布 / 属性面板）。
    /// embed=1 时为弹窗嵌入模式（Layout=null 独立壳 + postMessage 协议，供 DynDesignerDialog 使用）。
    /// </summary>
    [HttpGet("/Platform/Shell/Designer")]
    public IActionResult Designer(string embed = null)
    {
        ViewData["Title"] = embed == "1" ? "页面设计器（嵌入） - VueLibV4" : "页面设计器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewBag.Embed = embed;
        return View(string.Format(Page, "Designer"));
    }

    /// <summary>动态页面运行时（DynWebPage，按 code 或 id）</summary>
    [HttpGet("/Platform/Shell/PageRender")]
    public IActionResult WebPageRender(string code = null, string id = null)
    {
        ViewData["Title"] = "动态页面 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewData["PageCode"] = code;
        ViewData["PageId"] = id;
        return View(string.Format(Page, "WebPageRender"));
    }

    /// <summary>
    /// 模板引擎运行时入口：/Platform/Shell/Runtime?id={DynWebPageId}
    /// 流程：读实例 → 读模板 → 解析实例参数(ConfigJson) → TableName 自动补齐 dyndata Url →
    /// 组装 DynSharedModel（RawParams / EffectiveParams / PassThrough / TemplateConfig / 三屏 PageSetting）→ 渲染模板外壳视图。
    /// </summary>
    [HttpGet("/Platform/Shell/Runtime")]
    public IActionResult DynWebPage(long id)
    {
        var page = _webPages.GetById((int)id);
        if (page == null) return Content("页面实例不存在：" + id);
        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        if (template == null) return Content("页面实例未绑定模板（TemplateId 为空）");

        // 1) 原始参数：用户在参数面板填写的结果（存于实例 ParamsJson；积木模板另有 blocks 槽位分组）
        var rawParams = PageRenderSupport.TryParseDict(page.ParamsJson);

        // 2) 自动推导：TableName 有值且 Url 为空 → 补 dyndata 免 model 接口；手动填写优先
        var effective = PageRenderSupport.BuildEffectiveParams(rawParams);
        // 三屏 PageSettingId 作为实例参数存于 ConfigJson（rawParams→effective），不再作为 DynWebPage 独立列。

        // 3) 模板 ConfigJson 顶层 mPassThrough（透传属性，向下传给外壳 / createapp）
        var passThrough = new JObject();
        try
        {
            var cfg = JObject.Parse(template.ConfigJson ?? "{}");
            if (cfg["mPassThrough"] is JObject pt) passThrough = pt;
        }
        catch { }

        // 4) 模板默认配置（外壳可选用）
        var templateConfig = new JObject();
        try { templateConfig = JObject.Parse(template.DefaultJson ?? "{}"); } catch { }

        // 5) 引用 PageSetting 配置：列表页加载 Filter/List；Detail 屏一并预加载
        var f = PageRenderSupport.LoadSetting(_settings, PageRenderSupport.GetInt(effective, "FilterPageSettingId"));
        var l = PageRenderSupport.LoadSetting(_settings, PageRenderSupport.GetInt(effective, "ListPageSettingId"));
        var d = PageRenderSupport.LoadSetting(_settings, PageRenderSupport.GetInt(effective, "DetailPageSettingId"));
        var model = new DynSharedModel
        {
            DynWebPageId = page.Id,
            DynTemplateId = template.Id,
            PageTitle = page.Name,
            TemplateCode = template.Code,
            RawParams = rawParams,
            EffectiveParams = effective,
            PassThrough = passThrough,
            TemplateConfig = templateConfig,
            FilterConfig = f?.Config,
            FilterDefaultJson = f?.DefaultJson,
            FilterRenderMode = f?.RenderMode,
            FilterPartialPath = f?.PartialPath,
            ListConfig = l?.Config,
            ListDefaultJson = l?.DefaultJson,
            ListRenderMode = l?.RenderMode,
            ListPartialPath = l?.PartialPath,
            DetailConfig = d?.Config,
            DetailDefaultJson = d?.DefaultJson,
            DetailRenderMode = d?.RenderMode,
            DetailPartialPath = d?.PartialPath
        };

        ViewData["Title"] = page.Name + " - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewData["ShowDesignEntry"] = _config.GetValue<bool>("Dyn:ShowRuntimeDesignEntry");
        var viewPath = string.IsNullOrWhiteSpace(template.ViewPath) ? "~/Views/DynTemplates/CrudBasic.cshtml" : template.ViewPath;
        // 积木化模板（triscreen-blocks 三屏 / filterlist-crud 筛选列表 / tree-detail 树形管理 /
        // tree-master-detail、list-master-detail 布局壳）：模板自行从 SpecJson/ParamsJson 读实例规格
        // + PageSettingService 读取相关 PageSetting 并动态拼装根 model（业务 key 由模板决定），直接透传 DynWebPage 实例。
        if (string.Equals(template.Code, "triscreen-blocks", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filterlist-crud", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tree-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tree-detail-v2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tree-master-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "list-master-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-open-window", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-open-windowV2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-drawer-leftV2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "detail-modal-v2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-tabbed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tab-host", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tab-container", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tabs-basic", StringComparison.OrdinalIgnoreCase))
        {
            return View(viewPath, page);
        }
        return View(viewPath, model);
    }
}
