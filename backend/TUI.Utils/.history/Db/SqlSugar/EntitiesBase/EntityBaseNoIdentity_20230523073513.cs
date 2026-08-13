// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 数据库实体依赖基类
/// </summary>
[SuppressSniffer]
public abstract class EntityBaseNoIdentity : EntityBaseNoIdentity<long>
{
}

/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EntityBaseNoIdentity<TKey>
{
    /// <summary>
    /// 自增ID
    /// </summary>
    [SugarColumn(ColumnDescription = "ID", IsPrimaryKey = true)]
    public TKey Id { get; set; }
}