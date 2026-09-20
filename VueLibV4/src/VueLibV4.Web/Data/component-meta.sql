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

('DynElSelect','下拉选择','表单','ElementUI','/api/component/define/DynElSelect','/Areas/Component/Views/ElementUI/Select.cshtml','📋',0,'[]','["DynElFormItem"]','["default"]','[]',
 '{"component":"DynElSelect","modelname":"","options":{"comoptions":{"placeholder":"请选择","clearable":true,"filterable":false,"multiple":false,"optionValues":[]},"comlisteners":{},"labeloptions":{"label":"下拉选择","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1),

('DynElRadioGroup','单选框组','表单','ElementUI','/api/component/define/DynElRadioGroup','/Areas/Component/Views/ElementUI/Radio.cshtml','🔘',0,'[]','["DynElFormItem"]','["default"]','[]',
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
 '{"component":"DynGridContainer","modelname":"","options":{"comoptions":{"gridTemplateColumns":"1fr 1fr","gap":"16px"},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{},"class":"p-4"}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',1);
