-- ============================================================
-- VueLibV3  BusinessDb 业务数据库结构脚本（示例：学校业务）
-- 字段类型规则同平台库：
--   * 字符串文本字段 → NVARCHAR（禁止 VARCHAR）
--   * Id → BIGINT IDENTITY、Age/Hours → INT、Score/Credit → DECIMAL、
--   * CreateTime → DATETIME2、IsActive → BIT、SortNo → INT
-- 业务字典存放在本库 BusinessDict，平台字典在 PlatformDb.DynDict。
-- ============================================================

IF DB_ID(N'BusinessDb') IS NULL
    CREATE DATABASE [BusinessDb];
GO
USE [BusinessDb];
GO

-- ---------------- 1. 学生表 ----------------
IF OBJECT_ID(N'dbo.Student', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Student (
        Id         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name       NVARCHAR(100) NOT NULL,
        Gender     NVARCHAR(20)  NULL,
        Age        INT           NULL,
        ClassName  NVARCHAR(100) NULL,
        Score      DECIMAL(5,1)  NULL,
        Phone      NVARCHAR(50)  NULL,
        Address    NVARCHAR(500) NULL,
        CreateTime DATETIME2     NULL DEFAULT SYSUTCDATETIME(),
        ExtJson    NVARCHAR(MAX) NULL
    );
END;
GO

-- ---------------- 2. 课程表 ----------------
IF OBJECT_ID(N'dbo.Course', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Course (
        Id         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name       NVARCHAR(100) NOT NULL,
        Teacher    NVARCHAR(100) NULL,
        Credit     DECIMAL(3,1)  NULL,
        Hours      INT           NULL,
        CreateTime DATETIME2     NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- ---------------- 3. 业务数据字典 ----------------
IF OBJECT_ID(N'dbo.BusinessDict', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessDict (
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
