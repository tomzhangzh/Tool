// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Newtonsoft.Json;

namespace TUI.Utils;

/// <summary>
/// DateTimeOffset 类型序列化
/// </summary>
[SuppressSniffer]
public class NewtonsoftJsonDateTimeOffsetConverter : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
    }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var str = reader.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(str))
        {
            return DateTimeOffset.MinValue;
        }
        else if (str.StartsWith("0001-01-01 00:00:00") || str.StartsWith("0001/01/01 00:00:00"))
        {
            return DateTimeOffset.MinValue;
        }
        else return DateTimeOffset.Parse(str);
    }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteValue(value);
    }
}