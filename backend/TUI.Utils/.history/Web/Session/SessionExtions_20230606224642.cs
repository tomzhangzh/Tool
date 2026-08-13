// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public static class SessionExtions
{/// <summary>
 /// 删除session
 /// </summary>
 /// <param name="httpContextAccessor"></param>
 /// <param name="key"></param>
    public static void DeleteSession(this HttpContext httpContext, string key)
    {
        httpContext.Session.Remove(key.ToSha1());
    }

    /// <summary>
    /// 获取session值
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetSessionValue(this HttpContext httpContext, string key, string encryptKey = "")
    {
        return httpContext.GetSessionValue<string>(key, encryptKey);
    }

    /// <summary>
    /// 获取session值
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static T GetSessionValue<T>(this HttpContext httpContext, string key, string encryptKey = "")
    {
        var value = httpContext.Session.GetString(key.ToSha1());
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (string.IsNullOrWhiteSpace(encryptKey))
            {
                return value.To<T>();
            }
            else
            {
                return value.ToAESDecrypt(encryptKey).To<T>();
            }
        }
        return default(T);
    }

    /// <summary>
    /// 设置session（默认关闭浏览器失效）
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="encryptKey">加密密钥（长度为32的2次方数）</param>
    public static void SetSession(this HttpContext httpContext, string key, string value, string encryptKey = "")
    {
        httpContext.DeleteSession(key);//先删除下
        if (!string.IsNullOrWhiteSpace(encryptKey))
        {
            var v = value.ToAESEncrypt(encryptKey);
            httpContext.Session.SetString(key.ToSha1(), v);
        }
        else
        {
            httpContext.Session.SetString(key.ToSha1(), value);
        }
    }
}