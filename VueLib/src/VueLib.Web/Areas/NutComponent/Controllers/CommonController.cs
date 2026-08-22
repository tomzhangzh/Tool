using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.NutComponent.Controllers;

/// <summary>
/// NutUI 通用组件 Controller
/// </summary>
[Area("NutComponent")]
public class CommonController : Controller
{
    public IActionResult Button() => View();
    public IActionResult Image() => View();
}
