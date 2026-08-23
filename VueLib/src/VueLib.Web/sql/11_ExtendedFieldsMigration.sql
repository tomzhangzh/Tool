/* ============================================================
 * VueLib 低代码平台 - 扩展字段迁移
 * ComponentMeta: UiLibrary, CustomScriptJson
 * PageSetting: Platform, CustomScriptJson
 * ============================================================ */
USE VueLib;
GO

-- ComponentMeta: UiLibrary
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ComponentMeta') AND name = 'UiLibrary')
BEGIN
    ALTER TABLE dbo.ComponentMeta ADD UiLibrary NVARCHAR(50) NULL;
    PRINT '已添加 ComponentMeta.UiLibrary';
END
GO

-- ComponentMeta: CustomScriptJson
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ComponentMeta') AND name = 'CustomScriptJson')
BEGIN
    ALTER TABLE dbo.ComponentMeta ADD CustomScriptJson NVARCHAR(MAX) NULL;
    PRINT '已添加 ComponentMeta.CustomScriptJson';
END
GO

-- PageSetting: Platform
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PageSetting') AND name = 'Platform')
BEGIN
    ALTER TABLE dbo.PageSetting ADD Platform NVARCHAR(20) NULL;
    PRINT '已添加 PageSetting.Platform';
END
GO

-- PageSetting: CustomScriptJson
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PageSetting') AND name = 'CustomScriptJson')
BEGIN
    ALTER TABLE dbo.PageSetting ADD CustomScriptJson NVARCHAR(MAX) NULL;
    PRINT '已添加 PageSetting.CustomScriptJson';
END
GO

-- 初始化现有页面的平台
UPDATE dbo.PageSetting SET Platform = 'mobile' WHERE Platform IS NULL;
PRINT '已初始化现有页面平台为 mobile';
GO

PRINT '扩展字段迁移完成';
GO
