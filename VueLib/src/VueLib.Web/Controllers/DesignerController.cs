using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Controllers;

/// <summary>
/// 低代码设计器 Controller
/// </summary>
public class DesignerController : Controller
{
    /// <summary>设计器主页面（Element Plus）</summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>手机预览页面（NutUI，供 iframe 嵌入）</summary>
    public IActionResult Preview()
    {
        return View();
    }
}
