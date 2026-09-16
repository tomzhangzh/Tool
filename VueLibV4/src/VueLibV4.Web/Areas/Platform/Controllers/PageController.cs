using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面视图控制器：所有页面由服务器端 Razor View 返回（而非静态 HTML），
/// 便于后端注入全局配置（ApiBase / 页面参数 / 权限校验）。
/// 访问示例：
///   /Platform/Page/Desktop                    工作台桌面
///   /Platform/Page/Designer                   页面设计器
///   /Platform/Page/WebPageRender?code=student-manage   动态页面
///   /Platform/Page/Demo/ActionHelper          动作助手 Demo
/// </summary>
[Area("Platform")]
public class PageController : Controller
{
    private const string Page = "~/Views/Platform/Page/{0}.cshtml";
    private const string Demo = "~/Views/Platform/Demo/{0}.cshtml";

    /// <summary>工作台桌面（DesktopSolution + DesktopShortcut）</summary>
    [HttpGet("/Platform/Page/Desktop")]
    public IActionResult Desktop()
    {
        ViewData["Title"] = "VueLibV4 企业级低代码平台";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "Desktop"));
    }

    /// <summary>页面设计器（三栏：组件库 / 画布 / 属性面板）</summary>
    [HttpGet("/Platform/Page/Designer")]
    public IActionResult Designer()
    {
        ViewData["Title"] = "页面设计器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "Designer"));
    }

    /// <summary>动态页面运行时（DynWebPage，按 code 或 id）</summary>
    [HttpGet("/Platform/Page/WebPageRender")]
    public IActionResult WebPageRender(string code = null, string id = null)
    {
        ViewData["Title"] = "动态页面 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewData["PageCode"] = code;
        ViewData["PageId"] = id;
        return View(string.Format(Page, "WebPageRender"));
    }

    [HttpGet("/Platform/Page/Demo/ActionHelper")]
    public IActionResult DemoActionHelper()
    {
        ViewData["Title"] = "动作助手 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "ActionHelper"));
    }

    [HttpGet("/Platform/Page/Demo/Combination")]
    public IActionResult DemoCombination()
    {
        ViewData["Title"] = "组合组件 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Combination"));
    }

    [HttpGet("/Platform/Page/Demo/Grid")]
    public IActionResult DemoGrid()
    {
        ViewData["Title"] = "栅格布局 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Grid"));
    }

    [HttpGet("/Platform/Page/Demo/Crud")]
    public IActionResult DemoCrud()
    {
        ViewData["Title"] = "免模型 CRUD Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Crud"));
    }

    [HttpGet("/Platform/Page/Demo/TemplateImport")]
    public IActionResult DemoTemplateImport()
    {
        ViewData["Title"] = "模板导入 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "TemplateImport"));
    }

    [HttpGet("/Platform/Page/Demo/CodeMirror")]
    public IActionResult DemoCodeMirror()
    {
        ViewData["Title"] = "CodeMirror Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "CodeMirror"));
    }
}
