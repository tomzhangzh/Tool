// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 数据库配置
/// </summary>
public class SqlSugarDbTableConfig
{
    /// <summary>
    /// 数据库-连接字符串（为空表示，使用本项目数据库配置文件的第一个数据库去连接）
    /// </summary>
    [Description("数据库-连接字符串（为空表示，使用本项目数据库配置文件的第一个数据库去连接）")]
    public string DbConn { get; set; } = "";

    /// <summary>
    /// 数据库类型（sqlsugar）
    /// </summary>
    [Description("数据库类型（sqlsugar）")]
    public SqlSugar.DbType DbType { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    [Description("数据库名称")]
    public string DatabaseName { get; set; }
    /// <summary>
    /// 数据库目录（绝对路径）
    /// </summary>
    [Description("数据库目录（绝对路径）")]
    public string DatabaseDirectory { get; set; }
}