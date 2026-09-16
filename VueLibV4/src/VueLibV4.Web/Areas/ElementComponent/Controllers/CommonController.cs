using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.ElementComponent.Controllers;

[Area("ElementComponent")]
public class CommonController : Controller
{
    public IActionResult Button() => View();
    public IActionResult Tag() => View();
    public IActionResult Badge() => View();
    public IActionResult Avatar() => View();
    public IActionResult Progress() => View();
    public IActionResult Alert() => View();
    public IActionResult Divider() => View();
    public IActionResult Image() => View();
}
