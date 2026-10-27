-- ---------------- 20. 布局壳种子：filter-list-open-window（顶筛选 + 列表 + 明细弹窗）壳模板 + SysMenu 演示页 ----------------

INSERT INTO DynTemplate (Code, Name, Category, Icon, ViewPath, DefaultJson, ConfigJson, Description, SortNo, IsActive, SupportGen)
SELECT 'filter-list-open-windowV2','筛选列表弹窗(壳)V2','布局壳','🪟','~/Views/DynTemplates/FilterListOpenWindow.cshtml','{"layout":"filter-list-open-windowV2","provide":{"table":"","keyField":"Id","project":""},"slots":{"filter":{"block":"filter","settingId":null},"list":{"block":"list","settingId":null},"detail":{"settingId":null,"title":"明细","width":"60%","height":"80%"}}}',NULL,
'V2版：DynEventBus 通信；filter 查询→list 加载；list 新增/编辑→打开 detail 弹窗(layer)；detail 保存→emit refresh 刷新 list。',31,1,1
WHERE NOT EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-open-windowV2');
UPDATE DynTemplate SET Name='筛选列表弹窗(壳)V2', Category='布局壳', Icon='🪟', ViewPath='~/Views/DynTemplates/FilterListOpenWindow.cshtml',
    DefaultJson='{"layout":"filter-list-open-windowV2","provide":{"table":"","keyField":"Id","project":""},"slots":{"filter":{"block":"filter","settingId":null},"list":{"block":"list","settingId":null},"detail":{"settingId":null,"title":"明细","width":"60%","height":"80%"}}}', SupportGen=1,
    Description='V2版：DynEventBus 通信；filter 查询→list 加载；list 新增/编辑→打开 detail 弹窗(layer)；detail 保存→emit refresh 刷新 list。'
WHERE Code='filter-list-open-windowV2';

INSERT INTO DynWebPage (Code, Name, TemplateId, ProjectId, ParamsJson, SpecJson, Url, IsActive)
SELECT 'sysmenu-filter-openwindow-demo','菜单筛选列表弹窗(壳)',(SELECT Id FROM DynTemplate WHERE Code='filter-list-open-windowV2'),NULL,'{"TableName":"SysMenu","KeyField":"Id","Project":"__platform__","ModalPageId":0,"blocks":{"filter":{"settingId":49},"list":{"settingId":50}}}',NULL,
'/Platform/Shell/Runtime?id=',1
WHERE NOT EXISTS (SELECT 1 FROM DynWebPage WHERE Code='sysmenu-filter-openwindow-demo');
UPDATE DynWebPage SET TemplateId=(SELECT Id FROM DynTemplate WHERE Code='filter-list-open-windowV2'), ParamsJson='{"TableName":"SysMenu","KeyField":"Id","Project":"__platform__","ModalPageId":0,"blocks":{"filter":{"settingId":49},"list":{"settingId":50}}}', SpecJson=NULL, IsActive=1
WHERE Code='sysmenu-filter-openwindow-demo';
UPDATE DynWebPage SET Url='/Platform/Shell/Runtime?id=' || Id
WHERE Code='sysmenu-filter-openwindow-demo';