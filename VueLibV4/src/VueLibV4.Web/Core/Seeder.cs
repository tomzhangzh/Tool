using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace VueLibV4.Web.Core;

/// <summary>
/// 数据库初始化器（SQLite）：平台库不存在则执行 Data/platform.sql、Data/seeds/*.sql；
/// seed-extra.sql 为增量追加种子，business.sql 为业务库建表与演示数据，均幂等。
/// 全部以 SQL 脚本执行，不使用 SeedData JSON。
/// </summary>
public class Seeder
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private readonly string _dataDir;
    private readonly string _seedsDir;

    public Seeder(IConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        _seedsDir = Path.Combine(_dataDir, "seeds");
    }

    public void Run()
    {
        SeedPlatform();
        SeedBusiness();
    }

    /// <summary>在单事务内执行初始化：任何一步失败整体回滚，绝不允许留下"半迁移"库带病启动。</summary>
    private void WithTransaction(SqliteConnection conn, Action<SqliteConnection> work)
    {
        ExecInline(conn, "BEGIN IMMEDIATE;");
        try
        {
            work(conn);
            ExecInline(conn, "COMMIT;");
        }
        catch
        {
            try { ExecInline(conn, "ROLLBACK;"); } catch { /* 连接已失效时无需再回滚 */ }
            throw;
        }
    }

    private static void ExecInline(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
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

        WithTransaction(conn, SeedPlatformCore);
        _logger.LogInformation("[Init] 平台库初始化完成");
    }

    private void SeedPlatformCore(SqliteConnection conn)
    {
        // 0) 全新库：先建全部表
        if (!TableExists(conn, "ComponentMeta"))
        {
            _logger.LogInformation("[Init] 平台库为空，开始执行 platform.sql ...");
            ExecScript(conn, "platform.sql");
        }

        // 1) 列改名 / 补列（旧库迁移；必须早于引用这些列的种子脚本）
        EnsureTemplateDefaultJson(conn);
        EnsureWebPageParamsJson(conn);
        EnsureColumn(conn, "DynWebPage", "ExtViewPath", "TEXT NULL");
        EnsureColumn(conn, "DynWebPage", "SpecJson", "TEXT NULL");
        EnsureColumn(conn, "DynWebPage", "ResourceKey", "TEXT NULL");

        // 2) 结构迁移（建表）+ 内置种子（Data/seeds 按文件名顺序执行，全部幂等）。
        //    必须先于 seed-extra.sql 执行：该脚本会向 DynSchemaLabel 等"迁移期才建"的表写数据。
        MigrateSchema(conn);

        // 2.1) 清理历史坏占位 DB 动作（platform.sql 只在全新库执行，旧库在此幂等清理）
        RunSeed(conn, "33-cleanup-legacy-action-helpers.sql", () => TableExists(conn, "DynActionHelper"));

        // 3) ComponentMeta：空表才灌全量基线。新增组件一律由 seed-extra.sql 幂等补齐，
        //    绝不 DELETE 重灌（会抹掉用户手工注册的组件）。
        using (var cnt = conn.CreateCommand())
        {
            cnt.CommandText = "SELECT COUNT(*) FROM ComponentMeta;";
            if (Convert.ToInt32(cnt.ExecuteScalar()) == 0)
            {
                ExecScript(conn, "component-meta.sql");
            }
            else
            {
                _logger.LogInformation("[Init] ComponentMeta 已有数据，跳过基线脚本（增量由 seed-extra 幂等补齐）");
            }
        }

        // 4) 增量追加种子（新增项目/字典/快捷方式/组件，幂等，每次启动执行）
        ExecScript(conn, "seed-extra.sql");
        // 属性面板 PropertyConfigJson：仅补齐空值，幂等
        ExecScript(conn, "update-property-config.sql");
    }

    /// <summary>
    /// 旧库增量迁移（SQLite 不支持 IF NOT EXISTS 加列，用 PRAGMA table_info 检查后幂等 ALTER）。
    /// 建表/种子脚本位于 Data/seeds，编号即执行顺序；新库的建表 SQL 已包含这些列，ALTER 会因列已存在而跳过。
    /// </summary>
    private void MigrateSchema(SqliteConnection conn)
    {
        // 旧库补建 PageSetting 表（M4 三屏配置存储；platform.sql 仅空库执行）
        RunSeed(conn, "01-schema-pagesetting.sql", () => !TableExists(conn, "PageSetting"));
        EnsureColumn(conn, "PageSetting", "RenderMode", "TEXT NOT NULL DEFAULT 'Front'");
        EnsureColumn(conn, "PageSetting", "PartialPath", "TEXT NULL");
        EnsureColumn(conn, "PageSetting", "GeneratedViewPath", "TEXT NULL");
        EnsureColumn(conn, "PageSetting", "DefaultJson", "TEXT NULL");

        // 系统菜单 SysMenu（桌面快捷方式数据源，树形）。
        // Width/Height 支持像素("800")与百分比("50%")，必须是 TEXT（与模型 string? 及 platform.sql 一致）。
        // 历史迁移曾误补为 INTEGER：SQLite 类型亲和下"50%"按文本存储、"800"按整数存储，均可被 string 读出，
        // 不做表重建；仅保证【新迁移补列】一律 TEXT。
        RunSeed(conn, "02-schema-sysmenu.sql", () => !TableExists(conn, "SysMenu"));
        EnsureColumn(conn, "SysMenu", "Width", "TEXT NULL");
        EnsureColumn(conn, "SysMenu", "Height", "TEXT NULL");
        EnsureColumn(conn, "SysMenu", "IsAddToDesktopRoot", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(conn, "SysMenu", "IsAddToStartMenu", "INTEGER NOT NULL DEFAULT 1");
        RunSeed(conn, "03-seed-sysmenu-root.sql", () => Count(conn, "SELECT COUNT(*) FROM SysMenu;") == 0);
        RunSeed(conn, "04-seed-sysmenu-engine.sql", () => Count(conn, "SELECT COUNT(*) FROM SysMenu WHERE Code='platform-engine';") == 0);
        // 解决方案页面（幂等补插：独立于首次种子，每次启动检查）
        RunSeed(conn, "05-seed-sysmenu-solution.sql");
        RunSeed(conn, "06-seed-sysmenu-rpc.sql", () => Count(conn, "SELECT COUNT(*) FROM SysMenu WHERE Code='demo-rpc';") == 0);

        // DynTemplate 外壳视图路径列（旧库幂等补齐；新库 platform.sql 已包含）
        EnsureColumn(conn, "DynTemplate", "ViewPath", "TEXT NULL");
        EnsureColumn(conn, "DynTemplate", "SupportGen", "INTEGER NOT NULL DEFAULT 0");
        // DynWebPage 实例扩展视图路径列（页面级扩展：真实 cshtml，具名槽位 + dynconfig-ext 脚本合并）
        EnsureColumn(conn, "DynWebPage", "ExtViewPath", "TEXT NULL");
        // 布局壳规格列（壳模型 v1.1：spec 与老模板 ParamsJson 分家；旧库幂等补列，空值回退 ParamsJson）
        EnsureColumn(conn, "DynWebPage", "SpecJson", "TEXT NULL");
        EnsureColumn(conn, "DynWebPage", "ResourceKey", "TEXT NULL");

        var hasTemplate = TableExists(conn, "DynTemplate");
        var hasWebPage = TableExists(conn, "DynWebPage");

        // 表结构显示名字典（PageGen 中文名来源）+ 通用列名层种子
        RunSeed(conn, "07-schema-schema-label.sql", () => !TableExists(conn, "DynSchemaLabel"));
        RunSeed(conn, "08-seed-schema-label-common.sql");
        // 页面模板种子 + 页面实例种子（demo：页面设置管理 / 页面实例列表）
        RunSeed(conn, "09-seed-template-crud-basic.sql",
            () => hasTemplate && Count(conn, "SELECT COUNT(*) FROM DynTemplate WHERE Code='crud-basic';") == 0);
        RunSeed(conn, "10-seed-webpage-demo.sql",
            () => hasTemplate && hasWebPage && Count(conn, "SELECT COUNT(*) FROM DynWebPage WHERE Code='page-setting-mgmt';") == 0);
        // 幂等自愈：演示页 Url 回填自身 Id；setting-mgmt 菜单 Url 在页面插入之后无条件重算
        RunSeed(conn, "11-seed-webpage-url-fix.sql", () => hasTemplate && hasWebPage);
        // PageSetting 三屏种子（Filter/Detail=Front 前端 DynCom 动态控件；List=Back 后端 partial 演示）
        RunSeed(conn, "12-seed-pagesetting-triscreen.sql", () => TableExists(conn, "PageSetting"));

        // 动态积木：DynBlock / DynTemplateBlock（旧库幂等建表 + 内置种子 + 内置模板参数包重塑）
        RunSeed(conn, "13-schema-dyn-block.sql", () => !TableExists(conn, "DynBlock"));
        RunSeed(conn, "14-schema-dyn-template-block.sql", () => !TableExists(conn, "DynTemplateBlock"));
        RunSeed(conn, "15-seed-dyn-blocks.sql", () => TableExists(conn, "DynBlock"));
        var hasBlockTables = hasTemplate && TableExists(conn, "DynBlock") && TableExists(conn, "DynTemplateBlock");
        RunSeed(conn, "16-seed-template-config-packs.sql", () => hasBlockTables);
        RunSeed(conn, "17-seed-template-block-slots.sql", () => hasBlockTables);
        // tabs-basic 多页签容器模板（URL 片段驱动，无 Block 槽位；在参数包种子之后注册）
        RunSeed(conn, "18-seed-template-tabs-basic.sql", () => hasTemplate);
        // 布局壳 v1.1：list-master-detail 壳模板 + SysMenu 演示页（spec 存独立列 SpecJson）
        RunSeed(conn, "19-seed-shell-list-master-detail.sql", () => hasTemplate && hasWebPage);
        // 布局壳：filter-list-open-window（顶筛选+列表+明细弹窗）壳模板 + SysMenu 演示页
        RunSeed(conn, "20-seed-shell-filter-open-window.sql", () => hasTemplate && hasWebPage);
        // V2 布局壳：filter-list-drawer-left（顶筛选+列表+左侧抽屉编辑）
        RunSeed(conn, "21-seed-shell-filter-drawer-left.sql", () => hasTemplate);
        // V2 详情弹窗片段模板（layui layer 加载，Layout=null）
        RunSeed(conn, "22-seed-template-detail-modal-v2.sql", () => hasTemplate);
        // 早期手工入库模板的 class 名图标修正为 Emoji（平台图标统一 Emoji 直出）
        RunSeed(conn, "23-seed-template-icons.sql", () => hasTemplate);

        // 系统日志表（异常/操作日志持久化）
        RunSeed(conn, "24-schema-syslog.sql", () => !TableExists(conn, "SysLog"));
        // 看板（Trello 风格任务看板：列/卡片/评论）
        RunSeed(conn, "25-schema-kanban-list.sql", () => !TableExists(conn, "KanbanList"));
        RunSeed(conn, "26-schema-kanban-card.sql", () => !TableExists(conn, "KanbanCard"));
        // 旧库补 BoardId：ADD COLUMN 带 NOT NULL 必须给 DEFAULT，存量卡片统一回填到 1 号看板
        EnsureColumn(conn, "KanbanCard", "BoardId", "INTEGER NOT NULL DEFAULT 1");
        RunSeed(conn, "27-schema-kanban-card-index.sql");
        RunSeed(conn, "28-schema-kanban-comment.sql", () => !TableExists(conn, "KanbanComment"));
        RunSeed(conn, "29-seed-kanban.sql",
            () => TableExists(conn, "KanbanList") && TableExists(conn, "KanbanCard") && Count(conn, "SELECT COUNT(*) FROM KanbanList;") == 0);
        RunSeed(conn, "30-seed-kanban-pagesetting.sql", () => TableExists(conn, "PageSetting"));

        // 权限模块（轻量 RBAC：角色 / 用户角色 / 资源权限）
        RunSeed(conn, "31-schema-permission.sql");
        // admin 初始密码以 SHA256 入库（与 PermissionController.Sha256 同算法）；
        // 历史明文 'admin123' 在启动时自动升级为哈希。用户自行修改过的密码不受影响。
        var pwd = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("admin123"))).ToLowerInvariant();
        RunSeed(conn, "32-seed-admin.sql", null, ("@pwd", (object)pwd));
    }

    private void EnsureColumn(SqliteConnection conn, string table, string column, string definition)
    {
        if (!TableExists(conn, table)) return;
        if (HasColumn(conn, table, column)) return;
        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
        _logger.LogInformation("[Init] 迁移：{Table} 新增列 {Column}", table, column);
    }

    private bool HasColumn(SqliteConnection conn, string table, string column)
    {
        if (!TableExists(conn, table)) return false;
        using var check = conn.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        using var rd = check.ExecuteReader();
        while (rd.Read())
        {
            if (string.Equals(rd.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>
    /// DynTemplate：TemplateJson → DefaultJson（模板默认值）。须在 seed-extra.sql 之前执行，
    /// 因其 INSERT 引用 DefaultJson 列。新库建表已含 DefaultJson（幂等跳过），旧库 RENAME 保留数据。
    /// </summary>
    private void EnsureTemplateDefaultJson(SqliteConnection conn)
    {
        if (!TableExists(conn, "DynTemplate")) return;
        if (HasColumn(conn, "DynTemplate", "TemplateJson") && !HasColumn(conn, "DynTemplate", "DefaultJson"))
        {
            using var rn = conn.CreateCommand();
            rn.CommandText = "ALTER TABLE DynTemplate RENAME COLUMN TemplateJson TO DefaultJson;";
            rn.ExecuteNonQuery();
            _logger.LogInformation("[Init] 迁移：DynTemplate.TemplateJson → DefaultJson");
        }
        EnsureColumn(conn, "DynTemplate", "DefaultJson", "TEXT NULL");
    }

    /// <summary>
    /// DynWebPage：ConfigJson → ParamsJson（实例参数；新结构 = 模板自身参数 + blocks 槽位分组）。
    /// 新库建表已含 ParamsJson（幂等跳过），旧库 RENAME 保留全部已保存数据。
    /// </summary>
    private void EnsureWebPageParamsJson(SqliteConnection conn)
    {
        if (!TableExists(conn, "DynWebPage")) return;
        if (HasColumn(conn, "DynWebPage", "ConfigJson") && !HasColumn(conn, "DynWebPage", "ParamsJson"))
        {
            using var rn = conn.CreateCommand();
            rn.CommandText = "ALTER TABLE DynWebPage RENAME COLUMN ConfigJson TO ParamsJson;";
            rn.ExecuteNonQuery();
            _logger.LogInformation("[Init] 迁移：DynWebPage.ConfigJson → ParamsJson");
        }
        EnsureColumn(conn, "DynWebPage", "ParamsJson", "TEXT NULL");
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
        // business.sql 全部为 CREATE TABLE IF NOT EXISTS + WHERE NOT EXISTS 幂等语句，包事务保证整体成功或整体回滚
        WithTransaction(conn, c => ExecScript(c, "business.sql"));
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

    private static int Count(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>执行 Data 根目录下的脚本（platform.sql / component-meta.sql / business.sql 等）。</summary>
    private void ExecScript(SqliteConnection conn, string fileName, params (string Name, object Value)[] args)
    {
        ExecScriptFile(conn, _dataDir, fileName, args);
    }

    /// <summary>
    /// 执行 Data/seeds 下的种子脚本。
    /// when 为前置条件：不满足即跳过（对应旧逻辑中"表不存在/已有数据则不执行"的幂等语义）。
    /// </summary>
    private void RunSeed(SqliteConnection conn, string fileName, Func<bool> when = null, params (string Name, object Value)[] args)
    {
        if (when != null && !when()) return;
        ExecScriptFile(conn, _seedsDir, fileName, args);
    }

    private void ExecScriptFile(SqliteConnection conn, string dir, string fileName, (string Name, object Value)[] args)
    {
        var path = Path.Combine(dir, fileName);
        if (!File.Exists(path))
        {
            // 缺失种子脚本属于部署失败：静默跳过会导致建库不完整（甚至在清表后变空壳），必须让启动失败
            throw new FileNotFoundException($"[Init] 必需的种子脚本不存在: {path}", path);
        }
        var sql = File.ReadAllText(path, Encoding.UTF8);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
        cmd.ExecuteNonQuery();
        _logger.LogInformation("[Init] 执行脚本 {File} 完成", fileName);
    }
}
