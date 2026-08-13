// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

[Comment("租户-数据库配置")]
[Table("Sys_TenantDbConfig")]
public class SysTenantDbConfig : FullEntityNoIdentity, ITenantEntity
{
    /// <summary>
    /// 配置ID
    /// </summary>
    [Comment("配置ID")]
    public string ConfigId { get; set; }

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    [Comment("数据库连接字符串")]
    public string ConnString { get; set; }

    /// <summary>
    /// 数据库类型（为了方便，我们存数据库名称，就不存枚举值了）
    /// MySql = 0,SqlServer = 1,Sqlite = 2,Oracle = 3,PostgreSQL = 4,Dm = 5,Kdbndp = 6,Oscar = 7,Access = 9,OpenGauss = 10,QuestDB = 11,HG = 12,ClickHouse = 13,GBase = 14,Odbc = 0xF,Custom = 900  列出的可能以后会有删减，以 SqlSugar.DbType 枚举类型 为准
    /// </summary>
    [Comment("数据库类型")]
    public string DbType { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [Comment("是否启用")]
    public bool IsEanble { get; set; }

    #region ref

    /// <summary>
    /// 租户ID
    /// </summary>
    [Comment("租户ID")]
    public long TenantId { get; set; }

    #endregion ref
}