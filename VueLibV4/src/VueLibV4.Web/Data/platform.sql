-- ============================================================
-- VueLibV4  PlatformDb  平台库初始化脚本（SQLite）
-- 约定：
--   * 所有表 CREATE TABLE IF NOT EXISTS（可重复执行）
--   * ComponentMeta.LoadUrl = 前端动态加载组件定义接口；为空表示全局组件
--   * ComponentMeta.ViewPath = 后端 Razor View 路径（不再运行时轮询目录）
--   * ComponentMeta.UiPlatform = Common / ElementUI / NutUI
--   * PropsMeta = 右侧属性面板配置（由 DynDynamicCom 渲染，数组：{key,label,component,default,options}）
-- ============================================================

PRAGMA foreign_keys = OFF;

-- ---------------- 1. 桌面解决方案 ----------------
CREATE TABLE IF NOT EXISTS DesktopSolution (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Code        TEXT NOT NULL UNIQUE,
    Name        TEXT NOT NULL,
    Description TEXT NULL,
    Icon        TEXT NULL,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    ExtJson     TEXT NULL
);

-- ---------------- 2. 桌面快捷方式 ----------------
CREATE TABLE IF NOT EXISTS DesktopShortcut (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    Code           TEXT NOT NULL UNIQUE,
    SolutionId     INTEGER NULL,
    Name           TEXT NOT NULL,
    Url            TEXT NULL,
    Icon           TEXT NULL,
    TargetType     TEXT NOT NULL DEFAULT 'page',
    ActionHelperId INTEGER NULL,
    SortNo         INTEGER NOT NULL DEFAULT 0,
    IsActive       INTEGER NOT NULL DEFAULT 1,
    CreateTime     TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    ExtJson        TEXT NULL
);

-- ---------------- 3. 动态项目 ----------------
CREATE TABLE IF NOT EXISTS DynProject (
    Id               INTEGER PRIMARY KEY AUTOINCREMENT,
    Code             TEXT NOT NULL UNIQUE,
    SolutionId       INTEGER NULL,
    Name             TEXT NOT NULL,
    DbType           TEXT NOT NULL DEFAULT 'Sqlite',
    ConnectionString TEXT NULL,
    Description      TEXT NULL,
    SortNo           INTEGER NOT NULL DEFAULT 0,
    IsActive         INTEGER NOT NULL DEFAULT 1,
    CreateTime       TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    ExtJson          TEXT NULL
);

-- ---------------- 4. 平台数据字典 ----------------
CREATE TABLE IF NOT EXISTS DynDict (
    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
    DictType  TEXT NOT NULL,
    DictCode  TEXT NOT NULL,
    DictName  TEXT NULL,
    ParentId  INTEGER NULL,
    SortNo    INTEGER NOT NULL DEFAULT 0,
    IsActive  INTEGER NOT NULL DEFAULT 1,
    Remark    TEXT NULL,
    ExtJson   TEXT NULL
);

-- ---------------- 5. 组件元数据 ----------------
CREATE TABLE IF NOT EXISTS ComponentMeta (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    ComponentName  TEXT NOT NULL UNIQUE,
    Label          TEXT NULL,
    Category       TEXT NULL,
    UiPlatform     TEXT NOT NULL DEFAULT 'Common',
    LoadUrl        TEXT NULL,
    ViewPath       TEXT NULL,
    Icon           TEXT NULL,
    AcceptAll      INTEGER NOT NULL DEFAULT 0,
    AllowDrop      TEXT NULL,
    CanDropInto    TEXT NULL,
    SlotsDefine    TEXT NULL,
    PropsMeta      TEXT NULL,
    TemplateContent  TEXT NULL,
    ScriptContent    TEXT NULL,
    StyleContent     TEXT NULL,
    PropertyConfigJson TEXT NULL,
    DefaultConfigJson  TEXT NULL,
    IsActive       INTEGER NOT NULL DEFAULT 1,
    ExtJson        TEXT NULL
);

