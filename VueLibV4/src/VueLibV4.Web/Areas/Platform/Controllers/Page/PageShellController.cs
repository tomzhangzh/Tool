using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面外壳控制器：工作台桌面 / 设计器 / 动态页面运行时外壳。
/// 只做「解析请求 → 调运行时装配器 → 选视图」，页面装配逻辑见 <see cref="IPageRuntimeComposer"/>。
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

    private readonly IPageRuntimeComposer _composer;
    private readonly IConfiguration _config;
    private readonly IDynWebPageService _pages;

    public PageShellController(IPageRuntimeComposer composer, IConfiguration config, IDynWebPageService pages)
    {
        _composer = composer;
        _config = config;
        _pages = pages;
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
    /// embed=1 时为弹窗嵌入模式（Layout=null 独立壳 + postMessage 协议，供 DynDesignerDialog 使用）；
    /// 传 id/code 时直接载入已有 DynWebPage，刷新/重新打开不丢上下文。
    /// </summary>
    [HttpGet("/Platform/Shell/Designer")]
    public IActionResult Designer(string embed = null, string code = null, int? id = null)
    {
        ViewData["Title"] = embed == "1" ? "页面设计器（嵌入） - VueLibV4" : "页面设计器 - VueLibV4";
        ViewData["ApiBase"] = "/api";

        var page = ResolveDesignerPage(code, id);
        ViewBag.Embed = embed;
        ViewBag.PageId = page?.Id ?? 0;
        ViewBag.PageCode = page?.Code ?? string.Empty;
        ViewBag.PageName = page?.Name ?? string.Empty;
        ViewBag.InitialPageJson = page == null ? null : MinifyJson(page.PageJson);
        return View(string.Format(Page, "Designer"));
    }

    /// <summary>按 id 或 code 取待编辑页面；不存在时返回 null（设计器进入空白页）。</summary>
    private DynWebPage? ResolveDesignerPage(string? code, int? id)
    {
        try
        {
            if (id.HasValue && id.Value > 0) return _pages.GetById(id.Value);
            if (!string.IsNullOrWhiteSpace(code))
                return _pages.Query(p => p.Code == code).First();
        }
        catch
        {
            // 设计器入口不应因页面不存在而 500：回退空白画布
        }
        return null;
    }

    /// <summary>压缩 JSON（去掉换行/缩进），避免内嵌到单引号属性时被空白撑大。</summary>
    private static string? MinifyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JObject.Parse(json).ToString(Newtonsoft.Json.Formatting.None); }
        catch { return json; }
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
    /// 装配（实例 → 模板 → 参数推导 → 三屏 PageSetting → 共享模型）全部下沉到 <see cref="IPageRuntimeComposer"/>；
    /// 规格驱动模板（能力位在模板 ConfigJson 声明）由外壳视图自行读 SpecJson，控制器不做特判。
    /// </summary>
    [HttpGet("/Platform/Shell/Runtime")]
    public IActionResult DynWebPage(long id)
    {
        var result = _composer.Compose((int)id);
        if (!result.Ok) return Content(result.Error ?? "页面装配失败");

        ViewData["Title"] = result.Title + " - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewData["ShowDesignEntry"] = _config.GetValue<bool>("Dyn:ShowRuntimeDesignEntry");
        return View(result.ViewPath, result.ViewModel);
    }
}
