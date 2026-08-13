// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Abc.Utils;

public static class StringExtension
{
    /// <summary>
    /// 获取枚举类型的字符串
    /// </summary>
    /// <param name="obj">枚举对象</param>
    /// <returns>string</returns>
    public static string ToString(this System.Enum obj)
    {
        return System.Enum.GetName(obj.GetType(), obj);
    }

    /// <summary>
    /// 扩展EndTrim
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="trimstr">需要去除的字符串</param>
    /// <returns></returns>
    public static string Trim(this string str, string trimstr)
    {
        return str.Trim(trimstr.Trim().ToCharArray()); ;
    }

    #region TrimEnd

    /// <summary>
    /// 扩展EndTrim
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="trimstr">末尾需要去除的字符串</param>
    /// <returns></returns>
    public static string TrimEnd(this string str, string trimstr)
    {
        return str.TrimEnd(trimstr.Trim().ToCharArray()); ;
    }

    #endregion TrimEnd

    #region TrimStart

    /// <summary>
    /// 扩展EndTrim
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="trimstr">字符串开始需要去除的字符串</param>
    /// <returns></returns>
    public static string TrimStart(this string str, string trimstr)
    {
        return str.TrimStart(trimstr.Trim().ToCharArray());
    }

    #endregion TrimStart

    /// <summary>
    /// 检查字符串最大长度，返回指定长度的串
    /// </summary>
    /// <param name="input">输入字符串</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>返回值</returns>
    public static string Split(this string input, int maxLength)
    {
        if (!string.IsNullOrEmpty(input))
        {
            input = input.Trim();
            if (input.Length > maxLength) //按最大长度截取字符串
            {
                input = input.Substring(0, maxLength);
            }
        }
        return input;
    }

    /// <summary>
    /// 字符串拆分转 object[]
    /// </summary>
    /// <param name="str"></param>
    /// <param name="trimstr">默认英文逗号隔开</param>
    /// <returns></returns>
    public static object[] ToSplitObjects(this string str, string trimstr = ",")
    {
        var list = str.Split(trimstr.ToArray());
        object[] result = new object[list.Length];
        for (int i = 0; i < list.Length; i++)
        {
            result[i] = list[i];
        }
        return result;
    }

    #region ToSBC 转全角的函数(SBC case)

