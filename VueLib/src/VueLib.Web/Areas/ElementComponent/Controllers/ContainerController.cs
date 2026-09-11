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
    public IActionResult ForEach() => View();
    public IActionResult Fragment() => View();
    public IActionResult Grid3() => View();

    // ==================== 学校管理专用数据组件（配置化 CRUD / 树 / 左树右列） ====================
    public IActionResult DynCrudPage() => View();
    public IActionResult DynTree() => View();
    public IActionResult DynTreeList() => View();
}
