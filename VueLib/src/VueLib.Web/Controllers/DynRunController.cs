using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using VueLib.Web.Data;
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
    private readonly AppDbContext _appDb;

    public DynRunController(DynProjectService svc, DynCrudService crud, AppDbContext appDb)
    {
        _svc = svc;
        _crud = crud;
        _appDb = appDb;
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
    public IActionResult Detail(int projectId, int pageId, int id = 0, int settingId = 0, string? _params = null)
    {
        var project = _svc.GetProject(projectId);
        var page = _svc.GetPage(pageId);
        if (project == null || page == null) return BadRequest("工程或页面不存在");
        var def = ParseDef(page);
        if (def == null) return BadRequest("页面定义无效");

        using var db = _svc.CreateProjectClient(project);
        var row = _crud.GetByPk(db, page.TableName, id, def.PrimaryKey);
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

        // 显示层 PageSetting（Detail 屏用其 ConfigJson 渲染表单，数据仍由 DynPage 提供）
        var model = new DynRunDetailModel { Project = project, Page = page, Def = def, Row = row };
        if (settingId > 0)
        {
            model.PageSetting = _svc.GetPageSetting(settingId);
            if (model.PageSetting != null)
                return PartialView("_DetailSetting", model);
        }
        return PartialView("_Detail", model);
    }

    // ==================== 路由页面运行时（模板驱动） ====================

    /// <summary>字典下拉数据源（POST，避免 GET 缓存）：从设计库 DynDict 按 DictType 读取 {label,value} 选项</summary>
    [HttpPost("/DynRun/Dict")]
    public IActionResult Dict([FromBody] JObject? body, [FromQuery] string? dictType)
    {
        var dt = dictType;
        if (string.IsNullOrWhiteSpace(dt) && body != null && body["dictType"] is JValue jv)
            dt = jv.ToString();
        if (string.IsNullOrWhiteSpace(dt)) return Ok(new { success = false, message = "dictType 不能为空" });
        try
        {
            using var db = _appDb.Create();
            var rows = db.Ado.SqlQuery<DynDictOption>(
                "SELECT DictText, DictValue FROM DynDict WHERE DictType=@dt AND (IsEnabled=1 OR IsEnabled IS NULL) ORDER BY SortOrder, Id",
                new { dt = dt.Trim() });
            var items = rows.Select(r => new { label = r.DictText ?? "", value = r.DictValue ?? "" }).ToList();
            return Ok(new { success = true, items });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>字典选项（SqlSugar 强类型映射列）</summary>
    private class DynDictOption
    {
        public string? DictText { get; set; }
        public string? DictValue { get; set; }
    }

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

        // ===== 新架构：模板关联三屏 PageSetting（显示层）→ 专用视图 RouteListSetting =====
        if (renderView == "RouteList")
        {
            var (fps, lps, dps) = DynProjectService.EffectivePageSettingIds(wp, template);
            if (fps != null || lps != null || dps != null)
            {
                var psModel = new DynRouteListModel
                {
                    Project = project,
                    WebPage = wp,
                    Template = template,
                    TemplateConfig = tcfg,
                    PageConfig = DynProjectService.ParseWebPageConfig(wp),
                    Params = DynProjectService.ParseParams(wp),
                    FilterPageSettingId = fps,
                    ListPageSettingId = lps,
                    DetailPageSettingId = dps,
                    FilterPageSetting = fps != null ? _svc.GetPageSetting(fps) : null,
                    ListPageSetting = lps != null ? _svc.GetPageSetting(lps) : null,
                    DetailPageSetting = dps != null ? _svc.GetPageSetting(dps) : null
                };
                return View("RouteListSetting", psModel);
            }
        }

        if (renderView == "RouteList")

        // RouteCrud 模板：由设计器组件 DynCrudPage（Filter+List+Detail 三页 ID 可配置）渲染
        if (renderView == "RouteCrud")
        {
            var fp2 = fp > 0 ? _svc.GetPage(fp.Value) : null;
            var sp2 = sp > 0 ? _svc.GetPage(sp.Value) : null;
            var dp2 = dp > 0 ? _svc.GetPage(dp.Value) : null;
            if (sp2 == null) return BadRequest("模板未配置汇总屏(ListPageId)");
            var crudModel = new DynRouteListModel
            {
                Project = project,
                WebPage = wp,
                Template = template,
                FilterPage = fp2,
                SummaryPage = sp2,
                DetailPage = dp2,
                TemplateConfig = tcfg,
                PageConfig = DynProjectService.ParseWebPageConfig(wp),
                Params = DynProjectService.ParseParams(wp)
            };
            return View("RouteCrud", crudModel);
        }

        // RouteTreeList 模板：左树右列表（DynTreeList 组件，如 年级树→班级列表）
        if (renderView == "RouteTreeList")
        {
            var ft = fp > 0 ? _svc.GetPage(fp.Value) : null;
            var st = sp > 0 ? _svc.GetPage(sp.Value) : null;
            var dt = dp > 0 ? _svc.GetPage(dp.Value) : null;
            if (st == null) return BadRequest("模板未配置列表页(ListPageId)");
            var tlModel = new DynRouteListModel
            {
                Project = project,
                WebPage = wp,
                Template = template,
                FilterPage = ft,
                SummaryPage = st,
                DetailPage = dt,
                TemplateConfig = tcfg,
                PageConfig = DynProjectService.ParseWebPageConfig(wp),
                Params = DynProjectService.ParseParams(wp)
            };
            return View("RouteTreeList", tlModel);
        }

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
        // 前端可传排序（DynCrudPage 组件表头排序）
        if (!string.IsNullOrWhiteSpace(post?.PageInfo?.OrderBy))
        {
            summaryDef.OrderBy = post.PageInfo.OrderBy.Trim();
            summaryDef.OrderDir = string.Equals(post.PageInfo.OrderDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        }

        using var db = _svc.CreateProjectClient(project);
        var qd = DynCrudService.BuildQueryDef(summaryDef, filterDef);
        var result = _crud.ListPaged(db, summaryPage.TableName ?? "", qd, filter, pageIndex, pageSize, QuerySource(summaryPage));
        _crud.LoadNavs(db, summaryDef, result.Rows);
        return Json(result);
    }

    /// <summary>
    /// 新架构：三屏 PageSetting 数据接口（显示层组件树 + 数据源注入）
    /// GET /DynRun/Screen?projectId=&amp;route=&amp;screen=filter|list|detail&amp;id=&amp;page=&amp;size=
    /// 返回 { success, config(PageSetting.ConfigJson 组件树), model(数据), pageSetting }
    /// </summary>
    [HttpGet("/DynRun/Screen")]
    public IActionResult Screen(int projectId, string route, string screen, int? id = 0, int? page = 1, int? size = null)
    {
        var project = _svc.GetProject(projectId);
        var wp = _svc.FindWebPage(projectId, route);
        if (project == null || wp == null) return Json(new { success = false, message = "路由页面不存在" });
        var template = _svc.GetTemplate(wp.TemplateId);
        var (fps, lps, dps) = DynProjectService.EffectivePageSettingIds(wp, template);
        var psId = (screen ?? "").ToLower() switch
        {
            "filter" => fps,
            "list" => lps,
            _ => dps
        };
        var ps = _svc.GetPageSetting(psId);
        if (ps == null) return Json(new { success = false, message = "该屏未配置 PageSetting" });

        // 数据源：DynPage（旧字段兼容，模板/页面仍关联数据源定义）
        var (fp, sp, dp) = DynProjectService.EffectivePageIds(wp, template);
        object? model = null;
        if (screen == "list" && sp != null)
        {
            var spage = _svc.GetPage(sp.Value);
            var sdef = spage != null ? ParseDef(spage) : null;
            if (spage != null && sdef != null)
            {
                using var db = _svc.CreateProjectClient(project);
                var qd = DynCrudService.BuildQueryDef(sdef, null);
                var ps2 = page ?? 1;
                var ss2 = size ?? (sdef.PageSize > 0 ? sdef.PageSize : 10);
                var result = _crud.ListPaged(db, spage.TableName ?? "", qd, null, ps2, ss2, QuerySource(spage));
                _crud.LoadNavs(db, sdef, result.Rows);
                model = new
                {
                    Filter = new Dictionary<string, object?>(),
                    Rows = result.Rows,
                    PageInfo = new
                    {
                        CurrentPage = result.PageIndex,
                        PageSize = result.PageSize,
                        TotalCount = result.TotalCount,
                        TotalPages = result.TotalPages
                    }
                };
            }
        }
        else if (screen == "detail" && dp != null)
        {
            var dpage = _svc.GetPage(dp.Value);
            var ddef = dpage != null ? ParseDef(dpage) : null;
            if (dpage != null && ddef != null)
            {
                using var db = _svc.CreateProjectClient(project);
                var row = _crud.GetByPk(db, dpage.TableName ?? "", id ?? 0, ddef.PrimaryKey);
                if (id > 0 && row.Count > 0) _crud.LoadNavs(db, ddef, new[] { row });
                model = new { Row = row };
            }
        }
        model ??= new Dictionary<string, object?>();
        return Json(new
        {
            success = true,
            screen = screen,
            config = ps.ConfigJson, // 原始 JSON 字符串（JObject 经 System.Text.Json 序列化会损坏嵌套结构）
            model = model,
            pageSetting = new { ps.Id, ps.PageName, ps.PageCode }
        });
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

    /// <summary>
    /// 页面定义接口（DynCrudPage 等前端组合组件使用）
    /// GET /DynRun/PageDef?projectId=4&amp;pageId=10
    /// 返回页面定义 + 表名 + 关联细节页
    /// </summary>
    [HttpGet("/DynRun/PageDef")]
    public IActionResult PageDef(int projectId, int pageId)
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
        return Json(new
        {
            success = true,
            pageId,
            tableName = page.TableName,
            pageType = page.PageType,
            detailPageId = detailId,
            title = page.Title ?? page.Name,
            def
        });
    }

    /// <summary>
    /// 树形数据接口（DynTree / DynTreeList 组件使用）
    /// POST /DynRun/Data/Tree  body: { projectId, table, idField, parentIdField, nameField, rootId }
    /// 返回树节点 [{ id, label, value, children: [...] }]
    /// </summary>
    [HttpPost("/DynRun/Data/Tree")]
    public IActionResult DataTree([FromBody] DynTreeRequest? req)
    {
        if (req == null) return BadRequest("参数错误");
        var project = _svc.GetProject(req.ProjectId);
        if (project == null) return BadRequest("工程不存在");
        if (string.IsNullOrWhiteSpace(req.Table)) return BadRequest("table 必填");
        var idField = string.IsNullOrWhiteSpace(req.IdField) ? "Id" : req.IdField!.Trim();
        var parentField = string.IsNullOrWhiteSpace(req.ParentIdField) ? "ParentId" : req.ParentIdField!.Trim();
        var nameField = string.IsNullOrWhiteSpace(req.NameField) ? "Name" : req.NameField!.Trim();
        var rootId = req.RootId ?? 0;

        try
        {
            using var db = _svc.CreateProjectClient(project);
            var valueField = string.IsNullOrWhiteSpace(req.ValueField) ? idField : req.ValueField!.Trim();
            var sql = $"SELECT [{idField}] AS \"id\", [{parentField}] AS \"pid\", [{nameField}] AS \"label\", [{valueField}] AS \"value\" FROM [{req.Table}]";
            var dt = db.Ado.GetDataTable(sql);
            var rows = dt.Rows.Cast<System.Data.DataRow>();
            var nodes = new List<Dictionary<string, object?>>();
            foreach (var r in rows)
            {
                nodes.Add(new Dictionary<string, object?>
                {
                    ["id"] = Convert.ToInt64(r["id"]),
                    ["pid"] = r["pid"] != DBNull.Value ? Convert.ToInt64(r["pid"]) : 0L,
                    ["label"] = Convert.ToString(r["label"]) ?? "",
                    ["value"] = Convert.ToInt64(r["value"])
                });
            }
            var tree = BuildTree(nodes, rootId);
            return Json(new { success = true, data = tree, count = nodes.Count });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>把扁平列表按 pid 构造成树（rootId=0 表示根）</summary>
    private static List<object> BuildTree(List<Dictionary<string, object?>> nodes, long rootId)
    {
        // 父节点缺失（如 pid 指向其他表/不存在的 id）时提升为根，保证任意父子配置都能成树
        var byPid = new Dictionary<long, List<Dictionary<string, object?>>>();
        var ids = new HashSet<long>();
        foreach (var n in nodes)
        {
            var id = (long)(n["id"] ?? 0L);
            var pid = (long)(n["pid"] ?? 0L);
            ids.Add(id);
            if (!byPid.TryGetValue(pid, out var list)) { list = new List<Dictionary<string, object?>>(); byPid[pid] = list; }
            list.Add(n);
        }
        // visited 防环：父子字段数值冲突（如外键与 id 撞号）时避免无限递归
        List<object> children(long pid, HashSet<long> visited)
        {
            var result = new List<object>();
            if (!byPid.TryGetValue(pid, out var kids)) return result;
            foreach (var k in kids.OrderBy(k => (long)(k["id"] ?? 0L)))
            {
                var cid = (long)(k["id"] ?? 0L);
                if (!visited.Add(cid)) continue;
                result.Add(new
                {
                    id = k["id"],
                    value = k["value"],
                    label = k["label"],
                    children = children(cid, visited)
                });
            }
            return result;
        }
        // 根：rootId 指定，或父 id 不在节点集合中（提升为根）
        var roots = new List<object>();
        if (rootId != 0) return children(rootId, new HashSet<long>());
        foreach (var n in nodes)
        {
            var pid = (long)(n["pid"] ?? 0L);
            var id = (long)(n["id"] ?? 0L);
            if (pid == 0 || !ids.Contains(pid)) roots.Add(new
            {
                id = n["id"],
                value = n["value"],
                label = n["label"],
                children = children(id, new HashSet<long>())
            });
        }
        // 兜底：若提升后仍无根（父键为外键且数值与 id 冲突），全部平铺为根
        if (roots.Count == 0)
        {
            foreach (var n in nodes.OrderBy(k => (long)(k["id"] ?? 0L)))
            {
                var id = (long)(n["id"] ?? 0L);
                roots.Add(new
                {
                    id = n["id"],
                    value = n["value"],
                    label = n["label"],
                    children = children(id, new HashSet<long>())
                });
            }
        }
        return roots;
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

    ///// <summary>新增时按列定义生成空行模板（保证控件初始值类型正确）</summary>
    //private static Dictionary<string, object?> BuildEmptyRow(DynPageDefinition def)
    //{
    //    var row = new Dictionary<string, object?>();
    //    foreach (var c in def.Columns)
    //    {
    //        if (c.IsNullable)
    //        {
    //            row[c.Name] = null; // ✅可空字段直接null
    //        }
    //        else
    //        {
    //            row[c.Name] = c.DbType switch
    //            {
    //                "bool" => false,
    //                "int" or "long" or "decimal" => 0,
    //                "datetime" => (DateTime?)null,
    //                "guid" => (Guid?)null,
    //                _ => string.Empty
    //            };
    //        }
    //    }
    //    return row;
    //}

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
