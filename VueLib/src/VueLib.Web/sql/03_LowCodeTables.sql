/* ============================================================
 * VueLib 低代码平台 - 数据库扩展脚本
 * 新增: PageSetting(页面配置)、ComponentMeta(组件元数据)
 * ============================================================ */
USE VueLib;
GO

/* ---------- 页面配置表 ---------- */
IF OBJECT_ID('dbo.PageSetting', 'U') IS NOT NULL
    DROP TABLE dbo.PageSetting;
GO

CREATE TABLE dbo.PageSetting (
    Id                  INT IDENTITY(1,1)   NOT NULL,
    PageName            NVARCHAR(100)       NOT NULL,          -- 页面名称
    PageCode            NVARCHAR(100)       NOT NULL,          -- 页面编码（唯一，用于访问）
    Category            NVARCHAR(50)        NULL,              -- 分类
    Icon                NVARCHAR(100)       NULL,              -- 图标
    ConfigJson          NVARCHAR(MAX)       NOT NULL,          -- 页面组件树配置 JSON
    DefaultModelJson    NVARCHAR(MAX)       NULL,              -- 默认数据模型 JSON
    ApiBaseUrl          NVARCHAR(500)       NULL,              -- 数据接口地址
    Description         NVARCHAR(500)       NULL,
    IsEnabled           BIT                 NOT NULL CONSTRAINT DF_PageSetting_IsEnabled DEFAULT (1),
    SortOrder           INT                 NOT NULL CONSTRAINT DF_PageSetting_SortOrder DEFAULT (0),
    CreatedAt           DATETIME2           NOT NULL CONSTRAINT DF_PageSetting_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2           NOT NULL CONSTRAINT DF_PageSetting_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_PageSetting PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UK_PageSetting_Code UNIQUE NONCLUSTERED (PageCode)
);
GO

/* ---------- 组件元数据表 ---------- */
IF OBJECT_ID('dbo.ComponentMeta', 'U') IS NOT NULL
    DROP TABLE dbo.ComponentMeta;
GO

CREATE TABLE dbo.ComponentMeta (
    Id                  INT IDENTITY(1,1)   NOT NULL,
    ComponentName       NVARCHAR(100)       NOT NULL,          -- 组件注册名（如 NInput）
    ComponentType       TINYINT             NOT NULL,          -- 1=表单项, 2=容器, 3=展示, 4=通用
    Category            NVARCHAR(50)        NOT NULL,          -- 分类（表单/布局/展示等）
    Label               NVARCHAR(100)       NOT NULL,          -- 显示名称
    Icon                NVARCHAR(200)       NULL,              -- 图标（emoji 或 URL）
    DefaultConfigJson   NVARCHAR(MAX)       NOT NULL,          -- 默认配置 JSON
    DefaultOptionsJson  NVARCHAR(MAX)       NULL,              -- 默认 comoptions JSON
    LoadUrl             NVARCHAR(500)       NOT NULL,          -- 组件加载地址（Razor View 路径）
    Description         NVARCHAR(500)       NULL,
    IsEnabled           BIT                 NOT NULL CONSTRAINT DF_ComponentMeta_IsEnabled DEFAULT (1),
    SortOrder           INT                 NOT NULL CONSTRAINT DF_ComponentMeta_SortOrder DEFAULT (0),
    CreatedAt           DATETIME2           NOT NULL CONSTRAINT DF_ComponentMeta_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ComponentMeta PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UK_ComponentMeta_Name UNIQUE NONCLUSTERED (ComponentName)
);
GO

CREATE NONCLUSTERED INDEX IX_ComponentMeta_Type_Enabled
    ON dbo.ComponentMeta (ComponentType, IsEnabled)
    INCLUDE (ComponentName, Category, Label, SortOrder);
GO

PRINT '低代码平台扩展表创建完成。';
GO
