// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 数据库实体依赖基类
/// </summary>
[SuppressSniffer]
public abstract class EFEntityBase2 : EFEntityBase2<long, MasterDbContextLocator>
{
}

/// <summary>
///数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey> : EFEntityBase2<TKey, MasterDbContextLocator>
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1> : PrivateEntityBase2<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2> : PrivateEntityBase2<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3> : PrivateEntityBase2<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4> : PrivateEntityBase2<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5> : PrivateEntityBase2<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6> : PrivateEntityBase2<TKey>
    where TDbContextLocator1 : class, IDbContextLocator
    where TDbContextLocator2 : class, IDbContextLocator
    where TDbContextLocator3 : class, IDbContextLocator
    where TDbContextLocator4 : class, IDbContextLocator
    where TDbContextLocator5 : class, IDbContextLocator
    where TDbContextLocator6 : class, IDbContextLocator
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7> : PrivateEntityBase2<TKey>
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
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase2<TKey, TDbContextLocator1, TDbContextLocator2, TDbContextLocator3, TDbContextLocator4, TDbContextLocator5, TDbContextLocator6, TDbContextLocator7, TDbContextLocator8> : PrivateEntityBase2<TKey>
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
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class PrivateEntityBase2<TKey> : IPrivateEntity
{
    /// <summary>
    /// 自增ID
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Comment("ID")]
    public TKey Id { get; set; }
}