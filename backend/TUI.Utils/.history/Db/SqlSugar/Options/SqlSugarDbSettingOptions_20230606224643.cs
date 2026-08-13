// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// SqlSugar 默认设置
/// </summary>
public class SqlSugarDbSettingOptions : IConfigurableOptionsListener<SqlSugarDbSettingOptions>
{
    /// <summary>
    /// 雪花ID
    /// </summary>
    public int SnowFlakeSingle_WorkId { get; set; } = 1;

    /// <summary>
    /// 实体数据库是否自增
    /// </summary>
    public bool EntityIsIdentity { get; set; } = true;

    /// <summary>
    /// 主键不自增的数据库表名
    /// </summary>
    public List<string> NoIsIdentityDbTables { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DataBaseName { get; set; }

    /// <summary>
    /// 数据库创建路径
    /// </summary>
    public string DatabaseDirectory { get; set; }

    /// <summary>
    /// 数据库表名 驼峰转下划线，pgsql，oracle需要转，sqlserver可以不转，转了后代码生成可正常
    /// </summary>
    public bool EnableDbTableNameToUnderLine { get; set; }

    /// <summary>
    /// 数据库表列名 驼峰转下划线，pgsql，oracle需要转，sqlserver可以不转，转了后代码生成可正常
    /// </summary>
    public bool EnableDbColumnNameToUnderLine { get; set; }

    /// <summary>
    /// 是否初始化数据库
    /// </summary>
    public bool EnableInitDatabase { get; set; }

    /// <summary>
    /// 是否初始化数据种子
    /// </summary>
    public bool EnableInitDataSeed { get; set; }

    /// <summary>
    /// 字符串是否默认可空
    /// </summary>
    public bool StringIsNullable { get; set; } = true;

    ///// <summary>
    ///// 字符串默认长度
    ///// </summary>
    //public int StringDefaultLength { get; set; } = 500;

    /// <summary>
    /// 是否启用了租户，启用了租户会自动生成配置表：TenantDbConfig
    /// </summary>
    public bool EnableTanant { get; set; } = false;

    ///// <summary>
    ///// 实体命名空间前缀,数组，可多个
    ///// </summary>
    //public List<string> EntitiesNamespacePrefixs { get; set; }

    ///// <summary>
    ///// 实体所在程序集，可配置多个
    ///// </summary>
    //public List<string> EntitiesAssemblys { get; set; }

    /// <summary>
    /// 是否记录执行的sql
    /// </summary>
    public bool IsRecordExecSql { get; set; }

    /// <summary>
    /// 启用库表差异日志
    /// </summary>
    public bool EnableDiffLog { get; set; }

    /// <summary>
    /// 是否启用 Yitter.IdGenerator 的雪花ID
    /// 比较短点
    /// </summary>
    public bool EnableYitterIdGenerator { get; set; }

    /// <summary>
    /// 数据库配置集合
    /// </summary>
    public List<SqlSugar.ConnectionConfig> ConnectionConfigs { get; set; }

    public void OnListener(SqlSugarDbSettingOptions options, IConfiguration configuration)
    {
        //Name = options.Name;  // 实时的最新值
        //Version = options.Version;  // 实时的最新值
        //Company = options.Company;//公司

        Type entityType = GetType();//获得该类的Type

        var dics = options.GetDictionary();
        foreach (var item in dics)
        {
            PropertyInfo? propertyInfo = entityType.GetProperty(item.Key);
            propertyInfo?.FieldSetValue(item.Key, item.Value as object);
        }
    }

    public void PostConfigure(SqlSugarDbSettingOptions options, IConfiguration configuration)
    {
    }
}