    /// <summary>
    /// 转全角的函数(SBC case)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToSBC(this string str)
    {
        //半角转全角：
        char[] c = str.ToCharArray();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i] == 32)
            {
                c[i] = (char)12288;
                continue;
            }
            if (c[i] < 127)
                c[i] = (char)(c[i] + 65248);
        }
        return new string(c);
    }

    #endregion ToSBC 转全角的函数(SBC case)

    #region ToDBC 转半角的函数(SBC case)

    /// <summary>
    ///  转半角的函数(SBC case)
    /// </summary>
    /// <param name="str">输入</param>
    /// <returns></returns>
    public static string ToDBC(this string str)
    {
        char[] c = str.ToCharArray();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i] == 12288)
            {
                c[i] = (char)32;
                continue;
            }
            if (c[i] > 65280 && c[i] < 65375)
                c[i] = (char)(c[i] - 65248);
        }
        return new string(c);
    }

    #endregion ToDBC 转半角的函数(SBC case)

    /// <summary>
    /// 移除字符串末尾指定字符
    /// </summary>
    /// <param name="str">需要移除的字符串</param>
    /// <param name="removeStr">移除的指定字符</param>
    /// <returns>移除后的字符串</returns>
    public static string RemoveLastStr(this string str, string removeStr)
    {
        int _finded = str.LastIndexOf(removeStr);
        if (_finded != -1)
        {
            return str.Substring(0, _finded);
        }
        return str;
    }

    /// <summary>
    /// 移除字符串开始指定字符
    /// </summary>
    /// <param name="str">需要移除的字符串</param>
    /// <param name="removeStr">移除的指定字符</param>
    /// <returns>移除后的字符串</returns>
    public static string RemoveStartStr(this string str, string removeStr)
    {
        int _finded = str.LastIndexOf(removeStr);
        if (_finded != -1)
        {
            // return str.Substring(0, _finded);
            return str[_finded..];
        }
        return str;
    }

    ///// <summary>
    ///// 在字符串末尾，换行添加内容
    ///// </summary>
    ///// <param name="Str"></param>
    ///// <param name="appendstr"></param>
    //public static string AppendLine(this string Str, string appendstr) => $"{Str}\r\n{appendstr}";

    /// <summary>
    /// JSON 字符串格式化
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ConvertJsonString(this string str)
    {
        try
        {
            //格式化json字符串
            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
            TextReader tr = new StringReader(str);
            using (JsonTextReader jtr = new JsonTextReader(tr))
            {
                object obj = serializer.Deserialize(jtr);
                if (obj != null)
                {
                    StringWriter textWriter = new StringWriter();
                    JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                    {
                        Formatting = Formatting.Indented,
                        Indentation = 4,
                        IndentChar = ' '
                    };
                    serializer.Serialize(jsonWriter, obj);
                    return textWriter.ToString();
                }
                else
                {
                    return str;
                }
            }
        }
        catch (Exception)
        {
            //出现异常，原样输出

            return str;
        }
    }

    /// <summary>
    /// 执行cmd命令
    /// 多命令请使用批处理命令连接符：
    /// <![CDATA[
    /// &:同时执行两个命令
    /// |:将上一个命令的输出,作为下一个命令的输入
    /// &&：当&&前的命令成功时,才执行&&后的命令
    /// ||：当||前的命令失败时,才执行||后的命令]]>
    /// 其他请百度
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="output"></param>
    public static string RunCmd(this string cmd)
    {
        cmd = cmd.Trim().TrimEnd('&') + "&exit";  ////说明：不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
        using var _process = new Process();
        _process.StartInfo.FileName = "cmd.exe";
        _process.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
        _process.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
        _process.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
        _process.StartInfo.RedirectStandardError = true;//重定向标准错误输出
        _process.StartInfo.CreateNoWindow = true;//不显示程序窗口
        _process.Start();//启动程序

        //向cmd窗口写入命令
        _process.StandardInput.WriteLine(cmd);
        _process.StandardInput.AutoFlush = true;

        //获取cmd窗口的输出信息
        var output = _process.StandardOutput.ReadToEnd();
        _process.WaitForExit();//等待程序执行完退出进程
        _process.Close();

        //使用示例
        /*
        使用示例

        示例1：显示ipconfig信息

        string cmd =@"ipconfig/all";
        示例2：打开VS2010命令提示

        string cmd =@"C:&cd C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC&vcvarsall.bat";
        示例3：使用sn.exe工具产生密钥对并显示

        string cmd =@"C:&cd C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC&vcvarsall.bat&sn -k d:          \LicBase.snk&sn   -p d:\LicBase.snk d:\LicBasePubKey.snk&sn -tp d:\LicBasePubKey.snk";
        调用

        string output = "";
        CmdHelper.RunCmd(cmd, out output);
        MessageBox.Show(output);

         */
        return output;
    }

    /// <summary>
    /// 枚举转换为指定格式字符串
    /// </summary>
    /// <param name="value"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static string EnumToString(this Enum value, string format = "G")
    {
        if (value == null) return "";

        return value?.ToString(format) ?? "";
    }

    /// <summary>
    /// 字符串掩码
    /// 手机号邮箱等隐私信息进行*号打码
    /// </summary>
    /// <param name="s">字符串</param>
    /// <param name="mask">掩码符</param>
    /// <returns></returns>
    public static string Mask(this string s, char mask = '*')
    {
        if (string.IsNullOrWhiteSpace(s?.Trim()))
        {
            return s;
        }
        s = s.Trim();

        var i = s.IndexOf('@');

        var str = s.Substring(0, i);
        var hz = s.Substring(i, s.Length - str.Length);

        string masks = mask.ToString().PadLeft(4, mask);
        var nstr = str.Length switch
        {
            _ when str.Length >= 11 => Regex.Replace(str, @"(\w{3})\w*(\w{4})", $"$1{masks}$2"),
            _ when str.Length == 10 => Regex.Replace(str, @"(\w{3})\w*(\w{3})", $"$1{masks}$2"),
            _ when str.Length == 9 => Regex.Replace(str, @"(\w{2})\w*(\w{3})", $"$1{masks}$2"),
            _ when str.Length == 8 => Regex.Replace(str, @"(\w{2})\w*(\w{2})", $"$1{masks}$2"),
            _ when str.Length == 7 => Regex.Replace(str, @"(\w{1})\w*(\w{2})", $"$1{masks}$2"),
            _ when str.Length >= 2 && str.Length < 7 => Regex.Replace(str, @"(\w{1})\w*(\w{1})", $"$1{masks}$2"),
            _ => str + masks
        };

        return nstr + hz;
    }
}