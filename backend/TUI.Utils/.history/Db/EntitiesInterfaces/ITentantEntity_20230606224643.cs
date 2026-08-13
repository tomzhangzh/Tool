// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 租户接口
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// 租户id
    /// </summary>
    long TenantId { get; set; }
}