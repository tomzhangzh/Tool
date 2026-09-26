using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Services.Data;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Controllers;

/// <summary>
/// 数据库型组件公共接口（V1 DynCommonController 对齐，V4 走 ApiResult 统一信封 res.code/res.data）：
///   GET  /api/dyncommon/options    工程表下拉选项（valueField/textField，支持 parentField/parentValue 联动）
///   GET  /api/dyncommon/dicts      平台字典下拉（DynDict by DictType）
///   GET  /api/dyncommon/tablelist  工程表分页记录（ComLookup 查找带回）
///   POST /api/dyncommon/upload     文件/图片上传，返回可访问 url
/// </summary>
[Route("api/dyncommon")]
[ApiController]
public class DynCommonController : ControllerBase
{
    private readonly DynamicCrudService _svc;
    private readonly ProjectDbResolver _projects;
    private readonly IDynDictService _dicts;
    private readonly IWebHostEnvironment _env;

    public DynCommonController(DynamicCrudService svc,
        ProjectDbResolver projects, IDynDictService dicts, IWebHostEnvironment env)
    {
        _svc = svc;
        _projects = projects;
        _dicts = dicts;
        _env = env;
    }

    // ---------------- 工程表下拉选项 ----------------

    /// <summary>
    /// 查工程表返回 [{value,label}]。
    /// 所有字段名必须真实存在于表结构（白名单），杜绝 SQL 拼接注入；联动条件参数化。
    /// </summary>
    [HttpGet("options")]
    public ApiResult Options(string project, string table, string valueField = "Id", string textField = "Name",
        string parentField = null, string parentValue = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table 参数");
        using var db = _projects.Resolve(project);
        var colMap = _svc.Columns(db, table)
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
        if (!colMap.ContainsKey(valueField)) return ApiResult.Fail($"值字段 {valueField} 不存在");
        if (!colMap.ContainsKey(textField)) return ApiResult.Fail($"文本字段 {textField} 不存在");

        string where = null;
        object pars = null;
        if (!string.IsNullOrWhiteSpace(parentField) && !string.IsNullOrEmpty(parentValue) && colMap.TryGetValue(parentField, out var pf))
        {
            where = $"[{parentField}]=@pv";
            pars = new { pv = ConvertValue(parentValue, pf.DataType) };
        }

        var rows = _svc.Query(db, table, where, pars, $"[{textField}] ASC");
        var data = rows.Select(r => new
        {
            value = r[valueField],
            label = r[textField]
        }).ToList();
        return ApiResult.Ok(data);
    }

    // ---------------- 平台字典下拉 ----------------

    [HttpGet("dicts")]
    public ApiResult Dicts(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return ApiResult.Fail("缺少 type 参数");
        var rows = _dicts.ListByType(type);
        return ApiResult.Ok(rows.Select(d => new { value = d.DictCode, label = d.DictName }).ToList());
    }

    // ---------------- 工程表分页记录（查找带回） ----------------

    [HttpGet("tablelist")]
    public ApiResult TableList(string project, string table, int page = 1, int pageSize = 10,
        string columns = "Id,Name", string keyword = null, string searchField = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table 参数");
        using var db = _projects.Resolve(project);
        var colSet = _svc.Columns(db, table)
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var showCols = (columns ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(colSet.Contains)
            .ToList();
        if (showCols.Count == 0) showCols = colSet.Take(5).ToList();

        JObject filter = null;
        if (!string.IsNullOrWhiteSpace(keyword) && !string.IsNullOrWhiteSpace(searchField) && colSet.Contains(searchField))
        {
            filter = new JObject { [searchField] = new JObject { ["op"] = "like", ["value"] = keyword } };
        }
        var pageResult = _svc.Page(db, table, page, pageSize, filter);
        var rows = pageResult.Rows.Select(r =>
        {
            var o = new JObject();
            foreach (var c in showCols) o[c] = r[c];
            return o;
        }).ToList();
        return ApiResult.Ok(new { rows, total = pageResult.Total });
    }

    // ---------------- 文件上传 ----------------

    [HttpPost("upload")]
    public async Task<ApiResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return ApiResult.Fail("未接收到上传文件");
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        var allow = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".zip", ".rar", ".txt", ".csv" };
        if (ext.Length > 0 && !allow.Contains(ext)) return ApiResult.Fail($"不允许的文件类型 {ext}");
        if (file.Length > 20L * 1024 * 1024) return ApiResult.Fail("文件不能超过 20MB");

        var monthDir = DateTime.Now.ToString("yyyyMM");
        var relDir = Path.Combine("upload", monthDir);
        var absDir = Path.Combine(_env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), relDir);
        Directory.CreateDirectory(absDir);
        var fileName = Guid.NewGuid().ToString("N") + ext;
        var absPath = Path.Combine(absDir, fileName);
        await using (var fs = System.IO.File.Create(absPath))
            await file.CopyToAsync(fs);

        var url = "/" + Path.Combine(relDir, fileName).Replace('\\', '/');
        return ApiResult.Ok(new { url, name = file.FileName, size = file.Length }, "上传成功");
    }

    // ---------------- 辅助 ----------------

    /// <summary>按目标列类型转换查询参数（同 DynamicCrudService 规则），避免 SqlServer int 列与字符串比较失败</summary>
    private static object ConvertValue(string raw, string dataType)
    {
        var t = (dataType ?? string.Empty).ToLower();
        if (t.Contains("bigint")) return long.TryParse(raw, out var l) ? l : 0L;
        if (t.Contains("int")) return int.TryParse(raw, out var i) ? i : 0;
        if (t.Contains("bit")) return raw is "1" or "true" or "True" or "TRUE";
        if (t.Contains("decimal") || t.Contains("numeric") || t.Contains("money"))
            return decimal.TryParse(raw, out var d) ? d : 0m;
        if (t.Contains("float") || t.Contains("real"))
            return double.TryParse(raw, out var f) ? f : 0d;
        if (t.Contains("date") || t.Contains("time"))
            return DateTime.TryParse(raw, out var dt) ? dt : DateTime.Now;
        return raw;
    }
}
