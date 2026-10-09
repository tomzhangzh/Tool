using System.Dynamic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
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
            || string.Equals(template.Code, "detail-modal-v2", StringComparison.OrdinalIgnoreCase)
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

    /// <summary>
    /// 按 BlockName 独立渲染一个 Block 片段（V2 架构）。
    /// GET: /Platform/Page/LoadBlock?blockName=filter&webpageId=22&execId=T2-xxx
    /// POST: body { blockName, execId, rowId, config: { provide:{...}, blocks:{ filter:{...}, ... } } }
    /// ViewPath 从 DynBlock.Code 查出，不硬编码。
    /// provide 与 blocks[slot] 动态合并后注入 ViewData；URL 为空自动补 dyndata 默认端点。
    /// </summary>
    [HttpGet("/Platform/Page/LoadBlock")]
    [HttpPost("/Platform/Page/LoadBlock")]
    public IActionResult LoadBlock([FromQuery] string blockName, [FromQuery] long? webpageId = null,
        [FromQuery] string execId = null, [FromQuery] long? rowId = null,
        [FromBody] LoadBlockRequest? body = null)
    {
        // 1. 解析配置 JSON：POST body 优先，否则从 webpageId 加载
        JObject? cfgJson = null;
        DynWebPage? page = null;
        if (body != null && body.Config != null)
        {
            cfgJson = body.Config;
            blockName = blockName ?? body.BlockName;
            execId = execId ?? body.ExecId;
            rowId = rowId ?? body.RowId;
            // POST 模式也可能带 webpageId，从页面补基础配置
            if (webpageId != null)
            {
                page = _webPages.GetById(webpageId.Value);
            }
        }
        else if (webpageId != null)
        {
            page = _webPages.GetById(webpageId.Value);
            if (page == null) return Content("页面不存在：" + webpageId);
            if (!string.IsNullOrWhiteSpace(page.ParamsJson))
            {
                try { cfgJson = JObject.Parse(page.ParamsJson); } catch { cfgJson = new JObject(); }
            }
        }
        cfgJson ??= new JObject();

        // 2. 查 DynBlock 拿 ViewPath
        if (string.IsNullOrWhiteSpace(blockName)) return Content("缺少 blockName");
        var block = _blocks.Query(x => x.Code == blockName && x.IsActive).First();
        if (block == null) return Content("Block 未注册：" + blockName);
        if (string.IsNullOrWhiteSpace(block.ViewPath)) return Content("Block 未配置 ViewPath：" + blockName);

        // 3. 动态合并：provide（顶层参数）→ blocks[blockName]（槽位特化）
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var provide = cfgJson["provide"] as JObject;
        if (provide != null)
            foreach (var prop in provide.Properties())
                merged[prop.Name] = prop.Value?.Type switch
                {
                    JTokenType.Integer => (object)prop.Value.Value<long>(),
                    JTokenType.Float => prop.Value.Value<double>(),
                    JTokenType.Boolean => prop.Value.Value<bool>(),
                    JTokenType.Null => null,
                    JTokenType.Undefined => null,
                    _ => prop.Value?.ToString()
                };

        // 兼容旧结构：provide 平铺在顶层（TableName/KeyField/Project 等）
        foreach (var prop in cfgJson.Properties())
        {
            if (prop.Name is "provide" or "blocks") continue;
            if (!merged.ContainsKey(prop.Name))
            {
                merged[prop.Name] = prop.Value?.Type switch
                {
                    JTokenType.Integer => (object)prop.Value.Value<long>(),
                    JTokenType.Float => prop.Value.Value<double>(),
                    JTokenType.Boolean => prop.Value.Value<bool>(),
                    JTokenType.Null => null,
                    _ => prop.Value?.ToString()
                };
            }
        }

        // blocks[slot] 覆盖
        var blocksObj = cfgJson["blocks"] as JObject;
        var slot = blocksObj?[blockName] as JObject;
        if (slot != null)
            foreach (var prop in slot.Properties())
            {
                var v = prop.Value;
                merged[prop.Name] = v?.Type switch
                {
                    JTokenType.Integer => (object)v.Value<long>(),
                    JTokenType.Float => v.Value<double>(),
                    JTokenType.Boolean => v.Value<bool>(),
                    JTokenType.Object or JTokenType.Array => v.ToString(Newtonsoft.Json.Formatting.None),
                    JTokenType.Null => null,
                    _ => v?.ToString()
                };
            }

        // 4. 从 DynWebPage.ProjectId 兜底 Project（provide 里没传就用页面本身的 ProjectId）
        if (page != null && page.ProjectId.HasValue && page.ProjectId.Value > 0
            && string.IsNullOrEmpty(merged.GetValueOrDefault("Project")?.ToString()))
        {
            merged["Project"] = page.ProjectId.Value.ToString();
        }

        // 5. 补 URL 默认值
        var tableName = merged.GetValueOrDefault("TableName")?.ToString() ?? "";
        var proj = merged.GetValueOrDefault("Project")?.ToString() ?? "";
        if (string.IsNullOrEmpty(merged.GetValueOrDefault("LoadUrl")?.ToString())) merged["LoadUrl"] = DynPageViewHelper.SearchUrl;
        if (string.IsNullOrEmpty(merged.GetValueOrDefault("DeleteUrl")?.ToString())) merged["DeleteUrl"] = DynPageViewHelper.DeleteUrl;
        if (string.IsNullOrEmpty(merged.GetValueOrDefault("AddUrl")?.ToString())) merged["AddUrl"] = DynPageViewHelper.SaveUrl;
        if (string.IsNullOrEmpty(merged.GetValueOrDefault("EditUrl")?.ToString())) merged["EditUrl"] = DynPageViewHelper.SaveUrl;

        // settingId 映射：blocks[slot].settingId → ViewData 期望的键名
        if (merged.TryGetValue("settingId", out var sid) && sid != null)
        {
            var settingKey = blockName.ToLower() switch
            {
                "filter" => "FilterPageSettingId",
                "list" => "ListSettingId",
                "detail" => "DetailSettingId",
                _ => blockName + "SettingId"
            };
            merged[settingKey] = sid;
        }

        // 5. 合并成 dynamic model 直接传给视图（不再散写 ViewData）
        var extId = blockName + "-" + Guid.NewGuid().ToString("N")[..12];
        dynamic model = new ExpandoObject();
        var dict = (IDictionary<string, object?>)model;
        dict["ExtId"] = extId;
        dict["ExecId"] = execId ?? "";
        if (rowId != null) dict["RowId"] = rowId.ToString();
        foreach (var kv in merged) dict[kv.Key] = kv.Value;

        return PartialView(block.ViewPath, model);
    }

    public class LoadBlockRequest
    {
        public string? BlockName { get; set; }
        public string? ExecId { get; set; }
        public long? RowId { get; set; }
        public JObject? Config { get; set; }
    }

    /// <summary>
    /// Inspector 数据：返回 WebPage 的 provide + 各 slot 的当前值 + 对应 Block 的 ParamConfigJson。
    /// 前端 Inspector 面板据此动态渲染参数表单。
    /// </summary>
    [HttpGet("/Platform/Page/InspectorData")]
    public IActionResult InspectorData(long webpageId)
    {
        var page = _webPages.GetById((int)webpageId);
        if (page == null) return Json(new { code = 1, msg = "页面不存在" });

        JObject ps;
        try { ps = JObject.Parse(page.ParamsJson ?? "{}"); }
        catch { ps = new JObject(); }

        // provide：优先 ps.provide，否则把顶层非 blocks 字段当 provide（兼容旧结构）
        var provide = ps["provide"] as JObject;
        if (provide == null || !provide.HasValues)
        {
            provide = new JObject();
            foreach (var prop in ps.Properties())
            {
                if (prop.Name is "provide" or "blocks") continue;
                provide[prop.Name] = prop.Value.DeepClone();
            }
        }

        // blocks[slot] 当前值
        var blocksObj = ps["blocks"] as JObject ?? new JObject();

        // 查模板-槽位关系
        var slots = new List<object>();
        var knownSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (page.TemplateId != null)
        {
            var tplBlocks = _tplBlocks.Query(x => x.TemplateId == page.TemplateId.Value).OrderBy(x => x.SortNo).ToList();
            foreach (var tb in tplBlocks)
            {
                knownSlots.Add(tb.Slot);
                var blk = _blocks.GetById(tb.BlockId);
                if (blk == null) continue;
                JObject? pcfg = null;
                try { if (!string.IsNullOrWhiteSpace(blk.ParamConfigJson)) pcfg = JObject.Parse(blk.ParamConfigJson); } catch { }
                var current = blocksObj[tb.Slot] as JObject ?? new JObject();
                slots.Add(new
                {
                    slot = tb.Slot,
                    blockCode = blk.Code,
                    blockName = blk.Name,
                    description = blk.Description,
                    paramConfig = pcfg,
                    paramDefault = blk.ParamDefaultJson,
                    current = current
                });
            }
        }
        // 兜底：blocksObj 里出现但 DynTemplateBlock 没记录的 slot，按 blockName 反查 Block
        foreach (var prop in blocksObj.Properties())
        {
            if (knownSlots.Contains(prop.Name)) continue;
            var blk = _blocks.Query(x => x.Code == prop.Name && x.IsActive).ToList().FirstOrDefault();
            if (blk == null) continue;
            JObject? pcfg = null;
            try { if (!string.IsNullOrWhiteSpace(blk.ParamConfigJson)) pcfg = JObject.Parse(blk.ParamConfigJson); } catch { }
            var current = prop.Value as JObject ?? new JObject();
            slots.Add(new
            {
                slot = prop.Name,
                blockCode = blk.Code,
                blockName = blk.Name,
                description = blk.Description,
                paramConfig = pcfg,
                paramDefault = blk.ParamDefaultJson,
                current = current
            });
        }

        return Json(new
        {
            code = 0,
            data = new
            {
                webpageId = page.Id,
                pageName = page.Name,
                templateId = page.TemplateId,
                provide = provide,
                blocks = blocksObj,
                slots = slots
            }
        });
    }

    /// <summary>保存 WebPage.ParamsJson（Inspector 编辑后调用）</summary>
    [HttpPost("/Platform/Page/SaveParams")]
    public IActionResult SaveParams([FromBody] SaveParamsRequest req)
    {
        if (req?.WebpageId == null) return Json(new { code = 1, msg = "缺少 webpageId" });
        var page = _webPages.GetById((int)req.WebpageId.Value);
        if (page == null) return Json(new { code = 1, msg = "页面不存在" });
        page.ParamsJson = req.ParamsJson?.ToString(Newtonsoft.Json.Formatting.None) ?? "{}";
        _webPages.Update(page);
        return Json(new { code = 0, msg = "ok" });
    }

    public class SaveParamsRequest
    {
        public long? WebpageId { get; set; }
        public JObject? ParamsJson { get; set; }
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
