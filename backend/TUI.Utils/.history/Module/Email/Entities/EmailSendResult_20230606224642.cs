// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

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