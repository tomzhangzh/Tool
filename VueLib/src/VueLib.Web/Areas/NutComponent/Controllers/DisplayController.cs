using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.NutComponent.Controllers;

/// <summary>
/// NutUI 展示组件 Controller
/// </summary>
[Area("NutComponent")]
public class DisplayController : Controller
{
    public IActionResult Tag() => View();
    public IActionResult Text() => View();
    public IActionResult NoticeBar() => View();
    public IActionResult Progress() => View();
}
