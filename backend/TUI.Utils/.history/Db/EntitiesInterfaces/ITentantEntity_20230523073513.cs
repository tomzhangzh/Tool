// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

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