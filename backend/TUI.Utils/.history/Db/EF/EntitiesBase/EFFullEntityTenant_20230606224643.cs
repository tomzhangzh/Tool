// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 全部实体，添加租户ID
/// </summary>
public abstract class EFFullEntityTenant : EFFullEntity
{
    /// <summary>
    /// 租户id
    /// </summary>
    [Comment("租户id")]
    public virtual long? TenantId { get; set; }
}