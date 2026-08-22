/* ============================================================
 * VueLib 组件库 - 数据库初始化脚本
 * 数据库: SQL Server
 * 说明: 创建组件定义表，存储公共组件与页面组件的 template/script/style
 * ============================================================ */

IF DB_ID('VueLib') IS NULL
BEGIN
    CREATE DATABASE VueLib;
END
GO

USE VueLib;
GO

/* ---------- 组件定义表 ---------- */
IF OBJECT_ID('dbo.ComponentDefinitions', 'U') IS NOT NULL
    DROP TABLE dbo.ComponentDefinitions;
GO

CREATE TABLE dbo.ComponentDefinitions (
    Id              INT IDENTITY(1,1)   NOT NULL,
    ComponentName   NVARCHAR(100)       NOT NULL,
    ComponentType   TINYINT             NOT NULL,  -- 1=公共组件 Common, 2=页面组件 Page(Router)
    RoutePath       NVARCHAR(200)       NULL,      -- 页面组件的路由路径，公共组件为 NULL
    TemplateContent NVARCHAR(MAX)       NOT NULL,  -- Vue template HTML
    ScriptContent   NVARCHAR(MAX)       NOT NULL,  -- Vue script (setup/options) JS 代码
    StyleContent    NVARCHAR(MAX)       NULL,      -- 组件样式 CSS (可选)
    Description     NVARCHAR(500)       NULL,
    IsEnabled       BIT                 NOT NULL CONSTRAINT DF_ComponentDefinitions_IsEnabled DEFAULT (1),
    SortOrder       INT                 NOT NULL CONSTRAINT DF_ComponentDefinitions_SortOrder DEFAULT (0),
    CreatedAt       DATETIME2           NOT NULL CONSTRAINT DF_ComponentDefinitions_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2           NOT NULL CONSTRAINT DF_ComponentDefinitions_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ComponentDefinitions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UK_ComponentDefinitions_Name UNIQUE NONCLUSTERED (ComponentName)
);
GO

/* 按类型+启用状态查询索引 */
CREATE NONCLUSTERED INDEX IX_ComponentDefinitions_Type_Enabled
    ON dbo.ComponentDefinitions (ComponentType, IsEnabled)
    INCLUDE (ComponentName, RoutePath, SortOrder);
GO

PRINT '数据库表 ComponentDefinitions 创建完成。';
GO
