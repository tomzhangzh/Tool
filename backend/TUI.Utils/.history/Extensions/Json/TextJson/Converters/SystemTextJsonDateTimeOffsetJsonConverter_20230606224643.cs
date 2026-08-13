// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUI.Utils;

/// <summary>
/// DateTimeOffset 类型序列化
/// </summary>
[SuppressSniffer]
public class SystemTextJsonDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SystemTextJsonDateTimeOffsetConverter()
    {
        Format ??= "yyyy-MM-dd HH:mm:ss";
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="format"></param>
    public SystemTextJsonDateTimeOffsetConverter(string format)
    {
        Format = format;
    }

    /// <summary>
    /// 时间格式化格式
    /// </summary>
    public string Format { get; private set; }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override DateTimeOffset Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        //return DateTime.SpecifyKind(Convert.ToDateTime(reader.GetString()), Localized ? DateTimeKind.Local : DateTimeKind.Utc);
        var str = reader.GetString();
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
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // 判断是否序列化成当地时间
        var formatDateTime = value;
        writer.WriteStringValue(formatDateTime.ToString(Format));
    }
}

/// <summary>
/// DateTimeOffset? 类型序列化
/// </summary>
[SuppressSniffer]
public class SystemTextJsonNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SystemTextJsonNullableDateTimeOffsetConverter()
    {
        Format ??= "yyyy-MM-dd HH:mm:ss";
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="format"></param>
    public SystemTextJsonNullableDateTimeOffsetConverter(string format)
    {
        Format = format;
    }

    /// <summary>
    /// 时间格式化格式
    /// </summary>
    public string Format { get; private set; }

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options)
    {
        //return DateTime.SpecifyKind(Convert.ToDateTime(reader.GetString()), Localized ? DateTimeKind.Local : DateTimeKind.Utc);
        //return DateTimeOffset.Parse(reader.GetString());
        var str = reader.GetString();
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
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null) writer.WriteNullValue();
        else
        {
            // 判断是否序列化成当地时间
            var formatDateTime = value;
            writer.WriteStringValue(formatDateTime.Value.ToString(Format));
        }
    }
}