-- ---------------- 21. V2 布局壳：filter-list-drawer-left（顶筛选 + 列表 + 左侧抽屉编辑） ----------------

INSERT INTO DynTemplate (Code, Name, Category, Icon, ViewPath, DefaultJson, ConfigJson, Description, SortNo, IsActive, SupportGen)
SELECT 'filter-list-drawer-leftV2','筛选列表左侧抽屉(V2)','布局壳','📂','~/Views/DynTemplates/FilterListDrawerLeft.cshtml','{"layout":"filter-list-drawer-leftV2","provide":{"table":"","keyField":"Id","project":""},"slots":{"filter":{"block":"filter","settingId":null},"list":{"block":"list","settingId":null},"detail":{"settingId":null,"title":"明细","width":"45%"}}}',NULL,
'V2版：顶筛选+列表+左侧抽屉编辑；DynEventBus 通信；filter 查询→list 加载；list 新增/编辑→左侧抽屉滑出 detail；detail 保存→emit refresh 刷新 list。',32,1,1
WHERE NOT EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-drawer-leftV2');
UPDATE DynTemplate SET Name='筛选列表左侧抽屉(V2)', Category='布局壳', Icon='📂', ViewPath='~/Views/DynTemplates/FilterListDrawerLeft.cshtml',
    DefaultJson='{"layout":"filter-list-drawer-leftV2","provide":{"table":"","keyField":"Id","project":""},"slots":{"filter":{"block":"filter","settingId":null},"list":{"block":"list","settingId":null},"detail":{"settingId":null,"title":"明细","width":"45%"}}}', SupportGen=1,
    Description='V2版：顶筛选+列表+左侧抽屉编辑；DynEventBus 通信；filter 查询→list 加载；list 新增/编辑→左侧抽屉滑出 detail；detail 保存→emit refresh 刷新 list。'
WHERE Code='filter-list-drawer-leftV2';