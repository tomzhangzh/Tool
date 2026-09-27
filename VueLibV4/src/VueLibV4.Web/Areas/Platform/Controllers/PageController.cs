using System.IO;
using Microsoft.AspNetCore.Mvc;
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
/// </summary>
[Area("Platform")]
public class PageController : Controller
{
    private const string Page = "~/Views/Platform/Page/{0}.cshtml";
    private const string Demo = "~/Views/Platform/Demo/{0}.cshtml";

    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;

    public PageController(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
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

    /// <summary>页面生成向导（M4）：选项目/选表/选字段 → 生成三屏页面</summary>
    [HttpGet("/Platform/Page/PageGen")]
    public IActionResult PageGen()
    {
        ViewData["Title"] = "页面生成向导 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "PageGen"));
    }

    /// <summary>DSL 编辑器（类Markdown：DSL / JSON / 页面预览 三栏）</summary>
    [HttpGet("/Platform/Page/Dsl")]
    public IActionResult Dsl()
    {
        ViewData["Title"] = "DSL 编辑器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "Dsl"));
    }

    /// <summary>组件元配置编辑器（设计器右侧「配置元」入口；fragment 弹窗按 componentId 指定组件）</summary>
    [HttpGet("/Platform/Page/MetaDsl")]
    public IActionResult MetaDsl(string componentId)
    {
        ViewData["Title"] = "组件元配置编辑器 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        ViewBag.ComponentId = componentId;
        return View(string.Format(Page, "MetaDsl"));
    }

    [HttpGet("/Platform/Page/Demo/ActionHelper")]
    public IActionResult DemoActionHelper()
    {
        ViewData["Title"] = "动作助手 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "ActionHelper"));
    }

    [HttpGet("/Platform/Page/Demo/Combination")]
    public IActionResult DemoCombination()
    {
        ViewData["Title"] = "组合组件 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Combination"));
    }

    [HttpGet("/Platform/Page/Demo/Grid")]
    public IActionResult DemoGrid()
    {
        ViewData["Title"] = "栅格布局 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Grid"));
    }

    [HttpGet("/Platform/Page/Demo/Crud")]
    public IActionResult DemoCrud()
    {
        ViewData["Title"] = "免模型 CRUD Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Crud"));
    }

    [HttpGet("/Platform/Page/Demo/TemplateImport")]
    public IActionResult DemoTemplateImport()
    {
        ViewData["Title"] = "模板导入 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "TemplateImport"));
    }

    [HttpGet("/Platform/Page/Demo/CodeMirror")]
    public IActionResult DemoCodeMirror()
    {
        ViewData["Title"] = "CodeMirror Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "CodeMirror"));
    }

    /// <summary>Markdown 文档查看器（docs 目录内文档；安全：规范化路径并限制在 docs 内）</summary>
    [HttpGet("/Platform/Page/MdViewer")]
    public IActionResult MdViewer(string path)
    {
        ViewData["Title"] = "文档 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        // 解决方案根 docs 目录（AppContext.BaseDirectory = src/VueLibV4.Web/bin/Debug/net8.0/）
        var docsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs"));
        if (!Directory.Exists(docsRoot)) return Content("文档目录不存在：" + docsRoot);
        var safe = Path.GetFullPath(Path.Combine(docsRoot, path ?? ""));
        if (!safe.StartsWith(docsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !safe.Equals(docsRoot, StringComparison.OrdinalIgnoreCase))
            return Content("无效文档路径");
        if (!System.IO.File.Exists(safe)) return Content("文档不存在：" + path);
        ViewBag.MdPath = path;
        ViewBag.MdContent = System.IO.File.ReadAllText(safe, System.Text.Encoding.UTF8);
        ViewBag.MdName = Path.GetFileNameWithoutExtension(safe);
        return View(string.Format(Page, "MdViewer"));
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

        // 1) 原始参数：用户在 DynCom 参数面板填写的结果（存于实例 ConfigJson）
        var rawParams = TryParseDict(page.ConfigJson);

        // 2) 自动推导：TableName 有值且 Url 为空 → 补 dyndata 免 model 接口；手动填写优先
        var effective = BuildEffectiveParams(rawParams);
        // 2.1) DynWebPage 三屏 PageSettingId 一并注入 effective（模板 openDetail 弹窗需要 DetailPageSettingId）
        if (!HasValue(effective, "FilterPageSettingId") && page.FilterPageSettingId != null) effective["FilterPageSettingId"] = page.FilterPageSettingId.Value;
        if (!HasValue(effective, "ListPageSettingId") && page.ListPageSettingId != null) effective["ListPageSettingId"] = page.ListPageSettingId.Value;
        if (!HasValue(effective, "DetailPageSettingId") && page.DetailPageSettingId != null) effective["DetailPageSettingId"] = page.DetailPageSettingId.Value;

        // 3) 模板 ConfigJson 顶层 mPassThrough（透传属性，向下传给外壳 / createapp）
        var passThrough = new JObject();
        try
        {
            var cfg = JObject.Parse(template.ConfigJson ?? "{}");
            if (cfg["mPassThrough"] is JObject pt) passThrough = pt;
        }
        catch { }

        // 4) 模板页面配置树（外壳可选用）
        var templateConfig = new JObject();
        try { templateConfig = JObject.Parse(template.TemplateJson ?? "{}"); } catch { }

        // 5) 引用 PageSetting 配置：列表页加载 Filter/List；Detail 屏一并预加载
        //    （crud-basic 列表页不直接使用；tree-basic 树形模板右侧内嵌表单按需使用 Detail 屏配置）。
        //    新增/编辑弹窗场景仍走独立 Detail 页面（/Platform/Page/DynDetail），detailSettingId 经 EffectiveParams 传前端。
        var f = LoadSetting(page.FilterPageSettingId);
        var l = LoadSetting(page.ListPageSettingId);
        var d = LoadSetting(page.DetailPageSettingId);
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
        return View(string.IsNullOrWhiteSpace(template.ViewPath) ? "~/Views/DynTemplates/CrudBasic.cshtml" : template.ViewPath, model);
    }

    /// <summary>TableName 自动补齐 dyndata 免 model 接口 Url（手动填写优先）</summary>
    private static Dictionary<string, object> BuildEffectiveParams(Dictionary<string, object> raw)
    {
        var result = new Dictionary<string, object>(raw);
        if (raw.TryGetValue("TableName", out var t) && t != null && !string.IsNullOrWhiteSpace(t.ToString()))
        {
            var table = t.ToString();
            if (!HasValue(result, "ListUrl")) result["ListUrl"] = "/api/platform/dyndata/search";
            if (!HasValue(result, "AddUrl")) result["AddUrl"] = "/api/platform/dyndata/save?table=" + table;
            if (!HasValue(result, "EditUrl")) result["EditUrl"] = "/api/platform/dyndata/save?table=" + table;
            if (!HasValue(result, "DeleteUrl")) result["DeleteUrl"] = "/api/platform/dyndata/delete?table=" + table;
        }
        return result;
    }

    private static bool HasValue(Dictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out var v) && v != null && !string.IsNullOrWhiteSpace(v.ToString());
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
            // 弹窗窗口参数（open 动作经 query 传入，setwin 应用）
            WinTitle = Request.Query["winTitle"].FirstOrDefault(),
            WinWidth = Request.Query["winWidth"].FirstOrDefault(),
            WinHeight = Request.Query["winHeight"].FirstOrDefault(),
            WinMax = string.Equals(Request.Query["winMax"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase),
            // 数据访问库 + 新增预填（主从联动：子表新增预填外键）
            Db = Request.Query["db"].FirstOrDefault(),
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

    private class SettingBundle
    {
        public JObject? Config { get; set; }
        public string? DefaultJson { get; set; }
        public string? RenderMode { get; set; }
        public string? PartialPath { get; set; }
    }
}
