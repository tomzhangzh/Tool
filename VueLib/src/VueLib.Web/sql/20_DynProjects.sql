/* ============================================================
 * VueLib 动态工程模块 - 建表脚本
 * 1. VueLib 库：DynProject（工程）、DynPage（页面定义）
 * 2. 创建示例工程数据库 SunnySystem + 示例业务表 Customer
 * 3. 注册 SunnySystem 工程（名称=SunnySystem，数据库=SunnySystem）
 * 4. 桌面添加快捷方式：工程管理
 * ============================================================ */
USE [master];
GO

-- 示例工程数据库（不存在则创建）
IF DB_ID('SunnySystem') IS NULL
BEGIN
    CREATE DATABASE [SunnySystem];
    PRINT '已创建数据库: SunnySystem';
END
GO

-- ============================================================
-- 1) VueLib 元数据表
-- ============================================================
USE [VueLib];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DynProject')
BEGIN
    CREATE TABLE dbo.DynProject (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(100) NULL,
        ConnectionString NVARCHAR(MAX) NULL,
        DatabaseName NVARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        Icon NVARCHAR(50) NULL DEFAULT N'📦',
        IsEnabled BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE UNIQUE INDEX UX_DynProject_Name ON dbo.DynProject(Name);
    PRINT '已创建 DynProject 表';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DynPage')
BEGIN
    CREATE TABLE dbo.DynPage (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProjectId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Title NVARCHAR(100) NULL,
        PageType NVARCHAR(20) NOT NULL DEFAULT N'Summary',   -- Summary / Detail
        TableName NVARCHAR(100) NULL,
        ColumnDefs NVARCHAR(MAX) NULL,                        -- DynPageDefinition JSON
        DetailPageId INT NULL,                                -- 汇总屏关联的细节屏 Id
        SortOrder INT NOT NULL DEFAULT 0,
        IsEnabled BIT NOT NULL DEFAULT 1,
        Remark NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_DynPage_Project ON dbo.DynPage(ProjectId);
    PRINT '已创建 DynPage 表';
END
GO

-- ============================================================
-- 2) SunnySystem 示例业务表 Customer（覆盖多种列类型用于演示生成）
-- ============================================================
USE [SunnySystem];
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Customer')
BEGIN
    CREATE TABLE dbo.Customer (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        Gender NVARCHAR(10) NULL,
        Age INT NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(100) NULL,
        Level NVARCHAR(20) NULL,
        Balance DECIMAL(18,2) NOT NULL DEFAULT 0,
        Birthday DATETIME NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        Remark NVARCHAR(500) NULL
    );
    PRINT '已创建 SunnySystem.dbo.Customer 表';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customer)
BEGIN
    INSERT INTO dbo.Customer (Name, Gender, Age, Phone, Email, Level, Balance, Birthday, IsActive, Remark) VALUES
    (N'张三', N'男', 32, N'13800001111', N'zhangsan@demo.com', N'高级', 12800.50, '1992-05-18', 1, N'老客户'),
    (N'李四', N'女', 27, N'13800002222', N'lisi@demo.com', N'中级', 3500.00, '1997-11-02', 1, N''),
    (N'王五', N'男', 45, N'13800003333', N'wangwu@demo.com', N'初级', 800.20, '1979-03-25', 0, N'新客户'),
    (N'赵六', N'女', 38, N'13800004444', N'zhaoliu@demo.com', N'高级', 25600.00, '1986-07-09', 1, N'VIP'),
    (N'孙七', N'男', 29, N'13800005555', N'sunqi@demo.com', N'中级', 9200.75, '1995-01-30', 1, N'');
    PRINT '已写入 Customer 示例数据 5 条';
END
GO

-- ============================================================
-- 3) 注册 SunnySystem 工程（连接串使用 Windows 认证）
-- ============================================================
USE [VueLib];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DynProject WHERE Name = N'SunnySystem')
BEGIN
    INSERT INTO dbo.DynProject (Name, DisplayName, DatabaseName, ConnectionString, Description, Icon, IsEnabled)
    VALUES (
        N'SunnySystem',
        N'阳光系统',
        N'SunnySystem',
        N'Server=.;Database=SunnySystem;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True',
        N'示例工程：连接 SunnySystem 数据库，自动生成汇总屏/细节屏',
        N'☀️',
        1
    );
    PRINT '已注册工程: SunnySystem';
END
GO

-- ============================================================
-- 4) 桌面快捷方式：工程管理
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.DesktopShortcut WHERE Name = N'工程管理')
BEGIN
    INSERT INTO dbo.DesktopShortcut (Name, Icon, Url, OpenType, SolutionId, PosX, PosY, SortOrder, IsEnabled)
    VALUES (N'工程管理', N'🏗️', N'/DynProject/Index', N'iframe', 2, 260, 60, 3, 1);
    PRINT '已添加快捷方式: 工程管理';
END
GO

PRINT '动态工程模块初始化完成';
GO
