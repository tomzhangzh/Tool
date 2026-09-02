-- 42c_SeedCustomDemo.sql：新增 Custom 模板 + 页面实例 demo
USE VueLib;
GO

DECLARE @schema NVARCHAR(MAX) = N'[
  {"key":"title","label":"看板标题","type":"input","required":true,"default":"综合业务看板"},
  {"key":"refreshSeconds","label":"自动刷新(秒)","type":"number","default":30},
  {"key":"showChart","label":"显示图表","type":"switch","default":true},
  {"key":"color","label":"主题色","type":"select","options":[{"label":"蓝","value":"blue"},{"label":"绿","value":"green"},{"label":"橙","value":"orange"}],"default":"blue"},
  {"key":"linkPageId","label":"跳转页面","type":"pagePicker","pageType":"Summary"}
]';

IF NOT EXISTS (SELECT 1 FROM dbo.DynTemplate WHERE ProjectId = 2 AND Code = 'dashboard_demo')
BEGIN
    INSERT INTO dbo.DynTemplate (ProjectId, Name, Code, TemplateType, RenderView, ParamSchema, IsEnabled, SortOrder, CreatedAt, UpdatedAt)
    VALUES (2, '看板模板', 'dashboard_demo', 'Custom', 'RouteCustom', @schema, 1, 10, SYSUTCDATETIME(), SYSUTCDATETIME());
    DECLARE @tid INT = SCOPE_IDENTITY();
    IF NOT EXISTS (SELECT 1 FROM dbo.DynWebPage WHERE ProjectId = 2 AND Route = '/dashboard')
    BEGIN
        INSERT INTO dbo.DynWebPage (ProjectId, Route, Name, Title, TemplateId, Params, IsEnabled, SortOrder, CreatedAt, UpdatedAt)
        VALUES (2, '/dashboard', '综合看板', '综合业务看板', @tid,
                N'{"title":"综合业务看板","refreshSeconds":30,"showChart":true,"color":"blue","linkPageId":6}',
                1, 10, SYSUTCDATETIME(), SYSUTCDATETIME());
    END
    PRINT 'Custom 模板 demo 已创建 (TemplateId=' + CAST(@tid AS NVARCHAR(10)) + ')';
END
ELSE
    PRINT 'Custom 模板 demo 已存在，跳过';
GO
