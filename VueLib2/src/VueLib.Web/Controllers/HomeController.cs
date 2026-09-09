using Microsoft.AspNetCore.Mvc;

namespace VueLib.Web.Controllers;

/// <summary>入口：首页（页面列表）+ 设计器 + 预览</summary>
public class HomeController : Controller
{
    private readonly IConfiguration _config;
    public HomeController(IConfiguration config) => _config = config;

    public IActionResult Index()
    {
        ViewBag.Title = _config["VueLib:Title"] ?? "VueLib2";
        return View();
    }

    public IActionResult Designer()
    {
        ViewBag.Title = "设计器 - " + (_config["VueLib:Title"] ?? "VueLib2");
        return View("~/Views/Designer/Index.cshtml");
    }

    public IActionResult Error() => View();
}
