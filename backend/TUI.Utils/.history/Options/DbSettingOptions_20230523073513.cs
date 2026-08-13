// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

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