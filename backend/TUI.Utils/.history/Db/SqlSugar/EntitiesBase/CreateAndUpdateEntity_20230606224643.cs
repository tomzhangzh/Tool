// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 创建和更新实体
/// </summary>
[SuppressSniffer]
public abstract class CreateAndUpdateEntity : CreateAndUpdateEntity<long>
{
}

/// <summary>
/// 创建和更新实体
/// </summary>
/// <typeparam name="TKey"></typeparam>
[SuppressSniffer]
public abstract class CreateAndUpdateEntity<TKey> : ICreate, IUpdate
{
    /// <summary>
    /// 自增ID
    /// </summary>
    [SugarColumn(ColumnDescription = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public TKey Id { get; set; }

    /// <summary>
    /// 创建用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "创建用户ID", IsOnlyIgnoreUpdate = true)]
    public long? CreatorUserId { get; set; }

    /// <summary>
    /// 创建用户
    /// </summary>
    [SugarColumn(ColumnDescription = "创建用户", IsNullable = true, IsOnlyIgnoreUpdate = true)]
    public string? CreatorUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnDescription = "创建时间", IsOnlyIgnoreUpdate = true)]
    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 更新用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "更新用户ID", IsOnlyIgnoreInsert = true)]
    public long? UpdateUserId { get; set; }

    /// <summary>
    /// 更新用户
    /// </summary>
    [SugarColumn(ColumnDescription = "更新用户", IsNullable = true, IsOnlyIgnoreInsert = true)]
    public string? UpdateUserName { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnDescription = "更新时间", IsOnlyIgnoreInsert = true)]
    public DateTimeOffset? UpdateTime { get; set; }
}