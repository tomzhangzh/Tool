// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.UnifyResult;

using System.Data;

namespace Abc.Utils;

public static class ExtensionHelper
{
    /// <summary>
    /// 返回时间（DateTime）
    /// </summary>
    /// <param name="str"></param>
    /// <param name="state">状态 true 表示成功 false 表示失败</param>
    /// <returns>DateTime</returns>
    public static DateTime ToDateTime(this string str, out bool state)
    {
        var result = DateTime.Now;
        state = DateTime.TryParse(str, out result);
        return result;
    }

    #region ToString  把 List<T> 按照分隔符组装成 string

    /// <summary>
    /// 把 List<T> 按照分隔符组装成 string
    /// </summary>
    /// <param name="list"></param>
    /// <param name="speater"></param>
    /// <returns></returns>
    public static string ToString<T>(this List<T> list, string speater)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < list.Count; i++)
        {
            sb.Append(i == list.Count - 1 ? list[i].ToString() : $"{list[i]}{speater}");
        }
        return sb.ToString();
    }

    #endregion ToString  把 List<T> 按照分隔符组装成 string

    public static string ToStringEx(this Exception exception)
    {
        if (exception == null) return "";
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("====================EXCEPTION====================");
        stringBuilder.AppendLine("【Message】:" + exception.Message);
        stringBuilder.AppendLine("【Source】:" + exception.Source);
        stringBuilder.AppendLine("【TargetSite】:" + ((exception.TargetSite != null) ? exception.TargetSite.Name : "无"));
        stringBuilder.AppendLine("【StackTrace】:" + exception.StackTrace);
        //stringBuilder.AppendLine("【exception】:" + exception);
        stringBuilder.AppendLine("=================================================");
        if (exception.InnerException != null)
        {
            stringBuilder.AppendLine("====================INNER EXCEPTION====================");
            stringBuilder.AppendLine("【Message】:" + exception.InnerException.Message);
            stringBuilder.AppendLine("【Source】:" + exception.InnerException.Source);
            stringBuilder.AppendLine("【TargetSite】:" + ((exception.InnerException.TargetSite != null) ? exception.InnerException.TargetSite.Name : "无"));
            stringBuilder.AppendLine("【StackTrace】:" + exception.InnerException.StackTrace);
            //stringBuilder.AppendLine(("【InnerException】:" + exception.InnerException) ?? "");
            stringBuilder.AppendLine("=================================================");
        }
        return stringBuilder.ToString().Replace("/r", "").Replace("/n", "");
    }

    #region Dictionary

    /// <summary>
    /// 尝试将键和值添加到字典中：如果不存在，才添加；存在，不添加也不抛导常
    /// </summary>
    public static Dictionary<TKey, TValue> TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key,
        TValue value)
    {
        if (dict.ContainsKey(key) == false) dict.Add(key, value);
        return dict;
    }

    /// <summary>
    /// 将键和值添加或替换到字典中：如果不存在，则添加；存在，则替换
    /// </summary>
    public static Dictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key,
        TValue value)
    {
        if (!dict.ContainsKey(key))
        {
            dict.Add(key, value);
        }
        else
        {
            dict[key] = value;
        }

        return dict;
    }

    /// <summary>
    /// 判断集合中是否存在指定的Key，不区分大小写
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dictionary">数据字典</param>
    /// <param name="key">关键词</param>
    /// <returns></returns>
    public static bool ContainsKeyIgnoreCase<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        if (dictionary == null) return false;
        bool? keyExists;

        var keyString = key as string;
        if (keyString != null)
        {
            // Key is a string.
            // Using string.Equals to perform case insensitive comparison of the dictionary key.
            keyExists =
                dictionary.Keys.OfType<string>()
                .Any(k => string.Equals(k, keyString, StringComparison.InvariantCultureIgnoreCase));
        }
        else
        {
            // Key is any other type, use default comparison.
            keyExists = dictionary.ContainsKey(key);
        }

        return keyExists ?? false;
    }

    /// <summary>
    /// 判断集合中是否存在指定的Key，不区分大小写
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dictionary">数据字典</param>
    /// <param name="key">关键词</param>
    /// <returns></returns>
    public static bool ContainsKeyIgnoreCase<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value).ContainsKeyIgnoreCase(key);
    }

    public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
    {
        if (dict.ContainsKey(key)) return dict[key];
        return default;
    }

    /// <summary>
    /// 把字典的key转换为小写
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static Dictionary<string, TValue> ToKeyLowerCase<TValue>(this Dictionary<string, TValue> obj)
    {
        return obj.ToDictionary(k => k.Key.ToLower(), v => v.Value);
    }

    /// <summary>
    /// 把字典的key转换为小写
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static ConcurrentDictionary<string, TValue> ToKeyLowerCase<TValue>(this ConcurrentDictionary<string, TValue> obj)
    {
        var newdic = new ConcurrentDictionary<string, TValue>();
        foreach (var item in obj)
        {
            newdic.TryAdd(item.Key.ToLower(), item.Value);
        }
        return newdic;
    }

    #endregion Dictionary

    /// <summary>
    /// 数据验证返回
    /// </summary>
    /// <param name="modelState"></param>
    /// <returns></returns>
    public static object JsonResult(this Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, bool isReutrnMultipleErrors = false)
    {
        var resultdata = new RESTfulResult<string>();
        //var verifications = modelState.ToDictionary(x => x.Key, v => string.Join(",", v.Value.Errors.Select(o => o.ErrorMessage)));
        var verifications = modelState.Where(o => o.Value.Errors.Count > 0).ToDictionary(x => x.Key, v => string.Join(",", v.Value.Errors.Select(o => o.ErrorMessage)));
        if (isReutrnMultipleErrors)
        {
            resultdata.Errors = verifications;
        }
        else
        {
            resultdata.Errors = verifications.FirstOrDefault().Value;
        }

        //resultdata.Msg = string.Join(",", modelState.SelectMany(_ => _.Value.Errors.Select(e => e.ErrorMessage)).ToList());

        return resultdata;
    }

    /// <summary>
    /// 获取文件MimeType
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns></returns>
    public static string GetFileMimeType(this string fileName)
    {
        string contentType;
        new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
        return contentType ?? "application/octet-stream";
    }
}