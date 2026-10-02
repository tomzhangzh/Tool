-- ============================================================
-- VueLibV4  PlatformDb  追加种子（幂等，每次启动执行）
-- 用于已存在平台库增量补齐新种子，不影响既有数据。
-- ============================================================

-- ---------------- 业务项目：学校库（连接串留空 → 回退默认 BusinessDb） ----------------
INSERT INTO DynProject (Code, SolutionId, Name, DbType, ConnectionString, Description, SortNo, IsActive)
SELECT 'Business-School',
       (SELECT Id FROM DesktopSolution WHERE Code='default'),
       '学校业务库', 'Sqlite', NULL,
       '免模型 CRUD 演示项目：Student/Course 等业务表', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM DynProject WHERE Code='Business-School');

-- ---------------- 平台字典：性别 / 班级 ----------------
INSERT INTO DynDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'gender', 'M', '男', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM DynDict WHERE DictType='gender' AND DictCode='M');
INSERT INTO DynDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'gender', 'F', '女', 2, 1
WHERE NOT EXISTS (SELECT 1 FROM DynDict WHERE DictType='gender' AND DictCode='F');

INSERT INTO DynDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'classname', '高一(1)班', '高一(1)班', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM DynDict WHERE DictType='classname' AND DictCode='高一(1)班');
INSERT INTO DynDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'classname', '高一(2)班', '高一(2)班', 2, 1
WHERE NOT EXISTS (SELECT 1 FROM DynDict WHERE DictType='classname' AND DictCode='高一(2)班');
INSERT INTO DynDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'classname', '高二(1)班', '高二(1)班', 3, 1
WHERE NOT EXISTS (SELECT 1 FROM DynDict WHERE DictType='classname' AND DictCode='高二(1)班');
INSERT INTO DynDict (DictType, DictCode, DictName, SortNo, IsActive)
SELECT 'classname', '高二(2)班', '高二(2)班', 4, 1
WHERE NOT EXISTS (SELECT 1 FROM DynDict WHERE DictType='classname' AND DictCode='高二(2)班');

-- ---------------- 桌面快捷方式 ----------------
INSERT INTO DesktopShortcut (Code, SolutionId, Name, Url, Icon, TargetType, SortNo, IsActive)
SELECT 'student-manage',
       (SELECT Id FROM DesktopSolution WHERE Code='default'),
       '学生管理', '/Platform/Page/Demo/Crud', '🧑‍🎓', 'page', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM DesktopShortcut WHERE Code='student-manage');
INSERT INTO DesktopShortcut (Code, SolutionId, Name, Url, Icon, TargetType, SortNo, IsActive)
SELECT 'page-designer',
       (SELECT Id FROM DesktopSolution WHERE Code='default'),
       '页面设计器', '/Platform/Page/Designer', '🎨', 'page', 2, 1
WHERE NOT EXISTS (SELECT 1 FROM DesktopShortcut WHERE Code='page-designer');
INSERT INTO DesktopShortcut (Code, SolutionId, Name, Url, Icon, TargetType, SortNo, IsActive)
SELECT 'demo-action-helper',
       (SELECT Id FROM DesktopSolution WHERE Code='default'),
       '动作助手 Demo', '/Platform/Page/Demo/ActionHelper', '⚡', 'page', 3, 1
WHERE NOT EXISTS (SELECT 1 FROM DesktopShortcut WHERE Code='demo-action-helper');
INSERT INTO DesktopShortcut (Code, SolutionId, Name, Url, Icon, TargetType, SortNo, IsActive)
SELECT 'demo-rpc',
       (SELECT Id FROM DesktopSolution WHERE Code='default'),
       '能力桥 RPC', '/Platform/Page/Demo/Rpc', '🌉', 'page', 4, 1
WHERE NOT EXISTS (SELECT 1 FROM DesktopShortcut WHERE Code='demo-rpc');

-- ---------------- 动作助手演示种子（script / url / api / chain 四类） ----------------
INSERT INTO DynActionHelper (Code, Name, ActionType, Script, Language, ParamsJson, Description, SortNo, IsActive)
SELECT 'ToastDemo', '消息提示演示', 'script',
       'api.showMessage(args.message || ''（来自数据库脚本动作）'', ''success''); return args.message;',
       'javascript', '{"message":{"type":"string"}}', '弹出一条 layer/Element 消息', 10, 1
