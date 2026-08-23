using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.NutComponent.Controllers;

/// <summary>
/// NutUI 移动端业务组件 Controller
/// 每个 Action 对应一个组件，返回 Razor View（包含 template + comconfig script）
/// </summary>
[Area("NutComponent")]
public class MobileController : Controller
{
    public IActionResult Icon() => View();
    public IActionResult Empty() => View();
    public IActionResult StatCard() => View();
    public IActionResult MenuItem() => View();
    public IActionResult NavBar() => View();
    public IActionResult BottomNav() => View();
    public IActionResult HeroBanner() => View();
    public IActionResult GridMenu() => View();
    public IActionResult ReportCard() => View();
    public IActionResult ReportFilter() => View();
    public IActionResult EChart() => View();
    public IActionResult DataTable() => View();
    public IActionResult ViewToggle() => View();
    public IActionResult ProfileHeader() => View();
    public IActionResult LoginCard() => View();
}
