using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Controllers;

[Route("component-manage")]
public class ComponentManageController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/ComponentManage/Index.cshtml");
}
