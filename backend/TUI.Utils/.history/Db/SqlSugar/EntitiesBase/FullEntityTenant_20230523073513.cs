// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Base;

/// <summary>
/// 全部实体，添加租户ID
/// </summary>
public abstract class FullEntityTenant : FullEntity
{
    /// <summary>
    /// 租户id
    /// </summary>
    [Comment("租户id")]
    public virtual long? TenantId { get; set; }
}