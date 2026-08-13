// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 扩展数据接口
/// </summary>
public interface IExtensionData
{
    /// <summary>
    /// 扩展json数据字段
    /// <c><Dictionary<string,object></c>
    /// </summary>
    Dictionary<string, object>? ExtensionData { get; set; }
}