-- ---------------- 22. V2 详情弹窗片段模板 detail-modal-v2（Layout=null，layui layer 加载） ----------------

INSERT INTO DynTemplate (Code, Name, Category, Icon, ViewPath, DefaultJson, ConfigJson, Description, SortNo, IsActive)
SELECT 'detail-modal-v2','详情弹窗(V2)','布局壳','🪟','~/Views/DynTemplates/DetailModalV2.cshtml',NULL,NULL,
'V2 弹窗片段：Layout=null，layui layer 加载，DynEventBus 通信。',33,1
WHERE NOT EXISTS (SELECT 1 FROM DynTemplate WHERE Code='detail-modal-v2');
UPDATE DynTemplate SET ViewPath='~/Views/DynTemplates/DetailModalV2.cshtml', IsActive=1
WHERE Code='detail-modal-v2';