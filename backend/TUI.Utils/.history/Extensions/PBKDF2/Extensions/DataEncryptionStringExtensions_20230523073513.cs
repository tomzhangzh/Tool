// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// DataEncryption 字符串加密拓展
/// </summary>
[SuppressSniffer]
public static class DataEncryptionStringExtensions
{
    /// <summary>
    /// 密码（字符串） PBKDF2 加密
    /// </summary>
    /// <param name="text">需要加密的文本（字符串）</param>
    /// <returns></returns>
    public static string ToPBKDF2Encrypt(this string text)
    {
        return PBKDF2Encryption.Encrypt(text);
    }

    /// <summary>
    /// 使用 PBKDF2 算法验证密码（字符串）是否正确
    /// </summary>
    /// <param name="text">待验证的原始字符串（字符串）</param>
    /// <param name="encryptText"></param>
    /// <returns></returns>
    public static bool ToPBKDF2Compare(this string text, string encryptText)
    {
        return PBKDF2Encryption.Compare(text, encryptText);
    }
}