WHERE NOT EXISTS (SELECT 1 FROM DynActionHelper WHERE Code='ToastDemo');
INSERT INTO DynActionHelper (Code, Name, ActionType, Script, Language, ParamsJson, Description, SortNo, IsActive)
SELECT 'UrlDemo', '页面跳转演示', 'url',
       '/Platform/Page/Desktop',
       'javascript', '{"url":{"type":"string"}}', '跳转到桌面页', 11, 1
WHERE NOT EXISTS (SELECT 1 FROM DynActionHelper WHERE Code='UrlDemo');
INSERT INTO DynActionHelper (Code, Name, ActionType, Script, Language, ParamsJson, Description, SortNo, IsActive)
SELECT 'ApiDemo', '接口调用演示', 'api',
       '/api/platform/dynactionhelper/all',
       'javascript', NULL, 'GET 拉取动作助手列表接口', 12, 1
WHERE NOT EXISTS (SELECT 1 FROM DynActionHelper WHERE Code='ApiDemo');
INSERT INTO DynActionHelper (Code, Name, ActionType, Script, Language, ParamsJson, Description, SortNo, IsActive)
SELECT 'ChainDemo', '动作链演示', 'chain',
       '{"steps":[{"action":"toast","options":{"value":"动作链第 1 步：开始","type":"info"}},{"action":"toast","options":{"value":"动作链第 2 步：执行完成 ✅","type":"success"}}]}',
       'javascript', NULL, '顺序执行多个内置动作的动作链', 13, 1
WHERE NOT EXISTS (SELECT 1 FROM DynActionHelper WHERE Code='ChainDemo');

-- ---------------- 组件元数据补充：免模型增删改查页（Razor View 定义源） ----------------
INSERT INTO ComponentMeta (ComponentName, Label, Category, UiPlatform, LoadUrl, ViewPath, Icon, AcceptAll, AllowDrop, CanDropInto, SlotsDefine, PropsMeta, DefaultConfigJson, IsActive)
SELECT 'DynCrudPage', '免模型增删改查页', '业务', 'ElementUI',
       '/api/component/define/DynCrudPage',
       '/Areas/Component/Views/ElementUI/DynCrudPage.cshtml',
       '🗃️', 0, '[]', '[]', '[]', '[]',
       '{"component":"DynCrudPage","modelname":"","options":{"comoptions":{"title":"数据管理","showFilter":true,"tableOptions":{}},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
       1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynCrudPage');

-- ---------------- 组件元数据补充：栅格容器（设计器默认根组件；旧库增量补齐） ----------------
INSERT INTO ComponentMeta (ComponentName, Label, Category, UiPlatform, LoadUrl, ViewPath, Icon, AcceptAll, AllowDrop, CanDropInto, SlotsDefine, PropsMeta, DefaultConfigJson, IsActive)
SELECT 'DynGridContainer', '栅格容器', '容器', 'Common',
       '/api/component/define/DynGridContainer',
       '/Areas/Component/Views/ElementUI/DynGridContainer.cshtml',
       '🔲', 1, '[]', '[]', '[]', '[]',
       '{"component":"DynGridContainer","modelname":"","options":{"comoptions":{"gridTemplateColumns":"1fr 1fr","gap":"16px"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":"p-4"}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
       1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynGridContainer');

-- ---------------- 动态网页登记：学生管理 ----------------
INSERT INTO DynWebPage (Code, ProjectId, Name, TemplateId, Url, IsActive)
SELECT 'student-manage',
       (SELECT Id FROM DynProject WHERE Code='Business-School'),
       '学生管理', NULL, '/Platform/Page/Demo/Crud', 1
WHERE NOT EXISTS (SELECT 1 FROM DynWebPage WHERE Code='student-manage');

-- ---------------- M4 三屏固定模板（筛选区/列表区/详情区；固定 Razor View 渲染，配置树存 PageSetting） ----------------
INSERT INTO DynTemplate (Code, Name, Category, Icon, DefaultJson, ConfigJson, Description, SortNo, IsActive)
SELECT 'filter-list-detail', '三屏列表页（筛选/列表/详情）', '固定模板', '🗂️',
       NULL,
       '{"params":["code"],"regions":["filter","list","detail"],"runUrl":"/Business/Templates/Run?code={code}","detailUrl":"/Business/Templates/Detail?code={code}","saveUrl":"/Business/Templates/Save?code={code}"}',
       '筛选区与列表区共享 scope；详情区 layer 片段打开，保存后 dyn-actions 刷新表格并关窗',
       10, 1
