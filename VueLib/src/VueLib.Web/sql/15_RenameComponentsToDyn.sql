/* ============================================================
 * VueLib 低代码平台 - 组件名统一加 Dyn 前缀迁移脚本
 * 目的：避免与 ElementPlus / NutUI 全局组件名冲突导致无限递归
 * 规则：ElXxx → DynElXxx, NXxx → DynNXxx
 * 影响表：ComponentMeta, PageSetting
 * ============================================================ */
USE VueLib;
GO

/* ============================================================
 * 1. 更新 ComponentMeta.ComponentName
 * ============================================================ */

-- ElXxx → DynElXxx
UPDATE dbo.ComponentMeta
SET ComponentName = 'Dyn' + ComponentName
WHERE ComponentName LIKE 'El%';
GO

-- NXxx → DynNXxx（注意：DynEl 不以 N 开头，不会被误匹配）
UPDATE dbo.ComponentMeta
SET ComponentName = 'Dyn' + ComponentName
WHERE ComponentName LIKE 'N%';
GO

/* ============================================================
 * 2. 更新 ComponentMeta.DefaultConfigJson 中的 component 字段
 * ============================================================ */

-- "component":"ElXxx" → "component":"DynElXxx"
UPDATE dbo.ComponentMeta
SET DefaultConfigJson = REPLACE(DefaultConfigJson, '"component":"El', '"component":"DynEl')
WHERE DefaultConfigJson LIKE '%"component":"El%';
GO

-- "component":"NXxx" → "component":"DynNXxx"
UPDATE dbo.ComponentMeta
SET DefaultConfigJson = REPLACE(DefaultConfigJson, '"component":"N', '"component":"DynN')
WHERE DefaultConfigJson LIKE '%"component":"N%';
GO

/* ============================================================
 * 3. 更新 PageSetting.ConfigJson 中的 component 字段
 * ============================================================ */

-- "component":"ElXxx" → "component":"DynElXxx"
UPDATE dbo.PageSetting
SET ConfigJson = REPLACE(ConfigJson, '"component":"El', '"component":"DynEl')
WHERE ConfigJson LIKE '%"component":"El%';
GO

-- "component":"NXxx" → "component":"DynNXxx"
UPDATE dbo.PageSetting
SET ConfigJson = REPLACE(ConfigJson, '"component":"N', '"component":"DynN')
WHERE ConfigJson LIKE '%"component":"N%';
GO

/* ============================================================
 * 4. 验证
 * ============================================================ */
PRINT '=== ComponentMeta 组件名（前30条）===';
SELECT TOP 30 Id, ComponentName, UiLibrary FROM dbo.ComponentMeta ORDER BY Id;
GO

PRINT '=== PageSetting 页面配置中的 component 字段 ===';
SELECT Id, PageName, PageCode,
       CASE WHEN ConfigJson LIKE '%"component":"DynEl%' THEN '含DynEl'
            WHEN ConfigJson LIKE '%"component":"DynN%' THEN '含DynN'
            ELSE '其他' END AS ComponentPrefix
FROM dbo.PageSetting;
GO

PRINT '组件名迁移完成。';
GO
