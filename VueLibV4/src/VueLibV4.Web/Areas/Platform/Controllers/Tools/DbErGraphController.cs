using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 工具页面：数据库 ER 关系图。
/// 访问示例：/Platform/Tools/DbErGraph
/// </summary>
[Area("Platform")]
public class DbErGraphController : Controller
{
    private const string ToolView = "~/Areas/Platform/Views/Tools/{0}.cshtml";

    [HttpGet("/Platform/Tools/DbErGraph")]
    public IActionResult DbErGraph()
    {
        ViewData["Title"] = "数据库 ER 关系图 - VueLibV4";
        return View(string.Format(ToolView, "DbErGraph"));
    }
}