-- ---------------- 6. 动作助手 ----------------
CREATE TABLE IF NOT EXISTS DynActionHelper (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    Code        TEXT NOT NULL UNIQUE,
    Name        TEXT NOT NULL,
    ActionType  TEXT NOT NULL DEFAULT 'script',
    Script      TEXT NULL,
    Language    TEXT NOT NULL DEFAULT 'javascript',
    ParamsJson  TEXT NULL,
    Description TEXT NULL,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

-- ---------------- 7. 页面模板 ----------------
CREATE TABLE IF NOT EXISTS DynTemplate (
    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
    Code         TEXT NOT NULL UNIQUE,
    Name         TEXT NOT NULL,
    Category     TEXT NULL,
    Icon         TEXT NULL,
    DefaultJson  TEXT NULL,
    ConfigJson   TEXT NULL,
    Description  TEXT NULL,
    ViewPath     TEXT NULL,
    SortNo       INTEGER NOT NULL DEFAULT 0,
    IsActive     INTEGER NOT NULL DEFAULT 1,
    CreateTime   TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

-- ---------------- 8. 动态网页 ----------------
-- 实例参数统一存于 ParamsJson：模板自身参数（顶层扁平）+ blocks 槽位分组（各 Block 的 settingId/model/options）
CREATE TABLE IF NOT EXISTS DynWebPage (
    Id                    INTEGER PRIMARY KEY AUTOINCREMENT,
    Code                  TEXT NOT NULL UNIQUE,
    ProjectId             INTEGER NULL,
    Name                  TEXT NOT NULL,
    TemplateId            INTEGER NULL,
    PageJson              TEXT NULL,
    ParamsJson            TEXT NULL,
    SpecJson              TEXT NULL,
    Url                   TEXT NULL,
    ExtViewPath           TEXT NULL,
    IsActive              INTEGER NOT NULL DEFAULT 1,
    CreateTime            TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    ExtJson               TEXT NULL
);

-- ---------------- 8a2. 表结构显示名字典（PageGen 中文名来源；两层作用域） ----------------
-- ProjectId=0 + TableName='' 为通用列名层（种子只读）；否则为表字段层（PageGen 自动回写，只写此层）。
-- 键段 NOT NULL 默认 ''/0，避免 SQLite 多 NULL 导致唯一约束失效。
CREATE TABLE IF NOT EXISTS DynSchemaLabel (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId   INTEGER NOT NULL DEFAULT 0,
    TableName   TEXT NOT NULL DEFAULT '',
    ColumnName  TEXT NOT NULL DEFAULT '',
    Label       TEXT NOT NULL,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    UpdateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_DynSchemaLabel_Key ON DynSchemaLabel(ProjectId, TableName, ColumnName);

-- ---------------- 8b. 三屏页面设置（筛选/列表/详情） ----------------
CREATE TABLE IF NOT EXISTS PageSetting (
    Id              INTEGER PRIMARY KEY AUTOINCREMENT,
    Code            TEXT NOT NULL,
    Name            TEXT NOT NULL,
    SettingType     TEXT NOT NULL DEFAULT 'List',  -- Filter / List / Detail
    ProjectId       INTEGER NULL,
    TableName       TEXT NULL,
    ConfigJson      TEXT NULL,
    RenderMode      TEXT NOT NULL DEFAULT 'Front',
    PartialPath     TEXT NULL,
    DefaultJson     TEXT NULL,
    SortNo          INTEGER NOT NULL DEFAULT 0,
    IsActive        INTEGER NOT NULL DEFAULT 1,
    CreateTime      TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

-- ---------------- 8c. 动态积木 Block（独立功能单元，可 DB 动态注册） ----------------
CREATE TABLE IF NOT EXISTS DynBlock (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    Code              TEXT NOT NULL UNIQUE,
    Name              TEXT NOT NULL,
    Category          TEXT NULL,
    ImplementsRole    TEXT NOT NULL DEFAULT '',   -- filter/list/detail/tree
    ViewPath          TEXT NULL,
    HtmlCode          TEXT NULL,
    ScriptCode        TEXT NULL,
    ParamConfigJson   TEXT NULL,                  -- 接线参数 UI 包：参数表单 UI 树
    ParamDefaultJson  TEXT NULL,                  -- 接线参数 UI 包：参数 model 骨架
    Commands          TEXT NULL,                  -- 接受命令清单（JSON 数组）
    Events            TEXT NULL,                  -- 发出事件清单（JSON 数组）
    Description       TEXT NULL,
    SortNo            INTEGER NOT NULL DEFAULT 0,
    IsActive          INTEGER NOT NULL DEFAULT 1,
    CreateTime        TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

-- ---------------- 8d. 模板-积木槽位关系（(TemplateId,Slot) 唯一） ----------------
CREATE TABLE IF NOT EXISTS DynTemplateBlock (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TemplateId  INTEGER NOT NULL,
    Slot        TEXT NOT NULL,                    -- filter/list/detail/tree
    BlockId     INTEGER NOT NULL,
    Required    INTEGER NOT NULL DEFAULT 0,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_DynTemplateBlock_Template_Slot ON DynTemplateBlock(TemplateId, Slot);

-- ---------------- 9. 动态组件库 ----------------
CREATE TABLE IF NOT EXISTS DynCom (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Code          TEXT NOT NULL UNIQUE,
    Name          TEXT NOT NULL,
    Category      TEXT NULL,
    ComponentName TEXT NULL,
    ConfigJson    TEXT NULL,
    Description   TEXT NULL,
    SortNo        INTEGER NOT NULL DEFAULT 0,
    IsActive      INTEGER NOT NULL DEFAULT 1,
    CreateTime    TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    ExtJson       TEXT NULL
);

-- ============================================================
-- 种子数据（仅在 ComponentMeta 为空时由初始化器插入）
-- ============================================================

-- 解决方案
INSERT INTO DesktopSolution (Code, Name, Description, Icon, SortNo)
SELECT 'default', '默认工作台', '开箱即用的默认解决方案', '🗂️', 1
WHERE NOT EXISTS (SELECT 1 FROM DesktopSolution WHERE Code='default');

-- 基础动作
INSERT INTO DynActionHelper (Code, Name, ActionType, Script, Description)
SELECT 'reload', '刷新当前页', 'script', 'ctx.reload();', '重新查询并刷新'
WHERE NOT EXISTS (SELECT 1 FROM DynActionHelper WHERE Code='reload');

-- ---------------- 系统菜单（树形；桌面快捷方式数据源） ----------------
CREATE TABLE IF NOT EXISTS SysMenu (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    ParentId       INTEGER NULL,
    Code           TEXT NULL,
    Name           TEXT NOT NULL,
    Icon           TEXT NULL,
    Url            TEXT NULL,
    TargetType     TEXT NOT NULL DEFAULT 'Iframe',
    Width          TEXT NULL,
    Height         TEXT NULL,
    IsAddToDesktop INTEGER NOT NULL DEFAULT 1,
    IsAddToDesktopRoot INTEGER NOT NULL DEFAULT 1,
    IsAddToStartMenu    INTEGER NOT NULL DEFAULT 1,
    SortNo         INTEGER NOT NULL DEFAULT 0,
    IsActive       INTEGER NOT NULL DEFAULT 1,
    PermissionCode TEXT NULL,
    CreateTime     TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

-- ComponentMeta 由初始化器按 Data/component-meta.sql 批量插入
