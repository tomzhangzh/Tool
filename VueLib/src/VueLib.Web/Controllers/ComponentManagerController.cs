using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Controllers;

/// <summary>
/// 组件管理后台 - 可视化管理组件元数据和属性配置
/// </summary>
public class ComponentManagerController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
