using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面视图控制器：所有页面由服务器端 Razor View 返回（而非静态 HTML），
/// 便于后端注入全局配置（ApiBase / 页面参数 / 权限校验）。
/// 访问示例：
///   /Platform/Page/Desktop                    工作台桌面
///   /Platform/Page/Designer                   页面设计器
///   /Platform/Page/WebPageRender?code=student-manage   动态页面
///   /Platform/Page/DynWebPage?id=1            模板引擎运行时（外壳视图）
///   /Platform/Page/Demo/ActionHelper          动作助手 Demo
///   /Platform/Page/Demo/Rpc                   能力桥 RPC Demo
/// 注：Demo/工具/认证/权限等页面已拆分到对应 Controller。
/// </summary>
[Area("Platform")]
public class PageController : Controller
{
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;
    private readonly IDynTemplateBlockService _tplBlocks;
    private readonly IDynBlockService _blocks;
    private readonly IConfiguration _config;

    public PageController(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings,
        IDynTemplateBlockService tplBlocks,
        IDynBlockService blocks,
        IConfiguration config)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
        _tplBlocks = tplBlocks;
        _blocks = blocks;
        _config = config;
    }

    /// <summary>工作台桌面（DesktopSolution + DesktopShortcut）</summary>
    [HttpGet("/Platform/Page/Desktop")]
    public IActionResult Desktop()
    {
        ViewData["Title"] = "VueLibV4 企业级低代码平台";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "Desktop"));
    }

    /// <summary>
    /// 页面设计器（三栏：组件库 / 画布 / 属性面板）。
    /// embed=1 时为弹窗嵌入模式（Layout=null 独立壳 + postMessage 协议，供 DynDesignerDialog 使用）。
    /// </summary>
    [HttpGet("/Platform/Page/Designer")]
    public IActionResult Designer(string embed = null)
    {
        ViewData["Title"] = embed == "1" ? "页面设计器（嵌入） - VueLibV4" : "页面设计器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewBag.Embed = embed;
        return View(string.Format(Page, "Designer"));
    }

    /// <summary>动态页面运行时（DynWebPage，按 code 或 id）</summary>
    [HttpGet("/Platform/Page/WebPageRender")]
    public IActionResult WebPageRender(string code = null, string id = null)
    {
        ViewData["Title"] = "动态页面 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewData["PageCode"] = code;
        ViewData["PageId"] = id;
        return View(string.Format(Page, "WebPageRender"));
    }

    /// <summary>
    /// 模板引擎运行时入口：/Platform/Page/DynWebPage?id={DynWebPageId}
    /// 流程：读实例 → 读模板 → 解析实例参数(ConfigJson) → TableName 自动补齐 dyndata Url →
    /// 组装 DynSharedModel（RawParams / EffectiveParams / PassThrough / TemplateConfig / 三屏 PageSetting）→ 渲染模板外壳视图。
    /// </summary>
    [HttpGet("/Platform/Page/DynWebPage")]
    public IActionResult DynWebPage(long id)
    {
        var page = _webPages.GetById((int)id);
        if (page == null) return Content("页面实例不存在：" + id);
        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        if (template == null) return Content("页面实例未绑定模板（TemplateId 为空）");

        // 1) 原始参数：用户在参数面板填写的结果（存于实例 ParamsJson；积木模板另有 blocks 槽位分组）
        var rawParams = TryParseDict(page.ParamsJson);

        // 2) 自动推导：TableName 有值且 Url 为空 → 补 dyndata 免 model 接口；手动填写优先
        var effective = BuildEffectiveParams(rawParams);
        // 三屏 PageSettingId 作为实例参数存于 ConfigJson（rawParams→effective），不再作为 DynWebPage 独立列。

        // 3) 模板 ConfigJson 顶层 mPassThrough（透传属性，向下传给外壳 / createapp）
        var passThrough = new JObject();
        try
        {
            var cfg = JObject.Parse(template.ConfigJson ?? "{}");
            if (cfg["mPassThrough"] is JObject pt) passThrough = pt;
        }
        catch { }

        // 4) 模板默认配置（外壳可选用）
        var templateConfig = new JObject();
        try { templateConfig = JObject.Parse(template.DefaultJson ?? "{}"); } catch { }

        // 5) 引用 PageSetting 配置：列表页加载 Filter/List；Detail 屏一并预加载
        var f = LoadSetting(GetInt(effective, "FilterPageSettingId"));
        var l = LoadSetting(GetInt(effective, "ListPageSettingId"));
        var d = LoadSetting(GetInt(effective, "DetailPageSettingId"));
        var model = new DynSharedModel
        {
            DynWebPageId = page.Id,
            DynTemplateId = template.Id,
            PageTitle = page.Name,
            TemplateCode = template.Code,
            RawParams = rawParams,
            EffectiveParams = effective,
            PassThrough = passThrough,
            TemplateConfig = templateConfig,
            FilterConfig = f?.Config,
            FilterDefaultJson = f?.DefaultJson,
            FilterRenderMode = f?.RenderMode,
            FilterPartialPath = f?.PartialPath,
            ListConfig = l?.Config,
            ListDefaultJson = l?.DefaultJson,
            ListRenderMode = l?.RenderMode,
            ListPartialPath = l?.PartialPath,
            DetailConfig = d?.Config,
            DetailDefaultJson = d?.DefaultJson,
            DetailRenderMode = d?.RenderMode,
            DetailPartialPath = d?.PartialPath
        };

        ViewData["Title"] = page.Name + " - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewData["ShowDesignEntry"] = _config.GetValue<bool>("Dyn:ShowRuntimeDesignEntry");
        var viewPath = string.IsNullOrWhiteSpace(template.ViewPath) ? "~/Views/DynTemplates/CrudBasic.cshtml" : template.ViewPath;
        // 积木化模板（triscreen-blocks 三屏 / filterlist-crud 筛选列表 / tree-detail 树形管理 /
        // tree-master-detail、list-master-detail 布局壳）：模板自行从 SpecJson/ParamsJson 读实例规格
        // + PageSettingService 读取相关 PageSetting 并动态拼装根 model（业务 key 由模板决定），直接透传 DynWebPage 实例。
        if (string.Equals(template.Code, "triscreen-blocks", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filterlist-crud", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tree-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tree-master-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "list-master-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-open-window", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-open-windowV2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "filter-list-drawer-leftV2", StringComparison.OrdinalIgnoreCase)
            || string.Equals(template.Code, "tabs-basic", StringComparison.OrdinalIgnoreCase))
        {
            return View(viewPath, page);
        }
        return View(viewPath, model);
    }

    /// <summary>Detail 弹窗片段：供宿主页面 fetchPartial 拉取并注入弹窗。
    /// 读取 DynWebPage（detail-modal 模板实例），透传 rowId/project 入参，返回无 Layout 片段。</summary>
    [HttpGet("/Platform/Page/DetailModal")]
    public IActionResult DetailModal(long id, long? rowId = null, string project = null, string prefill = null)
    {
        var page = _webPages.GetById((int)id);
        if (page == null) return Content("弹窗页面实例不存在：" + id);
        // project 优先取请求参数，缺省用页面实例自身的 ProjectId
        ViewBag.RowId = rowId?.ToString() ?? "";
        ViewBag.ProjectId = string.IsNullOrEmpty(project) ? (page.ProjectId?.ToString() ?? "") : project;
        // 主子表"+子"预填：query 传入的 JSON 字符串（如 {"ParentId":12}），原样透传给片段解析
        ViewBag.Prefill = prefill ?? "";
        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        var viewPath = string.IsNullOrWhiteSpace(template?.ViewPath)
            ? "~/Views/DynTemplates/DetailModal.cshtml"
            : template.ViewPath;
        return View(viewPath, page);
    }

    /// <summary>Detail 弹窗片段 V2（DynEventBus 版）：供 V2 模板 fetchPartial 拉取。</summary>
    [HttpGet("/Platform/Page/DetailModalV2")]
    public IActionResult DetailModalV2(long id, long? rowId = null, string project = null, string execId = null)
    {
        var page = _webPages.GetById((int)id);
        if (page == null) return Content("弹窗页面实例不存在：" + id);
        ViewBag.RowId = rowId?.ToString() ?? "";
        ViewBag.ProjectId = string.IsNullOrEmpty(project) ? (page.ProjectId?.ToString() ?? "") : project;
        ViewBag.ExecId = execId ?? "";
        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        var viewPath = string.IsNullOrWhiteSpace(template?.ViewPath)
            ? "~/Views/DynTemplates/DetailModalV2.cshtml"
            : template.ViewPath;
        return View(viewPath, page);
    }

    /// <summary>TableName 自动补齐 dyndata 免 model 接口 Url（手动填写优先）</summary>
    private static Dictionary<string, object> BuildEffectiveParams(Dictionary<string, object> raw)
    {
        var result = new Dictionary<string, object>(raw);
        if (raw.TryGetValue("TableName", out var t) && t != null && !string.IsNullOrWhiteSpace(t.ToString()))
        {
            var table = t.ToString();
            // 唯一数据端点：表名随 body 发送（{table,data} / {table,keys}），数据域由 project 坐标决定
            if (!HasValue(result, "ListUrl")) result["ListUrl"] = DynPageViewHelper.SearchUrl;
            if (!HasValue(result, "AddUrl")) result["AddUrl"] = DynPageViewHelper.SaveUrl;
            if (!HasValue(result, "EditUrl")) result["EditUrl"] = DynPageViewHelper.SaveUrl;
            if (!HasValue(result, "DeleteUrl")) result["DeleteUrl"] = DynPageViewHelper.DeleteUrl;
        }
        return result;
    }

    private static bool HasValue(Dictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out var v) && v != null && !string.IsNullOrWhiteSpace(v.ToString());
    }

    /// <summary>从参数字典安全读取 int?（值缺失、空或不可解析时返回 null）</summary>
    private static int? GetInt(Dictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i)) return i;
        return null;
    }

    private static Dictionary<string, object> TryParseDict(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
        try { return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(); }
        catch { return new Dictionary<string, object>(); }
    }

    /// <summary>
    /// ECharts Demo 数据源：返回一组示例图表数据，演示 DynEChart 组件接口传数据。
    /// </summary>
    [HttpGet("/Platform/Page/EChartData")]
    public IActionResult EChartData()
    {
        var data = new
        {
            xData = new[] { "华东", "华南", "华北", "西南", "华中", "东北" },
            seriesData = new[] { 1280, 1560, 980, 720, 1120, 640 }
        };
        return Json(new { code = 0, msg = "ok", data });
    }

    /// <summary>
    /// 详情/表单独立页面：被列表页 open 动作以 fragment 弹窗拉取，也可独立访问。
    /// 只加载 Detail 屏的 PageSetting（ConfigJson=表单UI / DefaultJson=表单model骨架），
    /// 实现 Filter/List/Detail 三屏各自独立、按需取用，列表页不再预加载 Detail。
    /// </summary>
    [HttpGet("/Platform/Page/DynDetail")]
    public IActionResult DynDetail(long settingId, long? bizId, string table)
    {
        var s = settingId > 0 ? _settings.GetById((int)settingId) : null;
        if (s == null) return Content("Detail 页面配置不存在：" + settingId);
        var m = new DetailTemplateModel
        {
            SettingId = (int)settingId,
            Table = table ?? s.TableName ?? string.Empty,
            BizId = bizId ?? 0,
            PageTitle = s.Name,
            DetailDefaultJson = s.DefaultJson,
            RenderMode = s.RenderMode,
            PartialPath = s.PartialPath,
            WinTitle = Request.Query["winTitle"].FirstOrDefault(),
            WinWidth = Request.Query["winWidth"].FirstOrDefault(),
            WinHeight = Request.Query["winHeight"].FirstOrDefault(),
            WinMax = string.Equals(Request.Query["winMax"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase),
            Project = string.IsNullOrWhiteSpace(Request.Query["project"])
                ? DynPageViewHelper.PlatformProject
                : Request.Query["project"].FirstOrDefault(),
            PrefillJson = Request.Query["prefill"].FirstOrDefault()
        };
        if (!string.IsNullOrWhiteSpace(s.ConfigJson))
        {
            try { m.DetailConfig = JObject.Parse(s.ConfigJson); } catch { }
        }
        ViewData["Title"] = s.Name + " - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View("~/Views/DynTemplates/DetailTemplate.cshtml", m);
    }

    /// <summary>读取 PageSetting 引用包：Config(UI渲染树) + DefaultJson(model骨架) + RenderMode + PartialPath</summary>
    private SettingBundle? LoadSetting(int? settingId)
    {
        if (settingId == null || settingId <= 0) return null;
        var s = _settings.GetById(settingId.Value);
        if (s == null) return null;
        var b = new SettingBundle
        {
            DefaultJson = s.DefaultJson,
            RenderMode = s.RenderMode,
            PartialPath = s.PartialPath
        };
        if (!string.IsNullOrWhiteSpace(s.ConfigJson))
        {
            try { b.Config = JObject.Parse(s.ConfigJson); } catch { }
        }
        return b;
    }

    /// <summary>PageSetting 引用包：Config(UI渲染树) + DefaultJson(model骨架) + RenderMode + PartialPath</summary>
    private class SettingBundle
    {
        public JObject? Config { get; set; }
        public string? DefaultJson { get; set; }
        public string? RenderMode { get; set; }
        public string? PartialPath { get; set; }
    }
}
