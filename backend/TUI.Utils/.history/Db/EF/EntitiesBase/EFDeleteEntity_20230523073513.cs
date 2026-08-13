// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 删除实体
/// </summary>
[SuppressSniffer]
public class EFDeleteEntity
{
    /// <summary>
    /// 默认假删除
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [Comment("默认假删除")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除用户ID
    /// </summary>
    [Comment("删除用户ID")]
    public long? DeletedUserId { get; set; }

    /// <summary>
    /// 删除用户
    /// </summary>
    [Comment("删除用户")]
    public string? DeletedUserName { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    [Comment("删除时间")]
    public DateTimeOffset? DeletedTime { get; set; }
}