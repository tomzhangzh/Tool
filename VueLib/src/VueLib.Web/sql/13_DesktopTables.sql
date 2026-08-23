/* ============================================================
 * VueLib 桌面系统 - 数据库表
 * DesktopShortcut - 桌面快捷方式
 * DesktopSolution - 解决方案（分组）
 * ============================================================ */
USE VueLib;
GO

-- 解决方案表
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DesktopSolution')
BEGIN
    CREATE TABLE dbo.DesktopSolution (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Icon NVARCHAR(500) NULL,
        Description NVARCHAR(500) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        IsEnabled BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT '已创建 DesktopSolution 表';
END
GO

-- 快捷方式表
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DesktopShortcut')
BEGIN
    CREATE TABLE dbo.DesktopShortcut (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Icon NVARCHAR(500) NULL,
        Url NVARCHAR(500) NOT NULL,
        OpenType NVARCHAR(20) NULL DEFAULT 'iframe',
        SolutionId INT NULL,
        PosX INT NOT NULL DEFAULT 0,
        PosY INT NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        IsEnabled BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    PRINT '已创建 DesktopShortcut 表';
END
GO

-- 模拟数据 - 解决方案
IF NOT EXISTS (SELECT 1 FROM dbo.DesktopSolution WHERE Name = N'低代码平台')
BEGIN
    INSERT INTO dbo.DesktopSolution (Name, Icon, Description, SortOrder)
    VALUES (N'低代码平台', N'📦', N'低代码设计与管理工具', 1);
    PRINT '已添加解决方案: 低代码平台';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DesktopSolution WHERE Name = N'系统管理')
BEGIN
    INSERT INTO dbo.DesktopSolution (Name, Icon, Description, SortOrder)
    VALUES (N'系统管理', N'⚙️', N'系统配置与管理', 2);
    PRINT '已添加解决方案: 系统管理';
END
GO

-- 模拟数据 - 快捷方式
IF NOT EXISTS (SELECT 1 FROM dbo.DesktopShortcut WHERE Name = N'页面设计器')
BEGIN
    INSERT INTO dbo.DesktopShortcut (Name, Icon, Url, OpenType, SolutionId, SortOrder)
    VALUES (N'页面设计器', N'🎨', N'/designer', N'iframe', 1, 1);
    PRINT '已添加快捷方式: 页面设计器';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DesktopShortcut WHERE Name = N'组件管理')
BEGIN
    INSERT INTO dbo.DesktopShortcut (Name, Icon, Url, OpenType, SolutionId, SortOrder)
    VALUES (N'组件管理', N'🧩', N'/ComponentManager', N'iframe', 1, 2);
    PRINT '已添加快捷方式: 组件管理';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DesktopShortcut WHERE Name = N'页面管理')
BEGIN
    INSERT INTO dbo.DesktopShortcut (Name, Icon, Url, OpenType, SolutionId, SortOrder)
    VALUES (N'页面管理', N'📄', N'/Desktop/PageManage', N'iframe', 1, 3);
    PRINT '已添加快捷方式: 页面管理';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DesktopShortcut WHERE Name = N'快捷方式管理')
BEGIN
    INSERT INTO dbo.DesktopShortcut (Name, Icon, Url, OpenType, SolutionId, SortOrder)
    VALUES (N'快捷方式管理', N'🖥️', N'/Desktop/ShortcutManage', N'iframe', 2, 1);
    PRINT '已添加快捷方式: 快捷方式管理';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DesktopShortcut WHERE Name = N'解决方案管理')
BEGIN
    INSERT INTO dbo.DesktopShortcut (Name, Icon, Url, OpenType, SolutionId, SortOrder)
    VALUES (N'解决方案管理', N'📁', N'/Desktop/SolutionManage', N'iframe', 2, 2);
    PRINT '已添加快捷方式: 解决方案管理';
END
GO

PRINT '桌面系统数据库初始化完成';
GO
