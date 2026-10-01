using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Core;

/// <summary>
/// 动态模板（DynTemplates）Razor 公共 helper：收敛四个模板里重复的
/// 「PageSetting 读取 → ConfigJson/DefaultJson 解析 → JObject 拼装」与
/// 「数据接口 URL 缺省」逻辑（对应评审 #9/#8）。
/// </summary>
public static class DynPageViewHelper
{
    // 统一动态数据端点：全系统唯一数据入口，数据域由请求 project 坐标决定
    // （空=默认业务库；"__platform__"=平台元数据库；DynProject.Code/Id=项目库）。
    public const string SearchUrl = "/api/dyndata/search";
    public const string InsertUrl = "/api/dyndata/insert";
    public const string UpdateUrl = "/api/dyndata/update";
    public const string DeleteUrl = "/api/dyndata/delete";
    public const string GetUrl = "/api/dyndata/get";
    /// <summary>按主键自动判别增/改的保存端点（Block 写操作标准入口）。</summary>
    public const string SaveUrl = "/api/dyndata/save";

    /// <summary>平台元数据库的数据域坐标（与 ProjectDbResolver.PlatformProjectKey 对应）。</summary>
    public const string PlatformProject = "__platform__";

    /// <summary>配置值为空时取缺省 URL。</summary>
    public static string UrlOr(string configured, string fallback)
        => string.IsNullOrWhiteSpace(configured) ? fallback : configured;

    /// <summary>
    /// block 角色 → BlockApp 部分视图路径（布局壳按 spec.slots[name].block 数据驱动选槽内积木）。
    /// 未登记角色返回 null（壳视图负责拒绝渲染并报错）。
    /// </summary>
    public static string? BlockAppPath(string? block) => (block ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "tree" => "~/Views/DynBlocks/Apps/TreeApp.cshtml",
        "filter" => "~/Views/DynBlocks/Apps/FilterApp.cshtml",
        "list" => "~/Views/DynBlocks/Apps/ListApp.cshtml",
        "detail" => "~/Views/DynBlocks/Apps/DetailApp.cshtml",
        _ => null
    };

    /// <summary>
    /// 读取 Front 类型 PageSetting，把 ConfigJson / DefaultJson 解析后写入
    /// target[configProp] / target[defaultProp]（无设置或 id 为空时写入空对象，结构恒存在）。
    /// </summary>
    public static void ApplyFrontSetting(JObject target, IPageSettingService svc, int? id,
        string configProp, string defaultProp)
    {
        target[configProp] = new JObject();
        target[defaultProp] = new JObject();
        if (id == null || svc == null) return;
        var ps = svc.GetById(id.Value);
        if (ps == null) return;
        if (!string.IsNullOrWhiteSpace(ps.ConfigJson)) target[configProp] = JObject.Parse(ps.ConfigJson);
        if (!string.IsNullOrWhiteSpace(ps.DefaultJson)) target[defaultProp] = JObject.Parse(ps.DefaultJson);
    }

    /// <summary>
    /// 读取列表类型 PageSetting，把 ConfigJson 中的 columns 数组写入 target[columnsProp]
    /// （无设置时为空数组）。
    /// </summary>
    public static void ApplyListSetting(JObject target, IPageSettingService svc, int? id,
        string columnsProp = "columns")
    {
        target[columnsProp] = new JArray();
        if (id == null || svc == null) return;
        var ps = svc.GetById(id.Value);
        if (ps == null || string.IsNullOrWhiteSpace(ps.ConfigJson)) return;
        try
        {
            var lc = JObject.Parse(ps.ConfigJson);
            if (lc[columnsProp] is JArray cols) target[columnsProp] = cols;
        }
        catch
        {
            // 列表设置 JSON 非法时保持空数组，不让整页崩掉
        }
    }

    /// <summary>输出到内联 script 的 JSON，并转义 &lt;/ 防止提前闭合 script 标签。</summary>
    public static string SafeJson(JObject obj)
        => obj.ToString(Newtonsoft.Json.Formatting.None).Replace("</", "<\\/");

    // ---------------- 实例参数（ParamsJson）读取：blocks 槽位优先 + 旧扁平键回退 ----------------

    /// <summary>解析 WebPage 实例参数（ParamsJson）；空/非法返回空 JObject（结构恒存在）。</summary>
    public static JObject ParseParams(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new JObject();
        try { return JObject.Parse(json); }
        catch { return new JObject(); }
    }

    /// <summary>取某槽位 Block 的保存节点：params.blocks[slot]（无则 null）。</summary>
    public static JObject? SlotBlock(JObject ps, string slot)
        => ps["blocks"]?[slot] as JObject;

    /// <summary>
    /// 取槽位接线的 PageSetting Id：blocks[slot].settingId 优先，回退旧扁平键
    /// （FilterPageSettingId/ListPageSettingId/DetailPageSettingId）。
    /// </summary>
    public static int? SlotSettingId(JObject ps, string slot, string legacyKey)
    {
        var blk = SlotBlock(ps, slot);
        var v = blk?["settingId"];
        if (v is { Type: not JTokenType.Null } && int.TryParse(v.ToString(), out var i)) return i;
        v = ps[legacyKey];
        if (v is { Type: not JTokenType.Null } && int.TryParse(v.ToString(), out var j)) return j;
        return null;
    }

    /// <summary>
    /// 取槽位 Block 的运行参数（URL 等）：blocks[slot].model[key] 优先，
    /// 可回退旧扁平键（如 list.loadUrl → ListUrl）。空字符串视同未配置返回 null（交给 UrlOr 走缺省）。
    /// </summary>
    public static string? SlotValue(JObject ps, string slot, string key, string? legacyKey = null)
    {
        var v = SlotBlock(ps, slot)?["model"]?[key];
        var s = v?.Type == JTokenType.Null ? null : v?.ToString();
        if (!string.IsNullOrWhiteSpace(s)) return s;
        if (!string.IsNullOrEmpty(legacyKey))
        {
            var lv = ps[legacyKey];
            var ls = lv?.Type == JTokenType.Null ? null : lv?.ToString();
            if (!string.IsNullOrWhiteSpace(ls)) return ls;
        }
        return null;
    }
}
