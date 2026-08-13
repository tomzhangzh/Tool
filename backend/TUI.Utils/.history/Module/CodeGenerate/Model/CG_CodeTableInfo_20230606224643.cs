// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 表信息
/// </summary>
public class CG_CodeTableInfo
{
    public CG_CodeGenerateConfig Config { get; set; }

    #region

    /// <summary>
    /// 类名
    /// </summary>
    public string ClassName { get; set; }

    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 表列信息
    /// </summary>
    public List<CG_CodeColumnsInfo> ColumnsInfos { get; set; } = new();

    #endregion

    #region 自定义的

    /// <summary>
    /// 实体名称，可能包含前缀，根据替换定
    /// </summary>
    public string EntityName { get; set; }

    /// <summary>
    /// 应用层实体名称【服务层实体替换，例如：Sys_=Ht,TUI_=Ht或者Sys_=Ht,TUI_=Ht ;  等号前是被替换名称，等号后是替换的新内容】
    /// </summary>
    public string MvcApplicationEntityName { get; set; }

    /// <summary>
    /// 应用层实体名称【服务层实体替换，例如：Sys_=Ht,TUI_=Ht或者Sys_=Ht,TUI_=Ht ;  等号前是被替换名称，等号后是替换的新内容】
    /// </summary>
    public string ApiApplicationEntityName { get; set; }

    /// <summary>
    /// 控制体名称【Sys_=,TUI_=，等号前是被替换名称，等号后是替换的新内容】
    /// </summary>
    public string MvcControllerName { get; set; }

    /// <summary>
    /// 没有前缀的实体名称
    /// </summary>
    public string NoPrefixEntity { get; set; }

    #endregion
}