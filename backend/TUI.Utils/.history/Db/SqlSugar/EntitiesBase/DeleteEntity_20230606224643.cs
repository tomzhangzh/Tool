// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 删除实体
/// </summary>
[SuppressSniffer]
public class DeleteEntity : IDeleted
{
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