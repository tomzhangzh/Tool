using Furion.ConfigurableOptions;
using System.Collections.Generic;

namespace TUI.Core;

// </summary>
public enum CacheType
{
    /// <summary>
    /// 内存缓存
    /// </summary>
    MemoryCache,

    /// <summary>
    /// Redis缓存
    /// </summary>
    RedisCache
}

/// <summary>
/// 缓存配置
/// </summary>
public class CacheOptions : IConfigurableOptions
{
    /// <summary>
    /// 缓存类型
    /// </summary>
    public CacheType CacheType { get; set; }

    /// <summary>
    /// Redis配置
    /// </summary>
    public string RedisConnectionString { get; set; }
}
/// <summary>
/// 雪花Id配置
/// </summary>
public class SnowIdOptions : IConfigurableOptions
{
    /// <summary>
    /// 取值范围0~63,默认1
    /// </summary>
    public string WorkerId { get; set; } = "1";
}

/// <summary>
/// 数据库配置
/// </summary>
public class ConnectionStringsOptions : IConfigurableOptions
{
    /// <summary>
    /// 默认数据库编号
    /// </summary>
    public string DefaultDbNumber { get; set; }
    /// <summary>
    /// 默认数据库类型
    /// </summary>
    public string DefaultDbType { get; set; }
    /// <summary>
    /// 默认数据库连接字符串
    /// </summary>

    public string DefaultDbString { get; set; }
    /// <summary>
    /// 业务库集合
    /// </summary>
    public List<DbConfig> DbConfigs { get; set; }
}
/// <summary>
/// JWT配置
/// </summary>
public class JWTSettingsOptions : IConfigurableOptions
{
    /// <summary>
    /// 是否验证密钥
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; }
    /// <summary>
    /// 密钥
    /// </summary>
    public string IssuerSigningKey { get; set; }
    /// <summary>
    /// 是否验证签发方
    /// </summary>
    public bool ValidateIssuer { get; set; }
    /// <summary>
    /// 签发方
    /// </summary>
    public string ValidIssuer { get; set; }
    /// <summary>
    /// 是否验证签收方
    /// </summary>
    public bool ValidateAudience { get; set; }
    /// <summary>
    /// 签收方
    /// </summary>
    public string ValidAudience { get; set; }
    /// <summary>
    /// 是否验证过期时间
    /// </summary>
    public bool ValidateLifetime { get; set; }
    /// <summary>
    /// 过期时间
    /// </summary>
    public long ExpiredTime { get; set; }
    /// <summary>
    /// 过期时间容错值
    /// </summary>
    public long ClockSkew { get; set; }
}
/// <summary>
/// 数据库参数
/// </summary>
public class DbConfig
{
    /// <summary>
    /// 数据库编号
    /// </summary>
    public string DbNumber { get; set; }
    /// <summary>
    /// 数据库类型
    /// </summary>
    public string DbType { get; set; }
    /// <summary>
    /// 数据库连接字符串
    /// </summary>

    public string DbString { get; set; }
}


