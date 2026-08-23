/* ============================================================
 * VueLib 低代码平台 - 组合组件支持迁移
 * 在 ComponentMeta 表添加组合组件相关字段
 * ============================================================ */
USE VueLib;
GO

-- 添加 IsComposite 字段
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ComponentMeta') AND name = 'IsComposite')
BEGIN
    ALTER TABLE dbo.ComponentMeta ADD IsComposite BIT NOT NULL DEFAULT(0);
    PRINT '已添加 IsComposite 字段';
END
GO

-- 添加 CompositeConfigJson 字段
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ComponentMeta') AND name = 'CompositeConfigJson')
BEGIN
    ALTER TABLE dbo.ComponentMeta ADD CompositeConfigJson NVARCHAR(MAX) NULL;
    PRINT '已添加 CompositeConfigJson 字段';
END
GO

PRINT '组合组件迁移完成';
GO
