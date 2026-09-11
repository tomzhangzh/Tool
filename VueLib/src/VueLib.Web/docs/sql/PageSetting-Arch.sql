-- ============================================================
-- VueLib 架构升级：DynWebPage / DynTemplate 关联三屏 PageSetting
-- 模板 = 特殊组件(dyn-init) + 固定视图 + 动作写死
-- 页面 = URL + 模板 + filter/list/detail 三个 PageSetting(显示)
-- DynPage 保留为"选表数据源定义"（TableName/ColumnDefs/PageType）
-- 兼容：旧字段 FilterPageId/SummaryPageId/DetailPageId(指向 DynPage) 保留，
--       旧渲染链 RouteList(RouteCrud/RouteTreeList) 仍可用
-- ============================================================
USE VueLib;
GO

IF COL_LENGTH('DynWebPage','FilterPageSettingId') IS NULL
    ALTER TABLE DynWebPage ADD FilterPageSettingId int NULL;
IF COL_LENGTH('DynWebPage','ListPageSettingId') IS NULL
    ALTER TABLE DynWebPage ADD ListPageSettingId int NULL;
IF COL_LENGTH('DynWebPage','DetailPageSettingId') IS NULL
    ALTER TABLE DynWebPage ADD DetailPageSettingId int NULL;

IF COL_LENGTH('DynTemplate','FilterPageSettingId') IS NULL
    ALTER TABLE DynTemplate ADD FilterPageSettingId int NULL;
IF COL_LENGTH('DynTemplate','ListPageSettingId') IS NULL
    ALTER TABLE DynTemplate ADD ListPageSettingId int NULL;
IF COL_LENGTH('DynTemplate','DetailPageSettingId') IS NULL
    ALTER TABLE DynTemplate ADD DetailPageSettingId int NULL;
GO

SELECT 'DynWebPage' AS Tbl, name, user_type_id FROM sys.columns
WHERE object_id = OBJECT_ID('DynWebPage') AND name LIKE '%PageSetting%'
UNION ALL
SELECT 'DynTemplate', name, user_type_id FROM sys.columns
WHERE object_id = OBJECT_ID('DynTemplate') AND name LIKE '%PageSetting%';
GO
