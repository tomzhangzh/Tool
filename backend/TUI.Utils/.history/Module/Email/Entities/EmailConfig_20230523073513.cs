// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 邮件发送服务器配置
/// </summary>
public class EmailConfig
{
    public EmailConfig()
    { }

    public EmailConfig(string smtpHost, int smtpPort, string emailAccount, string emailPassword, bool isSsl, string displayName)
    {
        EmailSmtpHost = smtpHost;
        EmailSmtpPort = smtpPort;
        EmailAccount = emailAccount;
        EmailPassword = emailPassword;
        EmailIsSsl = isSsl;
        EmailDisplayName = displayName;
    }

    /// <summary>
    /// 邮箱SMTP服务器地址
    /// </summary>
    public string EmailSmtpHost { get; set; }

    /// <summary>
    /// 邮箱SMTP服务器端口
    /// </summary>
    public int EmailSmtpPort { get; set; }

    /// <summary>
    /// 是否启用IsSsl
    /// </summary>
    public bool EmailIsSsl { get; set; }

    ///// <summary>
    ///// 邮件编码
    ///// </summary>
    //public string MailEncoding { get; set; }

    /// <summary>
    /// 邮箱昵称(发送者显示名)
    /// </summary>
    public string EmailDisplayName { get; set; }

    /// <summary>
    /// 邮箱账号
    /// </summary>
    public string EmailAccount { get; set; }

    /// <summary>
    /// 邮箱密码
    /// </summary>
    public string EmailPassword { get; set; }
}