using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Areas.Runtime.Controllers;

/// <summary>运行时 —— 动态 CRUD（无需 model）+ 页面渲染</summary>
[Area("Runtime")]
public class DynCrudController : Controller
{
    private readonly DynCrudService _crud;
    private readonly BusinessDbFactory _factory;
    private readonly AppDbContext _platformDb;

    public DynCrudController(DynCrudService crud, BusinessDbFactory factory, AppDbContext platformDb)
    {
        _crud = crud;
        _factory = factory;
        _platformDb = platformDb;
    }

    /// <summary>获取业务库连接串（按 ProjectId）</summary>
    private string GetConn(int? projectId)
    {
        if (projectId.HasValue && projectId.Value > 0)
        {
            using var db = _platformDb.Create();
            var proj = db.Queryable<DynProject>().Where(p => p.Id == projectId).First();
            if (proj?.ConnectionString != null) return proj.ConnectionString;
        }
        return null!; // 默认从配置 Business 连接串
    }

    /// <summary>表元数据（主键+列信息）</summary>
    [HttpGet]
    [Route("Runtime/DynCrud/Meta")]
    public async Task<IActionResult> Meta(string table, int? projectId)
    {
        try
        {
            var meta = await _crud.GetTableMetaAsync(GetConn(projectId), table);
            return Json(OpResult.Ok(meta));
        }
        catch (Exception ex) { return Json(OpResult.Fail(ex.Message)); }
    }

    /// <summary>分页查询</summary>
    [HttpPost]
    [Route("Runtime/DynCrud/Query")]
    public async Task<IActionResult> Query([FromBody] DynCrudQuery q, int? projectId)
    {
        try
        {
            var result = await _crud.QueryAsync(GetConn(projectId), q);
            return Json(OpResult.Ok(result));
        }
        catch (Exception ex) { return Json(OpResult.Fail(ex.Message)); }
    }

    /// <summary>新增/更新</summary>
    [HttpPost]
    [Route("Runtime/DynCrud/Save")]
    public async Task<IActionResult> Save([FromBody] DynCrudSave save, int? projectId)
    {
        try
        {
            var result = await _crud.SaveAsync(GetConn(projectId), save);
            return Json(OpResult.Ok(result));
        }
        catch (Exception ex) { return Json(OpResult.Fail(ex.Message)); }
    }

    /// <summary>删除</summary>
    [HttpPost]
    [Route("Runtime/DynCrud/Delete")]
    public async Task<IActionResult> Delete([FromBody] DynCrudDelete del, int? projectId)
    {
        try
        {
            await _crud.DeleteAsync(GetConn(projectId), del);
            return Json(OpResult.Ok());
        }
        catch (Exception ex) { return Json(OpResult.Fail(ex.Message)); }
    }

    /// <summary>获取单条</summary>
    [HttpGet]
    [Route("Runtime/DynCrud/Get")]
    public async Task<IActionResult> Get(string table, string id, int? projectId)
    {
        try
        {
            var row = await _crud.GetByIdAsync(GetConn(projectId), table, id);
            return Json(OpResult.Ok(row));
        }
        catch (Exception ex) { return Json(OpResult.Fail(ex.Message)); }
    }

    /// <summary>列出业务库所有表</summary>
    [HttpGet]
    [Route("Runtime/DynCrud/Tables")]
    public IActionResult Tables(int? projectId)
    {
        try
        {
            var tables = _crud.GetTables(GetConn(projectId));
            return Json(OpResult.Ok(tables));
        }
        catch (Exception ex) { return Json(OpResult.Fail(ex.Message)); }
    }
}
