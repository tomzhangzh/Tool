// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 代码模板
/// </summary>
public class CG_CodeTemplate
{
    //public CodeTemplate(CodeTemplateType type, string templateName, string outFileExtensionName = ".cs")
    //{
    //    this.TemplateName = templateName;
    //    Type = type;
    //    this.OutFileExtensionName = outFileExtensionName;
    //}

    public CG_CodeTemplate(CodeTemplateType type, string templateName, string saveRelativeDirectory, string outFileExtensionName = ".cs", string outFileName = "")
    {
        this.TemplateName = templateName;
        this.SaveRelativeDirectory = saveRelativeDirectory;
        this.Type = type;
        this.OutFileExtensionName = outFileExtensionName;
        if (!string.IsNullOrWhiteSpace(outFileName))
        {
            this.OutFileName = outFileName;
        }
        else
        {
            this.OutFileName = templateName;
        }
    }

    public CodeTemplateType Type { get; set; }

    /// <summary>
    /// 输出文件扩展名
    /// </summary>
    public string OutFileExtensionName { get; set; } = ".cs";

    /// <summary>
    /// 输出文件名
    /// </summary>
    public string OutFileName { get; set; }

    /// <summary>
    /// 保存文件相对目录
    /// </summary>
    public string TemplateName { get; set; }

    /// <summary>
    /// 保存相对目录
    /// </summary>
    public string SaveRelativeDirectory { get; set; }
}

/// <summary>
/// 代码模板类型
/// </summary>
public enum CodeTemplateType
{
    MVC = 0,
    WebAPI = 1,
}