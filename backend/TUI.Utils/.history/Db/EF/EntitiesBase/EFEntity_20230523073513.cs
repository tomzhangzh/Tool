// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 数据库实体依赖基类（禁止外部继承）
/// </summary>
[SuppressSniffer]
public abstract class EFEntity : EFEntity<long>
{

}

/// <summary>
/// 数据库实体依赖基类（禁止外部继承）
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
[SuppressSniffer]
public abstract class EFEntity<TKey>
{
    /// <summary>
    /// 自增ID
    /// </summary>
    [SugarColumn(ColumnDescription = "ID", IsPrimaryKey = true, IsIdentity = true)]
    public TKey Id { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public virtual DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public virtual DateTimeOffset? UpdatedTime { get; set; }

    /// <summary>
    /// 软删除
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public virtual bool IsDeleted { get; set; }
}