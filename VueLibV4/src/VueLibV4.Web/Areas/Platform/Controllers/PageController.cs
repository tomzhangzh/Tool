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
/// </summary>
[Area("Platform")]
public class PageController : Controller
{
    private const string Page = "~/Views/Platform/Page/{0}.cshtml";
    private const string Demo = "~/Views/Platform/Demo/{0}.cshtml";

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

    /// <summary>组件长廊质检：逐个真实挂载全部启用组件，校验 define/渲染/运行时错误，红绿黄灯报告</summary>
    [HttpGet("/Platform/Page/ComponentCheck")]
    public IActionResult ComponentCheck()
    {
        ViewData["Title"] = "组件长廊质检 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "ComponentCheck"));
    }

    /// <summary>权限管理：角色 + 资源树授权 + 用户角色分配</summary>
    [HttpGet("/Platform/Page/PermissionAdmin")]
    public IActionResult PermissionAdmin()
    {
        ViewData["Title"] = "权限管理 - VueLibV4";
        return View(string.Format(Page, "PermissionAdmin"));
    }

    /// <summary>登录页（全局过滤器的唯一匿名页面入口）</summary>
    [HttpGet("/Platform/Page/Login")]
    [AllowAnonymousPermission]
    public IActionResult Login()
    {
        return View("~/Views/Platform/Page/Login.cshtml");
    }

    /// <summary>修改密码页</summary>
    [HttpGet("/Platform/Page/ChangePassword")]
    public IActionResult ChangePassword()
    {
        return View("~/Views/Platform/Page/ChangePassword.cshtml");
    }

    /// <summary>TabsBlock 演示：主表单 + 子表标签页联动</summary>
    [HttpGet("/Platform/Page/TabsBlockDemo")]
    public IActionResult TabsBlockDemo()
    {
        ViewData["Title"] = "TabsBlock 演示 - VueLibV4";
        ViewData["DemoEdit"] = Request.Query["demo"] == "edit";

        // 学生详情表单配置
        var nameInput = new { component = "DynElInput", modelname = "Name",
            options = new { labeloptions = new { label = "学生姓名" }, comoptions = new {} } };
        var noInput = new { component = "DynElInput", modelname = "StudentNo",
            options = new { labeloptions = new { label = "学号" }, comoptions = new {} } };
        var gradeInput = new { component = "DynElInput", modelname = "Grade",
            options = new { labeloptions = new { label = "年级" }, comoptions = new {} } };

        var detailCfg = new {
            component = "DynGridContainer",
            options = new { comoptions = new {}, itemoptions = new { style = new { gap = "8px" }, @class = "" } },
            childrenctrls = new[] { nameInput, noInput, gradeInput }
        };

        var detailBlkConfig = Newtonsoft.Json.JsonConvert.SerializeObject(new {
            table = "Student", keyField = "Id",
            detailConfig = detailCfg,
            detailDefault = new { Name = "", StudentNo = "", Grade = "" }
        });

        var tabsBlkConfig = Newtonsoft.Json.JsonConvert.SerializeObject(new {
            detailBlkConfig = detailBlkConfig,
            idField = "Id",
            subTabs = new[] {
                new { key = "course", label = "相关课程", fkField = "StudentId" },
                new { key = "score",  label = "成绩",     fkField = "StudentId" }
            }
        });

        ViewData["TabsBlkConfig"] = tabsBlkConfig;
        ViewData["ExtId"] = "tabsDemo-" + Guid.NewGuid().ToString("N");
        return View(string.Format(Page, "TabsBlockDemo"));
    }

    [HttpGet("/Platform/Page/KanbanDemo")]
    public IActionResult KanbanDemo()
    {
        ViewData["Title"] = "Kanban 看板 - VueLibV4";
        return View(string.Format(Page, "KanbanDemo"));
    }

