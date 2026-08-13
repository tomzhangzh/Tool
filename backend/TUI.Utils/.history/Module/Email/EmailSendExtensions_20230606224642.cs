// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// Email扩展
/// </summary>
public static class EmailSendExtensions
{
    /// <summary>
    /// 邮件发送模块扩展
    /// </summary>
    /// <param name="emailConfig">邮件配置</param>
    /// <param name="receive">接收邮箱,多个英文逗号隔开</param>
    /// <param name="subject">主题</param>
    /// <param name="body">内容</param>
    /// <returns></returns>
    public static async Task<EmailSendResult> SendAsync(this EmailConfig emailConfig, string receive, string subject, string body)
    {
        return await EmailSendHelper.SendAsync(emailConfig, new EmailBody(subject, body, receive));
    }

    /// <summary>
    /// 邮件发送模块扩展
    /// </summary>
    /// <param name="emailConfig">邮件配置</param>
    /// <param name="emailBody">邮件发送内容</param>
    /// <returns></returns>
    public static async Task<EmailSendResult> SendAsync(this EmailConfig emailConfig, EmailBody emailBody)
    {
        return await EmailSendHelper.SendAsync(emailConfig, emailBody);
    }
}