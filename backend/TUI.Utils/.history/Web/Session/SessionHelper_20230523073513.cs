// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

public static class SessionHelper
{
    /// <summary>
    /// 删除Session
    /// </summary>
    /// <param name="key"></param>
    public static void Clear(string key)
    {
        App.HttpContext.DeleteSession(key);
    }

    /// <summary>
    /// 获取Session值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string Get(string key, string encryptKey = "")
    {
        return App.HttpContext.GetSessionValue(key, encryptKey);
    }

    /// <summary>
    /// 获取Session值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T Get<T>(string key, string encryptKey = "")
    {
        return App.HttpContext.GetSessionValue<T>(key, encryptKey);
    }

    /// <summary>
    /// 设置Session（默认关闭浏览器失效）
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="encryptKey">加密密钥（长度为32的2次方数）</param>
    public static void Set(string key, string value, string encryptKey = "")
    {
        App.HttpContext.SetSession(key, value, encryptKey);
    }
}