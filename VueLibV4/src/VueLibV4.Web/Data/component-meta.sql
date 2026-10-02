-- ============================================================
-- ComponentMeta 种子数据（仅在 ComponentMeta 为空时由初始化器执行）
-- 命名规范（注册为 Vue 全局组件名，即 DefaultConfigJson.component 的取值）：
--   ElementUI -> DynElXXX   (DynElInput / DynElSelect ...)
--   公共      -> DynXXX     (DynText / DynForm / DynDynamicCom ...)
--   NutUI     -> DynNXXX    (DynNInput ...)
-- ViewPath 指向后端 Razor 视图文件（文件名可保持业务名）
-- ============================================================

INSERT INTO ComponentMeta
(ComponentName, Label, Category, UiPlatform, LoadUrl, ViewPath, Icon, AcceptAll, AllowDrop, CanDropInto, SlotsDefine, PropsMeta, DefaultConfigJson, IsActive)
VALUES
-- ============ 表单控件 FormItem (ElementUI -> DynEl*) ============
('DynElInput','输入框','表单','ElementUI','/api/component/define/DynElInput','/Areas/Component/Views/ElementUI/Input.cshtml','⌨️',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElInput","modelname":"","options":{"comoptions":{"placeholder":"请输入","clearable":true},"comlisteners":{},"labeloptions":{"label":"输入框","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElTextarea','多行文本','表单','ElementUI','/api/component/define/DynElTextarea','/Areas/Component/Views/ElementUI/ComTextArea.cshtml','📝',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElTextarea","modelname":"","options":{"comoptions":{"placeholder":"请输入","rows":3},"comlisteners":{},"labeloptions":{"label":"多行文本","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElInputNumber','数字输入','表单','ElementUI','/api/component/define/DynElInputNumber','/Areas/Component/Views/ElementUI/InputNumber.cshtml','🔢',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElInputNumber","modelname":"","options":{"comoptions":{"min":null,"max":null,"step":1},"comlisteners":{},"labeloptions":{"label":"数字输入","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElPassword','密码框','表单','ElementUI','/api/component/define/DynElPassword','/Areas/Component/Views/ElementUI/ComPassword.cshtml','🔒',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElPassword","modelname":"","options":{"comoptions":{"placeholder":"请输入密码"},"comlisteners":{},"labeloptions":{"label":"密码框","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElSelect','下拉选择','表单','ElementUI','/api/component/define/DynElSelect','/Areas/Component/Views/ElementUI/DynElSelect.cshtml','📋',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElSelect","modelname":"","options":{"comoptions":{"placeholder":"请选择","clearable":true,"filterable":false,"multiple":false,"optionValues":[]},"comlisteners":{},"labeloptions":{"label":"下拉选择","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElRadioGroup','单选框组','表单','ElementUI','/api/component/define/DynElRadioGroup','/Areas/Component/Views/ElementUI/DynElRadioGroup.cshtml','🔘',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElRadioGroup","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"单选框组","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElCheckboxGroup','多选框组','表单','ElementUI','/api/component/define/DynElCheckboxGroup','/Areas/Component/Views/ElementUI/Checkbox.cshtml','☑️',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElCheckboxGroup","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"多选框组","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElSwitch','开关','表单','ElementUI','/api/component/define/DynElSwitch','/Areas/Component/Views/ElementUI/Switch.cshtml','🔛',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElSwitch","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"开关","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElDatePicker','日期选择','表单','ElementUI','/api/component/define/DynElDatePicker','/Areas/Component/Views/ElementUI/DatePicker.cshtml','📅',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElDatePicker","modelname":"","options":{"comoptions":{"type":"date","valueFormat":"YYYY-MM-DD"},"comlisteners":{},"labeloptions":{"label":"日期选择","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElTimePicker','时间选择','表单','ElementUI','/api/component/define/DynElTimePicker','/Areas/Component/Views/ElementUI/TimePicker.cshtml','⏰',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElTimePicker","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"时间选择","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElSlider','滑块','表单','ElementUI','/api/component/define/DynElSlider','/Areas/Component/Views/ElementUI/Slider.cshtml','🎚️',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElSlider","modelname":"","options":{"comoptions":{"min":0,"max":100},"comlisteners":{},"labeloptions":{"label":"滑块","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElRate','评分','表单','ElementUI','/api/component/define/DynElRate','/Areas/Component/Views/ElementUI/Rate.cshtml','⭐',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElRate","modelname":"","options":{"comoptions":{"max":5},"comlisteners":{},"labeloptions":{"label":"评分","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElColorPicker','颜色选择','表单','ElementUI','/api/component/define/DynElColorPicker','/Areas/Component/Views/ElementUI/ColorPicker.cshtml','🎨',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElColorPicker","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"颜色选择","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

-- ============ 基础展示 (ElementUI -> DynEl*) ============
('DynElButton','按钮','基础','ElementUI','/api/component/define/DynElButton','/Areas/Component/Views/ElementUI/Button.cshtml','🔘',0,'[]','[]','["default"]','[]',
 '{"component":"DynElButton","modelname":"","options":{"comoptions":{"type":"primary"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElAlert','警告提示','基础','ElementUI','/api/component/define/DynElAlert','/Areas/Component/Views/ElementUI/Alert.cshtml','🚨',0,'[]','[]','[]','[]',
 '{"component":"DynElAlert","modelname":"","options":{"comoptions":{"title":"提示","type":"info"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElTag','标签','基础','ElementUI','/api/component/define/DynElTag','/Areas/Component/Views/ElementUI/Tag.cshtml','🏷️',0,'[]','[]','[]','[]',
 '{"component":"DynElTag","modelname":"","options":{"comoptions":{"text":"标签","type":""},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElDivider','分割线','基础','ElementUI','/api/component/define/DynElDivider','/Areas/Component/Views/ElementUI/Divider.cshtml','➖',0,'[]','[]','[]','[]',
 '{"component":"DynElDivider","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElImage','图片','基础','ElementUI','/api/component/define/DynElImage','/Areas/Component/Views/ElementUI/Image.cshtml','🌄',0,'[]','[]','[]','[]',
 '{"component":"DynElImage","modelname":"","options":{"comoptions":{"src":""},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElProgress','进度条','基础','ElementUI','/api/component/define/DynElProgress','/Areas/Component/Views/ElementUI/Progress.cshtml','📊',0,'[]','[]','[]','[]',
 '{"component":"DynElProgress","modelname":"","options":{"comoptions":{"percentage":0},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

-- ============ 容器 Container (ElementUI -> DynEl*) ============
('DynElRow','栅格行','容器','ElementUI','/api/component/define/DynElRow','/Areas/Component/Views/ElementUI/Row.cshtml','▬',1,'[]','["DynElRow","DynForm","DynElCard"]','["default"]','[]',
 '{"component":"DynElRow","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElCol','栅格列','容器','ElementUI','/api/component/define/DynElCol','/Areas/Component/Views/ElementUI/Col.cshtml','▫️',1,'[]','["DynElRow"]','["default"]','[]',
 '{"component":"DynElCol","modelname":"","options":{"comoptions":{"span":12},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElCard','卡片','容器','ElementUI','/api/component/define/DynElCard','/Areas/Component/Views/ElementUI/Card.cshtml','🗃️',1,'[]','["DynElRow","DynElCol","DynForm"]','["default"]','[]',
 '{"component":"DynElCard","modelname":"","options":{"comoptions":{"title":"卡片标题"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElTabs','标签页','容器','ElementUI','/api/component/define/DynElTabs','/Areas/Component/Views/ElementUI/Tabs.cshtml','🗂️',1,'[]','["DynElCard","DynForm"]','["default"]','[]',
 '{"component":"DynElTabs","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

-- ============ 公共 Common (Dyn*) ============
('DynText','文本','通用','Common','/api/component/define/DynText','/Areas/Component/Views/ElementUI/DynText.cshtml','🔤',0,'[]','[]','[]','[]',
 '{"component":"DynText","modelname":"","options":{"comoptions":{"text":"文本内容"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynForm','动态表单','容器','Common','/api/component/define/DynForm','/Areas/Component/Views/ElementUI/DynForm.cshtml','📋',1,'["DynElFormItem","DynElInput","DynElSelect","DynElDatePicker"]','[]','["default"]','[]',
 '{"component":"DynForm","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynTable','表格','容器','Common','/api/component/define/DynTable','/Areas/Component/Views/ElementUI/DynTable.cshtml','📑',1,'[]','[]','["default"]','[]',
 '{"component":"DynTable","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynDynamicCom','动态组件渲染器','通用','Common','/api/component/define/DynDynamicCom','/Areas/Component/Views/Common/DynDynamicCom.cshtml','🧩',1,'[]','[]','["default"]','[]',
 '{"component":"DynDynamicCom","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElContainer','容器','通用','ElementUI','/api/component/define/DynElContainer','/Areas/Component/Views/ElementUI/DynElContainer.cshtml','⬜',1,'[]','[]','["default"]','[]',
 '{"component":"DynElContainer","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),
('DynElCollapse','折叠面板','通用','ElementUI','/api/component/define/DynElCollapse','/Areas/Component/Views/ElementUI/DynElCollapse.cshtml','📂',1,'[]','[]','["default"]','[]',
 '{"component":"DynElCollapse","modelname":"","options":{"comoptions":{"accordion":false},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),
('DynElCollapseItem','折叠面板项','通用','ElementUI','/api/component/define/DynElCollapseItem','/Areas/Component/Views/ElementUI/DynElCollapseItem.cshtml','📁',1,'[]','[]','["default"]','[]',
 '{"component":"DynElCollapseItem","modelname":"","options":{"comoptions":{"name":"","label":"","expand":true},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),('DynElFormItem','表单项','表单','ElementUI',NULL,NULL,'🏷️',1,'[]','["DynForm"]','["default"]','[]',
 '{"component":"DynElFormItem","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"标签","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

-- ============ 栅格容器（设计器默认根组件） ============
('DynGridContainer','栅格容器','容器','Common','/api/component/define/DynGridContainer','/Areas/Component/Views/ElementUI/DynGridContainer.cshtml','🔲',1,'[]','[]','["default"]','[]',
 '{"component":"DynGridContainer","modelname":"","options":{"comoptions":{"gridTemplateColumns":"1fr 1fr","gap":"16px"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":"p-4"}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

-- ============ 2025-10-02 新增组件（与 seed-extra.sql 增量回流保持一致） ============
('DynElBadge','徽章','基础','ElementUI','/api/component/define/DynElBadge','/Areas/Component/Views/ElementUI/DynElBadge.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElBadge", "modelname": "", "options": {"comoptions": {"type": "danger", "max": 99, "isDot": false, "defaultValue": 5, "buttonText": "消息通知"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElCascader','级联选择','表单','ElementUI','/api/component/define/DynElCascader','/Areas/Component/Views/ElementUI/DynElCascader.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElCascader", "modelname": "", "options": {"comoptions": {"placeholder": "请选择地区", "clearable": true, "options": [{"value": "sh", "label": "上海", "children": [{"value": "pd", "label": "浦东新区"}, {"value": "hp", "label": "静安区"}]}, {"value": "bj", "label": "北京", "children": [{"value": "hd", "label": "海淀区"}, {"value": "cy", "label": "朝阳区"}]}]}, "comlisteners": {}, "labeloptions": {"label": "地区", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElDescriptions','描述列表','数据','ElementUI','/api/component/define/DynElDescriptions','/Areas/Component/Views/ElementUI/DynElDescriptions.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElDescriptions", "modelname": "", "options": {"comoptions": {"title": "详细信息", "column": 2, "border": true, "items": [{"label": "姓名", "value": "张三"}, {"label": "部门", "value": "技术部"}, {"label": "职位", "value": "工程师"}, {"label": "入职日期", "value": "2024-01-01"}]}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElEmpty','空状态','基础','ElementUI','/api/component/define/DynElEmpty','/Areas/Component/Views/ElementUI/DynElEmpty.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElEmpty", "modelname": "", "options": {"comoptions": {"description": "暂无数据", "imageSize": 80}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElPopover','气泡提示','基础','ElementUI','/api/component/define/DynElPopover','/Areas/Component/Views/ElementUI/DynElPopover.cshtml','📦',0,'[]','[]','[]','[]','{"component": "DynElPopover", "modelname": "", "options": {"comoptions": {"title": "帮助", "content": "这是一段帮助说明文字", "triggerText": "查看说明"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElStatistic','统计数字','数据','ElementUI','/api/component/define/DynElStatistic','/Areas/Component/Views/ElementUI/DynElStatistic.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElStatistic", "modelname": "", "options": {"comoptions": {"title": "今日订单", "defaultValue": 128, "precision": 0, "prefix": "", "suffix": "单", "subTitle": "环比昨日 +12%", "icon": "📦", "iconColor": "#409eff"}, "comlisteners": {}, "labeloptions": {"label": "", "required": false, "show": false}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElTimeline','时间线','数据','ElementUI','/api/component/define/DynElTimeline','/Areas/Component/Views/ElementUI/DynElTimeline.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElTimeline", "modelname": "", "options": {"comoptions": {"items": [{"time": "2024-01-01 10:00", "content": "提交申请", "type": "primary"}, {"time": "2024-01-01 11:30", "content": "部门经理审批通过", "type": "success"}, {"time": "2024-01-02 09:00", "content": "财务审核中", "type": "warning"}]}, "comlisteners": {}, "labeloptions": {"label": "审批记录", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElTransfer','穿梭框','表单','ElementUI','/api/component/define/DynElTransfer','/Areas/Component/Views/ElementUI/DynElTransfer.cshtml','📊',0,'[]','[]','[]','[]','{"component": "DynElTransfer", "modelname": "selectedIds", "options": {"comoptions": {"titles": ["待选角色", "已选角色"], "filterable": true, "props": {"key": "key", "label": "label"}, "sourceType": "static", "optionValues": [{"key": 1, "label": "管理员"}, {"key": 2, "label": "开发"}, {"key": 3, "label": "测试"}, {"key": 4, "label": "访客"}]}, "comlisteners": {}, "labeloptions": {"label": "分配角色", "required": false, "show": true, "labelwidth": "100px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElUpload','文件上传','表单','ElementUI','/api/component/define/DynElUpload','/Areas/Component/Views/ElementUI/DynElUpload.cshtml','📎',0,'[]','[]','[]','[]','{"component": "DynElUpload", "modelname": "", "options": {"comoptions": {"uploadType": "file", "buttonText": "点击上传", "accept": "", "limit": 1}, "comlisteners": {}, "labeloptions": {"label": "文件上传", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynHtmlBox','HTML嵌入','容器','ElementUI','/api/component/define/DynHtmlBox','/Areas/Component/Views/ElementUI/DynHtmlBox.cshtml','🌐',0,'[]','[]','[]','[]','{"component": "DynHtmlBox", "modelname": "", "options": {"comoptions": {"url": "", "params": {}, "refreshOnModelChange": "", "minHeight": "100px"}, "comlisteners": {}, "labeloptions": {"label": "HTML嵌入", "required": false, "show": true, "labelwidth": "80px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynLookup','弹窗选择器','表单','ElementUI','/api/component/define/DynLookup','/Areas/Component/Views/ElementUI/DynLookup.cshtml','🔍',0,'[]','[]','[]','[]','{"component": "DynLookup", "modelname": "customerId", "options": {"comoptions": {"url": "/Platform/Page/DynWebPage?id=12", "title": "选择客户", "width": "800px", "placeholder": "点击选择", "valueField": "Id", "urlParams": {"projectId": "{{model.projectId}}"}, "mappings": {"Name": "customerName", "Address": "customerAddress", "Phone": "customerPhone"}}, "comlisteners": {}, "labeloptions": {"label": "选择客户", "required": false, "show": true, "labelwidth": "100px"}, "itemoptions": {"style": {}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynMarkdown','Markdown 渲染','容器','ElementUI','/api/component/define/DynMarkdown','/Areas/Component/Views/ElementUI/DynMarkdown.cshtml','📝',0,NULL,NULL,NULL,NULL,'{"component": "DynMarkdown", "modelname": "", "options": {"comoptions": {"source": "# 标题\n\n在这里写 **Markdown** 内容...", "html": true}, "comlisteners": {}, "labeloptions": {"label": "Markdown", "required": false, "show": false}, "itemoptions": {}}, "validators": [], "slots": {}}',1),
('DynMermaid','Mermaid 图表','容器','ElementUI','/api/component/define/DynMermaid','/Areas/Component/Views/ElementUI/DynMermaid.cshtml','📊',0,NULL,NULL,NULL,NULL,'{"component": "DynMermaid", "modelname": "", "options": {"comoptions": {"code": "flowchart LR\n    A[开始] --> B{判断}\n    B -->|是| C[处理]\n    B -->|否| D[结束]\n    C --> D", "theme": "default"}, "comlisteners": {}, "labeloptions": {"label": "Mermaid", "required": false, "show": false}, "itemoptions": {}}, "validators": [], "slots": {}}',1),
('DynModelWatcher','联动监听器(隐藏)','其他','ElementUI','/api/component/define/DynModelWatcher','/Areas/Component/Views/ElementUI/DynModelWatcher.cshtml','🔌',0,'[]','[]','[]','[]','{"component": "DynModelWatcher", "modelname": "", "options": {"comoptions": {"watchModel": "provinceId", "onChangeJs": "// newVal 是新值, oldVal 是旧值, model 是整个表单\nconsole.log(''province changed:'', newVal);", "onChangeAction": ""}, "comlisteners": {}, "labeloptions": {"label": "监听器", "required": false, "show": false}, "itemoptions": {"style": {"display": "none"}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynRpcLoader','数据桥(隐藏)','其他','ElementUI','/api/component/define/DynRpcLoader','/Areas/Component/Views/ElementUI/DynRpcLoader.cshtml','🔌',0,'[]','[]','[]','[]','{"component": "DynRpcLoader", "modelname": "", "options": {"comoptions": {"url": "/api/user/current", "method": "GET", "autoLoad": true, "params": {}, "targetModel": "", "onSuccessJs": "// result 是接口返回数据, model 是表单 model\nconsole.log(''loaded:'', result);", "onSuccessAction": ""}, "comlisteners": {}, "labeloptions": {"label": "RPC调用", "required": false, "show": false}, "itemoptions": {"style": {"display": "none"}, "class": ""}}, "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": {}}',1),
('DynElDateRange','日期范围','表单','ElementUI','/api/component/define/DynElDateRange','/Areas/Component/Views/ElementUI/DynElDateRange.cshtml','📅',0,'[]','["DynElFormItem"]','["default"]','[]','{"component":"DynElDateRange","modelname":"","options":{"comoptions":{"startModel":"","endModel":"","pickerType":"datetimerange","valueFormat":"YYYY-MM-DD HH:mm:ss","startPlaceholder":"开始日期","endPlaceholder":"结束日期"},"comlisteners":{},"labeloptions":{"label":"日期范围","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1);

