using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页运行时渲染与参数 schema：解析 PageJson / ParamsJson，输出前端可直接消费的配置树。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynpage")]
[ApiController]
public class DynPageRenderController : ControllerBase
{
    private readonly IDynWebPageService _svc;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;
    private readonly IDynTemplateBlockService _templateBlocks;
    private readonly IDynBlockService _blocks;

    public DynPageRenderController(
        IDynWebPageService svc,
        IDynTemplateService templates,
        IPageSettingService settings,
        IDynTemplateBlockService templateBlocks,
        IDynBlockService blocks)
    {
        _svc = svc;
        _templates = templates;
        _settings = settings;
        _templateBlocks = templateBlocks;
        _blocks = blocks;
    }

    /// <summary>
    /// 运行时渲染数据：解析 PageJson；
    /// 若页面未实例化（PageJson 为空）则回退到模板 DefaultJson —— 选择模板+配置即完成页面。
    /// </summary>
    [HttpGet("render")]
    public ApiResult Render(string id, string code = null)
    {
        var row = string.IsNullOrEmpty(id)
            ? _svc.Query(p => p.Code == code).First()
            : FirstByIdOrCode(id);
        if (row == null) return ApiResult.Fail("页面不存在");
        var result = new JObject { ["id"] = row.Id, ["name"] = row.Name, ["code"] = row.Code };

        JObject config = null;
        if (!string.IsNullOrWhiteSpace(row.PageJson))
        {
            try { config = JObject.Parse(row.PageJson); } catch { config = null; }
        }

        // 模板只取一次：既用于回填默认 config，也用于返回 templateCode（之前 GetById 调了两次）
        var tpl = row.TemplateId != null ? _templates.GetById(row.TemplateId.Value) : null;
        if (config == null && tpl != null && row.TemplateId > 0)
        {
            try { config = JObject.Parse(tpl.DefaultJson ?? "{}"); } catch { config = null; }
        }

        result["config"] = config ?? new JObject();
        var ps = DynPageViewHelper.ParseParams(row.ParamsJson);
        // paramsjson 为新名；configjson 保留同内容兼容旧消费方
        result["paramsjson"] = ps;
        result["configjson"] = ps;

        // M4 三屏固定模板：返回模板 Code、运行 URL 与筛选/列表/详情三份配置树
        result["templateCode"] = tpl?.Code;
        result["url"] = row.Url;
        result["resourceKey"] = row.ResourceKey; // 权限资源 Key，前端渲染时注入 data-webpage-resource
        // 槽位 PageSettingId：blocks[slot].settingId 优先，回退旧扁平键
        result["filterConfig"] = LoadSettingConfig(DynPageViewHelper.SlotSettingId(ps, "filter", "FilterPageSettingId"));
        result["listConfig"] = LoadSettingConfig(DynPageViewHelper.SlotSettingId(ps, "list", "ListPageSettingId"));
        result["detailConfig"] = LoadSettingConfig(DynPageViewHelper.SlotSettingId(ps, "detail", "DetailPageSettingId"));
        return ApiResult.Ok(result);
    }

    /// <summary>槽位中文名（参数页折叠标题；装配契约的一部分）</summary>
    private static readonly Dictionary<string, string> SlotNames = new()
    {
        ["filter"] = "筛选区",
        ["list"] = "列表区",
        ["detail"] = "明细表单",
        ["tree"] = "左树"
    };

