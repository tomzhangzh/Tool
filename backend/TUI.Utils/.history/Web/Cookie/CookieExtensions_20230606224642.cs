// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// Cookie 扩展
/// </summary>
public static class CookieExtensions
{
    /// <summary>
    /// 删除Cookie
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    public static void DeleteCookies(this HttpContext httpContext, string key)
    {
        httpContext.Response.Cookies.Delete(key.ToSha1());
    }

    /// <summary>
    /// 获取cookie值
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string? GetCookiesValue(this HttpContext httpContext, string key, string encryptKey = "")
    {
        return httpContext.GetCookiesValue<string>(key, encryptKey);
    }

    /// <summary>
    /// 获取cookie值
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T? GetCookiesValue<T>(this HttpContext httpContext, string key, string encryptKey = "")
    {
        var state = httpContext.Request.Cookies.TryGetValue(key.ToSha1(), out var value);
        if (state && value != null)
        {
            if (string.IsNullOrWhiteSpace(encryptKey))
            {
                return value.To<T>();
            }
            else
            {
                return value.ToAESDecrypt(encryptKey).To<T>() ?? default(T);
            }
        }
        return default(T);
    }

    /// <summary>
    /// 设置cookie（默认关闭浏览器失效）
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="encryptKey">加密密钥（长度为32的2次方数）</param>
    /// <param name="cookieOptions">cookie选项，不设置，默认有效时间为30分钟</param>
    public static void SetCookies(this HttpContext httpContext, string key, string value, string encryptKey = "", CookieOptions? cookieOptions = null)
    {
        httpContext.DeleteCookies(key);//先删除下
        if (cookieOptions == null)
        {
            cookieOptions = new CookieOptions()
            {
                //Expires = DateTime.Now.AddMinutes(30),//默认30分钟失效,
                HttpOnly = true
            };
        }
        if (!string.IsNullOrWhiteSpace(encryptKey))
        {
            var v = value.ToAESEncrypt(encryptKey);
            httpContext.Response.Cookies.Append(key.ToSha1(), v, cookieOptions);
        }
        else
        {
            httpContext.Response.Cookies.Append(key.ToSha1(), value, cookieOptions);
        }
    }

    /// <summary>
    /// 刷新cookie有效时间
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    /// <param name="encryptKey">加密密钥（长度为32的2次方数）</param>
    /// <param name="cookieOptions"></param>
    /// <returns></returns>
    public static bool RefreshCookieExpires(this HttpContext httpContext, string key, string encryptKey = "", CookieOptions? cookieOptions = null)
    {
        var state = httpContext.Request.Cookies.TryGetValue(key.ToSha1(), out var value);
        if (state)
        {
            //var v = value.ToAESDecrypt(encryptKey);
            SetCookies(httpContext, key, value, encryptKey, cookieOptions);
            return true;
        }
        return false;
    }
}