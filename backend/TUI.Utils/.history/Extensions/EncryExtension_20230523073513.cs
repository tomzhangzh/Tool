// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.DataEncryption;

using System.Security.Cryptography;

namespace Abc.Utils;

/// <summary>
/// 加密解密类扩展
/// 虽然Furion已有对应扩展，但是要分别引用不同命名空间，比较麻烦，所以外面再套了一层。
/// </summary>
public static class EncryExtension
{
    /// <summary>
    /// 字符串的 Md5
    /// </summary>
    /// <param name="str"></param>
    /// <returns>string</returns>
    public static string ToMd5(this string str)
    {
        // return Md5Helper.Md5(str);
        return MD5Encryption.Encrypt(str);
    }

    /// <summary>
    /// 字符串的 ToPBKDF2
    /// </summary>
    /// <param name="str"></param>
    /// <returns>string</returns>
    public static string ToPBKDF2(this string str)
    {
        return PBKDF2Encryption.Encrypt(str);
    }

    /// <summary>
    /// 加密比较 ToPBKDF2
    /// </summary>
    /// <param name="inputText">输入文本</param>
    /// <param name="encryptText">加密文本</param>
    /// <returns>string</returns>
    public static bool ToPBKDF2Compare2(this string inputText, string encryptText)
    {
        return PBKDF2Encryption.Compare(inputText, encryptText);
    }

    #region AES

