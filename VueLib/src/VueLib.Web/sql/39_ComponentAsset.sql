USE [VueLib];
GO
-- ============================================================
-- 39_ComponentAsset.sql  组件快照 / 资产复用库
-- 把用户调好的组件存为可复用资产，跨页面/模板拖入
-- ============================================================

IF OBJECT_ID(N'dbo.ComponentAsset', N'U') IS NULL
BEGIN
CREATE TABLE dbo.ComponentAsset (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    AssetName   NVARCHAR(100) NOT NULL,          -- 资产名称
    Icon        NVARCHAR(20) NULL,               -- 图标（emoji）
    Category    NVARCHAR(50) NULL,               -- 分类
    ConfigJson  NVARCHAR(MAX) NOT NULL,          -- 组件树配置（子树根节点）
    Description NVARCHAR(500) NULL,              -- 描述
    CreatedAt   DATETIME NULL DEFAULT GETDATE(),
    UpdatedAt   DATETIME NULL DEFAULT GETDATE()
);
CREATE INDEX IX_ComponentAsset_Category ON dbo.ComponentAsset(Category);
END
GO
PRINT 'ComponentAsset 表创建完成';
GO
