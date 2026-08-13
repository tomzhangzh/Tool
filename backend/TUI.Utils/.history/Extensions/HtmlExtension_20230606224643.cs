// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Text.RegularExpressions;

namespace TUI.Utils;

public static class HtmlExtension
{
    #region DropHtml  删除HTMl 标签

    /// <summary>
    ///  删除HTMl 标签
    /// </summary>
    /// <param name="strHtml"></param>
    /// <returns></returns>
    public static string DropHtml(this string strHtml)
    {
        string[] aryReg ={
        @"<script[^>]*?>.*?</script>",
        @"<(\/\s*)?!?((\w+:)?\w+)(\w+(\s*=?\s*(([""'])(\\[""'tbnr]|[^\7])*?\7|\w+)|.{0})|\s)*?(\/\s*)?>",
        @"([\r\n])[\s]+",
        @"&(quot|#34);",
        @"&(amp|#38);",
        @"&(lt|#60);",
        @"&(gt|#62);",
        @"&(nbsp|#160);",
        @"&(iexcl|#161);",
        @"&(cent|#162);",
        @"&(pound|#163);",
        @"&(copy|#169);",
        @"&#(\d+);",
        @"-->",
        @"<!--.*\n"
        };

        // string newReg = aryReg[0];
        string strOutput = aryReg.Select(t => new Regex(t, RegexOptions.IgnoreCase)).Aggregate(strHtml, (current, regex) => regex.Replace(current, string.Empty));

        strOutput = strOutput.Replace("<", "");
        strOutput = strOutput.Replace(">", "");
        strOutput = strOutput.Replace("\r\n", "");

        return strOutput;
    }

    #endregion DropHtml  删除HTMl 标签

    #region DropHtml 清除HTML标记且返回相应的长度

    /// <summary>
    /// 清除HTML标记且返回相应的长度
    /// </summary>
    /// <param name="htmlstring">字符串</param>
    /// <param name="lenght">长度</param>
    /// <returns></returns>
    public static string DropHtml(this string htmlstring, int lenght)
    {
        var text = DropHtml(htmlstring);
        if (text.Trim().Length > lenght) return text.Trim().Substring(0, lenght);
        return text.Trim();
    }

    #endregion DropHtml 清除HTML标记且返回相应的长度

    /// <summary>
    /// 转换成 HTML code
    /// </summary>
    /// <param name="str">输入字符串</param>
    /// <returns>返回值</returns>
    public static string Encode(this string str)
    {
        str = str.Replace("&", "&amp;");
        str = str.Replace("'", "''");
        str = str.Replace("\"", "&quot;");
        str = str.Replace(" ", "&nbsp;");
        str = str.Replace("<", "&lt;");
        str = str.Replace(">", "&gt;");
        str = str.Replace("\n", "<br>");
        return str;
    }

    /// <summary>
    ///解析html成 普通文本
    /// </summary>
    /// <param name="str">输入字符串</param>
    /// <returns>返回值</returns>
    public static string Decode(this string str)
    {
        str = str.Replace("<br>", "\n");
        str = str.Replace("&gt;", ">");
        str = str.Replace("&lt;", "<");
        str = str.Replace("&nbsp;", " ");
        str = str.Replace("&quot;", "\"");
        return str;
    }
}