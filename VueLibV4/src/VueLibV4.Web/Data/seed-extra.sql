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

-- ============ 2025-10-02 新增组件：live 库手工注册回流（幂等补齐，旧库增量） ============
INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElBadge','徽章','基础','ElementUI','/api/component/define/DynElBadge','/Areas/Component/Views/ElementUI/DynElBadge.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElBadge", "modelname": "", "options": {"comoptions": {"type": "danger", "max": 99, "isDot": false, "defaultValue": 5, "buttonText": "消息通知"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElBadge');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElCascader','级联选择','表单','ElementUI','/api/component/define/DynElCascader','/Areas/Component/Views/ElementUI/DynElCascader.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElCascader", "modelname": "", "options": {"comoptions": {"placeholder": "请选择地区", "clearable": true, "options": [{"value": "sh", "label": "上海", "children": [{"value": "pd", "label": "浦东新区"}, {"value": "hp", "label": "静安区"}]}, {"value": "bj", "label": "北京", "children": [{"value": "hd", "label": "海淀区"}, {"value": "cy", "label": "朝阳区"}]}]}, "comlisteners": {}, "labeloptions": {"label": "地区", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElCascader');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElDescriptions','描述列表','数据','ElementUI','/api/component/define/DynElDescriptions','/Areas/Component/Views/ElementUI/DynElDescriptions.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElDescriptions", "modelname": "", "options": {"comoptions": {"title": "详细信息", "column": 2, "border": true, "items": [{"label": "姓名", "value": "张三"}, {"label": "部门", "value": "技术部"}, {"label": "职位", "value": "工程师"}, {"label": "入职日期", "value": "2024-01-01"}]}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElDescriptions');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElEmpty','空状态','基础','ElementUI','/api/component/define/DynElEmpty','/Areas/Component/Views/ElementUI/DynElEmpty.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElEmpty", "modelname": "", "options": {"comoptions": {"description": "暂无数据", "imageSize": 80}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElEmpty');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElPopover','气泡提示','基础','ElementUI','/api/component/define/DynElPopover','/Areas/Component/Views/ElementUI/DynElPopover.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElPopover", "modelname": "", "options": {"comoptions": {"title": "帮助", "content": "这是一段帮助说明文字", "triggerText": "查看说明"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElPopover');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElStatistic','统计数字','数据','ElementUI','/api/component/define/DynElStatistic','/Areas/Component/Views/ElementUI/DynElStatistic.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElStatistic", "modelname": "", "options": {"comoptions": {"title": "今日订单", "defaultValue": 128, "precision": 0, "prefix": "", "suffix": "单", "subTitle": "环比昨日 +12%", "icon": "📦", "iconColor": "#409eff"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElStatistic');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElTimeline','时间线','数据','ElementUI','/api/component/define/DynElTimeline','/Areas/Component/Views/ElementUI/DynElTimeline.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElTimeline", "modelname": "", "options": {"comoptions": {"items": [{"time": "2024-01-01 10:00", "content": "提交申请", "type": "primary"}, {"time": "2024-01-01 11:30", "content": "部门经理审批通过", "type": "success"}, {"time": "2024-01-02 09:00", "content": "财务审核中", "type": "warning"}]}, "comlisteners": {}, "labeloptions": {"label": "审批记录", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElTimeline');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElTransfer','穿梭框','表单','ElementUI','/api/component/define/DynElTransfer','/Areas/Component/Views/ElementUI/DynElTransfer.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElTransfer", "modelname": "selectedIds", "options": {"comoptions": {"titles": ["待选角色", "已选角色"], "filterable": true, "props": {"key": "key", "label": "label"}, "sourceType": "static", "optionValues": [{"key": 1, "label": "管理员"}, {"key": 2, "label": "开发"}, {"key": 3, "label": "测试"}, {"key": 4, "label": "访客"}]}, "comlisteners": {}, "labeloptions": {"label": "分配角色", "required": false, "show": true, "labelwidth": "100px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElTransfer');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElUpload','文件上传','表单','ElementUI','/api/component/define/DynElUpload','/Areas/Component/Views/ElementUI/DynElUpload.cshtml','📎',0,'[]','[]','[]','[]','{"component": "DynElUpload", "modelname": "", "options": {"comoptions": {"uploadType": "file", "buttonText": "点击上传", "accept": "", "limit": 1}, "comlisteners": {}, "labeloptions": {"label": "文件上传", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElUpload');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynHtmlBox','HTML嵌入','容器','ElementUI','/api/component/define/DynHtmlBox','/Areas/Component/Views/ElementUI/DynHtmlBox.cshtml','🌐',0,'[]','[]','[]','[]','{"component": "DynHtmlBox", "modelname": "", "options": {"comoptions": {"url": "", "params": {}, "refreshOnModelChange": "", "minHeight": "100px"}, "comlisteners": {}, "labeloptions": {"label": "HTML嵌入", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynHtmlBox');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynLookup','弹窗选择器','表单','ElementUI','/api/component/define/DynLookup','/Areas/Component/Views/ElementUI/DynLookup.cshtml','🔍',0,'[]','[]','[]','[]','{"component": "DynLookup", "modelname": "customerId", "options": {"comoptions": {"url": "/Platform/Page/DynWebPage?id=12", "title": "选择客户", "width": "800px", "placeholder": "点击选择", "valueField": "Id", "urlParams": {"projectId": "{{model.projectId}}"}, "mappings": {"Name": "customerName", "Address": "customerAddress", "Phone": "customerPhone"}}, "comlisteners": {}, "labeloptions": {"label": "选择客户", "required": false, "show": true, "labelwidth": "100px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynLookup');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynMarkdown','Markdown 渲染','容器','ElementUI','/api/component/define/DynMarkdown','/Areas/Component/Views/ElementUI/DynMarkdown.cshtml','📝',0,NULL,NULL,NULL,NULL,'{"component": "DynMarkdown", "modelname": "", "options": {"comoptions": {"source": "# 标题\n\n在这里写 **Markdown** 内容...", "html": true}, "comlisteners": {}, "labeloptions": {"label": "Markdown", "required": false, "show": false}, "itemoptions": {}}, "validators": [], "slots": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynMarkdown');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynMermaid','Mermaid 图表','容器','ElementUI','/api/component/define/DynMermaid','/Areas/Component/Views/ElementUI/DynMermaid.cshtml','📊',0,NULL,NULL,NULL,NULL,'{"component": "DynMermaid", "modelname": "", "options": {"comoptions": {"code": "flowchart LR\n    A[开始] --> B{判断}\n    B -->|是| C[处理]\n    B -->|否| D[结束]\n    C --> D", "theme": "default"}, "comlisteners": {}, "labeloptions": {"label": "Mermaid", "required": false, "show": false}, "itemoptions": {}}, "validators": [], "slots": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynMermaid');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynModelWatcher','联动监听器(隐藏)','其他','ElementUI','/api/component/define/DynModelWatcher','/Areas/Component/Views/ElementUI/DynModelWatcher.cshtml','🔌',0,'[]','[]','[]','[]','{"component": "DynModelWatcher", "modelname": "", "options": {"comoptions": {"watchModel": "provinceId", "onChangeJs": "// newVal 是新值, oldVal 是旧值, model 是整个表单\nconsole.log(''province changed:'', newVal);", "onChangeAction": ""}, "comlisteners": {}, "labeloptions": {"label": "监听器", "required": false, "show": false}, "itemoptions": {"style": {"display": "none"}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynModelWatcher');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynRpcLoader','数据桥(隐藏)','其他','ElementUI','/api/component/define/DynRpcLoader','/Areas/Component/Views/ElementUI/DynRpcLoader.cshtml','🔌',0,'[]','[]','[]','[]','{"component": "DynRpcLoader", "modelname": "", "options": {"comoptions": {"url": "/api/user/current", "method": "GET", "autoLoad": true, "params": {}, "targetModel": "", "onSuccessJs": "// result 是接口返回数据, model 是表单 model\nconsole.log(''loaded:'', result);", "onSuccessAction": ""}, "comlisteners": {}, "labeloptions": {"label": "RPC调用", "required": false, "show": false}, "itemoptions": {"style": {"display": "none"}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynRpcLoader');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,DefaultConfigJson,IsActive)
SELECT 'DynElDateRange','日期范围','表单','ElementUI','/api/component/define/DynElDateRange','/Areas/Component/Views/ElementUI/DynElDateRange.cshtml','📅',0,'[]','["DynElFormItem"]','["default"]','[]','{"component":"DynElDateRange","modelname":"","options":{"comoptions":{"startModel":"","endModel":"","pickerType":"datetimerange","valueFormat":"YYYY-MM-DD HH:mm:ss","startPlaceholder":"开始日期","endPlaceholder":"结束日期"},"comlisteners":{},"labeloptions":{"label":"日期范围","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElDateRange');

-- ViewPath 修正：旧种子指向已删除/已移走的视图文件，回退会渲染空定义
UPDATE ComponentMeta SET ViewPath='/Areas/Component/Views/ElementUI/DynElSelect.cshtml' WHERE ComponentName='DynElSelect' AND (ViewPath IS NULL OR ViewPath LIKE '%/Select.cshtml');
UPDATE ComponentMeta SET ViewPath='/Areas/Component/Views/ElementUI/DynElRadioGroup.cshtml' WHERE ComponentName='DynElRadioGroup' AND (ViewPath IS NULL OR ViewPath LIKE '%/Radio.cshtml');

-- DynElImage 默认配置补本地示例图：仅当仍是出厂形态（空 src 或官方外链 logo）时更新，不覆盖用户自定义配置
UPDATE ComponentMeta
SET DefaultConfigJson='{"component":"DynElImage","modelname":"","options":{"comoptions":{"src":"/img/sample-image.jpg"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{"width":"240px","height":"160px","borderRadius":"8px"},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}'
WHERE ComponentName='DynElImage'
  AND (DefaultConfigJson LIKE '%"src":""%'
       OR DefaultConfigJson LIKE '%element-plus.org/images/element-plus-logo.svg%');

-- ---------------- 新组件：DynSlotHost 运行时占位符 / 插槽宿主 ----------------
INSERT INTO ComponentMeta
(ComponentName, Label, Category, UiPlatform, LoadUrl, ViewPath, Icon, AcceptAll, AllowDrop, CanDropInto, SlotsDefine, PropsMeta, DefaultConfigJson, IsActive)
SELECT 'DynSlotHost','插槽宿主','容器','ElementUI','/api/component/define/DynSlotHost','/Areas/Component/Views/ElementUI/DynSlotHost.cshtml','🔲',0,'[]','[]','[]','[]',
'{"component":"DynSlotHost","modelname":"","options":{"comoptions":{"slotId":"","showLabel":false,"minHeight":"60px"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false,"labelwidth":"110px"},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynSlotHost');

-- ============ 2025-10-05 老组件回流：以下 11 个组件历史上只手工注册在 live 库，版本化种子缺失（全新库会丢） ============
INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynEChart','ECharts图表','图表','Web','/api/component/define/DynEChart','/Areas/Component/Views/ElementUI/DynEChart.cshtml','📊',0,'0','0',NULL,NULL,'{"component": "DynElContainer", "modelname": "", "options": {"comoptions": {"direction": "vertical"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"gap": "0"}, "class": ""}}, "validators": [], "childrenctrls": [{"component": "DynElSelect", "modelname": "options.comoptions.chartType", "options": {"comoptions": {"sourceType": "static", "optionValues": [{"label": "柱状图", "value": "bar"}, {"label": "折线图", "value": "line"}, {"label": "面积图", "value": "area"}, {"label": "饼图", "value": "pie"}, {"label": "雷达图", "value": "radar"}], "clearable": false, "placeholder": "图表类型"}, "labeloptions": {"label": "图表类型", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElInput", "modelname": "options.comoptions.title", "options": {"comoptions": {"placeholder": "图表标题"}, "labeloptions": {"label": "标题", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size"}}, {"component": "DynElInput", "modelname": "options.comoptions.height", "options": {"comoptions": {"placeholder": "320"}, "labeloptions": {"label": "高度(px)", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size"}}, {"component": "DynElInput", "modelname": "options.comoptions.seriesName", "options": {"comoptions": {"placeholder": "单系列名"}, "labeloptions": {"label": "系列名", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size"}}, {"component": "DynElSwitch", "modelname": "options.comoptions.smooth", "options": {"comoptions": {"activeValue": true, "inactiveValue": false}, "labeloptions": {"label": "平滑曲线", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size"}}]}','{"component": "DynEChart", "modelname": "chartData", "options": {"comoptions": {"chartType": "bar", "title": "图表", "height": 320, "seriesName": "数据"}, "comlisteners": {}, "labeloptions": {"label": "图表", "required": false, "show": true}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}}',1,0,NULL,NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynEChart');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynElEmojiPicker','Emoji图标选择','表单','ElementUI','/api/component/define/DynElEmojiPicker','/Areas/Component/Views/ElementUI/EmojiPicker.cshtml','😀',0,'[]','["DynElFormItem"]','["default"]','[]','{"component": "DynElContainer", "modelname": "", "options": {"comoptions": {"direction": "vertical"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"gap": "0"}, "class": ""}}, "validators": [], "childrenctrls": [{"component": "DynElSelect", "modelname": "options.comoptions.size", "options": {"labeloptions": {"label": "控件尺寸", "show": true}, "comoptions": {"sourceType": "static", "optionValuesText": "large,大\\ndefault,默认\\nsmall,小"}}}, {"component": "DynElSwitch", "modelname": "options.comoptions.disabled", "options": {"labeloptions": {"label": "禁用", "show": true}}}, {"component": "DynElSwitch", "modelname": "options.comoptions.clearable", "options": {"labeloptions": {"label": "可清空", "show": true}}}, {"component": "DynElInput", "modelname": "options.comoptions.placeholder", "options": {"labeloptions": {"label": "占位提示", "show": true}}}], "slots": {}, "extendinfo": {}}','{"component": "DynElEmojiPicker", "modelname": "", "options": {"comoptions": {"placeholder": "搜索 Emoji（中/英文）", "clearable": true}, "comlisteners": {}, "labeloptions": {"label": "图标", "required": false, "show": true}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1,0,NULL,NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElEmojiPicker');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynElSteps','步骤条','容器','ElementUI','/api/component/define/DynElSteps','/Areas/Component/Views/ElementUI/DynElSteps.cshtml','🪜',1,'[]','["DynElCard", "DynForm"]','["default"]','[]','{"component": "DynElContainer", "options": {"comoptions": {"direction": "vertical"}, "itemoptions": {"style": {"padding": "8px"}}}, "childrenctrls": [{"component": "DynElSelect", "modelname": "options.comoptions.direction", "options": {"comoptions": {"optionValues": [{"label": "水平", "value": "horizontal"}, {"label": "垂直", "value": "vertical"}]}, "labeloptions": {"label": "方向 direction", "show": true, "labelWidth": "110px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}, {"component": "DynElInputNumber", "modelname": "options.comoptions.active", "options": {"comoptions": {"min": 0}, "labeloptions": {"label": "当前步骤 active", "show": true, "labelWidth": "110px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}, {"component": "DynElSelect", "modelname": "options.comoptions.finishStatus", "options": {"comoptions": {"optionValues": [{"label": "等待 wait", "value": "wait"}, {"label": "进行中 process", "value": "process"}, {"label": "完成 finish", "value": "finish"}, {"label": "错误 error", "value": "error"}, {"label": "成功 success", "value": "success"}]}, "labeloptions": {"label": "完成状态 finishStatus", "show": true, "labelWidth": "110px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}, {"component": "DynElSelect", "modelname": "options.comoptions.processStatus", "options": {"comoptions": {"optionValues": [{"label": "等待 wait", "value": "wait"}, {"label": "进行中 process", "value": "process"}, {"label": "完成 finish", "value": "finish"}, {"label": "错误 error", "value": "error"}, {"label": "成功 success", "value": "success"}]}, "labeloptions": {"label": "进行中状态 processStatus", "show": true, "labelWidth": "110px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}, {"component": "DynElSwitch", "modelname": "options.comoptions.alignCenter", "options": {"comoptions": {}, "labeloptions": {"label": "标题居中 alignCenter", "show": true, "labelWidth": "110px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}, {"component": "DynElSwitch", "modelname": "options.comoptions.simple", "options": {"comoptions": {}, "labeloptions": {"label": "简洁模式 simple", "show": true, "labelWidth": "110px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}]}','{"component": "DynElSteps", "modelname": "", "options": {"comoptions": {"active": 0, "direction": "horizontal"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [{"component": "DynElStep", "options": {"comoptions": {"title": "步骤一", "description": "填写基本信息", "status": "", "icon": ""}}, "childrenctrls": []}, {"component": "DynElStep", "options": {"comoptions": {"title": "步骤二", "description": "提交审核", "status": "", "icon": ""}}, "childrenctrls": []}, {"component": "DynElStep", "options": {"comoptions": {"title": "步骤三", "description": "完成", "status": "", "icon": ""}}, "childrenctrls": []}], "slots": {}, "extendinfo": {}}',1,1,'{}','{"childrenSchemaPath": "childrenctrls", "childKey": "component", "childTitleField": "options.comoptions.title", "supportDragSort": true}','[{"operateKey": "stepsCfg", "label": "步骤项配置", "icon": "⚙", "type": "view", "viewName": "StepsConfigView"}]'
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElSteps');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynElTable','表格(模板)','表单','ElementUI','/api/component/define/DynElTable','/Areas/Component/Views/ElementUI/DynElTable.cshtml','📊',1,'[]','[]','["default"]','[]','{"component":"DynElContainer","modelname":"","validators":[],"childrenctrls":[{"component":"DynElSelect","modelname":"options.comoptions.size","options":{"labeloptions":{"label":"\u63A7\u4EF6\u5C3A\u5BF8","show":true},"comoptions":{"sourceType":"static","optionValuesText":"large,\u5927\ndefault,\u9ED8\u8BA4\nsmall,\u5C0F"},"itemoptions":{}}},{"component":"DynElSwitch","modelname":"options.comoptions.disabled","options":{"labeloptions":{"label":"\u7981\u7528","show":true},"itemoptions":{}}},{"component":"DynElSwitch","modelname":"options.comoptions.border","options":{"labeloptions":{"label":"\u663E\u793A\u8FB9\u6846","show":true},"itemoptions":{}}},{"component":"DynElSwitch","modelname":"options.comoptions.stripe","options":{"labeloptions":{"label":"\u6591\u9A6C\u7EB9","show":true},"itemoptions":{}}},{"component":"DynElTextarea","modelname":"options.comoptions.optionValuesText","options":{"labeloptions":{"label":"\u7B80\u6613\u5217\u914D\u7F6E(prop,label,width,align\u6362\u884C)","show":true},"comoptions":{"rows":6,"placeholder":"name,\u59D3\u540D,150,left"},"itemoptions":{}}},{"component":"DynElTextarea","modelname":"options.comoptions.columnTemplate","options":{"labeloptions":{"label":"\u9AD8\u7EA7Vue\u5217\u6A21\u677F(\u4F18\u5148\u4E8E\u7B80\u6613\u5217)","show":true},"comoptions":{"rows":12,"placeholder":"\u003Cel-table-column prop=\u0022name\u0022 label=\u0022\u59D3\u540D\u0022 width=\u0022150\u0022 /\u003E"},"itemoptions":{}}},{"component":"DynElTextarea","modelname":"options.comoptions.demoRows","options":{"labeloptions":{"label":"\u6F14\u793A\u6570\u636E(JSON\u6570\u7EC4,\u9884\u89C8\u7528)","show":true},"comoptions":{"rows":6,"placeholder":"[{\u0022name\u0022:\u0022\u5F20\u4E09\u0022,\u0022status\u0022:1}]"},"itemoptions":{}}},{"component":"DynElInput","modelname":"options.comoptions.height","options":{"labeloptions":{"label":"\u8868\u683C\u9AD8\u5EA6","show":true},"itemoptions":{}}},{"component":"DynElInputNumber","modelname":"options.comoptions.maxHeight","options":{"labeloptions":{"label":"\u6700\u5927\u9AD8\u5EA6","show":true},"comoptions":{},"itemoptions":{}}}],"slots":{},"extendinfo":{}}','{"component":"DynElTable","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1,0,'',NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynElTable');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynJsonDesigner','JSON配置设计器','表单','ElementUI','/api/component/define/DynJsonDesigner','/Areas/Component/Views/ElementUI/JsonDesigner.cshtml','🛠',0,'[]','["DynElFormItem"]','["default"]','[]','{"component": "DynElContainer", "modelname": "", "options": {"comoptions": {"direction": "vertical"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"gap": "0"}, "class": ""}}, "validators": [], "childrenctrls": [{"component": "DynElInputNumber", "modelname": "options.comoptions.rows", "options": {"labeloptions": {"label": "显示行数", "show": true}}}, {"component": "DynElInput", "modelname": "options.comoptions.title", "options": {"labeloptions": {"label": "设计器标题", "show": true}}}, {"component": "DynElSwitch", "modelname": "options.comoptions.disabled", "options": {"labeloptions": {"label": "禁用", "show": true}}}, {"component": "DynElInput", "modelname": "options.comoptions.placeholder", "options": {"labeloptions": {"label": "占位提示", "show": true}}}], "slots": {}, "extendinfo": {}}','{"component": "DynJsonDesigner", "modelname": "", "options": {"comoptions": {"rows": 8, "title": "组件配置设计器", "placeholder": "{}"}, "comlisteners": {}, "labeloptions": {"label": "JSON配置", "required": false, "show": true}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1,0,NULL,NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynJsonDesigner');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynListView','卡片列表ListView','表格&列表','Web','/api/component/define/DynListView','/Areas/Component/Views/ElementUI/DynListView.cshtml','🗂️',0,'0','0',NULL,NULL,'{"component": "DynElContainer", "modelname": "", "options": {"comoptions": {"direction": "vertical"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"gap": "0"}, "class": ""}}, "validators": [], "childrenctrls": [{"component": "DynElInput", "modelname": "options.comoptions.titleField", "options": {"comoptions": {"placeholder": "标题字段"}, "labeloptions": {"label": "标题字段", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElInput", "modelname": "options.comoptions.descField", "options": {"comoptions": {"placeholder": "描述字段"}, "labeloptions": {"label": "描述字段", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElInput", "modelname": "options.comoptions.tagField", "options": {"comoptions": {"placeholder": "标签字段"}, "labeloptions": {"label": "标签字段", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElInput", "modelname": "options.comoptions.imageField", "options": {"comoptions": {"placeholder": "图片字段"}, "labeloptions": {"label": "图片字段", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElInput", "modelname": "options.comoptions.columns", "options": {"comoptions": {"placeholder": "卡片列布局(grid-template)"}, "labeloptions": {"label": "卡片列布局(grid-template)", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElInput", "modelname": "options.comoptions.gap", "options": {"comoptions": {"placeholder": "卡片间距"}, "labeloptions": {"label": "卡片间距", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}]}','{"component": "DynListView", "modelname": "rows", "options": {"comoptions": {"titleField": "Name", "descField": "Remark", "tagField": "", "imageField": "", "gap": "16px", "columns": "repeat(auto-fill, minmax(220px, 1fr))"}, "comlisteners": {}, "labeloptions": {"label": "卡片列表", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}}',1,0,NULL,NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynListView');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynMacSidebar','Mac侧边栏','容器','ElementUI','/api/component/define/DynMacSidebar','/Areas/Component/Views/ElementUI/DynMacSidebar.cshtml','📑',1,'[]','["DynElCard", "DynForm"]','["default"]','[]','{"component": "DynElContainer", "options": {"comoptions": {"direction": "vertical"}, "itemoptions": {"style": {"padding": "8px"}}}, "childrenctrls": [{"component": "DynElInput", "modelname": "options.comoptions.title", "options": {"comoptions": {}, "labeloptions": {"label": "导航标题", "show": true, "labelWidth": "90px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}]}','{"component": "DynMacSidebar", "modelname": "", "options": {"comoptions": {"title": "导航"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}, "validators": [], "childrenctrls": [{"component": "DynMacSidebarItem", "options": {"comoptions": {"icon": "👤"}, "labeloptions": {"label": "Profile", "show": true}}, "childrenctrls": []}, {"component": "DynMacSidebarItem", "options": {"comoptions": {"icon": "📁"}, "labeloptions": {"label": "Projects", "show": true}}, "childrenctrls": []}, {"component": "DynMacSidebarItem", "options": {"comoptions": {"icon": "⭐"}, "labeloptions": {"label": "Favorites", "show": true}}, "childrenctrls": []}], "slots": {}, "extendinfo": {}}}',1,1,'{}','{"childrenSchemaPath": "childrenctrls", "childKey": "component", "childTitleField": "options.labeloptions.label", "supportDragSort": true}','[]'
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynMacSidebar');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynMacWindow','Mac窗口','容器','ElementUI','/api/component/define/DynMacWindow','/Areas/Component/Views/ElementUI/DynMacWindow.cshtml','🖥️',1,'[]','["DynElCard", "DynForm"]','["default"]','[]','{"component": "DynElContainer", "options": {"comoptions": {"direction": "vertical"}, "itemoptions": {"style": {"padding": "8px"}}}, "childrenctrls": [{"component": "DynElInput", "modelname": "options.comoptions.title", "options": {"comoptions": {}, "labeloptions": {"label": "窗口标题", "show": true, "labelWidth": "90px"}, "itemoptions": {"style": {}, "class": ""}}, "childrenctrls": []}]}','{"component": "DynMacWindow", "modelname": "", "options": {"comoptions": {"title": "Mac 窗口"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"width": "480px", "height": "320px"}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}}',1,1,'{}','{"childrenSchemaPath": "childrenctrls", "childKey": "component", "childTitleField": "options.comoptions.title", "supportDragSort": true}','[]'
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynMacWindow');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynSplitter','分隔面板','通用','ElementUI','/api/component/define/DynSplitter','/Areas/Component/Views/ElementUI/DynSplitter.cshtml','⇔',1,'[]','[]','[]','[]','{"component": "DynElContainer", "options": {"comoptions": {"direction": "vertical"}, "itemoptions": {"style": {"padding": "8px"}}}, "childrenctrls": [{"component": "DynElSelect", "modelname": "options.comoptions.direction", "options": {"comoptions": {"sourceType": "static", "staticData": [{"label": "左右分隔", "value": "horizontal"}, {"label": "上下分隔", "value": "vertical"}], "placeholder": "布局方向"}, "labeloptions": {"label": "布局方向", "show": true, "required": false}, "itemoptions": {"style": {"marginBottom": "12px"}}}}, {"component": "DynElInputNumber", "modelname": "options.comoptions.split", "options": {"labeloptions": {"label": "初始比例 %", "show": true, "required": false}, "comoptions": {"min": 5, "max": 95, "step": 5}, "itemoptions": {"style": {"marginBottom": "12px"}}}}, {"component": "DynElInputNumber", "modelname": "options.comoptions.minSize", "options": {"labeloptions": {"label": "最小尺寸 px", "show": true, "required": false}, "comoptions": {"min": 40, "max": 600}, "itemoptions": {"style": {"marginBottom": "12px"}}}}, {"component": "DynElInputNumber", "modelname": "options.comoptions.gutterSize", "options": {"labeloptions": {"label": "分隔条宽 px", "show": true, "required": false}, "comoptions": {"min": 2, "max": 24}, "itemoptions": {"style": {"marginBottom": "12px"}}}}]}','{"component": "DynSplitter", "modelname": "", "options": {"comoptions": {"direction": "horizontal", "split": 30, "minSize": 120, "gutterSize": 6}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"height": "100%"}}, "comInnerInfo": {}}, "validators": [], "childrenctrls": [{"component": "DynElContainer", "modelname": "", "options": {"comoptions": {}, "labeloptions": {"label": "面板A", "show": true}, "itemoptions": {"style": {"padding": "8px"}}}, "childrenctrls": []}, {"component": "DynElContainer", "modelname": "", "options": {"comoptions": {}, "labeloptions": {"label": "面板B", "show": true}, "itemoptions": {"style": {"padding": "8px"}}}, "childrenctrls": []}], "slots": {}, "extendinfo": {}}',1,1,NULL,NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynSplitter');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynViewSwitcher','视图切换容器(Grid/ListView)','容器','Web','/api/component/define/DynViewSwitcher','/Areas/Component/Views/ElementUI/DynViewSwitcher.cshtml','🔄',0,'1','1',NULL,NULL,'{"component": "DynElContainer", "modelname": "", "options": {"comoptions": {"direction": "vertical"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {"gap": "0"}, "class": ""}}, "validators": [], "childrenctrls": [{"component": "DynElSelect", "modelname": "options.comoptions.defaultMode", "options": {"comoptions": {"sourceType": "static", "optionValues": [{"label": "表格", "value": "grid"}, {"label": "卡片", "value": "list"}], "clearable": false, "placeholder": "默认视图"}, "labeloptions": {"label": "默认视图", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}, {"component": "DynElSwitch", "modelname": "options.comoptions.pager", "options": {"comoptions": {"activeValue": true, "inactiveValue": false}, "labeloptions": {"label": "共享分页", "required": false, "show": true}, "itemoptions": {"style": {"marginBottom": "8px"}}, "compassthrough": "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"}}]}','{"component": "DynViewSwitcher", "modelname": "", "options": {"comoptions": {"defaultMode": "grid", "pager": true, "pageSizes": [10, 20, 50, 100], "pageInfoPath": "pageInfo"}, "comlisteners": {}, "labeloptions": {"label": "视图切换", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}}',1,1,NULL,NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynViewSwitcher');

INSERT INTO ComponentMeta (ComponentName,Label,Category,UiPlatform,LoadUrl,ViewPath,Icon,AcceptAll,AllowDrop,CanDropInto,SlotsDefine,PropsMeta,PropertyConfigJson,DefaultConfigJson,IsActive,IsContainer,ExtJson,DesignerMeta,DesignerOperates)
SELECT 'DynVueTemplate','Vue模板','自定义','Common','/api/component/define/DynVueTemplate','/Areas/Component/Views/ElementUI/DynVueTemplate.cshtml','🧩',1,'[]','[]','["default"]','[]','{"component":"DynElContainer","options":{"comoptions":{"direction":"vertical"},"labeloptions":{"show":false}},"childrenctrls":[{"component":"DynElSwitch","modelname":"options.labeloptions.show","options":{"labeloptions":{"label":"显示标签","show":true}}},{"component":"DynElInput","modelname":"options.labeloptions.label","options":{"labeloptions":{"label":"标签文字","show":true}}},{"component":"DynElTextarea","modelname":"options.comoptions.template","options":{"comoptions":{"rows":14,"placeholder":"<div>...</div>"},"labeloptions":{"label":"Vue模板片段","show":true}}},{"component":"DynElInput","modelname":"options.comoptions.wrapperClass","options":{"labeloptions":{"label":"外层容器Class","show":true}}}]}','{"component":"DynVueTemplate","modelname":"","options":{"comoptions":{"template":"","wrapperClass":""},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1,0,'',NULL,NULL
WHERE NOT EXISTS (SELECT 1 FROM ComponentMeta WHERE ComponentName='DynVueTemplate');

