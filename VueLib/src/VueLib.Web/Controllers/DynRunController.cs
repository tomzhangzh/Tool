using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>
/// 动态预览运行时：按"工程 + 页面定义"连接工程数据库动态渲染汇总屏 / 细节屏，
/// 复用 dyn-lib 动态引擎（dyn-init / dyn-click-postback / dyn-click-open）。
/// </summary>
public class DynRunController : Controller
{
    private readonly DynProjectService _svc;
    private readonly DynCrudService _crud;

    public DynRunController(DynProjectService svc, DynCrudService crud)
    {
        _svc = svc;
        _crud = crud;
    }

    // ==================== 预览外壳 ====================

    public IActionResult Index(int projectId, int pageId = 0)
    {
        var project = _svc.GetProject(projectId);
        if (project == null) return NotFound("工程不存在");
        var pages = _svc.GetPages(projectId).Where(p => p.IsEnabled).OrderBy(p => p.PageType).ThenBy(p => p.Name).ToList();
        var page = pageId > 0 ? pages.FirstOrDefault(p => p.Id == pageId)
                              : pages.FirstOrDefault(p => p.PageType == "Summary");
        ViewBag.Project = project;
        ViewBag.Pages = pages;
        ViewBag.CurrentPage = page;
        return View();
    }

    // ==================== 汇总屏 / 细节屏（分部视图） ====================

    [HttpPost]
    public IActionResult Summary(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is not OkObjectResult ok || ok.Value is not DynRunSummaryModel m) return r;
        return PartialView("_Summary", m);
    }

    [HttpGet]
    public IActionResult Detail(int projectId, int pageId, int id = 0)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        using var db = _svc.CreateProjectClient(project);
        var row = id > 0
            ? _crud.GetByPk(db, page.TableName ?? "", id, def.PrimaryKey)
            : BuildEmptyRow(def);

        var model = new DynRunDetailModel { Project = project, Page = page, Def = def, Row = row };
        return PartialView("_Detail", model);
    }

    [HttpPost]
    public IActionResult Save(int projectId, int pageId, [FromBody] Dictionary<string, System.Text.Json.JsonElement>? data)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return Ok(new { Success = false, Message = "工程或页面不存在" });
        var def = ParseDef(page);
        if (def == null) return Ok(new { Success = false, Message = "页面定义无效" });
        if (data == null || data.Count == 0) return Ok(new { Success = false, Message = "没有提交数据" });

        // System.Text.Json 把值反序列化为 JsonElement，需先转换为 CLR 基础类型
        var dict = new Dictionary<string, object?>();
        foreach (var kv in data) dict[kv.Key] = JsonElementToObject(kv.Value);

        try
        {
            using var db = _svc.CreateProjectClient(project);
            var table = page.TableName ?? "";
            // 主键为 0（自增列的新记录）视为无主键 → 走新增；仅非空且非 0 才视为更新
            var hasPk = dict.TryGetValue(def.PrimaryKey, out var pkv)
                        && pkv != null && !string.IsNullOrEmpty(pkv.ToString())
                        && pkv.ToString() != "0";
            if (hasPk)
            {
                _crud.Update(db, table, dict, def);
                return Ok(new { Success = true, Message = "保存成功" });
            }
            var newId = _crud.Insert(db, table, dict, def);
            return Ok(new { Success = true, Message = "保存成功", Id = newId });
        }
        catch (Exception ex)
        {
            return Ok(new { Success = false, Message = "保存失败：" + ex.Message });
        }
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

    [HttpPost]
    public IActionResult Delete(int projectId, int pageId, int id)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return Ok(new { Success = false, Message = "工程或页面不存在" });
        var def = ParseDef(page);
        if (def == null) return Ok(new { Success = false, Message = "页面定义无效" });
        try
        {
            using var db = _svc.CreateProjectClient(project);
            _crud.Delete(db, page.TableName ?? "", id, def.PrimaryKey);
            return Ok(new { Success = true, Message = "已删除" });
        }
        catch (Exception ex)
        {
            return Ok(new { Success = false, Message = "删除失败：" + ex.Message });
        }
    }

    // ==================== 通用 JSON 数据接口（供外部调用生成屏的查询） ====================

    [HttpPost("/DynRun/Data/List")]
    public IActionResult DataList(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is OkObjectResult ok && ok.Value is DynRunSummaryModel m)
            return Json(m.Result);
        return r;
    }

    [HttpPost("/DynRun/Data/Get")]
    public IActionResult DataGet(int projectId, int pageId, int id)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");
        using var db = _svc.CreateProjectClient(project);
        var row = _crud.GetByPk(db, page.TableName ?? "", id, def.PrimaryKey);
        return Json(row);
    }

    // ==================== 内部 ====================

    private IActionResult BuildSummaryModel(int projectId, int pageId, DynSummaryPost? post)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        // 解析细节屏页面（用于编辑/新增按钮）
        var detailId = page.DetailPageId ?? 0;
        if (detailId == 0)
        {
            detailId = _svc.GetPages(projectId)
                .FirstOrDefault(p => p.PageType == "Detail" && p.TableName == page.TableName && p.IsEnabled)?.Id ?? 0;
        }

        var filter = new Dictionary<string, object?>();
        if (post?.Filter != null)
        {
            foreach (var kv in post.Filter)
                filter[kv.Key] = kv.Value is System.Text.Json.JsonElement je ? JsonElementToObject(je) : kv.Value;
        }
        var pageIndex = post?.PageInfo?.CurrentPage ?? 1;
        var pageSize = post?.PageInfo?.PageSize ?? (def.PageSize > 0 ? def.PageSize : 10);

        using var db = _svc.CreateProjectClient(project);
        var result = _crud.ListPaged(db, page.TableName ?? "", def, filter, pageIndex, pageSize);
        var model = new DynRunSummaryModel
        {
            Project = project,
            Page = page,
            Def = def,
            DetailPageId = detailId,
            Filter = filter,
            Result = result
        };
        return Ok(model);
    }

    private static DynPageDefinition? ParseDef(DynPage page)
    {
        if (string.IsNullOrWhiteSpace(page.ColumnDefs)) return null;
        try { return JsonConvert.DeserializeObject<DynPageDefinition>(page.ColumnDefs); }
        catch { return null; }
    }

    /// <summary>新增时按列定义生成空行模板（保证控件初始值类型正确）</summary>
    private static Dictionary<string, object?> BuildEmptyRow(DynPageDefinition def)
    {
        var row = new Dictionary<string, object?>();
        foreach (var c in def.Columns)
        {
            row[c.Name] = c.DbType switch
            {
                "bool" => false,
                "int" or "long" or "decimal" => 0,
                _ => ""
            };
        }
        return row;
    }

    public static string JsonModel(object model)
    {
        return JsonConvert.SerializeObject(model, new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver(),
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            NullValueHandling = NullValueHandling.Ignore
        });
    }
}
