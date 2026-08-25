using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>
/// 动态工程 / 页面管理（Windows 桌面内的工程管理与页面生成）
/// </summary>
public class DynProjectController : Controller
{
    private readonly DynProjectService _svc;

    public DynProjectController(DynProjectService svc)
    {
        _svc = svc;
    }

    public IActionResult Index() => View();

    // ==================== 工程 ====================

    [HttpGet("/api/dynproject/list")]
    public IActionResult ListProjects()
    {
        var list = _svc.GetProjects();
        return Ok(new { success = true, data = list, count = list.Count });
    }

    [HttpPost("/api/dynproject/save")]
    public IActionResult SaveProject([FromBody] DynProject p)
    {
        var r = _svc.SaveProject(p);
        return Ok(new { r.Success, r.Message, r.Data });
    }

    [HttpPost("/api/dynproject/test")]
    public IActionResult TestConnection([FromBody] DynProject p)
    {
        var r = _svc.TestConnection(p?.ConnectionString);
        return Ok(new { r.Success, r.Message });
    }

    [HttpDelete("/api/dynproject/{id}")]
    public IActionResult DeleteProject(int id)
    {
        var r = _svc.DeleteProject(id);
        return Ok(new { r.Success, r.Message });
    }

    // ==================== 元数据 ====================

    [HttpGet("/api/dynproject/{id}/tables")]
    public IActionResult Tables(int id)
    {
        var r = _svc.GetTables(id);
        return Ok(new { r.Success, r.Message, r.Data });
    }

    [HttpGet("/api/dynproject/{id}/columns")]
    public IActionResult Columns(int id, string table)
    {
        var r = _svc.GetColumns(id, table);
        return Ok(new { r.Success, r.Message, r.Data });
    }

    [HttpGet("/api/dynproject/{id}/generate")]
    public IActionResult Generate(int id, string table)
    {
        var p = _svc.GetProject(id);
        if (p == null) return Ok(new { success = false, message = "工程不存在" });
        try
        {
            using var db = _svc.CreateProjectClient(p);
            var cols = _svc.DynCrudColumns(db, table);
            var def = _svc.GenerateDefinition(table, cols);
            // 自动检测外键导航建议（多对一 / 一对多）
            def.Navs = _svc.BuildNavSuggestions(db, table);
            return Ok(new { success = true, data = def });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = "生成失败：" + ex.Message });
        }
    }

    // ==================== 页面 ====================

    [HttpGet("/api/dynproject/{id}/pages")]
    public IActionResult Pages(int id)
    {
        var list = _svc.GetPages(id);
        return Ok(new { success = true, data = list, count = list.Count });
    }

    [HttpPost("/api/dynproject/page/save")]
    public IActionResult SavePage([FromBody] DynPage p)
    {
        var r = _svc.SavePage(p);
        return Ok(new { r.Success, r.Message, r.Data });
    }

    [HttpDelete("/api/dynproject/page/{id}")]
    public IActionResult DeletePage(int id)
    {
        var r = _svc.DeletePage(id);
        return Ok(new { r.Success, r.Message });
    }

    /// <summary>生成视图：返回 SQL 视图脚本 + 独立 .cshtml 源码，可选写入文件 / 在工程库执行</summary>
    [HttpPost("/api/dynproject/page/view")]
    public IActionResult GenerateView([FromBody] DynPageViewRequest req)
    {
        if (req == null || req.ProjectId <= 0 || req.PageId <= 0)
            return Ok(new { success = false, message = "参数错误" });
        var project = _svc.GetProject(req.ProjectId);
        var page = _svc.GetPage(req.PageId);
        if (project == null || page == null) return Ok(new { success = false, message = "工程或页面不存在" });
        DynPageDefinition? def = null;
        try { def = JsonConvert.DeserializeObject<DynPageDefinition>(page.ColumnDefs ?? ""); } catch { }
        if (def == null) return Ok(new { success = false, message = "页面定义解析失败" });

        // 1. SQL 视图
        var sqlView = "";
        if (req.WithSqlView)
        {
            var r = _svc.BuildSqlView(project, page, def);
            sqlView = r.Data?.ToString() ?? "";
            if (req.ExecuteSql && !string.IsNullOrWhiteSpace(sqlView))
            {
                try
                {
                    using var db = _svc.CreateProjectClient(project);
                    db.Ado.ExecuteCommand(sqlView);
                }
                catch (Exception ex)
                {
                    return Ok(new { success = false, message = "SQL 视图执行失败：" + ex.Message });
                }
            }
        }

        // 2. 独立视图源码（自包含 HTML，可用浏览器直接打开预览）
        var host = $"{Request.Scheme}://{Request.Host}";
        var cshtml = DynViewGenerator.BuildStandaloneHtml(project, page, def, host);

        // 3. 可选写盘
        string? savedPath = null;
        if (req.WriteFiles)
        {
            try
            {
                var folder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "generated_views", project.Name);
                folder = Path.GetFullPath(folder);
                Directory.CreateDirectory(folder);
                var file = Path.Combine(folder, $"{page.Name}.html");
                System.IO.File.WriteAllText(file, cshtml, System.Text.Encoding.UTF8);
                savedPath = file;
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "写入视图文件失败：" + ex.Message });
            }
        }

        return Ok(new { success = true, data = new { sqlView, cshtml, savedPath } });
    }
}

public class DynPageViewRequest
{
    public int ProjectId { get; set; }
    public int PageId { get; set; }
    public bool WithSqlView { get; set; }
    public bool ExecuteSql { get; set; }
    public bool WriteFiles { get; set; }
}
