// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Abc.Utils;

/// <summary>
/// Newtonsoft.Json 扩展
/// </summary>
public static class NewtonsoftJsonExtension
{
    /// <summary>
    /// 序列化为Json字符串（Newtonsoft.Json）
    /// </summary>
    /// <typeparam name="T">对象</typeparam>
    /// <param name="obj">序列化的对象</param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static string ToJson2<T>(this T obj, Newtonsoft.Json.JsonSerializerSettings option = null)
    {
        try
        {
            if (option == null) option = GetDefaultOption;
            var result = Newtonsoft.Json.JsonConvert.SerializeObject(obj, option);
            return result;
        }
        catch (Exception ex)
        {
            LogEx.Error($"type:{typeof(T).FullName}\r\n{ex.ToStringEx()}", "系统\\错误");
            return "";
        }
    }

    /// <summary>
    /// 反序列化为对象（Newtonsoft.Json）
    /// </summary>
    /// <typeparam name="T">对象</typeparam>
    /// <param name="jsonStr">json字符串</param>
    /// <param name="option"></param>
    /// <returns></returns>
    public static T? ToObj2<T>(this string jsonStr, Newtonsoft.Json.JsonSerializerSettings option = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonStr)) return default(T);
            if (option == null) option = GetDefaultOption;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonStr, option);
        }
        catch (Exception ex)
        {
            LogEx.Error($"str:{jsonStr}\r\ntype:{typeof(T).FullName}\r\n{ex.ToStringEx()}", "系统\\错误");
            return default(T?);
        }
    }

    /// <summary>
    ///  设置 newtongsoftjson mvc选项配置
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <returns></returns>
    public static IMvcBuilder AddAbcNewtonsoftJson(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.AddLongTypeConverters();//long类型转换为字符串,按需使用

            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;//指定如何处理循环引用，None--不序列化，Error-抛出异常，Serialize--仍要序列化
                                                                                            //options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;//控制在反序列化期间如何处理缺少的成员（例如JSON包含对象上不是成员的属性）。 Ignore 忽略，error 报错
                                                                                            //ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                                                                            //options.SerializerSettings.Formatting = Formatting.Indented;//格式化 缩进
                                                                                            //options.SerializerSettings.MaxDepth = 10; //设置序列化的最大层数
                                                                                            //options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;//空值处理
                                                                                            //日期类型默认格式化处理
                                                                                            //options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;

            options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
            options.SerializerSettings.DateParseHandling = DateParseHandling.None;
            //options.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal });//解决数据为 0001-01-01 00:00:00 的情形下 反序列化为 DateTimeOffset 格式报错问题

            options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
            options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();//json字符串大小写原样输出
            options.SerializerSettings.Converters.Add(new NewtonsoftJsonDateTimeOffsetConverter());
        });
        return mvcBuilder;
    }

    /// <summary>
    /// 默认 Newtonsoft.Json 配置
    /// </summary>
    /// <returns></returns>
    public static JsonSerializerSettings GetDefaultOption
    {
        get
        {
            var setting = new JsonSerializerSettings
            {
                //WriteIndented = true,   // 缩进
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,  // 中文乱码
                //PropertyNameCaseInsensitive = true  // 忽略大小写
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,//指定如何处理循环引用，None--不序列化，Error-抛出异常，Serialize--仍要序列化
                //MissingMemberHandling = MissingMemberHandling.Ignore,//控制在反序列化期间如何处理缺少的成员（例如JSON包含对象上不是成员的属性）。 Ignore 忽略，error 报错
                //ContractResolver = new CamelCasePropertyNamesContractResolver(),
                //Formatting = Formatting.Indented,//格式化 缩进
                //MaxDepth = 10, //设置序列化的最大层数
                //NullValueHandling = NullValueHandling.Ignore,//空值处理
                //日期类型默认格式化处理
                // DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                //DateParseHandling = Newtonsoft.Json.DateParseHandling.None,
                //DateFormatString = "yyyy-MM-dd'T'HH:mm:ss",
                //DateTimeZoneHandling = DateTimeZoneHandling.Local,

                //DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat,
                DateFormatString = "yyyy-MM-dd HH:mm:ss",
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()//json字符串大小写原样输出
            };
            //setting.Converters.AddLongTypeConverters();//long类型转换为字符串
            //setting.Converters.Add(new IsoDateTimeConverter
            //{
            //    DateTimeStyles = DateTimeStyles.AssumeUniversal
            //});//解决数据为 0001-01-01 00:00:00 的情形下 反序列化为 DateTimeOffset 格式报错问题,序列化时间的格式会变为 ：2022-10-01T10:00:00Z

            setting.Converters.Add(new NewtonsoftJsonDateTimeOffsetConverter());

            return setting;
        }
    }
}