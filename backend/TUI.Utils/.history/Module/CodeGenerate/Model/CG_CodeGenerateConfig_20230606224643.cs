// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 代码生成配置
/// </summary>
public class CG_CodeGenerateConfig
{
    /// <summary>
    /// 项目名称
    /// </summary>
    [Description("项目名称")]
    public string ProjectName { get; set; } = "TUI";

    /// <summary>
    /// 数据库-连接字符串（为空表示，使用本项目数据库配置文件的第一个数据库去连接）
    /// </summary>
    [Description("数据库-连接字符串（为空表示，使用本项目数据库配置文件的第一个数据库去连接）")]
    public string DbConn { get; set; } = "";

    /// <summary>
    /// 数据库类型（sqlsugar）
    /// </summary>
    [Description("数据库类型（sqlsugar）")]
    public string DbType { get; set; }

    #region DB

    /// <summary>
    /// 实体-表前缀替换
    /// </summary>
    [Description("实体-表前缀替换")]
    public string DbTablePrefixReplace { get; set; } = "Sys";

    /// <summary>
    /// 实体-表名字符串替换，多个用英文逗号隔开
    /// </summary>
    [Description("实体-表名字符串替换")]
    public string DbTableReplace { get; set; } = "Sys=Sys,Sys_=Sys";

    /// <summary>
    /// 实体-表名去前缀，多个前缀用英文逗号隔开
    /// </summary>
    [Description("实体-表名去前缀")]
    public string DbTableNoPrefix { get; set; } = "Sys,Sys_";

    #endregion DB

    #region 应用层

    /// <summary>
    ///  应用层-区域
    /// </summary>
    [Description("应用层-区域")]
    public string MvcApplicationArea { get; set; } = "HtAdmin";

    /// <summary>
    ///  应用层-实体替换，例如：Sys_=Ht,APC_=Ht或者Sys_=Ht,APC_=Ht
    /// </summary>
    [Description("应用层-实体替换，例如：Sys=Ht,APC_=Ht或者Sys_=Ht,APC_=Ht")]
    public string ApplicationEntityReplace { get; set; } = "Sys=,Sys_=";

    /// <summary>
    /// 应用层-Dto实体前缀
    /// </summary>
    [Description("应用层-Dto实体前缀")]
    public string MvcApplicationDtoPrefix { get; set; } = "Ht";

    #endregion 应用层

    #region 控制器Controller

    /// <summary>
    /// MVC控制器-命名空间
    /// </summary>
    [Description("MVC控制器-命名空间")]
    public string MvcControllerNameSpaceName { get; set; } = "TUI.Web.Mvc.Areas.HtAdmin.Controllers";

    /// <summary>
    /// MVC控制器-继承的基础类库
    /// </summary>
    [Description("MVC控制器-继承的基础类库")]
    public string MvcControllerBaseName { get; set; } = "HtAdminBaseController";

    /// <summary>
    /// MVC控制器-区域
    /// </summary>
    [Description("MVC控制器-区域")]
    public string MvcControllerArea { get; set; } = "HtAdmin";

    /// <summary>
    /// MVC控制器-实体替换，等号前是被替换名称，等号后是替换的新内容
    /// </summary>
    [Description("MVC控制器-实体替换，等号前是被替换名称，等号后是替换的新内容")]
    public string MvcControllerEntityReplace { get; set; } = "Sys=,Sys_";

    /// <summary>
    /// MVC控制器-实体前缀
    /// </summary>
    [Description("MVC控制器-实体前缀")]
    public string MvcControllerEntityPrefix { get; set; } = "Ht";

    #endregion 控制器Controller

    #region API

    /// <summary>
    /// API 区域
    /// </summary>
    [Description("API 区域")]
    public string ApiArea { get; set; } = "Api";

    /// <summary>
    /// Api控制器-前缀
    /// </summary>
    [Description("Api控制器-前缀")]
    public string ApiControllerEntityPrefix { get; set; } = "Api";

    /// <summary>
    /// Api Dto-实体前缀
    /// 包括Input,Out,Query
    /// </summary>
    [Description("Api控制器-实体前缀")]
    public string ApiDtoEntityPrefix { get; set; } = "Api";

    #endregion API
}