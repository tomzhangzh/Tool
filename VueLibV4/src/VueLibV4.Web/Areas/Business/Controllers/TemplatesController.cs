using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Services.Data;
using VueLibV4.Web.Infrastructure;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Areas.Business.Controllers;

/// <summary>
/// 三屏固定模板（筛选区 / 列表区 / 详情区）运行控制器（M4）。
/// 走 MVC 约定路由：/Business/Templates/Run?code=xxx、/Business/Templates/Detail?code=xxx&id=1。
/// 页面与片段均为服务端 Razor View；保存返回 ApiResult + dyn-actions（刷新表格/关弹窗/提示），页面零业务 JS。
/// 平台库元数据（DynWebPage/PageSetting）走强类型服务；业务表数据走免模型（表名为运行时字符串）。
/// </summary>
[Area("Business")]
public class TemplatesController : Controller
{
    private readonly ProjectDbResolver _projects;
    private readonly DynamicCrudService _svc;
    private readonly IDynWebPageService _pages;
    private readonly IPageSettingService _settings;

    public TemplatesController(ProjectDbResolver projects, DynamicCrudService svc,
        IDynWebPageService pages, IPageSettingService settings)
    {
        _projects = projects;
        _svc = svc;
        _pages = pages;
        _settings = settings;
    }

    /// <summary>三屏整页：筛选区 + 列表区（详情区以 layer 片段方式打开）</summary>
    [HttpGet("/Business/Templates/Run")]
    public IActionResult Run(string code)
    {
        var page = _pages.Query(p => p.Code == code).First();
        if (page == null) return Content("页面不存在：" + code);

        var filter = page.FilterPageSettingId != null ? _settings.GetById(page.FilterPageSettingId.Value) : null;
        var list = page.ListPageSettingId != null ? _settings.GetById(page.ListPageSettingId.Value) : null;
        var detail = page.DetailPageSettingId != null ? _settings.GetById(page.DetailPageSettingId.Value) : null;

        ViewBag.PageName = page.Name;
        ViewBag.PageCode = page.Code;
        ViewBag.GridId = TryGetGridId(page, list);
        ViewBag.FilterCfgJson = filter?.ConfigJson ?? "{}";
        ViewBag.ListCfgJson = list?.ConfigJson ?? "{}";
        ViewBag.DetailUrl = "/Business/Templates/Detail?code=" + Uri.EscapeDataString(page.Code);
        return View("~/Areas/Business/Views/Templates/Run.cshtml");
    }

    /// <summary>详情区 HTML 片段（layer fragment 加载；id 为空=新增）</summary>
    [HttpGet("/Business/Templates/Detail")]
    public IActionResult Detail(string code, string id = null)
    {
        var page = _pages.Query(p => p.Code == code).First();
        if (page == null || page.DetailPageSettingId == null) return Content("详情配置不存在");
        var detailSetting = _settings.GetById(page.DetailPageSettingId.Value);
        if (detailSetting == null) return Content("详情配置不存在");
        var listSetting = page.ListPageSettingId != null ? _settings.GetById(page.ListPageSettingId.Value) : null;

        var (table, project) = ResolveTableProject(detailSetting, listSetting, page);
        ViewBag.DetailCfgJson = detailSetting.ConfigJson ?? "{}";
        ViewBag.SaveUrl = "/Business/Templates/Save?code=" + Uri.EscapeDataString(page.Code);

        JObject row = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            try
            {
                using var bdb = _projects.Resolve(project);
                var pks = _svc.PrimaryKeys(bdb, table);
                var pk = pks.Count > 0 ? pks[0] : "Id";
                row = _svc.First(bdb, table, $"[{pk}]=@v", new { v = id });
            }
            catch (Exception ex)
            {
                return Content("加载数据失败：" + ex.Message);
            }
        }
        ViewBag.RowJson = row?.ToString(Newtonsoft.Json.Formatting.None) ?? "{}";
        return PartialView("~/Areas/Business/Views/Templates/Detail.cshtml");
    }

    /// <summary>保存详情（有主键→更新，无主键→新增）；返回 ApiResult 携带 grid 刷新 + 关弹窗 + 提示三个 dyn-actions</summary>
    [HttpPost("/Business/Templates/Save")]
    public ApiResult Save(string code, [FromBody] JObject data)
    {
        var page = _pages.Query(p => p.Code == code).First();
        if (page == null || page.DetailPageSettingId == null) return ApiResult.Fail("页面配置不存在");
        var detailSetting = _settings.GetById(page.DetailPageSettingId.Value);
        var listSetting = page.ListPageSettingId != null ? _settings.GetById(page.ListPageSettingId.Value) : null;
        var (table, project) = ResolveTableProject(detailSetting, listSetting, page);

        object result;
        string msg;
        try
        {
            using var bdb = _projects.Resolve(project);
            var pks = _svc.PrimaryKeys(bdb, table);
            var hasPk = pks.Count > 0 && pks.All(p => data?[p] != null && data[p].Type != JTokenType.Null && !string.IsNullOrEmpty(data[p].ToString()));
            result = hasPk ? _svc.Update(bdb, table, data) : _svc.Insert(bdb, table, data);
            msg = hasPk ? "更新成功" : "新增成功";
        }
        catch (Exception ex)
        {
            // 反射调用（DynamicCrudService）抛 TargetInvocationException 时取最内层真实信息
            return ApiResult.Fail("保存失败：" + ex.GetBaseException().Message);
        }

        var gridId = TryGetGridId(page, listSetting);
        var actions = new List<DynJavaScript>
        {
            new FlashMessageJavaScript { Message = msg, Type = "success" },
            new GridReloadJavaScript { GridId = gridId },
            new CloseDialogJavaScript()
        };
        return ApiResult.Ok(new { result }, msg).WithActions(actions.ToArray());
    }

    // ---------------- 辅助 ----------------

    /// <summary>从页面配置/列表区配置解析 gridId（grid 动作用）</summary>
    private static string TryGetGridId(DynWebPage page, PageSetting listSetting)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(page.ConfigJson))
            {
                var pc = JObject.Parse(page.ConfigJson);
                var g = pc["gridId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(g)) return g;
            }
        }
        catch { }
        try
        {
            if (listSetting?.ConfigJson != null)
            {
                var lc = JObject.Parse(listSetting.ConfigJson);
                var g = lc["options"]?["comoptions"]?["gridId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(g)) return g;
            }
        }
        catch { }
        return "grid_" + page.Code;
    }

    /// <summary>解析业务表名与项目（优先 PageSetting 列，回退列表区 ConfigJson）</summary>
    private static (string table, string project) ResolveTableProject(PageSetting detail, PageSetting list, DynWebPage page)
    {
        string table = detail?.TableName;
        string project = detail?.ProjectId?.ToString();
        if (string.IsNullOrWhiteSpace(table) && list != null)
        {
            table = list.TableName;
            if (string.IsNullOrWhiteSpace(project)) project = list.ProjectId?.ToString();
        }
        if (string.IsNullOrWhiteSpace(table))
        {
            try
            {
                var src = list ?? detail;
                var cfg = JObject.Parse(src?.ConfigJson ?? "{}");
                table = cfg["options"]?["comoptions"]?["table"]?.ToString();
                var p = cfg["options"]?["comoptions"]?["project"]?.ToString();
                if (!string.IsNullOrWhiteSpace(p)) project = p;
            }
            catch { }
        }
        return (table, project);
    }
}
