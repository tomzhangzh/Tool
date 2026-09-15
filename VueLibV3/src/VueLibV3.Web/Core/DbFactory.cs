using SqlSugar;

namespace VueLibV3.Web.Core;

/// <summary>
/// SqlSugar 数据库工厂：
/// PlatformDb（平台库） + BusinessDb（业务库，可通过 DynProject 切换任意连接串）
/// </summary>
public class DbFactory
{
    private readonly IConfiguration _config;
    public DbFactory(IConfiguration config) => _config = config;

    public SqlSugarClient PlatformDb() => NewDb(_config.GetConnectionString("PlatformDb"));

    public SqlSugarClient BusinessDb(string connectionString = null)
        => NewDb(connectionString ?? _config.GetConnectionString("BusinessDb"));

    /// <summary>根据 DynProject 表中的连接串打开指定业务库</summary>
    public SqlSugarClient ProjectDb(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("项目未配置连接字符串");
        return NewDb(connectionString);
    }

    private static SqlSugarClient NewDb(string connStr)
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings
            {
                IsAutoRemoveDataCache = true,
                SqlServerCodeFirstNvarchar = true
            }
        });
        return db;
    }
}
