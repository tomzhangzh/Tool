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
    // 业务库（项目 SQLite）默认端点
    public const string BizSearch = "/api/business/dyndata/search";
    public const string BizInsert = "/api/business/dyndata/insert";
    public const string BizUpdate = "/api/business/dyndata/update";
    public const string BizDelete = "/api/business/dyndata/delete";
    public const string BizGet = "/api/business/dyndata/get";

    // 平台直连默认端点（save 端点自动判别增改，table 走 query）
    public const string PlatSearch = "/api/platform/dyndata/search";
    public const string PlatSave = "/api/platform/dyndata/save";
    public const string PlatDelete = "/api/platform/dyndata/delete";
    public const string PlatGet = "/api/platform/dyndata/get";

    /// <summary>配置值为空时取缺省 URL。</summary>
    public static string UrlOr(string configured, string fallback)
        => string.IsNullOrWhiteSpace(configured) ? fallback : configured;

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
}
