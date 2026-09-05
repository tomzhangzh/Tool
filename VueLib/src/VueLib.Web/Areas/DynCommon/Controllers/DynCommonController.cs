using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using VueLib.Web.Infrastructure;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Areas.DynCommon.Controllers;

/// <summary>
/// 公共动态 CRUD 接口（Area: DynCommon）。
/// 不依赖具体业务 Model：通过"工程 + 页面定义"（projectId/pageId）动态读写工程数据库，
/// 供任意 Filter / Grid / Detail 屏复用。
/// 每个动作提供两种返回：
///   - View 版（List / Filter / Summary / Detail）→ 分部视图（ElementUI + dyn-lib 渲染）
///   - JSON 版（Paged / Get / Save / Delete）      → JSON 数据（可携带 actions 指令，dyn-lib 自动执行）
/// </summary>
[Area("DynCommon")]
public class DynCommonController : Controller
{
    private readonly DynProjectService _svc;
    private readonly DynCrudService _crud;

    public DynCommonController(DynProjectService svc, DynCrudService crud)
    {
        _svc = svc;
        _crud = crud;
    }

    // ==================== View 版 ====================

    /// <summary>组合查询屏：查询条件 + 汇总表格 + 分页（含操作列），复用 _Summary 分部视图</summary>
    [HttpGet]
    public IActionResult List(int projectId, int pageId)
    {
        var r = BuildSummaryModel(projectId, pageId, null);
        if (r is not OkObjectResult ok || ok.Value is not DynRunSummaryModel m) return r;
        return PartialView("_Summary", m);
    }

