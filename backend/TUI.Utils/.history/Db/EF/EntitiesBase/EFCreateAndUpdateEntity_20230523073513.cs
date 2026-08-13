// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity : EFCreateAndUpdateEntity<long, MasterDbContextLocator>
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey> : EFCreateAndUpdateEntity<TKey, MasterDbContextLocator>
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1> : PrivateCreateAndUpdateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2> : PrivateCreateAndUpdateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateCreateAndUpdateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateCreateAndUpdateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateCreateAndUpdateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : PrivateCreateAndUpdateEntity<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : PrivateCreateAndUpdateEntity<TKey>
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
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFCreateAndUpdateEntity<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : PrivateCreateAndUpdateEntity<TKey>
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
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class PrivateCreateAndUpdateEntity<TKey> : IPrivateEntity, ICreate, IUpdate
{
    /// <summary>
    /// 自增ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Comment("ID")]
    public TKey Id { get; set; }

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
}