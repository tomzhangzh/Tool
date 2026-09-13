using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Areas.Platform.Controllers;

/// <summary>平台 API —— 组件元数据、ActionHelper 注册、字典、工程/模板/页面管理</summary>
[Area("Platform")]
[Route("api")]
public class ComponentApiController : Controller
{
    private readonly AppDbContext _db;
    public ComponentApiController(AppDbContext db) { _db = db; }

    /// <summary>列出所有启用的组件元数据（前端启动时拉取）</summary>
    [HttpGet("components")]
    public IActionResult Components()
    {
        using var db = _db.Create();
        var list = db.Queryable<ComponentMeta>().Where(c => c.IsEnabled)
            .OrderBy(c => c.SortOrder).OrderBy(c => c.Id).ToList();
        return Json(list);
    }

    /// <summary>获取单个组件元数据</summary>
    [HttpGet("components/{name}")]
    public IActionResult Component(string name)
    {
        using var db = _db.Create();
        var c = db.Queryable<ComponentMeta>().Where(x => x.ComponentName == name).First();
        if (c == null) return NotFound(new { success = false, message = "组件不存在: " + name });
        return Json(c);
    }

    /// <summary>获取组件的 cshtml 视图 HTML（运行时动态注册）</summary>
    [HttpGet("component-view/{name}")]
    public IActionResult ComponentView(string name)
    {
        // 先查 DB 拿 LoadUrl
        using var db = _db.Create();
        var meta = db.Queryable<ComponentMeta>().Where(x => x.ComponentName == name).First();
        string? viewPath = meta?.LoadUrl;

        // fallback: 按约定路径查找 /Areas/Components/Views/{category}/{name}.cshtml
        if (string.IsNullOrEmpty(viewPath))
        {
            viewPath = $"/Areas/Components/Views/{(meta?.Category ?? "Data")}/{name}.cshtml";
        }

        var physicalPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            viewPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        // 也尝试从 Areas 目录读
        var altPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            viewPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        string? content = null;
        if (System.IO.File.Exists(altPath))
            content = System.IO.File.ReadAllText(altPath);
        else if (System.IO.File.Exists(physicalPath))
            content = System.IO.File.ReadAllText(physicalPath);

        if (content == null)
            return NotFound(new { success = false, message = "视图文件不存在: " + viewPath });

        return Content(content, "text/plain; charset=utf-8");
    }

    /// <summary>列出所有启用的 ActionHelper（含脚本，前端动态注册）</summary>
    [HttpGet("actionhelpers")]
    public IActionResult ActionHelpers()
    {
        using var db = _db.Create();
        var list = db.Queryable<DynActionHelper>().Where(a => a.IsEnabled)
            .OrderBy(a => a.SortOrder).OrderBy(a => a.Id).ToList();
        return Json(list);
    }

    /// <summary>Platform 数据字典</summary>
    [HttpGet("dict/{dictType}")]
    public IActionResult Dict(string dictType)
    {
        using var db = _db.Create();
        var list = db.Queryable<DynDict>().Where(d => d.DictType == dictType && d.IsEnabled)
            .OrderBy(d => d.SortOrder).ToList();
        return Json(list);
    }

    /// <summary>列出所有工程</summary>
    [HttpGet("projects")]
    public IActionResult Projects()
    {
        using var db = _db.Create();
        var list = db.Queryable<DynProject>().Where(p => p.IsEnabled).OrderBy(p => p.Id).ToList();
        return Json(list);
    }

    /// <summary>列出所有模板</summary>
    [HttpGet("templates")]
    public IActionResult Templates()
    {
        using var db = _db.Create();
        var list = db.Queryable<DynTemplate>().Where(t => t.IsEnabled).OrderBy(t => t.SortOrder).ToList();
        return Json(list);
    }
}
