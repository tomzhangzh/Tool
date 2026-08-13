// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 创建时间字段
/// </summary>
public interface ICreateTenant<T> : ICreate
{
#nullable enable

    /// <summary>
    /// 租户id
    /// </summary>
    T? TenantId { get; set; }
}