using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.ElementComponent.Controllers;

[Area("ElementComponent")]
public class ContainerController : Controller
{
    public IActionResult DivContainer() => View();
    public IActionResult Card() => View();
    public IActionResult Row() => View();
    public IActionResult Col() => View();
    public IActionResult Tabs() => View();
}
