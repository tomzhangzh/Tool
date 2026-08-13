// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public static class EmailHelper
{
    /// <summary>
    /// 发送邮箱配置集合
    /// </summary>
    private static readonly ConcurrentDictionary<string, EmailConfig> EmailConfigs = new();

    /// <summary>
    /// 获取指定的配置
    /// </summary>
    /// <param name="userAddress"></param>
    /// <returns></returns>
    public static EmailConfig GetEmalConfig(string userAddress)
    {
        userAddress = userAddress.ToLower().Trim();
        if (EmailConfigs.ContainsKey(userAddress))
        {
            var state = EmailConfigs.TryGetValue(userAddress, out var emailConfig);
            if (state == false) return null;
            return emailConfig;
        }
        return null;
    }

    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="emailAccount">发件邮箱</param>
    /// <param name="emailBody">发送内容</param>
    /// <returns></returns>
    public static async Task<EmailSendResult> SendAsync(string emailAccount, EmailBody emailBody)
    {
        var emailconfig = GetEmalConfig(emailAccount);
        if (emailconfig == null)
        {
            var argumentNullException = new ArgumentNullException("没有初始化邮箱配置");
            throw argumentNullException;
        }

        return await EmailSendHelper.SendAsync(emailconfig, emailBody);
    }

    /// <summary>
    /// 发送默认邮件(仅限于一个发件箱的情况下使用，否则会出现问题)
    /// </summary>
    /// <param name="emailBody">发送内容</param>
    /// <returns></returns>
    public static async Task<EmailSendResult> SendDefaultAsync(EmailBody emailBody)
    {
        if (EmailConfigs.Count > 1)
        {
            var argumentNullException = new ArgumentNullException("发件邮箱配置已存在多个,不允许使用此方法！");
            throw argumentNullException;
        }

        var emailconfig = EmailConfigs.Values.FirstOrDefault();
        if (emailconfig == null) throw new ArgumentNullException("没有初始化邮箱配置");

        return await EmailSendHelper.SendAsync(emailconfig, emailBody);
    }

    /// <summary>
    /// 添加邮件配置
    /// </summary>
    /// <param name="emailConfig"> 邮件配置</param>
    public static void AddEmailConfig(EmailConfig emailConfig)
    {
        #region 验证邮件配置信息

        if (string.IsNullOrWhiteSpace(emailConfig.EmailSmtpHost))
        {
            throw new Exception("邮件服务器Host 不能为空");
        }
        if (emailConfig.EmailSmtpPort <= 0)
        {
            throw new Exception($"邮件服务器Port 设置错误:{emailConfig.EmailSmtpPort}");
        }
        if (string.IsNullOrWhiteSpace(emailConfig.EmailAccount))
        {
            throw new Exception("发送邮件的账号地址 不能为空");
        }

        if (string.IsNullOrWhiteSpace(emailConfig.EmailPassword))
        {
            throw new Exception("发现邮件所需的账号密码 不能为空");
        }

        #endregion 验证邮件配置信息

        emailConfig.EmailAccount = emailConfig.EmailAccount.ToLower().Trim();
        if (!EmailConfigs.ContainsKey(emailConfig.EmailAccount))
        {
            EmailConfigs.AddOrUpdate(emailConfig.EmailAccount, emailConfig);
        }
        else
        {
            var ec = EmailConfigs[emailConfig.EmailAccount];
            ec.EmailSmtpHost = emailConfig.EmailSmtpHost;
            ec.EmailSmtpPort = emailConfig.EmailSmtpPort;
            ec.EmailIsSsl = emailConfig.EmailIsSsl;
            ec.EmailDisplayName = emailConfig.EmailAccount;
            ec.EmailAccount = emailConfig.EmailAccount;
            ec.EmailPassword = emailConfig.EmailPassword;
        }
    }

    /// <summary>
    /// 添加邮件配置
    /// </summary>
    /// <param name="smtpHost"> 邮件服务器Host</param>
    /// <param name="smtpPort">邮件服务器Port</param>
    /// <param name="emailAccount">发送邮件的账号地址</param>
    /// <param name="emailPassword">发现邮件所需的账号密码</param>
    /// <param name="isSsl">邮件服务器是否是ssl</param>
    /// <param name="emailNick">发送邮件的账号昵称</param>
    public static void AddEmailConfig(string smtpHost, int smtpPort, string emailAccount, string emailPassword, bool isSsl = true, string emailNick = "")
    {
        AddEmailConfig(new EmailConfig(smtpHost, smtpPort, emailAccount, emailPassword, isSsl, emailNick));
    }

    /// <summary>
    /// 移除邮箱配置
    /// </summary>
    /// <param name="emailAccount"></param>
    public static void RemoveEmailConfig(string emailAccount)
    {
        emailAccount = emailAccount.ToLower().Trim();
        if (EmailConfigs.ContainsKey(emailAccount)) EmailConfigs.TryRemove(emailAccount, out _);
    }
}