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

        // 旧列改名迁移：必须最先执行（seed-extra.sql 的 INSERT 引用 DefaultJson 列；新库建表已含，旧库需先 RENAME）
        EnsureTemplateDefaultJson(conn);

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
    RenderMode      TEXT NOT NULL DEFAULT 'Front',
    PartialPath     TEXT NULL,
    DefaultJson     TEXT NULL,
    SortNo          INTEGER NOT NULL DEFAULT 0,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    CreateTime      TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);";
            create.ExecuteNonQuery();
            _logger.LogInformation("[Init] 迁移：新建表 PageSetting");
        }
        EnsureColumn(conn, "PageSetting", "RenderMode", "TEXT NOT NULL DEFAULT 'Front'");
        EnsureColumn(conn, "PageSetting", "PartialPath", "TEXT NULL");
        EnsureColumn(conn, "PageSetting", "DefaultJson", "TEXT NULL");
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
        // DynTemplate 外壳视图路径列（旧库幂等补齐；新库 platform.sql 已包含）
        EnsureColumn(conn, "DynTemplate", "ViewPath", "TEXT NULL");
        // 页面模板种子 + 页面实例种子（demo：页面设置管理 / 页面实例列表）
        SeedDynTemplates(conn);
        SeedDynWebPages(conn);
        SeedPageSettings(conn);
    }

    /// <summary>SysMenu 初始种子（幂等：仅当表为空时插入根菜单与示例子菜单）</summary>
    private void SeedSysMenu(SqliteConnection conn)
    {
        using var cnt = conn.CreateCommand();
        cnt.CommandText = "SELECT COUNT(*) FROM SysMenu;";
        if (Convert.ToInt32(cnt.ExecuteScalar()) == 0)
        {
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

        // 平台引擎组（幂等补插：仅当 Code 不存在时插入）
        using (var pe = conn.CreateCommand())
        {
            pe.CommandText = "SELECT COUNT(*) FROM SysMenu WHERE Code='platform-engine';";
            if (Convert.ToInt32(pe.ExecuteScalar()) == 0)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = "INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) SELECT NULL, 'platform-engine', '平台引擎', '⚙️', NULL, 'Iframe', 1, 3, 1, 'platform' WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='platform-engine');INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) SELECT (SELECT Id FROM SysMenu WHERE Code='platform-engine'), 'tpl-mgmt', '页面模板', '📋', '/Platform/Mgmt/DynTemplateMgmt', 'FullScreen', 1, 1, 1, 'platform.tpl' WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='tpl-mgmt');INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) SELECT (SELECT Id FROM SysMenu WHERE Code='platform-engine'), 'webpage-mgmt', '页面实例', '📦', '/Platform/Mgmt/DynWebPageMgmt', 'FullScreen', 1, 2, 1, 'platform.page' WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='webpage-mgmt');INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive, PermissionCode) SELECT (SELECT Id FROM SysMenu WHERE Code='platform-engine'), 'setting-mgmt', '页面设置管理', '📄', '/Platform/Page/DynWebPage?id=' || (SELECT Id FROM DynWebPage WHERE Code='page-setting-mgmt'), 'Iframe', 1, 3, 1, 'platform.setting' WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='setting-mgmt');";
                ins.ExecuteNonQuery();
                _logger.LogInformation("[Init] SysMenu 平台引擎组补插完成");
            }
        }

        // 能力桥 RPC Demo（幂等补插：仅当 Code 不存在时插入，桌面根级图标）
        using (var rpc = conn.CreateCommand())
        {
            rpc.CommandText = "SELECT COUNT(*) FROM SysMenu WHERE Code='demo-rpc';";
            if (Convert.ToInt32(rpc.ExecuteScalar()) == 0)
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = "INSERT INTO SysMenu (ParentId, Code, Name, Icon, Url, TargetType, IsAddToDesktop, SortNo, IsActive) SELECT NULL, 'demo-rpc', '能力桥 RPC', '🌉', '/Platform/Page/Demo/Rpc', 'FullScreen', 1, 4, 1 WHERE NOT EXISTS (SELECT 1 FROM SysMenu WHERE Code='demo-rpc');";
                ins.ExecuteNonQuery();
                _logger.LogInformation("[Init] SysMenu 能力桥 RPC Demo 补插完成");
            }
        }
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

    // ---------------- DynTemplate / DynWebPage 种子 ----------------

    /// <summary>页面模板种子（幂等：仅当 Code 不存在时插入）</summary>
    private void SeedDynTemplates(SqliteConnection conn)
    {
        if (!TableExists(conn, "DynTemplate")) return;
        using var cnt = conn.CreateCommand();
        cnt.CommandText = "SELECT COUNT(*) FROM DynTemplate WHERE Code='crud-basic';";
        if (Convert.ToInt32(cnt.ExecuteScalar()) > 0) return;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO DynTemplate (Code, Name, Category, Icon, ViewPath, DefaultJson, ConfigJson, Description, SortNo, IsActive) VALUES ('crud-basic', '基础CRUD页面', '平台', '📋', '~/Views/DynTemplates/CrudBasic.cshtml', '{\"type\": \"page\", \"meta\": {\"title\": \"基础CRUD页面\"}, \"columns\": [{\"field\": \"Id\", \"label\": \"Id\", \"width\": 90}, {\"field\": \"Code\", \"label\": \"编码\", \"width\": 180}, {\"field\": \"Name\", \"label\": \"名称\", \"width\": 220}, {\"field\": \"IsActive\", \"label\": \"启用\", \"width\": 100, \"type\": \"tag\"}, {\"field\": \"CreateTime\", \"label\": \"创建时间\", \"width\": 190}], \"formFields\": [{\"field\": \"Code\", \"label\": \"编码\", \"type\": \"input\", \"required\": true}, {\"field\": \"Name\", \"label\": \"名称\", \"type\": \"input\", \"required\": true}, {\"field\": \"IsActive\", \"label\": \"启用\", \"type\": \"switch\"}], \"filterFields\": [{\"field\": \"Keyword\", \"label\": \"关键字\", \"type\": \"input\", \"placeholder\": \"名称/编码模糊搜索\"}, {\"field\": \"IsActive\", \"label\": \"启用\", \"type\": \"select\", \"options\": [{\"value\": \"\", \"label\": \"全部\"}, {\"value\": \"1\", \"label\": \"启用\"}, {\"value\": \"0\", \"label\": \"停用\"}]}], \"buttons\": [\"新增\", \"查询\", \"重置\"]}', '{\"type\": \"form\", \"mPassThrough\": {\"comoptions\": {\"labelWidth\": \"140px\", \"labelPosition\": \"right\"}}, \"childrenctrls\": [{\"field\": \"TableName\", \"type\": \"input\", \"labeloptions\": {\"label\": \"数据表名\", \"show\": true, \"required\": true}, \"comoptions\": {\"placeholder\": \"填写后自动生成dyndata的List/Save/Delete地址\"}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"ListUrl\", \"type\": \"input\", \"labeloptions\": {\"label\": \"列表接口Url\", \"show\": true, \"required\": false}, \"comoptions\": {\"placeholder\": \"为空则根据TableName自动生成\"}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"AddUrl\", \"type\": \"input\", \"labeloptions\": {\"label\": \"新增保存Url\", \"show\": true, \"required\": false}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"EditUrl\", \"type\": \"input\", \"labeloptions\": {\"label\": \"编辑保存Url\", \"show\": true, \"required\": false}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"DeleteUrl\", \"type\": \"input\", \"labeloptions\": {\"label\": \"删除Url\", \"show\": true, \"required\": false}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"FilterPageSettingId\", \"type\": \"settingSelect\", \"labeloptions\": {\"label\": \"筛选配置Id\", \"show\": true, \"required\": false}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"ListPageSettingId\", \"type\": \"settingSelect\", \"labeloptions\": {\"label\": \"列表配置Id\", \"show\": true, \"required\": false}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}, {\"field\": \"DetailPageSettingId\", \"type\": \"settingSelect\", \"labeloptions\": {\"label\": \"详情/编辑配置Id\", \"show\": true, \"required\": false}, \"itemoptions\": {\"style\": {\"marginBottom\": \"14px\"}}}]}', '免Model CRUD外壳：TableName 自动推导 dyndata 接口；可引用 Filter/List/Detail PageSetting 三屏配置', 1, 1);";
        cmd.ExecuteNonQuery();
        _logger.LogInformation("[Init] DynTemplate 种子 crud-basic 插入完成");
    }

    /// <summary>页面实例种子（demo：用 crud-basic 模板生成 PageSetting 管理 / DynWebPage 列表）</summary>
    private void SeedDynWebPages(SqliteConnection conn)
    {
        if (!TableExists(conn, "DynWebPage") || !TableExists(conn, "DynTemplate")) return;
        using var cnt = conn.CreateCommand();
        cnt.CommandText = "SELECT COUNT(*) FROM DynWebPage WHERE Code='page-setting-mgmt';";
        if (Convert.ToInt32(cnt.ExecuteScalar()) > 0) return;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO DynWebPage (Code, Name, TemplateId, PageJson, ConfigJson, Url, IsActive) VALUES ('page-setting-mgmt', '页面设置管理', (SELECT Id FROM DynTemplate WHERE Code='crud-basic'), NULL, '{\"TableName\":\"PageSetting\",\"ListUrl\":null,\"AddUrl\":null,\"EditUrl\":null,\"DeleteUrl\":null,\"FilterPageSettingId\":null,\"ListPageSettingId\":null,\"DetailPageSettingId\":null}', '/Platform/Page/DynWebPage?id=', 1), ('dyn-webpage-list', '页面实例列表', (SELECT Id FROM DynTemplate WHERE Code='crud-basic'), NULL, '{\"TableName\":\"DynWebPage\",\"ListUrl\":null,\"AddUrl\":null,\"EditUrl\":null,\"DeleteUrl\":null,\"FilterPageSettingId\":null,\"ListPageSettingId\":null,\"DetailPageSettingId\":null}', '/Platform/Page/DynWebPage?id=', 1);";
        cmd.ExecuteNonQuery();
        _logger.LogInformation("[Init] DynWebPage 实例种子插入完成");
    }

    /// <summary>
    /// PageSetting 三屏种子（Filter/Detail=Front 前端 DynCom 动态控件；List=Back 后端 partial 演示）。
    /// 幂等：仅当 Code 不存在时插入；并给 page-setting-mgmt 实例补挂三屏引用（原为 NULL）。
    /// ConfigJson=UI渲染配置（DynCom 组件树/列定义）；DefaultJson=Vue model 默认骨架。
    /// </summary>
    private void SeedPageSettings(SqliteConnection conn)
    {
        if (!TableExists(conn, "PageSetting")) return;
        // 种子 JSON 里含双引号，统一用单引号包裹的 SQL 字面量
                        var filterConfig = "{\"component\":\"DynGridContainer\",\"options\":{\"comoptions\":{\"direction\":\"vertical\",\"size\":\"small\"},\"labeloptions\":{\"required\":false,\"show\":false,\"labelposition\":\"right\",\"labelwidth\":\"150px\"},\"itemoptions\":{\"class\":\"grid grid-cols-4 gap-x-3 gap-y-2 items-center\"},\"compassthrough\":\"comoptions.size,labeloptions.labelposition,labeloptions.labelwidth\",\"comInnerInfo\":{\"labelWidth\":\"100px\"}},\"childrenctrls\":[{\"component\":\"DynElInput\",\"modelname\":\"Code.value\",\"options\":{\"comoptions\":{\"placeholder\":\"名称/编码模糊搜索\",\"clearable\":true,\"size\":\"small\"},\"labeloptions\":{\"label\":\"关键字\",\"required\":false,\"show\":true},\"compassthrough\":\"comoptions.size,labeloptions.labelposition,labeloptions.labelwidth\"}},{\"component\":\"DynElSelect\",\"modelname\":\"IsActive.value\",\"options\":{\"comoptions\":{\"sourceType\":\"static\",\"optionValues\":[{\"label\":\"启用\",\"value\":1},{\"label\":\"停用\",\"value\":0}],\"placeholder\":\"请选择是否启用\",\"clearable\":true,\"size\":\"small\"},\"labeloptions\":{\"label\":\"启用\",\"required\":false,\"show\":true},\"compassthrough\":\"comoptions.size,labeloptions.labelposition,labeloptions.labelwidth\"}}]}";
        var filterDefault = "{\"Code\": {\"op\": \"like\", \"value\": null}, \"IsActive\": {\"op\": \"eq\", \"value\": null}}";

                var listConfig = "{\"columns\":[{\"field\":\"Id\",\"label\":\"Id\",\"width\":90},{\"field\":\"Code\",\"label\":\"编码\",\"width\":180},{\"field\":\"Name\",\"label\":\"名称\",\"width\":220},{\"field\":\"IsActive\",\"label\":\"启用\",\"width\":100,\"cellType\":\"tag\"},{\"field\":\"CreateTime\",\"label\":\"创建时间\",\"width\":190}]}";
        var listDefault = "{\"pageInfo\": {\"PageIndex\": 1, \"PageSize\": 20, \"TotalCount\": 0}, \"sort\": {\"field\": \"CreateTime\", \"order\": \"desc\"}}";

                var detailConfig = "{\"component\":\"DynElContainer\",\"modelname\":\"\",\"options\":{\"comoptions\":{\"direction\":\"vertical\"},\"comlisteners\":{},\"labeloptions\":{\"label\":\"\",\"required\":false,\"show\":false},\"itemoptions\":{\"style\":{},\"class\":\"\"}},\"validators\":[],\"childrenctrls\":[{\"component\":\"DynElInput\",\"modelname\":\"Code\",\"options\":{\"comoptions\":{\"placeholder\":\"请输入编码\",\"clearable\":true},\"comlisteners\":{},\"labeloptions\":{\"label\":\"编码\",\"required\":true,\"show\":true},\"itemoptions\":{\"style\":{},\"class\":\"\"}},\"validators\":[],\"childrenctrls\":[],\"slots\":{},\"extendinfo\":{}},{\"component\":\"DynElInput\",\"modelname\":\"Name\",\"options\":{\"comoptions\":{\"placeholder\":\"请输入名称\",\"clearable\":true},\"comlisteners\":{},\"labeloptions\":{\"label\":\"名称\",\"required\":true,\"show\":true},\"itemoptions\":{\"style\":{},\"class\":\"\"}},\"validators\":[],\"childrenctrls\":[],\"slots\":{},\"extendinfo\":{}}],\"slots\":{},\"extendinfo\":{}}";
        var detailDefault = "{\"Code\": null, \"Name\": null, \"IsActive\": true}";

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO PageSetting (Code, Name, SettingType, TableName, ConfigJson, RenderMode, PartialPath, DefaultJson, SortNo, IsActive)
SELECT 'setting-filter-basic', '通用筛选配置', 'Filter', NULL, @fc, 'Front', NULL, @fd, 1, 1
WHERE NOT EXISTS (SELECT 1 FROM PageSetting WHERE Code='setting-filter-basic');
INSERT INTO PageSetting (Code, Name, SettingType, TableName, ConfigJson, RenderMode, PartialPath, DefaultJson, SortNo, IsActive)
SELECT 'setting-list-basic', '通用列表配置', 'List', NULL, @lc, 'Back', '~/Views/DynTemplates/Partials/ListBack.cshtml', @ld, 2, 1
WHERE NOT EXISTS (SELECT 1 FROM PageSetting WHERE Code='setting-list-basic');
INSERT INTO PageSetting (Code, Name, SettingType, TableName, ConfigJson, RenderMode, PartialPath, DefaultJson, SortNo, IsActive)
SELECT 'setting-detail-basic', '通用表单配置', 'Detail', NULL, @dc, 'Front', NULL, @dd, 3, 1
WHERE NOT EXISTS (SELECT 1 FROM PageSetting WHERE Code='setting-detail-basic');
        // 三屏配置Id（Filter/List/DetailPageSettingId）已作为实例参数写入 DynWebPage.ConfigJson，无需独立列回填";
        cmd.Parameters.AddWithValue("@fc", filterConfig);
        cmd.Parameters.AddWithValue("@fd", filterDefault);
        cmd.Parameters.AddWithValue("@lc", listConfig);
        cmd.Parameters.AddWithValue("@ld", listDefault);
        cmd.Parameters.AddWithValue("@dc", detailConfig);
        cmd.Parameters.AddWithValue("@dd", detailDefault);
        cmd.ExecuteNonQuery();
        _logger.LogInformation("[Init] PageSetting 三屏种子插入完成");
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
