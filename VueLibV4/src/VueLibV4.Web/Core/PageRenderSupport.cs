using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Core;

/// <summary>
/// 页面渲染共享 helper：参数推导、PageSetting 引用包读取。
/// 从 PageController 抽出，供拆分后的 Page* 控制器复用。
/// </summary>
internal static class PageRenderSupport
{
    /// <summary>TableName 自动补齐 dyndata 免 model 接口 Url（手动填写优先）</summary>
    internal static Dictionary<string, object> BuildEffectiveParams(Dictionary<string, object> raw)
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

    internal static bool HasValue(Dictionary<string, object> d, string key)
    {
        return d.TryGetValue(key, out var v) && v != null && !string.IsNullOrWhiteSpace(v.ToString());
    }

    /// <summary>从参数字典安全读取 int?（值缺失、空或不可解析时返回 null）</summary>
    internal static int? GetInt(Dictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i)) return i;
        return null;
    }

    internal static Dictionary<string, object> TryParseDict(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
        try { return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(); }
        catch { return new Dictionary<string, object>(); }
    }

    /// <summary>读取 PageSetting 引用包：Config(UI渲染树) + DefaultJson(model骨架) + RenderMode + PartialPath</summary>
    internal static SettingBundle? LoadSetting(IPageSettingService settings, int? settingId)
    {
        if (settingId == null || settingId <= 0) return null;
        var s = settings.GetById(settingId.Value);
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
    internal class SettingBundle
    {
        public JObject? Config { get; set; }
        public string? DefaultJson { get; set; }
        public string? RenderMode { get; set; }
        public string? PartialPath { get; set; }
    }
}
