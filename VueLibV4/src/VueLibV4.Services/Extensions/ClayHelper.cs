using Newtonsoft.Json.Linq;
using Shapeless;

namespace VueLibV4.Services.Extensions;

/// <summary>
/// Shapeless(Clay) 辅助工具。
/// 仅用于 JSON 配置（如 DynCom ConfigJson、DynWebPage ParamJson）的中间动态处理；
/// 处理完毕统一转回 JObject / Dictionary / string，不在调用链上扩散 dynamic。
/// </summary>
public static class ClayHelper
{
    /// <summary>
    /// JSON 字符串 → Clay 动态对象；null/空字符串返回空对象（不会抛异常）
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    public static dynamic Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Clay.Parse("{}");
        }
        return Clay.Parse(json);
    }

    /// <summary>
    /// 对象（含 Clay）→ JSON 字符串（紧凑格式；UZ=不缩进且保留 null）
    /// </summary>
    public static string ToJson(dynamic clay)
    {
        if (clay == null) return "{}";
        return clay.ToString("UZ");
    }

    /// <summary>
    /// Clay → Newtonsoft JObject（供项目既有 JObject 处理链使用）
    /// </summary>
    public static JObject ToJObject(dynamic clay)
    {
        var json = ToJson(clay);
        return string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);
    }

    /// <summary>
    /// Clay → Dictionary&lt;string, object&gt;（嵌套对象保留 JObject 形态，与项目既有口径一致）
    /// </summary>
    public static Dictionary<string, object> ToDictionary(dynamic clay)
    {
        var json = ToJson(clay);
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
        return JObject.Parse(json).ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// 安全读取 Clay 属性（不存在返回 null，不抛异常）
    /// </summary>
    public static object Get(dynamic clay, string key)
    {
        try
        {
            return clay[key];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 安全读取字符串属性（不存在返回 null）
    /// </summary>
    public static string GetString(dynamic clay, string key)
    {
        var value = Get(clay, key);
        return value?.ToString();
    }

    /// <summary>
    /// 设置属性（不存在自动新增）；value 为 null 时删除该属性
    /// </summary>
    public static void Set(dynamic clay, string key, object value)
    {
        if (value == null)
        {
            try { clay.Remove(key); } catch { /* 属性不存在时忽略 */ }
            return;
        }
        clay[key] = value;
    }
}
