using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 工具页面：组件检查（在线测试组件）。
/// 访问示例：/Platform/Tools/ComponentCheck
/// </summary>
[Area("Platform")]
public class ComponentCheckController : Controller
{
    private const string ToolView = "~/Areas/Platform/Views/Tools/{0}.cshtml";

    /// <summary>组件检查：在线测试组件</summary>
    [HttpGet("/Platform/Tools/ComponentCheck")]
    public IActionResult ComponentCheck()
    {
        ViewData["Title"] = "组件检查 - VueLibV4";
        return View(string.Format(ToolView, "ComponentCheck"));
    }
}
