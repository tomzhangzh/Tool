/* ============================================================
 * VueLib 低代码平台 - 42_TemplateParametric.sql
 * 模板体系参数化重构：
 *   1. DynTemplate 增加 RenderView（模板统一渲染视图名）
 *   2. DynWebPage 增加 Params（页面实例按模板 ParamSchema 填写的参数值 JSON）
 *   3. 为现有模板填充 ParamSchema（参数定义 JSON）
 *   4. 为现有页面实例迁移三屏 Id 到 Params
 *   5. Seed 演示：3屏模板 + Home 九宫格模板 + 页面实例
 * ============================================================ */
USE VueLib;
GO

-- ============ 1. DynTemplate 增加 RenderView 列 ============
IF COL_LENGTH('dbo.DynTemplate', 'RenderView') IS NULL
BEGIN
    ALTER TABLE dbo.DynTemplate ADD RenderView NVARCHAR(100) NULL;
    PRINT '已添加 DynTemplate.RenderView';
END
ELSE
    PRINT 'DynTemplate.RenderView 已存在';
GO

-- ============ 2. DynWebPage 增加 Params 列 ============
IF COL_LENGTH('dbo.DynWebPage', 'Params') IS NULL
BEGIN
    ALTER TABLE dbo.DynWebPage ADD Params NVARCHAR(MAX) NULL;
    PRINT '已添加 DynWebPage.Params';
END
ELSE
    PRINT 'DynWebPage.Params 已存在';
GO

-- ============ 3. 现有模板填充 RenderView + ParamSchema ============

-- 3屏（List）模板：参数 = 三个页面选择器
DECLARE @listSchema NVARCHAR(MAX) = N'[
  {"key":"filterPageId","label":"筛选屏页面","type":"pagePicker","pageType":"Filter","required":false},
  {"key":"summaryPageId","label":"汇总屏页面","type":"pagePicker","pageType":"Summary","required":true},
  {"key":"detailPageId","label":"细节屏页面","type":"pagePicker","pageType":"Detail","required":false}
]';

UPDATE dbo.DynTemplate SET RenderView = 'RouteList', ParamSchema = @listSchema
WHERE TemplateType = 'List';

-- Home 模板：参数 = 横幅标题 + 九宫格入口数组
DECLARE @homeSchema NVARCHAR(MAX) = N'[
  {"key":"banner","label":"横幅标题","type":"input","default":"欢迎使用"},
  {"key":"gridItems","label":"九宫格入口","type":"gridItems","fields":[
     {"key":"icon","label":"图标"},
     {"key":"title","label":"标题"},
     {"key":"route","label":"路由"}
  ]}
]';

UPDATE dbo.DynTemplate SET RenderView = 'RouteHome', ParamSchema = @homeSchema
WHERE TemplateType = 'Home';

-- ============ 4. 现有页面实例迁移三屏 Id 到 Params ============
-- /drawings -> 模板 drawing_list (Filter=17, Summary=6, Detail=5)
UPDATE dbo.DynWebPage SET Params = N'{"filterPageId":17,"summaryPageId":6,"detailPageId":5}'
WHERE Route = '/drawings';
-- /components -> 模板 component_list (Filter=20, Summary=10, Detail=9)
UPDATE dbo.DynWebPage SET Params = N'{"filterPageId":20,"summaryPageId":10,"detailPageId":9}'
WHERE Route = '/components';
-- /reviewlogs -> 模板 reviewlog_list (Summary=8, Detail=7)
UPDATE dbo.DynWebPage SET Params = N'{"summaryPageId":8,"detailPageId":7}'
WHERE Route = '/reviewlogs';
-- /home -> 模板 home (九宫格 demo)
UPDATE dbo.DynWebPage SET Params = N'{"banner":"欢迎使用 VueLib 示例工程","gridItems":[
  {"icon":"📄","title":"图纸管理","route":"/drawings"},
  {"icon":"🧩","title":"部件明细","route":"/components"},
  {"icon":"📋","title":"审图记录","route":"/reviewlogs"}
]}'
WHERE Route = '/home';

PRINT '模板参数化重构完成';
GO
