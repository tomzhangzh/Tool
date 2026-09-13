using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Areas.Platform.Controllers;

/// <summary>平台管理 CRUD —— 组件/动作/模板/页面/词典/工程 的增删改查 API</summary>
[Area("Platform")]
[Route("api/admin")]
public class AdminApiController : Controller
{
    private readonly AppDbContext _db;
    public AdminApiController(AppDbContext db) { _db = db; }

    // ===== ComponentMeta =====
    [HttpGet("components")]
    public IActionResult Components(int page = 1, int size = 50, string? keyword = null)
    {
        using var db = _db.Create();
        var q = db.Queryable<ComponentMeta>();
        if (!string.IsNullOrEmpty(keyword)) q = q.Where(c => c.ComponentName.Contains(keyword) || (c.Label ?? "").Contains(keyword));
        var total = q.Count();
        var rows = q.OrderBy(c => c.SortOrder).OrderBy(c => c.Id)
            .Skip((page - 1) * size).Take(size).ToList();
        return Json(new OpResult { Success = true, Data = new { rows, total, page, size } });
    }

    [HttpPost("components")]
    public IActionResult SaveComponent([FromBody] ComponentMeta meta)
    {
        using var db = _db.Create();
        meta.UpdatedAt = DateTime.UtcNow;
        if (meta.Id > 0) { db.Updateable(meta).ExecuteCommand(); }
        else { meta.CreatedAt = DateTime.UtcNow; db.Insertable(meta).ExecuteReturnIdentity(); }
        return Json(OpResult.Ok(meta));
    }

    [HttpPost("components/delete")]
    public IActionResult DeleteComponent([FromBody] int id)
    {
        using var db = _db.Create();
        db.Deleteable<ComponentMeta>().Where(c => c.Id == id).ExecuteCommand();
        return Json(OpResult.Ok());
    }

    // ===== DynActionHelper =====
    [HttpGet("actionhelpers")]
    public IActionResult ActionHelpers(int page = 1, int size = 50, string? keyword = null)
    {
        using var db = _db.Create();
        var q = db.Queryable<DynActionHelper>();
        if (!string.IsNullOrEmpty(keyword)) q = q.Where(a => a.Name.Contains(keyword) || a.Code.Contains(keyword));
        var total = q.Count();
        var rows = q.OrderBy(a => a.SortOrder).OrderBy(a => a.Id)
            .Skip((page - 1) * size).Take(size).ToList();
        return Json(OpResult.Ok(new { rows, total, page, size }));
    }

    [HttpPost("actionhelpers")]
    public IActionResult SaveActionHelper([FromBody] DynActionHelper a)
    {
        using var db = _db.Create();
        a.UpdatedAt = DateTime.UtcNow;
        if (a.Id > 0) { db.Updateable(a).ExecuteCommand(); }
        else { a.CreatedAt = DateTime.UtcNow; db.Insertable(a).ExecuteReturnIdentity(); }
        return Json(OpResult.Ok(a));
    }

    [HttpPost("actionhelpers/delete")]
    public IActionResult DeleteActionHelper([FromBody] int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynActionHelper>().Where(a => a.Id == id).ExecuteCommand();
        return Json(OpResult.Ok());
    }

    // ===== DynTemplate =====
    [HttpGet("templates")]
    public IActionResult Templates(int page = 1, int size = 50)
    {
        using var db = _db.Create();
        var total = db.Queryable<DynTemplate>().Count();
        var rows = db.Queryable<DynTemplate>().OrderBy(t => t.SortOrder)
            .Skip((page - 1) * size).Take(size).ToList();
        return Json(OpResult.Ok(new { rows, total, page, size }));
    }

    [HttpPost("templates")]
    public IActionResult SaveTemplate([FromBody] DynTemplate t)
    {
        using var db = _db.Create();
        t.UpdatedAt = DateTime.UtcNow;
        if (t.Id > 0) { db.Updateable(t).ExecuteCommand(); }
        else { t.CreatedAt = DateTime.UtcNow; db.Insertable(t).ExecuteReturnIdentity(); }
        return Json(OpResult.Ok(t));
    }

    [HttpPost("templates/delete")]
    public IActionResult DeleteTemplate([FromBody] int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynTemplate>().Where(t => t.Id == id).ExecuteCommand();
        return Json(OpResult.Ok());
    }

    // ===== DynWebPage =====
    [HttpGet("webpages")]
    public IActionResult WebPages(int page = 1, int size = 50, int? projectId = null)
    {
        using var db = _db.Create();
        var q = db.Queryable<DynWebPage>();
        if (projectId.HasValue) q = q.Where(w => w.ProjectId == projectId);
        var total = q.Count();
        var rows = q.OrderBy(w => w.SortOrder).OrderBy(w => w.Id)
            .Skip((page - 1) * size).Take(size).ToList();
        return Json(OpResult.Ok(new { rows, total, page, size }));
    }

    [HttpPost("webpages")]
    public IActionResult SaveWebPage([FromBody] DynWebPage w)
    {
        using var db = _db.Create();
        w.UpdatedAt = DateTime.UtcNow;
        if (w.Id > 0) { db.Updateable(w).ExecuteCommand(); }
        else { w.CreatedAt = DateTime.UtcNow; db.Insertable(w).ExecuteReturnIdentity(); }
        return Json(OpResult.Ok(w));
    }

    [HttpPost("webpages/delete")]
    public IActionResult DeleteWebPage([FromBody] int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynWebPage>().Where(w => w.Id == id).ExecuteCommand();
        return Json(OpResult.Ok());
    }

    // ===== DynDict =====
    [HttpGet("dict")]
    public IActionResult Dict(int page = 1, int size = 100, string? dictType = null)
    {
        using var db = _db.Create();
        var q = db.Queryable<DynDict>();
        if (!string.IsNullOrEmpty(dictType)) q = q.Where(d => d.DictType == dictType);
        var total = q.Count();
        var rows = q.OrderBy(d => d.DictType).OrderBy(d => d.SortOrder)
            .Skip((page - 1) * size).Take(size).ToList();
        return Json(OpResult.Ok(new { rows, total, page, size }));
    }

    [HttpPost("dict")]
    public IActionResult SaveDict([FromBody] DynDict d)
    {
        using var db = _db.Create();
        if (d.Id > 0) { db.Updateable(d).ExecuteCommand(); }
        else { d.CreatedAt = DateTime.UtcNow; db.Insertable(d).ExecuteReturnIdentity(); }
        return Json(OpResult.Ok(d));
    }

    [HttpPost("dict/delete")]
    public IActionResult DeleteDict([FromBody] int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynDict>().Where(d => d.Id == id).ExecuteCommand();
        return Json(OpResult.Ok());
    }
}
