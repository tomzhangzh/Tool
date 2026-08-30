/* ============================================================
 * VueLib 低代码平台 - 模板管理数据结构扩展
 * DynTemplate 增加 ParamSchema（模板参数定义 JSON）
 * ============================================================ */
USE VueLib;
GO

IF COL_LENGTH('dbo.DynTemplate', 'ParamSchema') IS NULL
BEGIN
    ALTER TABLE dbo.DynTemplate ADD ParamSchema NVARCHAR(MAX) NULL;
    PRINT '已添加 DynTemplate.ParamSchema 列';
END
ELSE
    PRINT 'DynTemplate.ParamSchema 已存在';
GO

PRINT '模板管理数据结构扩展完成：DynTemplate.ParamSchema';
GO