    /// <summary>
    /// 字符串Aes 加密
    /// </summary>
    /// <param name="text">需要加密的字符串</param>
    /// <param name="skey">密钥（长度为32的2次方数）</param>
    /// <returns>string</returns>
    public static string ToAESEncrypt(this string text, string skey)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(skey)) throw new ArgumentNullException($"AES密钥不能为空");
        return AESEncryption.Encrypt(text, skey);
    }

    /// <summary>
    /// 字符串Aes 解密
    /// </summary>
    /// <param name="text"></param>
    /// <param name="skey">密钥（长度为32的2次方数）</param>
    /// <returns>string</returns>
    public static string ToAESDecrypt(this string text, string skey)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(skey)) throw new ArgumentNullException($"AES密钥不能为空");
        return AESEncryption.Decrypt(text, skey);
    }

    #endregion AES

    #region RSA

    /// <summary>
    /// RSA 加密
    /// </summary>
    /// <param name="text">加密字符串</param>
    /// <param name="publicKey">公钥</param>
    /// <param name="keySize">密钥长度(秘钥大小必须为 2048 到 16384，并且是 8 的倍数)</param>
    /// <returns></returns>
    public static string ToRsaEncyrpt(this string text, string publicKey, int keySize = 2048)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(publicKey)) throw new ArgumentNullException($"公钥不能为空");
        return RSAEncryption.Encrypt(text, publicKey, keySize);
    }

    /// <summary>
    /// RSA解密
    /// </summary>
    /// <param name="text">需要解密字符串</param>
    /// <param name="privateKey">私钥</param>
    /// <param name="keySize">密钥长度(秘钥大小必须为 2048 到 16384，并且是 8 的倍数)</param>
    /// <returns></returns>
    public static string ToRsaDeCrypt(this string text, string privateKey, int keySize = 2048)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(privateKey)) throw new ArgumentNullException($"私钥不能为空");
        return RSAEncryption.Decrypt(text, privateKey, keySize);
    }

    /// <summary>
    /// RSA签名
    /// </summary>
    /// <param name="text">明文内容</param>
    /// <param name="halg">算法如:SHA256</param>
    /// <param name="privateKey">私钥</param>
    /// <returns>签名内容</returns>
    public static string ToRsaSign(this string text, string halg, string privateKey)
    {
        string encryptedContent;
        using var rsa = new System.Security.Cryptography.RSACryptoServiceProvider();
        rsa.FromXmlString(privateKey);

        var encryptedData = rsa.SignData(Encoding.Default.GetBytes(text), halg);
        encryptedContent = Convert.ToBase64String(encryptedData);

        return encryptedContent;
    }

    /// <summary>
    /// RSA签名校验
    /// </summary>
    /// <param name="text">明文内容</param>
    /// <param name="halg">算法如:SHA256</param>
    /// <param name="publicKey">公钥</param>
    /// <param name="sign">签名内容</param>
    /// <returns>是否一致</returns>
    public static bool ToRsaSignVerify(string text, string halg, string publicKey, string sign)
    {
        using var rsa = new System.Security.Cryptography.RSACryptoServiceProvider();
        rsa.FromXmlString(publicKey);

        return rsa.VerifyData(Encoding.Default.GetBytes(text), halg, Convert.FromBase64String(sign));
    }

    #endregion RSA

    #region DESC

    /// <summary>
    /// 字符串DES 加密
    /// </summary>
    /// <param name="text">需要加密的字符串</param>
    /// <param name="skey">密钥（长度为32的2次方数）</param>
    /// <returns>string</returns>
    public static string ToDESCEncrypt(this string text, string skey)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(skey)) throw new ArgumentNullException($"DES密钥不能为空");
        return DESCEncryption.Encrypt(text, skey);
    }

    /// <summary>
    /// 字符串DES 解密
    /// </summary>
    /// <param name="text"></param>
    /// <param name="skey">密钥（长度为32的2次方数）</param>
    /// <returns>string</returns>
    public static string ToDESCDecrypt(this string text, string skey)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(skey)) throw new ArgumentNullException($"DES密钥不能为空");
        return DESCEncryption.Decrypt(text, skey);
    }

    #endregion DESC

    #region SHA

    #region SHA1

    /// <summary>
    /// SHA1 加密
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns></returns>
    public static string ToSha1(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException(nameof(str));

        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] bytes_sha1_in = Encoding.UTF8.GetBytes(str);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            string str_sha1_out = BitConverter.ToString(bytes_sha1_out);
            str_sha1_out = str_sha1_out.Replace("-", "");
            return str_sha1_out;
        }
    }

    #endregion SHA1

    #region SHA256

    /// <summary>
    /// SHA256 加密
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns></returns>
    public static string ToSha256(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException(nameof(str));

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes_sha256_in = Encoding.UTF8.GetBytes(str);
            byte[] bytes_sha256_out = sha256.ComputeHash(bytes_sha256_in);
            string str_sha256_out = BitConverter.ToString(bytes_sha256_out);
            str_sha256_out = str_sha256_out.Replace("-", "");
            return str_sha256_out;
        }
    }

    #endregion SHA256

    #region SHA384

    /// <summary>
    /// SHA384 加密
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns></returns>
    public static string ToSha384(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException(nameof(str));

        using (SHA384 sha384 = SHA384.Create())
        {
            byte[] bytes_sha384_in = Encoding.UTF8.GetBytes(str);
            byte[] bytes_sha384_out = sha384.ComputeHash(bytes_sha384_in);
            string str_sha384_out = BitConverter.ToString(bytes_sha384_out);
            str_sha384_out = str_sha384_out.Replace("-", "");
            return str_sha384_out;
        }
    }

    #endregion SHA384

    #region SHA512

    /// <summary>
    /// SHA512 加密
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns></returns>
    public static string ToSha512(this string str)
    {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException(nameof(str));

        using (SHA512 sha512 = SHA512.Create())
        {
            byte[] bytes_sha512_in = Encoding.UTF8.GetBytes(str);
            byte[] bytes_sha512_out = sha512.ComputeHash(bytes_sha512_in);
            string str_sha512_out = BitConverter.ToString(bytes_sha512_out);
            str_sha512_out = str_sha512_out.Replace("-", "");
            return str_sha512_out;
        }
    }

    #endregion SHA512

    #endregion SHA
}