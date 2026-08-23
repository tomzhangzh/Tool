/* ============================================================
 * VueLib 低代码平台 - 动态属性配置支持
 * 在 ComponentMeta 和 ComponentDefinitions 表中加入属性配置字段
 * ============================================================ */
USE VueLib;
GO

/* ==================== ComponentMeta 表 ==================== */
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ComponentMeta' AND COLUMN_NAME = 'PropertyConfigJson'
)
BEGIN
    ALTER TABLE dbo.ComponentMeta
    ADD PropertyConfigJson NVARCHAR(MAX) NULL;
    PRINT 'ComponentMeta.PropertyConfigJson 字段已添加';
END
ELSE
BEGIN
    PRINT 'ComponentMeta.PropertyConfigJson 字段已存在';
END
GO

/* ==================== ComponentDefinitions 表 ==================== */
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ComponentDefinitions' AND COLUMN_NAME = 'PropertyConfigJson'
)
BEGIN
    ALTER TABLE dbo.ComponentDefinitions
    ADD PropertyConfigJson NVARCHAR(MAX) NULL;
    PRINT 'ComponentDefinitions.PropertyConfigJson 字段已添加';
END
ELSE
BEGIN
    PRINT 'ComponentDefinitions.PropertyConfigJson 字段已存在';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ComponentDefinitions' AND COLUMN_NAME = 'DefaultConfigJson'
)
BEGIN
    ALTER TABLE dbo.ComponentDefinitions
    ADD DefaultConfigJson NVARCHAR(MAX) NULL;
    PRINT 'ComponentDefinitions.DefaultConfigJson 字段已添加';
END
ELSE
BEGIN
    PRINT 'ComponentDefinitions.DefaultConfigJson 字段已存在';
END
GO

PRINT '动态属性配置字段迁移完成';
GO
