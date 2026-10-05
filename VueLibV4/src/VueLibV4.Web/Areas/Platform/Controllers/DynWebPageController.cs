using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Text;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页：真正的动态页面（强类型 Model + 强类型服务，外键 int Id）。
/// 选择一个 DynTemplate，结合用户配置参数（ParamsJson：模板自身参数 + blocks 槽位分组）实例化 PageJson，即可运行。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynwebpage")]
[ApiController]
public class DynWebPageController : ControllerBase
{
    private readonly IDynWebPageService _svc;
    private readonly IDynProjectService _projects;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;
    private readonly IDynTemplateBlockService _templateBlocks;
    private readonly IDynBlockService _blocks;
    private readonly IDynPageExtService? _pageExt;

    public DynWebPageController(
        IDynWebPageService svc,
        IDynProjectService projects,
        IDynTemplateService templates,
        IPageSettingService settings,
        IDynTemplateBlockService templateBlocks,
        IDynBlockService blocks,
        IDynPageExtService? pageExt = null)
    {
        _svc = svc;
        _projects = projects;
        _templates = templates;
        _settings = settings;
        _templateBlocks = templateBlocks;
        _blocks = blocks;
        _pageExt = pageExt;
    }

    [HttpGet("all")]
    public ApiResult All(string projectId = null)
    {
        // 兼容：projectId 可为数字 Id 或项目 Code
        if (!string.IsNullOrEmpty(projectId) && !int.TryParse(projectId, out _))
        {
            var proj = _projects.List(x => x.Code == projectId).FirstOrDefault();
            if (proj == null) return ApiResult.Ok(new List<DynWebPage>());
            projectId = proj.Id.ToString();
        }
        int? pid = int.TryParse(projectId, out var v) ? v : null;

        var rows = _svc.Query(p => p.IsActive && (pid == null || p.ProjectId == pid))
            .OrderBy(p => p.CreateTime, OrderByType.Desc)
            .ToList();
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
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

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynWebPage data)
    {
        if (data.Id <= 0)
        {
            _svc.Insert(data);
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        _svc.Update(data);
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        _svc.DeleteById(id);
        return ApiResult.Ok(true, "删除成功");
    }

    /// <summary>
    /// 导出手写模板：把 PageSet 的 ConfigJson 转成嵌套的 dyn-xxx 标签格式。
    /// 入参 {id}，返回 { template: "..." }。
    /// </summary>
    [HttpPost("export-template")]
    public ApiResult ExportTemplate([FromBody] JObject body)
    {
        var id = body["id"]?.Value<int>() ?? body["Id"]?.Value<int>() ?? 0;
        var page = id > 0 ? _svc.GetById(id) : null;
        if (page == null) return ApiResult.Fail("页面不存在");
        if (string.IsNullOrWhiteSpace(page.PageJson)) return ApiResult.Fail("页面没有配置");

        try
        {
            var cfg = JObject.Parse(page.PageJson);
            var template = BuildTemplate(cfg, 0);
            return ApiResult.Ok(new { template = template }, "导出成功");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("导出失败: " + ex.Message);
        }
    }

    /// <summary>递归把 JSON 配置转成手写模板标签</summary>
    private string BuildTemplate(JObject cfg, int indent)
    {
        var pad = new string(' ', indent * 4);
        var component = cfg["component"]?.ToString() ?? "dyn-el-container";
        var modelname = cfg["modelname"]?.ToString();
        var options = cfg["options"] as JObject;
        var comoptions = options?["comoptions"] as JObject;
        var labeloptions = options?["labeloptions"] as JObject;
        var children = cfg["childrenctrls"] as JArray;

        var sb = new StringBuilder();
        sb.Append(pad).Append("<").Append(component);
        if (!string.IsNullOrEmpty(modelname)) sb.Append(" modelname=\"").Append(modelname).Append("\"");

        // labeloptions → 独立属性
        if (labeloptions != null)
        {
            var label = labeloptions["label"]?.ToString();
            if (!string.IsNullOrEmpty(label)) sb.Append(" label=\"").Append(label).Append("\"");
            var required = labeloptions["required"]?.Value<bool>() ?? false;
            if (required) sb.Append(" required");
        }

        // comoptions 常用属性 → 独立属性
        if (comoptions != null)
        {
            AppendComOption(sb, comoptions, "placeholder", "placeholder");
            AppendComOption(sb, comoptions, "type", "type");
            AppendComOption(sb, comoptions, "size", "size");
            AppendComOption(sb, comoptions, "disabled", "disabled");
            AppendComOption(sb, comoptions, "clearable", "clearable");
            AppendComOption(sb, comoptions, "readonly", "readonly");
            AppendComOption(sb, comoptions, "min", "min");
            AppendComOption(sb, comoptions, "max", "max");
            AppendComOption(sb, comoptions, "multiple", "multiple");
            AppendComOption(sb, comoptions, "filterable", "filterable");
            AppendComOption(sb, comoptions, "show-word-limit", "show-word-limit");
            AppendComOption(sb, comoptions, "maxlength", "maxlength");
            // 剩余的特殊值用 :prop 绑定
            // options 数组用 :options
            var optionsArr = comoptions["options"] as JArray;
            if (optionsArr != null && optionsArr.HasValues)
            {
                sb.Append(" :options='").Append(optionsArr.ToString(Newtonsoft.Json.Formatting.None)).Append("'");
            }
        }

        if (children != null && children.Count > 0)
        {
            sb.AppendLine(">");
            foreach (var child in children)
            {
                sb.Append(BuildTemplate((JObject)child, indent + 1));
            }
            sb.Append(pad).AppendLine("</" + component + ">");
        }
        else
        {
            sb.AppendLine(" />");
        }
        return sb.ToString();
    }

    /// <summary>把 comoptions 里的属性转成独立标签属性</summary>
    private void AppendComOption(StringBuilder sb, JObject comoptions, string key, string attrName)
    {
        var token = comoptions[key];
        if (token == null || token.Type == JTokenType.Null) return;

        if (token.Type == JTokenType.Boolean)
        {
            if (token.Value<bool>()) sb.Append(" ").Append(attrName);
        }
        else if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            sb.Append(" ").Append(attrName).Append("=\"").Append(token.ToString()).Append("\"");
        }
        else
        {
            var val = token.ToString();
            if (!string.IsNullOrEmpty(val)) sb.Append(" ").Append(attrName).Append("=\"").Append(val).Append("\"");
        }
    }

