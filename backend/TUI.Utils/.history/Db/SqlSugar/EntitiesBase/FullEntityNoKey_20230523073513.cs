// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
///创建、更新、删除实体
/// </summary>
[SuppressSniffer]
public abstract class FullEntityNoKey : ICreate, IUpdate, IDeleted
{
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

    /// <summary>
    /// 默认假删除
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [SugarColumn(ColumnDescription = "默认假删除", DefaultValue = "false")]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "删除用户ID", IsOnlyIgnoreInsert = true)]
    public long? DeletedUserId { get; set; }

    /// <summary>
    /// 删除用户
    /// </summary>
    [SugarColumn(ColumnDescription = "删除用户", IsNullable = true, IsOnlyIgnoreInsert = true)]
    public string? DeletedUserName { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    [SugarColumn(ColumnDescription = "删除时间", IsOnlyIgnoreInsert = true)]
    public DateTimeOffset? DeletedTime { get; set; }
}