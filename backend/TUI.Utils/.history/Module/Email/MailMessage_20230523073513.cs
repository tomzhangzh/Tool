// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using MimeKit;
using MimeKit.Text;

namespace Abc.Utils;

/// <summary>
/// 邮件信息
/// </summary>
public static class MailMessage
{
    /// <summary>
    /// 组装邮件文本/附件邮件信息
    /// </summary>
    /// <param name="emailBody">邮件消息实体</param>
    /// <returns></returns>
    public static MimeMessage AssemblyMailMessage(EmailConfig emailConfig, EmailBody emailBody)
    {
        if (emailBody == null)
        {
            throw new ArgumentNullException(nameof(emailBody));
        }
        var message = new MimeMessage();

        //设置邮件基本信息
        SetMailBaseMessage(emailConfig, message, emailBody);

        var multipart = new Multipart("mixed");

        //插入文本消息
        if (!string.IsNullOrEmpty(emailBody.Body))
        {
            var alternative = new MultipartAlternative
            {
                AssemblyMailTextMessage(emailBody.Body, emailBody.EmailBodyType)
             };
            multipart.Add(alternative);
        }

        //插入附件
        foreach (var emailAttachment in emailBody.EmailAttachments)
        {
            var mimePart = AssemblyMailAttachmentMessage(emailAttachment);
            multipart.Add(mimePart);
        }

        //组合邮件内容
        message.Body = multipart;
        return message;
    }

    /// <summary>
    /// 设置邮件基础信息
    /// </summary>
    /// <param name="minMessag"></param>
    /// <param name="mailBodyEntity"></param>
    /// <returns></returns>
    public static MimeMessage SetMailBaseMessage(EmailConfig emailConfig, MimeMessage minMessag, EmailBody emailBody)
    {
        if (minMessag == null)
        {
            throw new ArgumentNullException();
        }
        if (emailBody == null)
        {
            throw new ArgumentNullException();
        }

        //插入发件人
        if (string.IsNullOrWhiteSpace(emailBody.Sender)) emailBody.Sender = emailConfig.EmailAccount;
        minMessag.From.Add(new MailboxAddress(emailBody.Sender, emailConfig.EmailAccount));

        //插入收件人
        if (emailBody.Recipients.Any())
        {
            foreach (var recipients in emailBody.Recipients)
            {
                minMessag.To.Add(recipients);
            }
        }

        //插入抄送人
        if (emailBody.Cc != null && emailBody.Cc.Any())
        {
            foreach (var cC in emailBody.Cc)
            {
                minMessag.Cc.Add(cC);
            }
        }

        //插入密送人
        if (emailBody.Bcc != null && emailBody.Bcc.Any())
        {
            foreach (var bcc in emailBody.Bcc)
            {
                minMessag.Bcc.Add(bcc);
            }
        }

        //插入主题
        minMessag.Subject = emailBody.Subject;
        return minMessag;
    }

    /// <summary>
    /// 组装邮件文本信息
    /// </summary>
    /// <param name="mailBody">邮件内容</param>
    /// <param name="textPartType">邮件类型(plain,html,rtf,xml)</param>
    /// <returns></returns>
    public static TextPart AssemblyMailTextMessage(string mailBody, TextFormat textPartType)
    {
        if (string.IsNullOrEmpty(mailBody))
        {
            throw new ArgumentNullException();
        }
        //var textBody = new TextPart(textPartType)
        //{
        //    Text = mailBody,
        //};

        //处理查看源文件有乱码问题
        var textBody = new TextPart(textPartType);
        textBody.SetText(Encoding.Default, mailBody);
        return textBody;
    }

    /// <summary>
    /// 组装邮件附件信息
    /// </summary>
    /// <param name="fileAttachmentType">附件类型(image,application)</param>
    /// <param name="fileAttachmentSubType">附件子类型 </param>
    /// <param name="fileAttachmentPath">附件路径</param>
    /// <returns></returns>
    public static MimePart AssemblyMailAttachmentMessage(EmailAttachment emailAttachment)
    {
        //if (string.IsNullOrEmpty(fileAttachmentSubType))
        //{
        //    throw new ArgumentNullException();
        //}
        //if (string.IsNullOrEmpty(fileAttachmentType))
        //{
        //    throw new ArgumentNullException();
        //}
        //if (string.IsNullOrEmpty(fileAttachmentPath))
        //{
        //    throw new ArgumentNullException();
        //}

        if (string.IsNullOrWhiteSpace(emailAttachment.FileName))
        {
            emailAttachment.FileName = Path.GetFileName(emailAttachment.FullFilePath);
        }
        var contentType = MimeTypes.GetMimeType(emailAttachment.FullFilePath);
        // var fileName = Path.GetFileName(fileAttachmentPath);
        var attachment = new MimePart(contentType)
        {
            Content = new MimeContent(emailAttachment.Stream),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            //FileName = fileName,
        };

        //qq邮箱附件文件名中文乱码问题
        //var charset = "GB18030";
        attachment.ContentType.Parameters.Add(Encoding.Default, "name", emailAttachment.FileName);
        attachment.ContentDisposition.Parameters.Add(Encoding.Default, "filename", emailAttachment.FileName);

        //处理文件名过长
        foreach (var param in attachment.ContentDisposition.Parameters)
            param.EncodingMethod = ParameterEncodingMethod.Rfc2047;
        foreach (var param in attachment.ContentType.Parameters)
            param.EncodingMethod = ParameterEncodingMethod.Rfc2047;

        return attachment;
    }

    /// <summary>
    /// 创建邮件日志文件
    /// </summary>
    /// <returns></returns>
    public static string CreateMailLog(EmailConfig emailConfig)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory + "logs\\SendEmailLogs\\";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var logPath = dir + $"{emailConfig.EmailAccount}_{DateTimeOffset.UtcNow:yyyyMMdd}.txt";
        if (File.Exists(logPath)) return logPath;
        var fs = File.Create(logPath);
        fs.Close();
        return logPath;
    }
}