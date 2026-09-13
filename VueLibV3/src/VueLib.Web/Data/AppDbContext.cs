using System.Reflection;
using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Data;

/// <summary>
/// Platform 数据库上下文（元数据/平台表）
/// </summary>
public class AppDbContext
{
    private readonly IConfiguration _configuration;
    private readonly string _connStr;

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
        _connStr = configuration.GetConnectionString("Platform")
            ?? throw new InvalidOperationException("未配置连接字符串 ConnectionStrings:Platform");
    }

    public ISqlSugarClient Create()
    {
        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = _connStr,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityService = (property, column) =>
                {
                    // 所有 string 列强制 nvarchar（支持中文 + emoji）
                    if (property.PropertyType == typeof(string))
                    {
                        column.DataType = "nvarchar";
                        if (Nullable.GetUnderlyingType(property.PropertyType) == null &&
                            new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable)
                        {
                            column.IsNullable = true;
                        }
                    }
                }
            }
        },
        db =>
        {
            if (_configuration.GetValue<bool>("Logging:SqlSugar:EnableSqlLog"))
                db.Aop.OnLogExecuting = (sql, pars) => Console.WriteLine($"[SqlSugar] {sql}");
        });
        return db;
    }

    /// <summary>首启动：建库 + 建表 + 种子数据</summary>
    public void EnsureInit()
    {
        using var db = Create();
        db.DbMaintenance.CreateDatabase();
        db.CodeFirst.InitTables(
            typeof(ComponentMeta),
            typeof(DesktopSolution),
            typeof(DesktopShortcut),
            typeof(DynActionHelper),
            typeof(DynDict),
            typeof(DynProject),
            typeof(DynTemplate),
            typeof(DynWebPage)
        );
    }
}

/// <summary>
/// Business 数据库工厂 —— 按 Project 的连接串动态创建 SqlSugarClient
/// 支持无需 model 的动态 CRUD（通过 db.Dynamic / SqlQueryable 拿表结构）
/// </summary>
public class BusinessDbFactory
{
    private readonly IConfiguration _configuration;

    public BusinessDbFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>按连接串创建业务库客户端</summary>
    public ISqlSugarClient Create(string? connectionString)
    {
        var cs = connectionString
            ?? _configuration.GetConnectionString("Business")
            ?? throw new InvalidOperationException("未配置业务库连接串");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = cs,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
        return db;
    }
}
