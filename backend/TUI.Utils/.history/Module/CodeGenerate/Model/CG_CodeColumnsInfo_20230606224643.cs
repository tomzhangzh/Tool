// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 表列信息
/// </summary>
public class CG_CodeColumnsInfo
{
    /// <summary>
    /// 类属性名
    /// </summary>
    public string ClassProperName { get; set; }

    /// <summary>
    /// 数据库列名
    /// </summary>
    public string DbColumnName { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string Description { get; set; }

    public string CodeType { get; set; }

    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 长度
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// 属性类型
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 特殊类型
    /// </summary>
    public bool IsSpecialType { get; set; }

    /// <summary>
    /// 是否是为NULL
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否忽略
    /// </summary>
    public bool IsIgnore { get; set; }
}