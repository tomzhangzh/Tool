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

    /// <summary>只读查询屏（筛选 + 表格 + 分页，无增删改操作列）</summary>
    [HttpPost]
    public IActionResult Filter(int projectId, int pageId, [FromBody] DynSummaryPost? post)
    {
        var r = BuildSummaryModel(projectId, pageId, post);
        if (r is not OkObjectResult ok || ok.Value is not DynRunSummaryModel m) return r;
        return PartialView("_Filter", m);
    }

    [HttpGet]
    public IActionResult Detail(int projectId, int pageId, int id = 0, string? _params = null)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        using var db = _svc.CreateProjectClient(project);
        var row = id > 0
            ? _crud.GetByPk(db, page.TableName ?? "", id, def.PrimaryKey, QuerySource(page))
            : BuildEmptyRow(def);
        // 新增时模板预填参数（addParams）：合并进空行
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
        // 外键导航注入（多对一 object / 一对多 array）
        if (id > 0 && row.Count > 0) _crud.LoadNavs(db, def, new[] { row });

        var model = new DynRunDetailModel { Project = project, Page = page, Def = def, Row = row };
        return PartialView("_Detail", model);
    }

    // ==================== 路由页面运行时（模板驱动） ====================

    /// <summary>按"路由"渲染页面：路由 → 模板（List=Filter+Summary+Detail / Home=主页）</summary>
    [HttpGet("/DynRun/Route")]
    public IActionResult Route(int projectId, string route)
    {
        var project = _svc.GetProject(projectId);
        var wp = _svc.FindWebPage(projectId, route);
        if (project == null || wp == null) return NotFound("路由页面不存在");
        var template = _svc.GetTemplate(wp.TemplateId);
        if (template == null) return NotFound("模板不存在");

        var tcfg = DynProjectService.ParseTemplateConfig(template);
        var (fp, sp, dp) = DynProjectService.EffectivePageIds(wp, template);

        // 模板统一页面：按 RenderView 分派（兼容旧数据：未配置时按 TemplateType 推导）
        var renderView = string.IsNullOrEmpty(template.RenderView)
            ? (template.TemplateType == "Home" ? "RouteHome" : "RouteList")
            : template.RenderView;

        if (renderView == "RouteHome")
        {
            var homeModel = new DynRouteHomeModel
            {
                Project = project,
                WebPage = wp,
                Template = template,
                PageConfig = DynProjectService.ParseWebPageConfig(wp),
                Pages = _svc.GetWebPages(projectId).Where(x => x.Id != wp.Id && x.IsEnabled).ToList(),
                Params = DynProjectService.ParseParams(wp)
            };
            // 主页模板可配置一个 Summary 屏做数据看板
            if (sp > 0)
            {
                var spage = _svc.GetPage(sp.Value);
                var sdef = spage != null ? ParseDef(spage) : null;
                if (spage != null && sdef != null)
                {
                    homeModel.HomeSummaryDef = sdef;
                    using var db = _svc.CreateProjectClient(project);
                    homeModel.HomeResult = _crud.ListPaged(db, spage.TableName ?? "", sdef, null, 1, sdef.PageSize > 0 ? sdef.PageSize : 10, QuerySource(spage));
                }
            }
            return View("RouteHome", homeModel);
        }

        if (renderView == "RouteList")
        {
        // List 模板（默认）
        var filterPage = fp > 0 ? _svc.GetPage(fp.Value) : null;
        var summaryPage = sp > 0 ? _svc.GetPage(sp.Value) : null;
        var detailPage = dp > 0 ? _svc.GetPage(dp.Value) : null;
        if (summaryPage == null) return BadRequest("模板未配置汇总屏");
        var filterDef = filterPage != null ? ParseDef(filterPage) : null;
        var summaryDef = ParseDef(summaryPage);
        if (summaryDef == null) return BadRequest("汇总屏定义无效");

        // 初始数据（通用后端）；若配置了自定义数据 url，则由前端另行请求
        var listModel = new DynRouteListModel
        {
            Project = project,
            WebPage = wp,
            Template = template,
            FilterPage = filterPage,
            SummaryPage = summaryPage,
            DetailPage = detailPage,
            FilterDef = filterDef,
            SummaryDef = summaryDef,
            TemplateConfig = tcfg,
            PageConfig = DynProjectService.ParseWebPageConfig(wp),
            Filter = new Dictionary<string, object?>(),
            Params = DynProjectService.ParseParams(wp)
        };
        if (string.IsNullOrWhiteSpace(tcfg?.DataUrl))
        {
            using var db = _svc.CreateProjectClient(project);
            var qd = DynCrudService.BuildQueryDef(summaryDef, filterDef);
            var result = _crud.ListPaged(db, summaryPage.TableName ?? "", qd, null, 1, summaryDef.PageSize > 0 ? summaryDef.PageSize : 10, QuerySource(summaryPage));
            _crud.LoadNavs(db, summaryDef, result.Rows);
            listModel.Result = result;
        }
        else
        {
            listModel.Result = new PagedResult<Dictionary<string, object?>>();
        }
            return View("RouteList", listModel);
        }

        // 通用模板：RouteCustom（按 Params 动态渲染）
        var customModel = new DynRouteCustomModel
        {
            Project = project,
            WebPage = wp,
            Template = template,
            Params = DynProjectService.ParseParams(wp),
            Schema = DynProjectService.ParseParamSchema(template),
            Pages = _svc.GetPages(projectId)
        };
        return View("RouteCustom", customModel);
    }

    /// <summary>List 模板的数据接口：Filter 屏定义筛选字段，Summary 屏定义表格/排序/分页</summary>
    [HttpPost("/DynRun/Route/List")]
    public IActionResult RouteList(int projectId, string route, [FromBody] DynSummaryPost? post)
    {
        var project = _svc.GetProject(projectId);
        var wp = _svc.FindWebPage(projectId, route);
        if (project == null || wp == null) return BadRequest("路由页面不存在");
        var template = _svc.GetTemplate(wp.TemplateId);
        if (template == null) return BadRequest("模板不存在");
        var (fp, sp, _) = DynProjectService.EffectivePageIds(wp, template);
        var summaryPage = sp > 0 ? _svc.GetPage(sp.Value) : null;
        if (summaryPage == null) return BadRequest("模板未配置汇总屏");
        var summaryDef = ParseDef(summaryPage);
        if (summaryDef == null) return BadRequest("汇总屏定义无效");

        var filterDef = fp > 0 ? ParseDef(_svc.GetPage(fp.Value)) : null;

        var filter = new Dictionary<string, object?>();
        if (post?.Filter != null)
            foreach (var kv in post.Filter)
                filter[kv.Key] = kv.Value is System.Text.Json.JsonElement je ? JsonElementToObject(je) : kv.Value;
        var pageIndex = post?.PageInfo?.CurrentPage ?? 1;
        var pageSize = post?.PageInfo?.PageSize ?? (summaryDef.PageSize > 0 ? summaryDef.PageSize : 10);

        using var db = _svc.CreateProjectClient(project);
        var qd = DynCrudService.BuildQueryDef(summaryDef, filterDef);
        var result = _crud.ListPaged(db, summaryPage.TableName ?? "", qd, filter, pageIndex, pageSize, QuerySource(summaryPage));
        _crud.LoadNavs(db, summaryDef, result.Rows);
        return Json(result);
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
        // 数据源模式：View → 从真实视图读取；Dynamic → 动态查表
        var result = _crud.ListPaged(db, page.TableName ?? "", def, filter, pageIndex, pageSize, QuerySource(page));
        // 外键导航注入
        _crud.LoadNavs(db, def, result.Rows);
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

    /// <summary>读取数据源：页面配置为真实视图且视图名非空时用视图，否则用真实表</summary>
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
