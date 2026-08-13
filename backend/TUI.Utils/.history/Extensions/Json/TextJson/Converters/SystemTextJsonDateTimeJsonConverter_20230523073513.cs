// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Abc.Utils;

/// <summary>
/// DateTime 类型序列化
/// </summary>
[SuppressSniffer]
public class SystemTextJsonDateTimeConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SystemTextJsonDateTimeConverter()
    {
        Format ??= "yyyy-MM-dd HH:mm:ss";
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="format"></param>
    public SystemTextJsonDateTimeConverter(string format)
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
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        //var str = reader.GetString();
        return Convert.ToDateTime(reader.GetString());
    }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}

/// <summary>
/// DateTime? 类型序列化
/// </summary>
[SuppressSniffer]
public class SystemTextJsonNullableDateTimeConverter : JsonConverter<DateTime?>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SystemTextJsonNullableDateTimeConverter()
    {
        Format ??= "yyyy-MM-dd HH:mm:ss";
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="format"></param>
    public SystemTextJsonNullableDateTimeConverter(string format)
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
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Convert.ToDateTime(reader.GetString());
    }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null) writer.WriteNullValue();
        else writer.WriteStringValue(value.Value.ToString(Format));
    }
}