    /// <summary>
    /// WebPage 参数页 schema：模板自身参数 UI 包（ConfigJson/DefaultJson）+ 各槽位 Block 的
    /// 参数 UI 包与已保存 values。values 已在后端完成「Block 默认值打底 + 新结构 blocks 读取 +
    /// 旧扁平 ConfigJson 迁移」，前端直接渲染/回显。
    /// 入参：templateId（必传）；pageId（编辑态，用于取已保存 ParamsJson）。
    /// </summary>
    [HttpGet("paramschema")]
    public ApiResult ParamSchema(int templateId, int? pageId = null)
    {
        var tpl = templateId > 0 ? _templates.GetById(templateId) : null;
        if (tpl == null) return ApiResult.Fail("模板不存在");

        DynWebPage? page = null;
        if (pageId is > 0) page = _svc.GetById(pageId.Value);
        var pageParams = DynPageViewHelper.ParseParams(page?.ParamsJson);

        var data = new JObject
        {
            ["template"] = new JObject
            {
                ["id"] = tpl.Id,
                ["code"] = tpl.Code,
                ["name"] = tpl.Name,
                ["version"] = tpl.Version,
                ["configJson"] = tpl.ConfigJson,
                ["defaultJson"] = tpl.DefaultJson
            },
            // 已保存的实例参数（含 blocks）；前端模板自身参数按 DSL 键从顶层取值
            ["params"] = pageParams,
            ["slots"] = new JArray()
        };
        var slotsArr = (JArray)data["slots"]!;

        foreach (var rel in _templateBlocks.ListByTemplate(tpl.Id))
        {
            var block = _blocks.GetById(rel.BlockId);
            if (block == null || !block.IsActive) continue;
            var slot = new JObject
            {
                ["slot"] = rel.Slot,
                ["slotName"] = SlotNames.TryGetValue(rel.Slot, out var sn) ? sn : rel.Slot,
                ["required"] = rel.Required,
                ["sortNo"] = rel.SortNo,
                ["block"] = new JObject
                {
                    ["id"] = block.Id,
                    ["code"] = block.Code,
                    ["name"] = block.Name,
                    ["version"] = block.Version,
                    ["implementsRole"] = block.ImplementsRole,
                    ["paramConfigJson"] = block.ParamConfigJson,
                    ["commands"] = ParseJsonArray(block.Commands),
                    ["events"] = ParseJsonArray(block.Events),
                    ["description"] = block.Description
                },
                ["values"] = BuildSlotValues(pageParams, rel.Slot, block)
            };
            slotsArr.Add(slot);
        }

        // 兜底：实例 blocks 中已存在、但 DynTemplateBlock 未登记的槽位（如 tpl21/22），按 slot(blockCode) 反查积木
        var coveredSlots = new HashSet<string>(
            slotsArr.Select(s => s["slot"]?.Value<string>() ?? string.Empty), StringComparer.OrdinalIgnoreCase);
        if (pageParams["blocks"] is JObject savedBlocks)
        {
            foreach (var prop in savedBlocks.Properties())
            {
                var slotName = prop.Name;
                if (coveredSlots.Contains(slotName)) continue;
                var extraBlock = _blocks.Query(x => x.Code == slotName && x.IsActive).ToList().FirstOrDefault();
                if (extraBlock == null) continue;
                slotsArr.Add(new JObject
                {
                    ["slot"] = slotName,
                    ["slotName"] = SlotNames.TryGetValue(slotName, out var sn2) ? sn2 : extraBlock.Name,
                    ["required"] = false,
                    ["sortNo"] = 99,
                    ["block"] = new JObject
                    {
                        ["id"] = extraBlock.Id,
                        ["code"] = extraBlock.Code,
                        ["name"] = extraBlock.Name,
                        ["version"] = extraBlock.Version,
                        ["implementsRole"] = extraBlock.ImplementsRole,
                        ["paramConfigJson"] = extraBlock.ParamConfigJson,
                        ["commands"] = ParseJsonArray(extraBlock.Commands),
                        ["events"] = ParseJsonArray(extraBlock.Events),
                        ["description"] = extraBlock.Description
                    },
                    ["values"] = BuildSlotValues(pageParams, slotName, extraBlock)
                });
            }
        }
        return ApiResult.Ok(data);
    }

    /// <summary>槽位默认值 → 已保存 blocks → 旧扁平键 三层合并，统一输出 {settingId,model,options}。</summary>
    private static JObject BuildSlotValues(JObject pageParams, string slot, DynBlock block)
    {
        var def = DynPageViewHelper.ParseParams(block.ParamDefaultJson);
        var settingId = def["settingId"]?.Type == JTokenType.Null ? null : def["settingId"];
        var model = new JObject();
        foreach (var p in def.Properties())
            if (p.Name != "settingId") model[p.Name] = p.Value;

        JObject? options = null;
        var saved = DynPageViewHelper.SlotBlock(pageParams, slot);
        if (saved != null)
        {
            // 新结构：blocks[slot] = {settingId, model, options}
            if (saved["settingId"] is { Type: not JTokenType.Null } sv) settingId = sv;
            if (saved["model"] is JObject sm)
                foreach (var p in sm.Properties())
                    if (p.Value.Type != JTokenType.Null) model[p.Name] = p.Value;
            options = saved["options"] as JObject;
        }
        else
        {
            // 旧扁平 ConfigJson 迁移（仅回填非空值）
            var legacySetting = slot switch
            {
                "filter" => "FilterPageSettingId",
                "list" => "ListPageSettingId",
                "detail" => "DetailPageSettingId",
                _ => null
            };
            if (legacySetting != null)
            {
                var lv = pageParams[legacySetting];
                if (lv is { Type: not JTokenType.Null } && int.TryParse(lv.ToString(), out var li))
                    settingId = new JValue(li);
            }
            var legacyUrls = slot switch
            {
                "list" => new[] { ("loadUrl", "ListUrl"), ("deleteUrl", "DeleteUrl") },
                "detail" => new[] { ("addUrl", "AddUrl"), ("editUrl", "EditUrl"), ("deleteUrl", "DeleteUrl") },
                _ => Array.Empty<(string, string)>()
            };
            foreach (var (modelKey, legacyKey) in legacyUrls)
            {
                var lv = pageParams[legacyKey];
                var ls = lv?.Type == JTokenType.Null ? null : lv?.ToString();
                if (!string.IsNullOrWhiteSpace(ls)) model[modelKey] = ls;
            }
        }

        return new JObject
        {
            ["settingId"] = settingId ?? JValue.CreateNull(),
            ["model"] = model,
            ["options"] = options ?? new JObject()
        };
    }

    private static JArray ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new JArray();
        try { return JToken.Parse(json) as JArray ?? new JArray(); }
        catch { return new JArray(); }
    }

    private JObject LoadSettingConfig(int? settingId)
    {
        if (settingId == null) return null;
        var s = _settings.GetById(settingId.Value);
        if (s == null || string.IsNullOrWhiteSpace(s.ConfigJson)) return null;
        try { return JObject.Parse(s.ConfigJson); } catch { return null; }
    }

    private DynWebPage FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(p => p.Code == key).First();
    }
}
