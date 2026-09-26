using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VueLibV4.Services.Extensions;

/// <summary>JSON 通用扩展（Newtonsoft.Json），供平台各层统一序列化口径</summary>
public static class JsonExtensions
{
    /// <summary>序列化为 JSON 字符串；indented=true 输出缩进格式</summary>
    public static string ToJson(this object obj, bool indented = false)
    {
        if (obj == null) return "null";
        return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
    }

    /// <summary>序列化（忽略循环引用与 null 属性），适合直接回传前端的实体对象</summary>
    public static string ToJsonSafe(this object obj, bool indented = false)
    {
        if (obj == null) return "null";
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatString = "yyyy-MM-dd HH:mm:ss"
        };
        return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None, settings);
    }

    /// <summary>序列化：忽略 null，日期格式 yyyy-MM-dd HH:mm:ss</summary>
    public static string ToJsonWithoutNull(this object obj)
    {
        if (obj == null) return "null";
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new[] { new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss" } }
        };
        return JsonConvert.SerializeObject(obj, settings);
    }

    /// <summary>反序列化为强类型对象</summary>
    public static T FromJson<T>(this string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>反序列化为指定 Type（配合反射场景）</summary>
    public static object FromJson(this string json, Type type)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonConvert.DeserializeObject(json, type);
    }

    /// <summary>尝试反序列化；失败返回 default 而不抛异常</summary>
    public static T TryFromJson<T>(this string json, T defaultValue = default)
    {
        try { return string.IsNullOrWhiteSpace(json) ? defaultValue : JsonConvert.DeserializeObject<T>(json); }
        catch { return defaultValue; }
    }
}