    /// <summary>只读查询屏（查询条件 + 表格 + 分页，无操作列）</summary>
    [HttpPost]
    public IActionResult Filter(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is not OkObjectResult ok || ok.Value is not DynRunSummaryModel m) return r;
        return PartialView("_Filter", m);
    }

    /// <summary>汇总屏（表格 + 分页 + 操作列）</summary>
    [HttpPost]
    public IActionResult Summary(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is not OkObjectResult ok || ok.Value is not DynRunSummaryModel m) return r;
        return PartialView("_Summary", m);
    }

    /// <summary>筛选面板（仅查询条件控件，供 3 屏组件 Filter 屏）</summary>
    [HttpPost]
    public IActionResult FilterPanel(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        return PartialView("_FilterPanel", new DynRunSummaryModel
        {
            Project = project,
            Page = page,
            Def = def,
            Filter = post?.Filter ?? new Dictionary<string, object?>(),
            Result = null
        });
    }

    /// <summary>列表面板（仅表格 + 分页 + 操作列，供 3 屏组件 List 屏）</summary>
    [HttpPost]
    public IActionResult ListPanel(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is not OkObjectResult ok || ok.Value is not DynRunSummaryModel m) return r;
        return PartialView("_ListPanel", m);
    }
    /// <summary>详情/编辑屏（id=0 为新增）</summary>
    [HttpGet]
    public IActionResult Detail(int projectId, int pageId, int id = 0, string? _params = null)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        using var db = _svc.CreateProjectClient(project);
        var row = _crud.GetByPk(db, page.TableName!, id, def.PrimaryKey);
        if (id <= 0 && !string.IsNullOrWhiteSpace(_params))
        {
            try
            {
                var extra = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(_params);
                if (extra != null)
                    foreach (var kv in extra)
                        if (!string.IsNullOrWhiteSpace(kv.Key) && row.ContainsKey(kv.Key))
                            row[kv.Key] = kv.Value is System.Text.Json.JsonElement je ? JsonElementToObject(je) : kv.Value;
            }
            catch { }
        }
        if (id > 0 && row.Count > 0) _crud.LoadNavs(db, def, new[] { row });

        return PartialView("_Detail", new DynRunDetailModel { Project = project, Page = page, Def = def, Row = row });
    }

    // ==================== JSON 版 ====================

    /// <summary>分页数据（JSON）：{ rows, pageInfo, ... }</summary>
    [HttpPost("/DynCommon/DynCommon/Data/Paged")]
    public IActionResult Paged(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is OkObjectResult ok && ok.Value is DynRunSummaryModel m)
        {
            var result = new
            {
                success = true,
                rows = m.Result?.Rows ?? new List<Dictionary<string, object?>>(),
                pageInfo = new
                {
                    currentPage = m.Result?.PageIndex ?? 1,
                    pageSize = m.Result?.PageSize ?? 10,
                    totalCount = m.Result?.TotalCount ?? 0,
                    totalPages = m.Result?.TotalPages ?? 0
                },
                detailPageId = m.DetailPageId
            };
            return Json(result);
        }
        return r;
    }

    /// <summary>单行详情（JSON）</summary>
    [HttpGet("/DynCommon/DynCommon/Data/Get")]
    public IActionResult Get(int projectId, int pageId, int id)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");
        using var db = _svc.CreateProjectClient(project);
        var row = _crud.GetByPk(db, page.TableName!, id, def.PrimaryKey);
        return Json(new { success = true, data = row });
    }

    /// <summary>保存（新增/更新），JSON 返回并携带 actions 指令</summary>
    [HttpPost("/DynCommon/DynCommon/Data/Save")]
    public IActionResult Save(int projectId, int pageId, [FromBody] Dictionary<string, System.Text.Json.JsonElement>? data)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return Json(new { success = false, message = "工程或页面不存在" });
        var def = ParseDef(page);
        if (def == null) return Json(new { success = false, message = "页面定义无效" });
        if (data == null || data.Count == 0) return Json(new { success = false, message = "没有提交数据" });

        var dict = new Dictionary<string, object?>();
        foreach (var kv in data) dict[kv.Key] = JsonElementToObject(kv.Value);

        try
        {
            using var db = _svc.CreateProjectClient(project);
            var table = page.TableName ?? "";
            var hasPk = dict.TryGetValue(def.PrimaryKey, out var pkv)
                        && pkv != null && !string.IsNullOrEmpty(pkv.ToString())
                        && pkv.ToString() != "0";
            object result;
            if (hasPk)
            {
                _crud.Update(db, table, dict, def);
                result = new { success = true, message = "保存成功", actions = new object[] { new { action = "showmessage", options = new { message = "保存成功", type = "success" } }, new { action = "setwindow", options = new { close = true } } } };
            }
            else
            {
                var newId = _crud.Insert(db, table, dict, def);
                result = new { success = true, message = "保存成功", id = newId, actions = new object[] { new { action = "showmessage", options = new { message = "保存成功", type = "success" } }, new { action = "setwindow", options = new { close = true } } } };
            }
            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "保存失败：" + ex.Message });
        }
    }

    /// <summary>删除（JSON）</summary>
    [HttpPost("/DynCommon/DynCommon/Data/Delete")]
    public IActionResult Delete(int projectId, int pageId, int id)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return Json(new { success = false, message = "工程或页面不存在" });
        var def = ParseDef(page);
        if (def == null) return Json(new { success = false, message = "页面定义无效" });
        try
        {
            using var db = _svc.CreateProjectClient(project);
            _crud.Delete(db, page.TableName!, id, def.PrimaryKey);
            return Json(new
            {
                success = true,
                message = "已删除",
                actions = new object[]
                {
                    new { action = "showmessage", options = new { message = "已删除", type = "success" } }
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "删除失败：" + ex.Message });
        }
    }

    // ==================== 内部 ====================

    private IActionResult BuildSummaryModel(int projectId, int pageId, DynSummaryPost? post)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        var detailId = page.DetailPageId ?? 0;
        if (detailId == 0)
        {
            detailId = _svc.GetPages(projectId)
                .FirstOrDefault(p => p.PageType == "Detail" && p.TableName == page.TableName && p.IsEnabled)?.Id ?? 0;
        }

        var filter = new Dictionary<string, object?>();
        if (post?.Filter != null)
            foreach (var kv in post.Filter)
                filter[kv.Key] = kv.Value is System.Text.Json.JsonElement je ? JsonElementToObject(je) : kv.Value;

        var pageIndex = post?.PageInfo?.CurrentPage ?? 1;
        var pageSize = post?.PageInfo?.PageSize ?? (def.PageSize > 0 ? def.PageSize : 10);

        using var db = _svc.CreateProjectClient(project);
        var result = _crud.ListPaged(db, page.TableName ?? "", def, filter, pageIndex, pageSize, QuerySource(page));
        _crud.LoadNavs(db, def, result.Rows);

        return Ok(new DynRunSummaryModel
        {
            Project = project,
            Page = page,
            Def = def,
            DetailPageId = detailId,
            Filter = filter,
            Result = result
        });
    }

    private static string? QuerySource(DynPage page)
    {
        if (string.Equals(page.DataSource, "View", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(page.ViewName))
            return page.ViewName.Trim();
        return null;
    }

    private static DynPageDefinition? ParseDef(DynPage page)
    {
        if (string.IsNullOrWhiteSpace(page.ColumnDefs)) return null;
        try { return JsonConvert.DeserializeObject<DynPageDefinition>(page.ColumnDefs); }
        catch { return null; }
    }

    private static object? JsonElementToObject(System.Text.Json.JsonElement e)
    {
        switch (e.ValueKind)
        {
            case System.Text.Json.JsonValueKind.String: return e.GetString();
            case System.Text.Json.JsonValueKind.Number:
                if (e.TryGetInt64(out var l)) return l;
                if (e.TryGetDecimal(out var d)) return d;
                return e.GetDouble();
            case System.Text.Json.JsonValueKind.True: return true;
            case System.Text.Json.JsonValueKind.False: return false;
            case System.Text.Json.JsonValueKind.Null: return null;
            default: return e.GetRawText();
        }
    }
}
