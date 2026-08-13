// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 完整的基础实体创建、更新、删除实体
/// </summary>
[SuppressSniffer]
public abstract class EFFullEntityNoKey : EFFullEntityNoKey<long, MasterDbContextLocator>
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey> : EFFullEntityNoKey<TKey, MasterDbContextLocator>
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
    where TDbContextLocator7 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFFullEntityNoKey<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : PrivateFullEntityNoKey<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
    where TDbContextLocator7 : class, IDbContextLocator
    where TDbContextLocator8 : class, IDbContextLocator
{
}

/// <summary>
///创建、更新、删除实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class PrivateFullEntityNoKey<TKey> : IPrivateEntity, ICreate, IUpdate, IDeleted
{
    /// <summary>
    /// 创建用户ID
    /// </summary>
    [Comment("创建用户ID")]
    public long? CreatorUserId { get; set; }

    /// <summary>
    /// 创建用户
    /// </summary>
    [Comment("创建用户")]
    public string? CreatorUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Comment("创建时间")]
    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 更新用户ID
    /// </summary>
    [Comment("更新用户ID")]
    public long? UpdateUserId { get; set; }

    /// <summary>
    /// 更新用户
    /// </summary>
    [Comment("更新用户")]
    public string? UpdateUserName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [Comment("更新时间")]
    public DateTimeOffset? UpdateTime { get; set; }

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