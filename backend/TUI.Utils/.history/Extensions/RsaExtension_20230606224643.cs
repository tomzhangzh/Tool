// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public static class RsaExtension
{
    /// <summary>
    /// RSA公钥/私钥内容前后缀，换行过滤
    /// </summary>
    /// <returns>过滤后的内容</returns>
    public static string RsaKeyFilter(this string keyContent)
    {
        if (string.IsNullOrWhiteSpace(keyContent)) return keyContent;
        //规范的写法
        keyContent = keyContent.Replace("-----BEGIN RSA PRIVATE KEY-----", "");
        keyContent = keyContent.Replace("-----END RSA PRIVATE KEY-----", "");
        //不规范写法情况
        keyContent = keyContent.Replace("-----BEGIN PRIVATE KEY-----", "");
        keyContent = keyContent.Replace("-----END PRIVATE KEY-----", "");

        keyContent = keyContent.Replace("-----BEGIN PUBLIC KEY-----", "");
        keyContent = keyContent.Replace("-----END PUBLIC KEY-----", "");

        keyContent = keyContent.Replace("-----BEGIN RSA PUBLIC KEY-----", "");
        keyContent = keyContent.Replace("-----END RSA PUBLIC KEY-----", "");

        keyContent = keyContent.Replace("\r", "");
        keyContent = keyContent.Replace("\n", "");

        return keyContent.Trim();
    }
}