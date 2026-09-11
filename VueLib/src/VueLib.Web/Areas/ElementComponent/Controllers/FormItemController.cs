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

    // ==================== 常用组件（简化配置，参考 SunnySystem EditorTemplates） ====================
    public IActionResult ComInput() => View();
    public IActionResult ComTextArea() => View();
    public IActionResult ComPassword() => View();
    public IActionResult ComHidden() => View();
    public IActionResult ComLabel() => View();
    public IActionResult ComSwitch() => View();
    public IActionResult ComCheckbox() => View();
    public IActionResult ComDate() => View();
    public IActionResult ComPrompt() => View();
    public IActionResult ComSelect() => View();
    public IActionResult ComRadio() => View();
    public IActionResult ComCheckboxGroup() => View();
    public IActionResult ComDbSelect() => View();
    public IActionResult ComLookup() => View();
    public IActionResult ComUploadImage() => View();
    public IActionResult ComRichText() => View();

    // ==================== 新架构：List 屏表格（ElementUI table + 分页 + 共享 model） ====================
    public IActionResult Table() => View();
}
