using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.ElementComponent.Controllers;

[Area("ElementComponent")]
public class FormItemController : Controller
{
    public IActionResult Input() => View();
    public IActionResult InputNumber() => View();
    public IActionResult Select() => View();
    public IActionResult Switch() => View();
    public IActionResult Radio() => View();
    public IActionResult Checkbox() => View();
    public IActionResult DatePicker() => View();
    public IActionResult TimePicker() => View();
    public IActionResult Slider() => View();
    public IActionResult Rate() => View();
    public IActionResult ColorPicker() => View();
}
