// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using MimeKit;

namespace Abc.Utils;

public class EmailAttachment : IDisposable
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="fullFilePath">文件完整路径</param>
    /// <param name="fileName">文件名</param>
    public EmailAttachment(string fullFilePath, string fileName = "")
    {
        FullFilePath = fullFilePath;
        FileName = fileName;

        //if (string.IsNullOrWhiteSpace(FileName))
        //{
        //    FileName = Path.GetFileName(fullFilePath);
        //}
        //if (string.IsNullOrWhiteSpace(ContentType))
        //{
        //    ContentType = MimeTypes.GetMimeType(fullFilePath);
        //    //var contentTypeArr = fileType.Split('/');
        //    //var contentType = new ContentType(contentTypeArr[0], contentTypeArr[1]);

        //    //return contentType.ToString();
        //}
        if (!File.Exists(fullFilePath))
        {
            throw new ArgumentException(fullFilePath + "文件不存在");
        }
        Stream = File.OpenRead(FullFilePath);
    }

    /// <summary>
    /// 使用指定的内容类型值创建新的MimeKit.MimePart
    /// </summary>
    public ContentType ContentType { get; set; }

    /// <summary>
    ///  邮件附件文件路径  例如：图片 MailFilePath=@"C:\Files\123.png"
    /// </summary>
    public string FullFilePath
    {
        get; set

;
    }

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; }

    public Stream Stream;

    /// <summary>
    /// 释放Stream
    /// </summary>
    void IDisposable.Dispose()
    {
        if (Stream != null)
        {
            Stream.Dispose();
        }
    }
}