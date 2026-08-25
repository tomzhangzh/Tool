/* ============================================================
 * VueLib 低代码平台 - 桌面快捷方式添加窗口大小字段
 * 为 DesktopShortcut 表添加 Width、Height 列
 * ============================================================ */
USE VueLib;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.DesktopShortcut') AND name = 'Width')
BEGIN
    ALTER TABLE dbo.DesktopShortcut ADD Width INT NOT NULL DEFAULT 0;
    PRINT '已添加 Width 列';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.DesktopShortcut') AND name = 'Height')
BEGIN
    ALTER TABLE dbo.DesktopShortcut ADD Height INT NOT NULL DEFAULT 0;
    PRINT '已添加 Height 列';
END
GO

PRINT '桌面快捷方式窗口大小字段添加完成';
GO
