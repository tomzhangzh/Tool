using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.NutComponent.Controllers;

/// <summary>
/// NutUI 容器组件 Controller
/// </summary>
[Area("NutComponent")]
public class ContainerController : Controller
{
    public IActionResult Form() => View();
    public IActionResult CellGroup() => View();
    public IActionResult DivContainer() => View();
    public IActionResult Divider() => View();
    public IActionResult Grid() => View();
}
