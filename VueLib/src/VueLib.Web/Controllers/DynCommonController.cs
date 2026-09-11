using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>
/// 常用组件通用数据服务：
///  - 数据字典（DynDict 表，替代 SunnySystem 的 DICTSETTINGManager）
///  - 任意工程表下拉选项（DropdownAjax / Selector / Lookup 数据源）
///  - 通用表分页查询（Lookup 选择器）
///  - 文件上传（UploadImage / UploadButton）
/// </summary>
[ApiController]
[Route("api/dyncommon")]
public class DynCommonController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly DynProjectService _projectService;
    private readonly IWebHostEnvironment _env;

    public DynCommonController(AppDbContext dbContext, DynProjectService projectService, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _projectService = projectService;
        _env = env;
    }

    /// <summary>
    /// 数据字典选项（主库 DynDict 表）
    /// GET /api/dyncommon/dicts?type=Gender
    /// </summary>
    [HttpGet("dicts")]
    public async Task<IActionResult> Dicts(string type, bool enabled = true)
    {
        if (string.IsNullOrWhiteSpace(type)) return Ok(new { success = false, message = "type 必填" });
        using var db = _dbContext.Create();
        var q = db.Queryable<DynDict>().Where(d => d.DictType == type && d.IsEnabled == enabled);
        var rows = await q.OrderBy(d => d.SortOrder).OrderBy(d => d.Id)
            .Select(d => new { label = d.DictText, value = d.DictValue }).ToListAsync();
        return Ok(new { success = true, data = rows, count = rows.Count });
    }

    /// <summary>
    /// 任意工程表下拉选项（DropdownAjax）
    /// GET /api/dyncommon/options?projectId=1&table=Class&valueField=Id&textField=Name&filter=&orderBy=
    /// </summary>
    [HttpGet("options")]
    public IActionResult Options(int projectId, string table, string valueField = "Id", string textField = "Name",
        string? filter = null, string? orderBy = null, int limit = 500)
    {
        var p = _projectService.GetProject(projectId);
        if (p == null) return Ok(new { success = false, message = "工程不存在" });
        if (string.IsNullOrWhiteSpace(table)) return Ok(new { success = false, message = "table 必填" });
        if (!System.Text.RegularExpressions.Regex.IsMatch(valueField, "^[A-Za-z0-9_]+$")
            || !System.Text.RegularExpressions.Regex.IsMatch(textField, "^[A-Za-z0-9_]+$"))
            return Ok(new { success = false, message = "字段名非法" });
        try
        {
            using var db = _projectService.CreateProjectClient(p);
            // SqlSugar 字典反序列化不支持别名列，查询原始列名后在 C# 侧映射
            var sql = $"SELECT TOP ({limit}) [{valueField}], [{textField}] FROM [{table}]";
            if (!string.IsNullOrWhiteSpace(filter)) sql += " WHERE " + filter;
            if (!string.IsNullOrWhiteSpace(orderBy)) sql += " ORDER BY " + orderBy;
            else sql += $" ORDER BY [{textField}]";
            var raw = db.Ado.GetDataTable(sql);
            var rows = raw.Rows.Cast<System.Data.DataRow>().Select(r =>
                new { value = r[valueField], label = r[textField] }).ToList();
            return Ok(new { success = true, data = rows, count = rows.Count });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 通用表分页查询（Lookup 选择器数据源；columns 逗号分隔）
    /// GET /api/dyncommon/tablelist?projectId=1&table=Student&page=1&pageSize=10&keyword=&columns=Id,Name
    /// </summary>
    [HttpGet("tablelist")]
    public IActionResult TableList(int projectId, string table, int page = 1, int pageSize = 10,
        string? keyword = null, string? columns = null, string? searchField = null)
    {
        var p = _projectService.GetProject(projectId);
        if (p == null) return Ok(new { success = false, message = "工程不存在" });
        if (string.IsNullOrWhiteSpace(table)) return Ok(new { success = false, message = "table 必填" });
        try
        {
            using var db = _projectService.CreateProjectClient(p);
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 10;
            var sel = string.IsNullOrWhiteSpace(columns) ? "*" : string.Join(",", columns.Split(',').Select(c => $"[{c.Trim()}]"));
            var where = "";
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var fld = string.IsNullOrWhiteSpace(searchField) ? "Name" : searchField.Trim();
                where = $" WHERE [{fld}] LIKE N'%{keyword.Replace("'", "''")}%'";
            }
            var total = db.Ado.GetInt($"SELECT COUNT(1) FROM [{table}]{where}");
            var sql = $"SELECT {sel} FROM [{table}]{where} ORDER BY (SELECT NULL) OFFSET {(page - 1) * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            var dt = db.Ado.GetDataTable(sql);
            var rows = dt.Rows.Cast<System.Data.DataRow>()
                .Select(r => dt.Columns.Cast<System.Data.DataColumn>()
                    .ToDictionary(c => c.ColumnName, c => (object?)r[c]))
                .ToList();
            return Ok(new { success = true, data = rows, total, page, pageSize });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// 文件上传（UploadImage / UploadButton）
    /// POST /api/dyncommon/upload  multipart/form-data: file
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile? file)
    {
        if (file == null || file.Length == 0) return Ok(new { success = false, message = "未选择文件" });
        var uploadDir = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploadDir);
        var ext = Path.GetExtension(file.FileName);
        var name = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var full = Path.Combine(uploadDir, name);
        await using (var fs = System.IO.File.Create(full)) await file.CopyToAsync(fs);
        return Ok(new { success = true, url = "/uploads/" + name, name = file.FileName, size = file.Length });
    }
}
