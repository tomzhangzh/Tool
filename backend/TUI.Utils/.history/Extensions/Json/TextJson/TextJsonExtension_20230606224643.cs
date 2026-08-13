// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TUI.Utils;

/// <summary>
/// System.Text.Json 扩展
/// </summary>
public static class TextJsonExtension
{
    /// <summary>
    /// 序列化为Json字符串（System.Text.Json）
    /// </summary>
    /// <typeparam name="T">对象</typeparam>
    /// <param name="obj">序列化的对象</param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static string ToJson<T>(this T obj, System.Text.Json.JsonSerializerOptions option = null)
    {
        try
        {
            if (option == null) option = GetDefaultOption;
            var result = System.Text.Json.JsonSerializer.Serialize(obj, option);
            return result;
        }
        catch (Exception ex)
        {
            LogEx.Error($"type:{typeof(T).FullName}\r\n{ex.ToStringEx()}", "系统\\错误");
            return "";
        }
    }

    /// <summary>
    /// 反序列化为对象（System.Text.Json）
    /// </summary>
    /// <typeparam name="T">对象</typeparam>
    /// <param name="jsonStr">json字符串</param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static T? ToObj<T>(this string jsonStr, System.Text.Json.JsonSerializerOptions option = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonStr)) return default(T);
            if (option == null) option = GetDefaultOption;
            return System.Text.Json.JsonSerializer.Deserialize<T>(json: jsonStr, option);
        }
        catch (Exception ex)
        {
            LogEx.Error($"str:{jsonStr}\r\ntype:{typeof(T).FullName}\r\n{ex.ToStringEx()}", "系统\\错误");
            return default(T?);
        }
    }

    /// <summary>
    /// 获取  System.Text.Json 默认设置
    /// </summary>
    /// <returns></returns>
    public static System.Text.Json.JsonSerializerOptions GetDefaultOption
    {
        get
        {
            var options = new JsonSerializerOptions
            {
                //Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(UnicodeRanges.All),//设置虚拟化中文的时候不编码
                //ReferenceHandler = ReferenceHandler.IgnoreCycles

                // Converters.AddLongTypeConverters();//long类型转换为字符串
                // 设置时间输出统一格式化
                //Converters.AddDateTimeTypeConverters("yyyy-MM-dd HH:mm:ss"),
                //options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Creat(System.Text.Unicode.UnicodeRanges.All);
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,//处理乱码问题
                PropertyNameCaseInsensitive = true,//不区分大小写
                PropertyNamingPolicy = null,//序列化属性名大写（属性原样输出）
                ReferenceHandler = ReferenceHandler.IgnoreCycles, //忽略循环引用
                IncludeFields = true,//包含成员字段序列化
                AllowTrailingCommas = true,//允许尾随逗号
                ReadCommentHandling = JsonCommentHandling.Skip,//允许注释
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
            };
            options.Converters.AddLongTypeConverters();//long类型转换为字符串
            options.Converters.AddDateTimeConverters("yyyy-MM-dd HH:mm:ss");// 设置时间输出统一格式化
            return options;
        }
    }

    /// <summary>
    /// 设置text.json mvc选项配置
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="isLongToString"></param>
    /// <returns></returns>
    public static IMvcBuilder AddTUIJsonOptions(this IMvcBuilder builder, bool isLongToString = false)
    {
        builder.AddJsonOptions(options =>
        {
            if (isLongToString)
            {
                options.JsonSerializerOptions.Converters.AddLongTypeConverters();//long类型转换为字符串
            }
            options.JsonSerializerOptions.Converters.AddDateTimeConverters("yyyy-MM-dd HH:mm:ss");// 设置时间输出统一格式化
            //options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Creat(System.Text.Unicode.UnicodeRanges.All);
            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;//处理乱码问题
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;//不区分大小写
            options.JsonSerializerOptions.PropertyNamingPolicy = null;//序列化属性名大写（属性原样输出）
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; //忽略循环引用
            options.JsonSerializerOptions.IncludeFields = true;//包含成员字段序列化
            options.JsonSerializerOptions.AllowTrailingCommas = true;//允许尾随逗号
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;//允许注释
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });

        return builder;
    }

    /// <summary>
    /// 添加 DateTime/DateTime?/DateTimeOffset/DateTimeOffset? 类型序列化处理
    /// </summary>
    /// <param name="converters"></param>
    /// <param name="outputFormat"></param>
    /// <returns></returns>
    public static IList<JsonConverter> AddDateTimeConverters(this IList<JsonConverter> converters, string outputFormat = default)
    {
        converters.Add(new SystemTextJsonDateTimeConverter(outputFormat));
        converters.Add(new SystemTextJsonNullableDateTimeConverter(outputFormat));

        converters.Add(new SystemTextJsonDateTimeOffsetConverter(outputFormat));
        converters.Add(new SystemTextJsonNullableDateTimeOffsetConverter(outputFormat));

        return converters;
    }
}