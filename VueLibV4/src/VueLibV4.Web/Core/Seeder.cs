using Microsoft.Data.Sqlite;

namespace VueLibV4.Web.Core;

/// <summary>
/// 平台库初始化器（SQLite）：
/// 1. 打开平台 SQLite 库（不存在则自动创建文件）
/// 2. 若 ComponentMeta 表不存在，依次执行 Data/platform.sql、Data/component-meta.sql
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
        var cs = _config.GetConnectionString("PlatformDb");
        if (string.IsNullOrWhiteSpace(cs))
        {
            _logger.LogWarning("[Init] 未配置 PlatformDb 连接串，跳过初始化");
            return;
        }

        // Data Source=App_Data/Platform.db → 相对 ContentRoot
        var builder = new SqliteConnectionStringBuilder(cs);
        var ds = builder.DataSource;
        if (!Path.IsPathRooted(ds))
        {
            var root = AppContext.BaseDirectory;
            builder.DataSource = Path.Combine(root, ds.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
        var finalCs = builder.ConnectionString;

        using var conn = new SqliteConnection(finalCs);
        conn.Open();

        if (TableExists(conn, "ComponentMeta"))
        {
            // 自愈：若种子新增的组件缺失（开发期换表结构/加组件），清空后重跑种子
            using (var chk = conn.CreateCommand())
            {
            // 齐全 = 已有 DynElContainer 且无旧名 ElFormItem
            using (var hasNew = conn.CreateCommand())
            {
                hasNew.CommandText = "SELECT COUNT(*) FROM ComponentMeta WHERE ComponentName='DynElContainer'";
                var has = Convert.ToInt32(hasNew.ExecuteScalar());
                using var hasOld = conn.CreateCommand();
                hasOld.CommandText = "SELECT COUNT(*) FROM ComponentMeta WHERE ComponentName='ElFormItem'";
                var old = Convert.ToInt32(hasOld.ExecuteScalar());
                if (has > 0 && old == 0)
                {
                    _logger.LogInformation("[Init] ComponentMeta 已存在且组件齐全，跳过种子");
                    return;
                }
            }
            }
            _logger.LogWarning("[Init] ComponentMeta 缺新组件，清空后重跑种子");
            using (var del = conn.CreateCommand()) { del.CommandText = "DELETE FROM ComponentMeta;"; del.ExecuteNonQuery(); }
            ExecScript(conn, "component-meta.sql");
            return;
        }

        _logger.LogInformation("[Init] 平台库为空，开始执行 SQL 脚本...");
        ExecScript(conn, "platform.sql");
        ExecScript(conn, "component-meta.sql");
        _logger.LogInformation("[Init] 平台库初始化完成");
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
