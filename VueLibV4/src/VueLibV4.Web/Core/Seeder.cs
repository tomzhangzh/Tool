using Microsoft.Data.Sqlite;

namespace VueLibV4.Web.Core;

/// <summary>
/// 数据库初始化器（SQLite）：
/// 1. 平台库 PlatformDb：不存在则依次执行 Data/platform.sql、Data/component-meta.sql；
///    Data/seed-extra.sql 为增量追加种子（项目/字典/快捷方式/网页），幂等，每次启动都执行。
/// 2. 业务库 BusinessDb：不存在则创建，并幂等执行 Data/business.sql（Student/Course/BusinessDict + 演示数据）。
/// 不使用 SeedData JSON，全部以 SQL 脚本执行。
/// </summary>
public class Seeder
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private readonly string _dataDir;

    public Seeder(IConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
    }

    public void Run()
    {
        SeedPlatform();
        SeedBusiness();
    }

    // ---------------- 平台库 ----------------

    private void SeedPlatform()
    {
        var cs = _config.GetConnectionString("PlatformDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _logger.LogWarning("[Init] 未配置 PlatformDb 连接串，跳过平台库初始化");
            return;
        }

        using var conn = OpenSqlite(cs);
        conn.Open();

        if (TableExists(conn, "ComponentMeta"))
        {
            // 自愈：若种子新增的组件缺失（开发期换表结构/加组件），清空后重跑种子
            using (var hasNew = conn.CreateCommand())
            {
                hasNew.CommandText = "SELECT COUNT(*) FROM ComponentMeta WHERE ComponentName='DynElContainer'";
                var has = Convert.ToInt32(hasNew.ExecuteScalar());
                using var hasOld = conn.CreateCommand();
                hasOld.CommandText = "SELECT COUNT(*) FROM ComponentMeta WHERE ComponentName='ElFormItem'";
                var old = Convert.ToInt32(hasOld.ExecuteScalar());
                if (has > 0 && old == 0)
                {
                    _logger.LogInformation("[Init] ComponentMeta 已存在且组件齐全");
                    // 增量追加种子仍需执行（新增项目/字典/快捷方式幂等补齐）
                    ExecScript(conn, "seed-extra.sql");
                    return;
                }
            }
            _logger.LogWarning("[Init] ComponentMeta 缺新组件，清空后重跑种子");
            using (var del = conn.CreateCommand()) { del.CommandText = "DELETE FROM ComponentMeta;"; del.ExecuteNonQuery(); }
            ExecScript(conn, "component-meta.sql");
            ExecScript(conn, "seed-extra.sql");
            return;
        }

        _logger.LogInformation("[Init] 平台库为空，开始执行 SQL 脚本...");
        ExecScript(conn, "platform.sql");
        ExecScript(conn, "component-meta.sql");
        ExecScript(conn, "seed-extra.sql");
        _logger.LogInformation("[Init] 平台库初始化完成");
    }

    // ---------------- 业务库 ----------------

    private void SeedBusiness()
    {
        var cs = _config.GetConnectionString("BusinessDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _logger.LogWarning("[Init] 未配置 BusinessDb 连接串，跳过业务库初始化");
            return;
        }
        using var conn = OpenSqlite(cs);
        conn.Open();
        // business.sql 全部为 CREATE TABLE IF NOT EXISTS + WHERE NOT EXISTS 幂等语句，每次启动执行安全
        ExecScript(conn, "business.sql");
        _logger.LogInformation("[Init] 业务库初始化完成");
    }

    // ---------------- 公共 ----------------

    /// <summary>打开 SQLite 连接；相对路径 Data Source 基于程序目录解析并确保目录存在</summary>
    private SqliteConnection OpenSqlite(string cs)
    {
        var builder = new SqliteConnectionStringBuilder(cs);
        var ds = builder.DataSource;
        if (!Path.IsPathRooted(ds))
        {
            builder.DataSource = Path.Combine(
                AppContext.BaseDirectory,
                ds.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        return new SqliteConnection(builder.ConnectionString);
    }

    private static bool TableExists(SqliteConnection conn, string table)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$t";
        cmd.Parameters.AddWithValue("$t", table);
        return cmd.ExecuteScalar() != null;
    }

    private void ExecScript(SqliteConnection conn, string fileName)
    {
        var path = Path.Combine(_dataDir, fileName);
        if (!File.Exists(path))
        {
            _logger.LogWarning("[Init] 未找到脚本 {File}", fileName);
            return;
        }
        var sql = File.ReadAllText(path, System.Text.Encoding.UTF8);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
        _logger.LogInformation("[Init] 执行脚本 {File} 完成", fileName);
    }
}
