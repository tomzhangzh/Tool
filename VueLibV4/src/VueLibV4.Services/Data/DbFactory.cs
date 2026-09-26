using Microsoft.Extensions.Configuration;
using SqlSugar;

namespace VueLibV4.Services.Data;

/// <summary>
/// 数据库工厂：平台库 PlatformDb + 业务库 BusinessDb。
/// 提供程序由配置 Db:Provider 决定（默认 Sqlite，本地零依赖即可运行；可切换 SqlServer）。
/// </summary>
public class DbFactory
{
    private readonly IConfiguration _config;
    private readonly DbType _dbType;

    public DbFactory(IConfiguration config)
    {
        _config = config;
        var provider = (config["Db:Provider"] ?? "Sqlite").Trim();
        _dbType = provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? DbType.SqlServer
            : DbType.Sqlite;
    }

    /// <summary>当前平台库提供程序</summary>
    public DbType CurrentDbType => _dbType;

    public SqlSugarClient PlatformDb() => NewDb(_config.GetConnectionString("PlatformDb"));

    public SqlSugarClient BusinessDb(string connectionString = null)
        => NewDb(connectionString ?? _config.GetConnectionString("BusinessDb"));

    /// <summary>根据 DynProject 表中的连接串打开指定业务库（按连接串特征推断提供程序）</summary>
    public SqlSugarClient ProjectDb(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("项目未配置连接字符串");
        return NewDb(connectionString);
    }

    private SqlSugarClient NewDb(string connStr)
    {
        // 连接串里显式带 Data Source 且不含 Server= 视为 SQLite
        var dbType = _dbType;
        if (connStr != null && connStr.Contains("Data Source", StringComparison.OrdinalIgnoreCase)
            && !connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            dbType = DbType.Sqlite;
            // SQLite 相对路径（如 Data Source=App_Data/Platform.db）解析为相对应用基目录的绝对路径，
            // 否则 SqlSugar 会按进程工作目录解析，导致库文件位置不一致。
            var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr);
            if (!string.IsNullOrEmpty(csb.DataSource) && !Path.IsPathRooted(csb.DataSource))
            {
                csb.DataSource = Path.Combine(AppContext.BaseDirectory, csb.DataSource.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(csb.DataSource);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                connStr = csb.ConnectionString;
            }
        }
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connStr,
            DbType = dbType,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings
            {
                IsAutoRemoveDataCache = true,
                SqlServerCodeFirstNvarchar = (dbType == DbType.SqlServer)
            }
        });
        return db;
    }
}
