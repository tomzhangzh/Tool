using System.Reflection;
using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Data;

/// <summary>
/// SqlSugar 数据库上下文
/// </summary>
public class AppDbContext
{
    private readonly IConfiguration _configuration;

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 创建 SqlSugar 客户端实例（每次请求一个新实例，用完即释放）
    /// </summary>
    public ISqlSugarClient Create()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("未配置连接字符串 DefaultConnection");

        var db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityService = (property, column) =>
                {
                    // 全局约定：可空引用类型映射为可空列
                    if (property.PropertyType == typeof(string) &&
                        Nullable.GetUnderlyingType(property.PropertyType) == null &&
                        new NullabilityInfoContext().Create(property).ReadState == NullabilityState.Nullable)
                    {
                        column.IsNullable = true;
                    }
                }
            }
        },
        db =>
        {
            // SQL 日志（开发环境输出）
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                if (_configuration.GetValue<bool>("Logging:SqlSugar:EnableSqlLog"))
                {
                    Console.WriteLine($"[SqlSugar] {sql}");
                }
            };
        });

        return db;
    }

    /// <summary>
    /// 确保数据库和表已创建（CodeFirst 模式，可选）
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        using var db = Create();
        db.DbMaintenance.CreateDatabase();
        db.CodeFirst.InitTables(typeof(ComponentDefinition));
    }
}
