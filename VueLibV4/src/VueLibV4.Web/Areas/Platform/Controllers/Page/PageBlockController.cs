using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面积木控制器：Block 片段渲染、参数配置面板、Inspector 数据与参数保存。
/// 访问示例：
///   /Platform/Block/Load?blockName=filter&amp;webpageId=22
///   /Platform/Block/ParamsPanel?templateId=21&amp;pageId=6
///   /Platform/Block/Inspector?webpageId=22
///   POST /Platform/Block/SaveParams
/// </summary>
[Area("Platform")]
public class PageBlockController : Controller
{
    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateBlockService _tplBlocks;
    private readonly IDynBlockService _blocks;

    public PageBlockController(
        IDynWebPageService webPages,
        IDynTemplateBlockService tplBlocks,
        IDynBlockService blocks)
    {
        _webPages = webPages;
        _tplBlocks = tplBlocks;
        _blocks = blocks;
    }

    /// <summary>
    /// 按 BlockName 独立渲染一个 Block 片段（V2 架构）。
    /// GET: /Platform/Block/Load?blockName=filter&amp;webpageId=22&amp;execId=T2-xxx
    /// POST: body { blockName, execId, rowId, config: { provide:{...}, blocks:{ filter:{...}, ... } } }
    /// ViewPath 从 DynBlock.Code 查出，不硬编码。
    /// provide 与 blocks[slot] 动态合并后注入 ViewData；URL 为空自动补 dyndata 默认端点。
    /// </summary>
    [HttpGet("/Platform/Block/Load")]
    [HttpPost("/Platform/Block/Load")]
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

        // 2. 查 DynBlock 拿 ViewPath（SqlSugar First() 无记录返回 null，不会抛异常）
        if (string.IsNullOrWhiteSpace(blockName)) return Content("缺少 blockName");
        var block = _blocks.Query(x => x.Code == blockName && x.IsActive).First();
        if (block == null) return Content("Block 未注册或已停用：" + blockName);
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

        // blocks[slot] 覆盖；兼容顶层槽位形状 params[blockName]（如 {"filter":{"settingId":1},"list":{...}}）
        var blocksObj = cfgJson["blocks"] as JObject;
        var slot = blocksObj?[blockName] as JObject ?? cfgJson[blockName] as JObject;
        if (slot != null)
        {
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
            // 展开 slot.model 子对象：block 专属参数（Relations/Mode/IsMaster 等）平铺到 merged，
            // 不覆盖 slot 顶层已有的 settingId 等键。
            var slotModel = slot["model"] as JObject;
            if (slotModel != null)
                foreach (var prop in slotModel.Properties())
                {
                    if (merged.ContainsKey(prop.Name)) continue;
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
                "listview" => "ListSettingId",
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
    [HttpGet("/Platform/Block/Inspector")]
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
    [HttpPost("/Platform/Block/SaveParams")]
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

    /// <summary>
    /// WebPage 参数配置面板：自包含独立 Vue App。
    /// 浏览器直访 → 套布局的完整页（standalone 模式，面板内"保存参数"按钮直接写 ParamsJson）；
    /// 需要裸片段注入任意容器时加 raw=1（ActionHelper.Load/updateEl 场景）。
    /// GET /Platform/Block/ParamsPanel?templateId=21&amp;pageId=6
    /// </summary>
    [HttpGet("/Platform/Block/ParamsPanel")]
    public IActionResult ParamsPanel(int templateId = 0, long pageId = 0, bool raw = false)
    {
        ViewData["TemplateId"] = templateId;
        ViewData["PageId"] = pageId;
        if (raw) return PartialView("~/Views/DynPages/Ext/_WebPageParamsPanel.cshtml");
        return View("~/Views/DynPages/Ext/ParamsPanelStandalone.cshtml");
    }
}