    [HttpGet("/Platform/Page/PageGraphDemo")]
    public IActionResult PageGraphDemo(int id = 34)
    {
        ViewData["Title"] = "页面组成图 Demo - VueLibV4";
        var wp = _webPages.GetById(id);
        if (wp == null) return Content($"WebPage {id} 不存在");

        var tpl = wp.TemplateId > 0 ? _templates.GetById(wp.TemplateId) : null;
        var param = string.IsNullOrEmpty(wp.ParamsJson) ? new JObject() : JObject.Parse(wp.ParamsJson);
        var blocksNode = param["blocks"] as JObject ?? new JObject();

        // 取 Template 槽位定义
        var slots = tpl != null
            ? _tplBlocks.Query(x => x.TemplateId == tpl.Id).OrderBy(x => x.SortNo).ToList()
            : new List<VueLibV4.Platform.Models.DynTemplateBlock>();

        // 拼 Mermaid
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("flowchart LR");
        // WebPage 节点
        sb.AppendLine($"    WP[\"📄 {wp.Name}<br/><small>id={wp.Id}</small>\"]:::wp");
        // Template
        if (tpl != null)
        {
            sb.AppendLine($"    T[\"📐 {tpl.Name}<br/><small>{tpl.Code}</small>\"]:::tpl");
            sb.AppendLine("    WP --- T");
        }
        // 每个槽位一个 Block 节点 + 一个 PageSetting 节点
        int idx = 0;
        foreach (var s in slots)
        {
            var slot = s.Slot;
            var blk = _blocks.GetById(s.BlockId);
            var blkName = blk != null ? blk.Name : $"Block#{s.BlockId}";
            var role = blk != null ? (blk.ImplementsRole ?? "") : "";

            var bId = $"B{idx}";
            var psId = $"PS{idx}";
            var icon = role == "filter" ? "🔍" : role == "list" ? "📋" : role == "detail" ? "📝" : "🧩";
            sb.AppendLine($"    {bId}[\"{icon} {blkName}<br/><small>slot={slot}</small>\"]:::blk");

            // PageSetting
            int settingId = 0;
            if (blocksNode[slot] != null) int.TryParse(blocksNode[slot]["settingId"]?.ToString(), out settingId);
            if (settingId > 0)
            {
                var ps = _settings.GetById(settingId);
                if (ps != null)
                {
                    sb.AppendLine($"    {psId}[\"📄 {ps.Name}<br/><small>{ps.Code}</small>\"]:::ps");
                    sb.AppendLine($"    {bId} -.->|配置| {psId}");
                }
            }
            idx++;
        }
        // 内置消息流（filter->list）
        if (slots.Count >= 2)
        {
            sb.AppendLine("    B0 ==>|筛选条件| B1");
        }
        sb.AppendLine("    classDef wp fill:#eaf2ff,stroke:#409eff,stroke-width:2px,color:#1f3a68");
        sb.AppendLine("    classDef tpl fill:#fdf6ec,stroke:#e6a23c,stroke-width:2px,color:#7a5b17");
        sb.AppendLine("    classDef blk fill:#ecf5ff,stroke:#409eff,stroke-width:1.5px,color:#1f3a68");
        sb.AppendLine("    classDef ps fill:#f4f0fa,stroke:#9b59b6,stroke-width:1.5px,color:#5b2c6f");

        ViewData["Mermaid"] = sb.ToString();
        ViewData["WpName"] = wp.Name;
        return View(string.Format(Page, "PageGraphDemo"));
    }

    [HttpGet("/Platform/Page/DbErGraph")]
    public IActionResult DbErGraph()
    {
        ViewData["Title"] = "数据库 ER 关系图 - VueLibV4";
        return View(string.Format(Page, "DbErGraph"));
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

    [HttpGet("/Platform/Page/Demo/Gallery")]
    public IActionResult DemoGallery()
    {
        ViewData["Title"] = "组件画廊 - VueLibV4";
        return View(string.Format(Demo, "Gallery"));
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

    /// <summary>能力桥 RPC Demo：dyn.service 调用后端 DI 服务 / dyn.eval 执行 C# 取数（含调用规范）</summary>
    [HttpGet("/Platform/Page/Demo/Rpc")]
    public IActionResult DemoRpc()
    {
        ViewData["Title"] = "能力桥 RPC Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Rpc"));
    }

    /// <summary>统一参数上下文 Demo：多来源合并/就近优先、片段继承、Tabs 共享层 commit（快照型 vs 跟随型）</summary>
    [HttpGet("/Platform/Page/Demo/TabsParams")]
    public IActionResult DemoTabsParams()
    {
        ViewData["Title"] = "统一参数上下文 Tabs 传参 Demo - VueLibV4";
        return View(string.Format(Demo, "TabsParams"));
    }

    /// <summary>Mac 风格桌面 Demo（仿 macOS / portfolio.zxh.me 拟物风格，独立 _LayoutMac 布局）</summary>
    [HttpGet("/Platform/Page/MacDesktopDemo")]
    public IActionResult DemoMac()
    {
        ViewData["Title"] = "Mac 桌面 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View("~/Views/DynTemplates/MacDesktopDemo.cshtml");
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
        //    （crud-basic 列表页不直接使用；tree-basic 树形模板右侧内嵌表单按需使用 Detail 屏配置）。
        //    新增/编辑弹窗场景仍走独立 Detail 页面（/Platform/Page/DynDetail），detailSettingId 经 EffectiveParams 传前端。
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
        // 运行时右上角「进入设计器」入口：由配置 Dyn:ShowRuntimeDesignEntry 控制显隐（无认证时以此充当"管理员可见"开关）
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
            // 弹窗窗口参数（open 动作经 query 传入，setwin 应用）
            WinTitle = Request.Query["winTitle"].FirstOrDefault(),
            WinWidth = Request.Query["winWidth"].FirstOrDefault(),
            WinHeight = Request.Query["winHeight"].FirstOrDefault(),
            WinMax = string.Equals(Request.Query["winMax"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase),
            // 数据域坐标 project（统一端点 /api/dyndata；缺省=平台库 __platform__）+ 新增预填（主从联动）
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

    private class SettingBundle
    {
        public JObject? Config { get; set; }
        public string? DefaultJson { get; set; }
        public string? RenderMode { get; set; }
        public string? PartialPath { get; set; }
    }
}
