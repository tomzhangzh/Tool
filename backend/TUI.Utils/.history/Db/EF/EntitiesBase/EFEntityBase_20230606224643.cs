// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 数据库实体依赖基类
/// </summary>
[SuppressSniffer]
public abstract class EFEntityBase : EFEntityBase<long>
{

}
/// <summary>
/// 数据库实体依赖基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class EFEntityBase<TKey>
{
    /// <summary>
    /// 自增ID
    /// </summary>
    [SugarColumn(ColumnDescription = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public TKey Id { get; set; }
}