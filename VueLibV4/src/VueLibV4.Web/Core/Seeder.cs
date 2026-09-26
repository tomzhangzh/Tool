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
                    // 属性面板 PCJ（M3）：仅补齐空值，幂等
                    ExecScript(conn, "update-property-config.sql");
                    // 旧库结构增量迁移（幂等）
                    MigrateSchema(conn);
                    return;
                }
            }
            _logger.LogWarning("[Init] ComponentMeta 缺新组件，清空后重跑种子");
            using (var del = conn.CreateCommand()) { del.CommandText = "DELETE FROM ComponentMeta;"; del.ExecuteNonQuery(); }
            ExecScript(conn, "component-meta.sql");
            ExecScript(conn, "seed-extra.sql");
            ExecScript(conn, "update-property-config.sql");
            MigrateSchema(conn);
            return;
        }

        _logger.LogInformation("[Init] 平台库为空，开始执行 SQL 脚本...");
        ExecScript(conn, "platform.sql");
        ExecScript(conn, "component-meta.sql");
        ExecScript(conn, "seed-extra.sql");
        ExecScript(conn, "update-property-config.sql");
        MigrateSchema(conn);
        _logger.LogInformation("[Init] 平台库初始化完成");
    }

    /// <summary>
    /// 旧库增量迁移（SQLite 不支持 IF NOT EXISTS 加列，用 PRAGMA table_info 检查后幂等 ALTER）。
    /// 新库的建表 SQL 已包含这些列，ALTER 会因列已存在而跳过。
    /// </summary>
    private void MigrateSchema(SqliteConnection conn)
    {
        // 旧库补建 PageSetting 表（M4 三屏配置存储；platform.sql 仅空库执行）
        if (!TableExists(conn, "PageSetting"))
        {
            using var create = conn.CreateCommand();
            create.CommandText = @"
CREATE TABLE PageSetting (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Code            TEXT NOT NULL,
    Name            TEXT NOT NULL,
    SettingType     TEXT NOT NULL DEFAULT 'List',
    ProjectId       INTEGER NULL,
    TableName       TEXT NULL,
    ConfigJson      TEXT NULL,
    ColumnDefsJson  TEXT NULL,
    SortNo          INTEGER NOT NULL DEFAULT 0,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    CreateTime      TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);";
            create.ExecuteNonQuery();
            _logger.LogInformation("[Init] 迁移：新建表 PageSetting");
        }
        EnsureColumn(conn, "DynWebPage", "FilterPageSettingId", "INTEGER NULL");
        EnsureColumn(conn, "DynWebPage", "ListPageSettingId", "INTEGER NULL");
        EnsureColumn(conn, "DynWebPage", "DetailPageSettingId", "INTEGER NULL");
        // 系统菜单 SysMenu（桌面快捷方式数据源，树形）；旧库幂等建表 + 初始种子
        if (!TableExists(conn, "SysMenu"))
        {
            using var create = conn.CreateCommand();
            create.CommandText = @"
CREATE TABLE SysMenu (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    ParentId       INTEGER NULL,
    Code           TEXT NULL,
    Name           TEXT NOT NULL,
    Icon           TEXT NULL,
    Url            TEXT NULL,
    TargetType     TEXT NOT NULL DEFAULT 'Iframe',
    IsAddToDesktop INTEGER NOT NULL DEFAULT 1,
    SortNo         INTEGER NOT NULL DEFAULT 0,
    IsActive       INTEGER NOT NULL DEFAULT 1,
    PermissionCode TEXT NULL,
    CreateTime     TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);";
            create.ExecuteNonQuery();
            _logger.LogInformation("[Init] 迁移：新建表 SysMenu");
        }
        EnsureColumn(conn, "SysMenu", "Width", "INTEGER NULL");
        EnsureColumn(conn, "SysMenu", "Height", "INTEGER NULL");
        EnsureColumn(conn, "SysMenu", "IsAddToDesktopRoot", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(conn, "SysMenu", "IsAddToStartMenu", "INTEGER NOT NULL DEFAULT 1");
        SeedSysMenu(conn);
    }

    /// <summary>SysMenu 初始种子（幂等：仅当表为空时插入根菜单与示例子菜单）</summary>
    private void SeedSysMenu(SqliteConnection conn)
    {
        using var cnt = conn.CreateCommand();
        cnt.CommandText = "SELECT COUNT(*) FROM SysMenu;";
        if (Convert.ToInt32(cnt.ExecuteScalar()) > 0) return;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) VALUES
(NULL, 'system', '系统管理', '📁', NULL, 'Iframe', 1, 1, 1, 'system'),
(NULL, 'apps',   '应用中心', '🗂️', NULL, 'Iframe', 1, 2, 1, 'apps'),
(1,   'menu-mgmt',   '菜单管理', '🧭', '/Platform/Mgmt/SysMenuMgmt', 'FullScreen', 1, 1, 1, 'system.menu'),
(1,   'shortcut-mgmt','桌面快捷管理', '🖥️', '/Platform/Mgmt/DesktopShortcut', 'Iframe', 1, 2, 1, 'system.shortcut'),
(2,   'designer', '页面设计器', '🎨', '/Platform/Page/Designer', 'FullScreen', 1, 1, 1, 'apps.designer'),
(2,   'student',  '学生管理', '🎓', '/Platform/Page/WebPageRender?code=student-manage', 'Iframe', 1, 2, 1, 'apps.student');";
        cmd.ExecuteNonQuery();
        _logger.LogInformation("[Init] SysMenu 初始种子插入完成");
    }

    private void EnsureColumn(SqliteConnection conn, string table, string column, string definition)
    {
        if (!TableExists(conn, table)) return;
        using var check = conn.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        var exists = false;
        using (var rd = check.ExecuteReader())
        {
            while (rd.Read())
            {
                if (string.Equals(rd.GetString(1), column, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
            }
        }
        if (exists) return;
        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
        _logger.LogInformation("[Init] 迁移：{Table} 新增列 {Column}", table, column);
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