WHERE NOT EXISTS (SELECT 1 FROM DynTemplate WHERE Code='filter-list-detail');

-- ---------------- 桌面快捷方式：页面生成向导 ----------------
INSERT INTO DesktopShortcut (Code, SolutionId, Name, Url, Icon, TargetType, SortNo, IsActive)
SELECT 'page-gen',
       (SELECT Id FROM DesktopSolution WHERE Code='default'),
       '页面生成向导', '/Platform/Page/PageGen', '🧩', 'page', 4, 1
WHERE NOT EXISTS (SELECT 1 FROM DesktopShortcut WHERE Code='page-gen');

-- ============================================================
-- 通用列名显示名（ProjectId=0, TableName=''）
-- 三屏生成器/管理页未配表字段层时，统一从这里取中文列名；表字段层可覆盖。
-- 靠唯一索引 UX_DynSchemaLabel_Key 幂等，重复执行不报错不重复。
-- ============================================================
INSERT OR IGNORE INTO DynSchemaLabel (ProjectId, TableName, ColumnName, Label) VALUES
 (0,'','Id','主键ID'),
 (0,'','Code','编码'),
 (0,'','Name','名称'),
 (0,'','Category','分类'),
 (0,'','Icon','图标'),
 (0,'','Description','描述'),
 (0,'','SortNo','排序号'),
 (0,'','IsActive','是否启用'),
 (0,'','CreateTime','创建时间'),
 (0,'','UpdateTime','更新时间'),
 (0,'','ExtJson','扩展配置'),
 (0,'','Remark','备注'),
 (0,'','Label','显示名'),
 (0,'','Url','链接地址'),
 (0,'','TargetType','打开方式'),
 (0,'','ParentId','上级'),
 (0,'','DictType','字典类型'),
 (0,'','DictCode','字典编码'),
 (0,'','DictName','字典项'),
 (0,'','DbType','数据库类型'),
 (0,'','ConnectionString','连接字符串'),
 (0,'','SolutionId','所属方案'),
 (0,'','ComponentName','组件标识'),
 (0,'','UiPlatform','UI 平台'),
 (0,'','LoadUrl','加载地址'),
 (0,'','ViewPath','视图路径'),
 (0,'','AcceptAll','接受全部拖入'),
 (0,'','AllowDrop','可拖入类型'),
 (0,'','CanDropInto','可放入容器'),
 (0,'','SlotsDefine','槽位定义'),
 (0,'','PropsMeta','属性面板'),
 (0,'','TemplateContent','模板内容'),
 (0,'','ScriptContent','脚本内容'),
 (0,'','StyleContent','样式内容'),
 (0,'','PropertyConfigJson','属性面板配置'),
 (0,'','DefaultConfigJson','默认配置'),
 (0,'','DefaultJson','默认值'),
 (0,'','ConfigJson','配置'),
 (0,'','ParamsJson','参数配置'),
 (0,'','PageJson','页面配置'),
 (0,'','SpecJson','规格'),
 (0,'','ExtViewPath','扩展视图'),
 (0,'','SettingType','设置类型'),
 (0,'','TableName','数据表'),
 (0,'','RenderMode','渲染模式'),
 (0,'','PartialPath','分部视图'),
 (0,'','ImplementsRole','实现角色'),
 (0,'','HtmlCode','HTML 模板'),
 (0,'','ScriptCode','脚本'),
 (0,'','ParamConfigJson','接线参数配置'),
 (0,'','ParamDefaultJson','接线参数默认值'),
 (0,'','Commands','接受命令'),
 (0,'','Events','发出事件'),
 (0,'','TemplateId','页面模板'),
 (0,'','BlockId','积木'),
 (0,'','Slot','槽位'),
 (0,'','Required','是否必填'),
 (0,'','ActionType','动作类型'),
 (0,'','Script','脚本内容'),
 (0,'','Language','脚本语言');
