// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

public static class CookieHelper
{
    /// <summary>
    /// 删除Cookie
    /// </summary>
    /// <param name="key"></param>
    public static void Clear(string key)
    {
        App.HttpContext.DeleteCookies(key);
    }

    /// <summary>
    /// 获取cookie值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string Get(string key, string encryptKey = "")
    {
        return App.HttpContext?.GetCookiesValue(key, encryptKey) ?? "";
    }

    /// <summary>
    /// 获取cookie值
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T Get<T>(string key, string encryptKey = "")
    {
        if (App.HttpContext == null) return default(T);
        return App.HttpContext!.GetCookiesValue<T>(key, encryptKey);
    }

    /// <summary>
    /// 设置cookie（默认关闭浏览器失效）
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="encryptKey">加密密钥（长度为32的2次方数）</param>
    /// <param name="cookieOptions">cookie选项，不设置，默认有效时间为30分钟</param>
    public static void Set(string key, string value, string encryptKey = "", CookieOptions cookieOptions = null)
    {
        App.HttpContext.SetCookies(key, value, encryptKey, cookieOptions);
    }

    /// <summary>
    /// 刷新cookie有效时间
    /// </summary>
    /// <param name="key"></param>
    /// <param name="encryptKey">加密密钥（长度为32的2次方数）</param>
    /// <param name="cookieOptions"></param>
    /// <returns></returns>
    public static bool Refresh(string key, string encryptKey = "", CookieOptions cookieOptions = null)
    {
        return App.HttpContext.RefreshCookieExpires(key, encryptKey, cookieOptions);
    }
}