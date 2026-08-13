// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;

namespace TUI.Utils;

/// <summary>
/// 发送邮件
/// </summary>
public static class EmailSendHelper
{
    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="emailConfig">发件人基础信息</param>
    /// <param name="emailBody">邮件基础信息</param>
    public static async Task<EmailSendResult> SendAsync(EmailConfig emailConfig, EmailBody emailBody)
    {
        if (emailBody == null)
        {
            throw new ArgumentNullException();
        }

        if (emailConfig == null)
        {
            throw new ArgumentNullException();
        }

        var emailSendResult = new EmailSendResult();

        using (var client = new SmtpClient(new ProtocolLogger(MailMessage.CreateMailLog(emailConfig))))
        {
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await ConnectionAsync(client, emailConfig, emailSendResult);

            if (emailSendResult.Success == false)
            {
                return emailSendResult;
            }

            //SmtpClientBaseMessage(client);

            // Note: since we don't have an OAuth2 token, disable
            // the XOAUTH2 authentication mechanism.
            //client.AuthenticationMechanisms.Remove("XOAUTH2");

            await AuthenticateAsync(client, emailConfig, emailSendResult);

            if (emailSendResult.Success == false)
            {
                return emailSendResult;
            }

            var mailMessage = MailMessage.AssemblyMailMessage(emailConfig, emailBody);

            await SendAsync(client, mailMessage, emailSendResult);
            if (emailBody.EmailAttachments != null && emailBody.EmailAttachments.Any())
            {
                foreach (var att in emailBody.EmailAttachments)
                {
                    ((IDisposable)att).Dispose();
                }
            }
            if (emailSendResult.Success == false)
            {
                return emailSendResult;
            }
            client.Disconnect(true);

            emailSendResult.Success = true;//发送邮件成功了
            emailSendResult.Message = "发送成功！";
        }
        return emailSendResult;
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="client">客户端对象</param>
    /// <param name="emailConfig">发送配置</param>
    /// <param name="sendResultEntity">发送结果</param>
    private static async Task ConnectionAsync(SmtpClient client, EmailConfig emailConfig, EmailSendResult emailSendResult)
    {
        try
        {
            if (emailConfig.EmailSmtpPort == 25)
            {
                //25端口不支持ssl/tls，所以连接不上；最后使用Connect(String、Int32、SecureSocketOptions、CancellationToken)重载就可以了
                await client.ConnectAsync(emailConfig.EmailSmtpHost, emailConfig.EmailSmtpPort, SecureSocketOptions.None);
            }
            else
            {
                await client.ConnectAsync(emailConfig.EmailSmtpHost, emailConfig.EmailSmtpPort, emailConfig.EmailIsSsl);
            }
        }
        catch (SmtpCommandException ex)
        {
            emailSendResult.Message = $"尝试连接时出错:{ex.Message}";
            emailSendResult.Success = false;
        }
        catch (SmtpProtocolException ex)
        {
            emailSendResult.Message = $"尝试连接时的协议错误:{ex.Message}";
            emailSendResult.Success = false;
        }
        catch (Exception ex)
        {
            emailSendResult.Message = $"服务器连接错误:{ex.Message}";
            emailSendResult.Success = false;
        }
    }

    /// <summary>
    /// 账户认证
    /// </summary>
    /// <param name="client">客户端对象</param>
    /// <param name="emailConfig">发送配置</param>
    /// <param name="sendResultEntity">发送结果</param>
    private static async Task AuthenticateAsync(SmtpClient client, EmailConfig emailConfig, EmailSendResult emailSendResult)
    {
        try
        {
            await client.AuthenticateAsync(emailConfig.EmailAccount, emailConfig.EmailPassword);
        }
        catch (AuthenticationException ex)
        {
            emailSendResult.Message = $"无效的用户名或密码:{ex.Message}";
            emailSendResult.Success = false;
        }
        catch (SmtpCommandException ex)
        {
            emailSendResult.Message = $"尝试验证错误:{ex.Message}";
            emailSendResult.Success = false;
        }
        catch (SmtpProtocolException ex)
        {
            emailSendResult.Message = $"尝试验证时的协议错误:{ex.Message}";
            emailSendResult.Success = false;
        }
        catch (Exception ex)
        {
            emailSendResult.Message = $"账户认证错误:{ex.Message}";
            emailSendResult.Success = false;
        }
    }

    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="client">客户端对象</param>
    /// <param name="mailMessage">邮件内容</param>
    /// <param name="sendResultEntity">发送结果</param>
    private static async Task SendAsync(SmtpClient client, MimeMessage mailMessage, EmailSendResult emailSendResult)
    {
        try
        {
            await client.SendAsync(mailMessage);
        }
        catch (SmtpCommandException ex)
        {
            switch (ex.ErrorCode)
            {
                case SmtpErrorCode.RecipientNotAccepted:
                    emailSendResult.Message = $"收件人未被接受:{ex.Message}";
                    break;

                case SmtpErrorCode.SenderNotAccepted:
                    emailSendResult.Message = $"发件人未被接受:{ex.Message}";
                    break;

                case SmtpErrorCode.MessageNotAccepted:
                    emailSendResult.Message = $"消息未被接受:{ex.Message}";
                    break;
            }
            emailSendResult.Success = false;
        }
        catch (SmtpProtocolException ex)
        {
            emailSendResult.Message = $"发送消息时的协议错误:{ex.Message}";
            emailSendResult.Success = false;
        }
        catch (Exception ex)
        {
            emailSendResult.Message = $"邮件接收失败:{ex.Message}";
            emailSendResult.Success = false;
        }
    }

    ///// <summary>
    ///// 获取SMTP基础信息
    ///// </summary>
    ///// <param name="client">客户端对象</param>
    ///// <returns></returns>
    //public static EmailServerInformation SmtpClientBaseMessage(SmtpClient client)
    //{
    //    var mailServerInformation = new EmailServerInformation
    //    {
    //        Authentication = client.Capabilities.HasFlag(SmtpCapabilities.Authentication),
    //        BinaryMime = client.Capabilities.HasFlag(SmtpCapabilities.BinaryMime),
    //        Dsn = client.Capabilities.HasFlag(SmtpCapabilities.Dsn),
    //        EightBitMime = client.Capabilities.HasFlag(SmtpCapabilities.EightBitMime),
    //        Size = client.MaxSize
    //    };

    //    return mailServerInformation;
    //}
}