// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 数据库设置
/// </summary>
public class DbSettingOptions : IConfigurableOptions
{
    /// <summary>
    ///  数据库提供器选项
    /// </summary>
    public string DbProvider { get; set; }
}