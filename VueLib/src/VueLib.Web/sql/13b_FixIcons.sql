/* ============================================================
 * VueLib 桌面系统 - 修复图标编码（emoji 需要 N 前缀）
 * 如果之前执行过 13_DesktopTables.sql，图标可能变成问号
 * 执行此脚本修复
 * ============================================================ */
USE VueLib;
GO

-- 修复解决方案图标
UPDATE dbo.DesktopSolution SET Icon = N'📦' WHERE Name = N'低代码平台';
UPDATE dbo.DesktopSolution SET Icon = N'⚙️' WHERE Name = N'系统管理';
GO

-- 修复快捷方式图标
UPDATE dbo.DesktopShortcut SET Icon = N'🎨' WHERE Name = N'页面设计器';
UPDATE dbo.DesktopShortcut SET Icon = N'🧩' WHERE Name = N'组件管理';
UPDATE dbo.DesktopShortcut SET Icon = N'📄' WHERE Name = N'页面管理';
UPDATE dbo.DesktopShortcut SET Icon = N'🖥️' WHERE Name = N'快捷方式管理';
UPDATE dbo.DesktopShortcut SET Icon = N'📁' WHERE Name = N'解决方案管理';
GO

PRINT '图标修复完成';
GO

-- 验证
SELECT Id, Name, Icon, Url FROM dbo.DesktopShortcut ORDER BY SortOrder;
SELECT Id, Name, Icon FROM dbo.DesktopSolution ORDER BY SortOrder;
GO
