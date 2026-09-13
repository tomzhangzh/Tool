using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Controllers;

/// <summary>首页/桌面/设计器/demo 入口</summary>
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) { _db = db; }

    [Route("/")]
    public IActionResult Index()
    {
        using var db = _db.Create();
        ViewBag.Solutions = db.Queryable<DesktopSolution>().Where(s => s.IsEnabled).OrderBy(s => s.SortOrder).ToList();
        return View();
    }

    /// <summary>设计器</summary>
    [Route("/designer")]
    public IActionResult Designer() => View();

    /// <summary>Demo 汇总页</summary>
    [Route("/demo")]
    public IActionResult Demo() => View();

    /// <summary>ActionHelper Demo</summary>
    [Route("/demo/actionhelper")]
    public IActionResult DemoActionHelper() => View();

    /// <summary>Chain Editor Demo</summary>
    [Route("/demo/chain-editor")]
    public IActionResult DemoChainEditor() => View();

    /// <summary>组合组件 Demo</summary>
    [Route("/demo/composite")]
    public IActionResult DemoComposite() => View();

    /// <summary>WebPage Demo（CRUD 学生表）</summary>
    [Route("/demo/webpage")]
    public IActionResult DemoWebPage() => View();

    /// <summary>CRUD 三屏 Demo（Teachers 表）</summary>
    [Route("/demo/crud")]
    public IActionResult DemoCrud() => View();

    // ===== Platform 管理页面 =====
    [Route("/Platform/Component")]
    public IActionResult PlatformComponent() => View();

    [Route("/Platform/ActionHelper")]
    public IActionResult PlatformActionHelper() => View();

    [Route("/Platform/Template")]
    public IActionResult PlatformTemplate() => View();

    [Route("/Platform/WebPage")]
    public IActionResult PlatformWebPage() => View();

    [Route("/Platform/Dict")]
    public IActionResult PlatformDict() => View();
}
