-- ============================================================
-- VueLibV3  PlatformDb 平台数据库结构脚本
-- 字段类型规则：
--   * 字符串文本字段 → NVARCHAR(n) / NVARCHAR(MAX)（禁止 VARCHAR）
--   * Id 主键       → BIGINT IDENTITY(1,1)（自增，业务键用 Code 列）
--   * IsActive      → BIT
--   * SortNo        → INT
--   * CreateTime    → DATETIME2
-- 跨表引用一律存对方表的 Code（业务唯一键），如 SolutionId=DesktopSolution.Code。
-- 种子数据由启动 Seeder 从 *.seed.json 导入。
-- ============================================================

IF DB_ID(N'PlatformDb') IS NULL
    CREATE DATABASE [PlatformDb];
GO
USE [PlatformDb];
GO

-- ---------------- 1. 桌面解决方案 ----------------
IF OBJECT_ID(N'dbo.DesktopSolution', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DesktopSolution (
        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name        NVARCHAR(100) NOT NULL,
        Code        NVARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        Icon        NVARCHAR(200) NULL,
        SortNo      INT           NULL DEFAULT 0,
        IsActive    BIT           NOT NULL DEFAULT 1,
        CreateTime  DATETIME2     NULL DEFAULT SYSUTCDATETIME(),
        ExtJson     NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 2. 桌面快捷方式 ----------------
IF OBJECT_ID(N'dbo.DesktopShortcut', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DesktopShortcut (
        Id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SolutionId      NVARCHAR(100) NULL,          -- 引用 DesktopSolution.Code
        Name            NVARCHAR(100) NOT NULL,
        Code            NVARCHAR(100) NULL,
        Url             NVARCHAR(500) NULL,
        Icon            NVARCHAR(200) NULL,
        TargetType      NVARCHAR(50)  NULL DEFAULT N'page',   -- page / action / url
        ActionHelperId  NVARCHAR(100) NULL,          -- 引用 DynActionHelper.Code
        SortNo          INT           NULL DEFAULT 0,
        IsActive        BIT           NOT NULL DEFAULT 1,
        CreateTime      DATETIME2     NULL DEFAULT SYSUTCDATETIME(),
        ExtJson         NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 3. 动态项目（= 不同数据库） ----------------
IF OBJECT_ID(N'dbo.DynProject', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynProject (
        Id               BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SolutionId       NVARCHAR(100) NULL,          -- 引用 DesktopSolution.Code
        Name             NVARCHAR(100) NOT NULL,
        Code             NVARCHAR(100) NULL,
        DbType           NVARCHAR(50)  NULL DEFAULT N'SqlServer',
        ConnectionString NVARCHAR(1000) NULL,
        Description      NVARCHAR(500) NULL,
        SortNo           INT           NULL DEFAULT 0,
        IsActive         BIT           NOT NULL DEFAULT 1,
        CreateTime       DATETIME2     NULL DEFAULT SYSUTCDATETIME(),
        ExtJson          NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 4. 平台数据字典 ----------------
IF OBJECT_ID(N'dbo.DynDict', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynDict (
        Id        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DictType  NVARCHAR(100) NOT NULL,
        DictCode  NVARCHAR(100) NOT NULL,
        DictName  NVARCHAR(200) NULL,
        ParentId  NVARCHAR(100) NULL,
        SortNo    INT           NULL DEFAULT 0,
        IsActive  BIT           NOT NULL DEFAULT 1,
        Remark    NVARCHAR(500) NULL,
        ExtJson   NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 5. 组件元数据 ----------------
IF OBJECT_ID(N'dbo.ComponentMeta', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComponentMeta (
        Id             BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ComponentName  NVARCHAR(100) NOT NULL,   -- ElInput / DynGridContainer ...
        Label          NVARCHAR(100) NULL,
        Category       NVARCHAR(50)  NULL,       -- 表单/数据/容器/布局/展示/通用
        Icon           NVARCHAR(200) NULL,       -- Emoji
        AcceptAll      BIT           NOT NULL DEFAULT 0,  -- 是否接受任意子组件
        AllowDrop      NVARCHAR(MAX) NULL,       -- JSON数组：允许拖入的组件名
        CanDropInto    NVARCHAR(MAX) NULL,       -- JSON数组：可被拖入哪些父组件
        SlotsDefine    NVARCHAR(MAX) NULL,       -- JSON数组：支持的插槽
        PropsMeta      NVARCHAR(MAX) NULL,       -- JSON数组：属性面板元数据
        IsElementPlus  BIT           NOT NULL DEFAULT 1,
        Version        NVARCHAR(50)  NULL,
        IsActive       BIT           NOT NULL DEFAULT 1,
        ExtJson        NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 6. 动作助手 ----------------
IF OBJECT_ID(N'dbo.DynActionHelper', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynActionHelper (
        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name        NVARCHAR(100) NOT NULL,
        Code        NVARCHAR(100) NULL,
        ActionType  NVARCHAR(50)  NULL DEFAULT N'script',  -- script / url / api / chain
        Script      NVARCHAR(MAX) NULL,
        Language    NVARCHAR(50)  NULL DEFAULT N'javascript',
        ParamsJson  NVARCHAR(MAX) NULL,
        Description NVARCHAR(500) NULL,
        SortNo      INT           NULL DEFAULT 0,
        IsActive    BIT           NOT NULL DEFAULT 1,
        CreateTime  DATETIME2     NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- ---------------- 7. 页面模板 ----------------
IF OBJECT_ID(N'dbo.DynTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynTemplate (
        Id           BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name         NVARCHAR(100) NOT NULL,
        Code         NVARCHAR(100) NULL,
        Category     NVARCHAR(50)  NULL,          -- 学生管理CRUD / 左树右列表 / 主页 / 空白页 ...
        Icon         NVARCHAR(200) NULL,          -- Emoji
        TemplateJson NVARCHAR(MAX) NULL,          -- 可实例化的配置树
        ConfigJson   NVARCHAR(MAX) NULL,          -- 模板参数 schema
        Description  NVARCHAR(500) NULL,
        SortNo       INT           NULL DEFAULT 0,
        IsActive     BIT           NOT NULL DEFAULT 1,
        CreateTime   DATETIME2     NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- ---------------- 8. 动态网页 ----------------
IF OBJECT_ID(N'dbo.DynWebPage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynWebPage (
        Id          BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ProjectId   NVARCHAR(100) NULL,           -- 引用 DynProject.Code
        Name        NVARCHAR(100) NOT NULL,
        Code        NVARCHAR(100) NULL,
        TemplateId  NVARCHAR(100) NULL,           -- 引用 DynTemplate.Code
        PageJson    NVARCHAR(MAX) NULL,           -- 实例化后的配置树
        ConfigJson  NVARCHAR(MAX) NULL,           -- 用户配置参数
        Url         NVARCHAR(500) NULL,
        IsActive    BIT           NOT NULL DEFAULT 1,
        CreateTime  DATETIME2     NULL DEFAULT SYSUTCDATETIME(),
        ExtJson     NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 9. 动态组件库（组件配置入库） ----------------
IF OBJECT_ID(N'dbo.DynCom', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DynCom (
        Id            BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name          NVARCHAR(100) NOT NULL,
        Code          NVARCHAR(100) NULL,
        Category      NVARCHAR(50)  NULL,
        ComponentName NVARCHAR(100) NULL,         -- 对应 dyn-com.js 中的实现名
        ConfigJson    NVARCHAR(MAX) NULL,          -- 组合组件/容器组件的默认配置
        Description   NVARCHAR(500) NULL,
        SortNo        INT           NULL DEFAULT 0,
        IsActive      BIT           NOT NULL DEFAULT 1,
        CreateTime    DATETIME2     NULL DEFAULT SYSUTCDATETIME(),
        ExtJson       NVARCHAR(MAX) NULL
    );
END;
GO
