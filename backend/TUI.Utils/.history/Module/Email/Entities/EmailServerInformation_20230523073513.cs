// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 邮件服务器基础信息
/// </summary>
public class EmailServerInformation
{
    /// <summary>
    /// SMTP服务器支持SASL机制类型
    /// </summary>
    public bool Authentication { get; set; }

    /// <summary>
    /// SMTP服务器对消息的大小
    /// </summary>
    public uint Size { get; set; }

    /// <summary>
    /// SMTP服务器支持传递状态通知
    /// </summary>
    public bool Dsn { get; set; }

    /// <summary>
    /// SMTP服务器支持Content-Transfer-Encoding
    /// </summary>
    public bool EightBitMime { get; set; }

    /// <summary>
    /// SMTP服务器支持Content-Transfer-Encoding
    /// </summary>
    public bool BinaryMime { get; set; }

    /// <summary>
    /// SMTP服务器在消息头中支持UTF-8
    /// </summary>
    public string UTF8 { get; set; }
}