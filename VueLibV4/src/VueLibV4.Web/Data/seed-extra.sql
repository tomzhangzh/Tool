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

-- ---------------- 动态网页登记：学生管理 ----------------
INSERT INTO DynWebPage (Code, ProjectId, Name, TemplateId, Url, IsActive)
SELECT 'student-manage',
       (SELECT Id FROM DynProject WHERE Code='Business-School'),
       '学生管理', NULL, '/Platform/Page/Demo/Crud', 1
WHERE NOT EXISTS (SELECT 1 FROM DynWebPage WHERE Code='student-manage');
