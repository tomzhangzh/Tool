using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 元数据页面：DSL 编辑器、Meta DSL、页面生成向导
/// </summary>
[Area("Platform")]
public class MetaController : Controller
{
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

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
}