    /// <summary>
    /// 为已保存的页面实例生成（或定位）扩展视图骨架文件，并把路径回填到 ExtViewPath。
    /// 文件已存在时不覆盖（保护手工定制）。入参 {id}。
    /// </summary>
    [HttpPost("ext-skeleton")]
    public async Task<ApiResult> ExtSkeleton([FromBody] JObject body, CancellationToken ct)
    {
        var id = body["id"]?.Value<int>() ?? body["Id"]?.Value<int>() ?? 0;
        var page = id > 0 ? _svc.GetById(id) : null;
        if (page == null) return ApiResult.Fail("页面不存在，请先保存页面");
        if (string.IsNullOrWhiteSpace(page.Code)) return ApiResult.Fail("页面 Code 为空，无法生成扩展视图");
        if (_pageExt == null) return ApiResult.Fail("扩展视图服务未注册");
        var path = await _pageExt.EnsureSkeletonAsync(page.Code, page.Name, ct);
        if (!string.Equals(page.ExtViewPath, path, StringComparison.OrdinalIgnoreCase))
        {
            page.ExtViewPath = path;
            _svc.Update(page);
        }
        return ApiResult.Ok(new { viewPath = path }, "扩展视图骨架已就绪");
    }

    private DynWebPage FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(p => p.Code == key).First();
    }
}
