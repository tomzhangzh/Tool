/* ============================================================
 * VueLib 低代码平台 - 修复 ElementUI 组件 Icon 显示
 * 用 N 前缀重新更新所有 24 个 ElementUI 组件的 Icon 字段
 * 解决 SSMS 中显示 ?? 的问题（确保 Unicode emoji 正确存储）
 * ============================================================ */
USE VueLib;
GO

-- 表单类组件
UPDATE dbo.ComponentMeta SET Icon = N'📝' WHERE ComponentName = N'DynElInput';
UPDATE dbo.ComponentMeta SET Icon = N'🔢' WHERE ComponentName = N'DynElInputNumber';
UPDATE dbo.ComponentMeta SET Icon = N'📋' WHERE ComponentName = N'DynElSelect';
UPDATE dbo.ComponentMeta SET Icon = N'🎚️' WHERE ComponentName = N'DynElSwitch';
UPDATE dbo.ComponentMeta SET Icon = N'🔘' WHERE ComponentName = N'DynElRadio';
UPDATE dbo.ComponentMeta SET Icon = N'☑️' WHERE ComponentName = N'DynElCheckbox';
UPDATE dbo.ComponentMeta SET Icon = N'📅' WHERE ComponentName = N'DynElDatePicker';
UPDATE dbo.ComponentMeta SET Icon = N'⏰' WHERE ComponentName = N'DynElTimePicker';
UPDATE dbo.ComponentMeta SET Icon = N'🎚️' WHERE ComponentName = N'DynElSlider';
UPDATE dbo.ComponentMeta SET Icon = N'⭐' WHERE ComponentName = N'DynElRate';
UPDATE dbo.ComponentMeta SET Icon = N'🎨' WHERE ComponentName = N'DynElColorPicker';

-- 通用类组件
UPDATE dbo.ComponentMeta SET Icon = N'🔘' WHERE ComponentName = N'DynElButton';

-- 展示类组件
UPDATE dbo.ComponentMeta SET Icon = N'🏷️' WHERE ComponentName = N'DynElTag';
UPDATE dbo.ComponentMeta SET Icon = N'🔴' WHERE ComponentName = N'DynElBadge';
UPDATE dbo.ComponentMeta SET Icon = N'👤' WHERE ComponentName = N'DynElAvatar';
UPDATE dbo.ComponentMeta SET Icon = N'📊' WHERE ComponentName = N'DynElProgress';
UPDATE dbo.ComponentMeta SET Icon = N'⚠️' WHERE ComponentName = N'DynElAlert';
UPDATE dbo.ComponentMeta SET Icon = N'➖' WHERE ComponentName = N'DynElDivider';
UPDATE dbo.ComponentMeta SET Icon = N'🖼️' WHERE ComponentName = N'DynElImage';

-- 布局类组件
UPDATE dbo.ComponentMeta SET Icon = N'📦' WHERE ComponentName = N'DynElDivContainer';
UPDATE dbo.ComponentMeta SET Icon = N'🗂️' WHERE ComponentName = N'DynElCard';
UPDATE dbo.ComponentMeta SET Icon = N'↔️' WHERE ComponentName = N'DynElRow';
UPDATE dbo.ComponentMeta SET Icon = N'↕️' WHERE ComponentName = N'DynElCol';
UPDATE dbo.ComponentMeta SET Icon = N'📑' WHERE ComponentName = N'DynElTabs';

PRINT 'ElementUI 组件 Icon 更新完成（24个）';
GO

-- 验证
SELECT Id, ComponentName, Label, Icon, LEN(Icon) AS IconLen
FROM dbo.ComponentMeta
WHERE UiLibrary = N'DynElementui'
ORDER BY Id;
GO
