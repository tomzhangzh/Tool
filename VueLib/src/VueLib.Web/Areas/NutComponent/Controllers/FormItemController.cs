using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Areas.NutComponent.Controllers;

/// <summary>
/// NutUI 表单项组件 Controller
/// 每个 Action 对应一个组件，返回 Razor View（包含 template + comconfig script）
/// </summary>
[Area("NutComponent")]
public class FormItemController : Controller
{
    public IActionResult Input() => View();
    public IActionResult Textarea() => View();
    public IActionResult Switch() => View();
    public IActionResult Radio() => View();
    public IActionResult Checkbox() => View();
    public IActionResult Stepper() => View();
    public IActionResult Rate() => View();
    public IActionResult Slider() => View();
    public IActionResult Picker() => View();
    public IActionResult DatePicker() => View();
    public IActionResult Uploader() => View();
}
