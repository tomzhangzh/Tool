using System.IO;
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

    /// <summary>
    /// 页面设计器（三栏：组件库 / 画布 / 属性面板）。
    /// embed=1 时为弹窗嵌入模式（Layout=null 独立壳 + postMessage 协议，供 DynDesignerDialog 使用）。
    /// </summary>
    [HttpGet("/Platform/Page/Designer")]
    public IActionResult Designer(string embed = null)
    {
        ViewData["Title"] = embed == "1" ? "页面设计器（嵌入） - VueLibV4" : "页面设计器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewBag.Embed = embed;
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

    /// <summary>页面生成向导（M4）：选项目/选表/选字段 → 生成三屏页面</summary>
    [HttpGet("/Platform/Page/PageGen")]
    public IActionResult PageGen()
    {
        ViewData["Title"] = "页面生成向导 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "PageGen"));
    }

    /// <summary>DSL 编辑器（类Markdown：DSL / JSON / 页面预览 三栏）</summary>
    [HttpGet("/Platform/Page/Dsl")]
    public IActionResult Dsl()
    {
        ViewData["Title"] = "DSL 编辑器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "Dsl"));
    }

    /// <summary>组件元配置编辑器（设计器右侧「配置元」入口；fragment 弹窗按 componentId 指定组件）</summary>
    [HttpGet("/Platform/Page/MetaDsl")]
    public IActionResult MetaDsl(string componentId)
    {
        ViewData["Title"] = "组件元配置编辑器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewBag.ComponentId = componentId;
        return View(string.Format(Page, "MetaDsl"));
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

    /// <summary>Markdown 文档查看器（docs 目录内文档；安全：规范化路径并限制在 docs 内）</summary>
    [HttpGet("/Platform/Page/MdViewer")]
    public IActionResult MdViewer(string path)
    {
        ViewData["Title"] = "文档 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        // 解决方案根 docs 目录（AppContext.BaseDirectory = src/VueLibV4.Web/bin/Debug/net8.0/）
        var docsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs"));
        if (!Directory.Exists(docsRoot)) return Content("文档目录不存在：" + docsRoot);
        var safe = Path.GetFullPath(Path.Combine(docsRoot, path ?? ""));
        if (!safe.StartsWith(docsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !safe.Equals(docsRoot, StringComparison.OrdinalIgnoreCase))
            return Content("无效文档路径");
        if (!System.IO.File.Exists(safe)) return Content("文档不存在：" + path);
        ViewBag.MdPath = path;
        ViewBag.MdContent = System.IO.File.ReadAllText(safe, System.Text.Encoding.UTF8);
        ViewBag.MdName = Path.GetFileNameWithoutExtension(safe);
        return View(string.Format(Page, "MdViewer"));
    }
}
