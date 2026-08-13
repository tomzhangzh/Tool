// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 邮件发送结果
/// </summary>
public class EmailSendResult
{
    /// <summary>
    /// 结果信息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 结果状态
    /// </summary>
    public bool Success { get; set; } = true;
}