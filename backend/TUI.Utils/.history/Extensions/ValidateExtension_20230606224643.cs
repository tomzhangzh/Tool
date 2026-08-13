// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Text.RegularExpressions;

namespace TUI.Utils;

/// <summary>
/// 验证扩展
/// </summary>
public static class ValidateExtension
{
    #region 数字字符串检查

    /// <summary>
    /// 是否数字字符串(0~9的字符串)【数字：^[0-9]*$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值 bool</returns>
    public static bool IsNum(this string input)
    {
        Regex regNumber = new Regex("^[0-9]+$");
        Match m = regNumber.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 是否N个数字字符串(0~9的字符串)【n位的数字：^\d{n}$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="length">数字字符串长度</param>
    /// <returns>返回值 bool</returns>
    public static bool IsnNum(this string input, int length)
    {
        Regex regNumber = new Regex("^\\d{" + length + "}$");
        Match m = regNumber.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 至少N个数字字符串(0~9的字符串)【至少n位的数字：^\d{n,}$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="length">数字字符串长度</param>
    /// <returns>返回值 bool</returns>
    public static bool IsLessNum(this string input, int length)
    {
        Regex regNumber = new Regex("^\\d{" + length + ",}$");
        Match m = regNumber.Match(input);
        return m.Success;
    }

    /// <summary>
    /// M~N个数字字符串(0~9的字符串)【m-n位的数字：^\d{m,n}$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="length">数字字符串长度</param>
    /// <returns>返回值 bool</returns>
    public static bool IsLessNum(this string input, int lesslength, int manylength)
    {
        Regex regNumber = new Regex("^\\d{" + lesslength + "," + manylength + "}$");
        Match m = regNumber.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 是否N位小数【有n位小数的正实数：^[0-9]+(.[0-9]{n})?$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="decimallength">小数长度</param>
    /// <returns>返回值 bool</returns>
    public static bool IsnDecimal(this string input, int decimallength)
    {
        Regex regNumber = new Regex(@"^[0-9]+(.[0-9]{" + decimallength + "})?$");
        Match m = regNumber.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 正数、负数、和小数【正数、负数、和小数：^(\-|\+)?\d+(\.\d+)?$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="length">数字字符串长度</param>
    /// <returns>返回值 bool</returns>
    public static bool IsnNum2(this string input, int length)
    {
        Regex regNumber = new Regex(@"^(\-|\+)?\d+(\.\d+)?$");
        Match m = regNumber.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 是否是int
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值 bool</returns>
    public static bool IsInt(this string input)
    {
        return Regex.IsMatch(input, @"^[1-9]\d*\.?[0]*$");
    }

    /// <summary>
    /// 是否数字字符串 可带正负号
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值 bool</returns>
    public static bool IsNumberSign(this string input)
    {
        Regex regNumberSign = new Regex("^[+-]?[0-9]+$");
        Match m = regNumberSign.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 是否是浮点数
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsDecimal(this string input)
    {
        Regex regDecimal = new Regex("^[0-9]+[.]?[0-9]+$");
        Match m = regDecimal.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 是否是浮点数 可带正负号
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsDecimalSign(this string input)
    {
        Regex regDecimalSign = new Regex("^[+-]?[0-9]+[.]?[0-9]+$"); //等价于^[+-]?\d+[.]?\d+$
        Match m = regDecimalSign.Match(input);
        return m.Success;
    }

    #endregion 数字字符串检查

    #region 字符串校验

    /// <summary>
    /// 检测是否有中文字符
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsChzn(this string input)
    {
        Regex regChzn = new Regex("[\u4e00-\u9fa5]");
        Match m = regChzn.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 检测是否是英文字母【由26个英文字母组成的字符串：^[A-Za-z]+$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsAToZ(this string input)
    {
        Regex regChzn = new Regex("^[A-Za-z]+$");
        Match m = regChzn.Match(input);
        return m.Success;
    }

    #endregion 字符串校验

    #region 特殊校验

    /// <summary>
    /// 验证身份证是否合法  15 和  18位两种
    /// </summary>
    /// <param name="idCard">要验证的身份证</param>
    /// <returns>返回值</returns>
    public static bool IsIdCard(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        if (input.Length == 15)
        {
            return Regex.IsMatch(input, @"^[1-9]\d{7}((0\d)|(1[0-2]))(([0|1|2]\d)|3[0-1])\d{3}$");
        }
        if (input.Length == 18)
        {
            return Regex.IsMatch(input,
                                 @"^[1-9]\d{5}[1-9]\d{3}((0\d)|(1[0-2]))(([0|1|2]\d)|3[0-1])((\d{4})|\d{3}[A-Z])$",
                                 RegexOptions.IgnoreCase);
        }
        return false;
    }

    /// <summary>
    /// 是否是邮件地址【^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$】
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsEmail(this string input)
    {
        Regex regEmail = new Regex(@"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        Match m = regEmail.Match(input);
        return m.Success;
    }

    /// <summary>
    /// 邮编有效性
    /// </summary>
    /// <param name="zip">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsZipCode(this string zip)
    {
        var rx = new Regex(@"^\d{6}$", RegexOptions.None);
        Match m = rx.Match(zip);
        return m.Success;
    }

    /// <summary>
    /// Url有效性
    /// </summary>
    /// <param name="url">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsUrl(this string url)
    {
        return Regex.IsMatch(url,
                             @"^(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&%\$#\=~])*[^\.\,\)\(\s]$");
    }

    /// <summary>
    /// Domain
    /// </summary>
    /// <param name="domain">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsDomain(this string domain)
    {
        return Regex.IsMatch(domain,
            @"[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(/.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+/.?");
    }

    /// <summary>
    /// IP有效性
    /// </summary>
    /// <param name="ip">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsIpv4(this string ip)
    {
        return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");
    }

    /// <summary>
    /// 判断是否为base64字符串
    /// </summary>
    /// <param name="str">输入字符串</param>
    /// <returns>返回值</returns>
    public static bool IsBase64(this string str)
    {
        return Regex.IsMatch(str, @"[A-Za-z0-9\+\/\=]");
    }

    /// <summary>
    /// 验证字符串是否是GUID
    /// </summary>
    /// <param name="guid">字符串</param>
    /// <returns>返回值</returns>
    public static bool IsGuid(this string guid)
    {
        Guid nguid;
        return Guid.TryParse(guid, out nguid);
    }

    #endregion 特殊校验
}