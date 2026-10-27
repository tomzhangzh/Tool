-- ---------------- 19. 布局壳 v1.1 种子：list-master-detail 壳模板 + SysMenu 演示页（spec 存独立列 SpecJson） ----------------

INSERT INTO DynTemplate (Code, Name, Category, Icon, ViewPath, DefaultJson, ConfigJson, Description, SortNo, IsActive)
SELECT 'list-master-detail','列表主从(壳)','布局壳','🧱','~/Views/DynLayouts/ListMasterDetail.cshtml','{"layout":"list-master-detail","layoutProps":{"leftWidth":50,"childFkField":""},"provide":{"table":"","keyField":"Id","project":""},"slots":{"filter":{"block":"filter","settingId":null},"master":{"block":"list","settingId":null},"detail":{"block":"detail","settingId":null}}}',NULL,
'布局壳 v1.1：顶部筛选(可选)+左列表+右明细同屏；spec(SpecJson)声明槽位 Block/PageSetting，默认连线 filter→list→detail；childFkField 支持主子预填。',30,1
WHERE NOT EXISTS (SELECT 1 FROM DynTemplate WHERE Code='list-master-detail');
UPDATE DynTemplate SET Name='列表主从(壳)', Category='布局壳', Icon='🧱', ViewPath='~/Views/DynLayouts/ListMasterDetail.cshtml',
    Description='布局壳 v1.1：顶部筛选(可选)+左列表+右明细同屏；spec(SpecJson)声明槽位 Block/PageSetting，默认连线 filter→list→detail；childFkField 支持主子预填。'
WHERE Code='list-master-detail';
UPDATE DynTemplate SET DefaultJson='{"layout":"list-master-detail","layoutProps":{"leftWidth":50,"childFkField":""},"provide":{"table":"","keyField":"Id","project":""},"slots":{"filter":{"block":"filter","settingId":null},"master":{"block":"list","settingId":null},"detail":{"block":"detail","settingId":null}}}' WHERE Code='list-master-detail' AND (DefaultJson IS NULL OR DefaultJson='');

INSERT INTO DynWebPage (Code, Name, TemplateId, ProjectId, ParamsJson, SpecJson, Url, IsActive)
SELECT 'sysmenu-list-shell-demo','菜单列表主从(壳模型v1.1)',(SELECT Id FROM DynTemplate WHERE Code='list-master-detail'),NULL,NULL,'{"layout":"list-master-detail","layoutProps":{"leftWidth":45,"childFkField":"ParentId"},"provide":{"table":"SysMenu","keyField":"Id","project":"__platform__"},"slots":{"filter":{"block":"filter","settingId":49},"master":{"block":"list","settingId":50},"detail":{"block":"detail","settingId":51}}}',
'/Platform/Shell/Runtime?id=',1
WHERE NOT EXISTS (SELECT 1 FROM DynWebPage WHERE Code='sysmenu-list-shell-demo');
UPDATE DynWebPage SET TemplateId=(SELECT Id FROM DynTemplate WHERE Code='list-master-detail'), SpecJson='{"layout":"list-master-detail","layoutProps":{"leftWidth":45,"childFkField":"ParentId"},"provide":{"table":"SysMenu","keyField":"Id","project":"__platform__"},"slots":{"filter":{"block":"filter","settingId":49},"master":{"block":"list","settingId":50},"detail":{"block":"detail","settingId":51}}}', IsActive=1
WHERE Code='sysmenu-list-shell-demo';
UPDATE DynWebPage SET Url='/Platform/Shell/Runtime?id=' || Id
WHERE Code='sysmenu-list-shell-demo';