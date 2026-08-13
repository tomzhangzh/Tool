// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using MimeKit;
using MimeKit.Text;

namespace TUI.Utils;

/// <summary>
/// 邮件内容实体
/// </summary>
public class EmailBody
{
    public EmailBody()
    { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件内容</param>
    /// <param name="receive">收件箱</param>
    /// <param name="sender">发送者(昵称)</param>
    /// <param name="emailBodyType">邮件内容类型，默认为html</param>
    public EmailBody(string subject, string body, string receive, string sender = "", TextFormat emailBodyType = TextFormat.Html)
    {
        var receiveMails = receive.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var mail in receiveMails)
        {
            Recipients.Add(new MailboxAddress(mail, mail));
        }
        Subject = subject;
        Body = body;

        Sender = sender;
        EmailBodyType = emailBodyType;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件内容</param>
    /// <param name="receive">收件箱</param>
    /// <param name="sender">发送者(昵称)</param>
    /// <param name="emailBodyType">邮件内容类型，默认为html</param>
    public EmailBody(string subject, string body, MailboxAddress receive, string sender = "", TextFormat emailBodyType = TextFormat.Html)
    {
        Subject = subject;
        Body = body;
        Recipients.Add(receive);
        Sender = sender;
        EmailBodyType = emailBodyType;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="subject">邮件主题</param>
    /// <param name="body">邮件内容</param>
    /// <param name="receive">收件箱</param>
    /// <param name="sender">发送者(昵称)</param>
    /// <param name="emailBodyType">邮件内容类型，默认为html</param>
    public EmailBody(string subject, string body, List<MailboxAddress> receives, string sender = "", TextFormat emailBodyType = TextFormat.Html)
    {
        Subject = subject;
        Body = body;
        Recipients.AddRange(receives);
        Sender = sender;
        EmailBodyType = emailBodyType;
    }

    ///// <summary>
    ///// 邮件文本内容
    ///// </summary>
    //public string MailTextBody { get; set; }

    /// <summary>
    /// 邮件内容类型
    /// </summary>
    public TextFormat EmailBodyType { get; set; } = TextFormat.Html;

    /// <summary>
    /// 邮件附件集合
    /// </summary>
    public List<EmailAttachment> EmailAttachments { get; set; } = new List<EmailAttachment>();

    /// <summary>
    /// 收件人
    /// </summary>
    public List<MailboxAddress> Recipients { get; set; } = new List<MailboxAddress>();

    /// <summary>
    /// 抄送
    /// </summary>
    public List<MailboxAddress> Cc { get; set; } = new List<MailboxAddress>();

    /// <summary>
    /// 密送
    /// </summary>
    public List<MailboxAddress> Bcc { get; set; } = new List<MailboxAddress>();

    /// <summary>
    /// 发件人
    /// </summary>
    public string Sender { get; set; }

    ///// <summary>
    ///// 发件人地址
    ///// </summary>
    //public string SenderAddress { get; set; }

    /// <summary>
    /// 邮件主题
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// 邮件内容
    /// </summary>
    public string Body { get; set; }
}