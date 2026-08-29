using Microsoft.AspNetCore.Mvc;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>
/// 设计层工作台：工程级全屏 Windows 桌面，内含 页面管理(路由) / 模板管理 / 数据库管理 / 屏设计。
/// 由工程管理列表的"管理"按钮打开。
/// </summary>
public class DesignController : Controller
{
    private readonly DynProjectService _svc;
    public DesignController(DynProjectService svc) { _svc = svc; }

    public IActionResult Desktop(int projectId)
    {
        var project = _svc.GetProject(projectId);
        if (project == null) return NotFound("工程不存在");
        ViewBag.Project = project;
        return View();
    }

    public IActionResult WebPages(int projectId)
    {
        var project = _svc.GetProject(projectId);
        if (project == null) return NotFound("工程不存在");
        ViewBag.Project = project;
        return View();
    }

    public IActionResult Templates(int projectId)
    {
        var project = _svc.GetProject(projectId);
        if (project == null) return NotFound("工程不存在");
        ViewBag.Project = project;
        return View();
    }

    public IActionResult Database(int projectId)
    {
        var project = _svc.GetProject(projectId);
        if (project == null) return NotFound("工程不存在");
        ViewBag.Project = project;
        return View();
    }

    public IActionResult Screens(int projectId)
    {
        var project = _svc.GetProject(projectId);
        if (project == null) return NotFound("工程不存在");
        ViewBag.Project = project;
        return View();
    }
}
