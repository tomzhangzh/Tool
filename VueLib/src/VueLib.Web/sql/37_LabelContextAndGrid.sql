USE [VueLib];
GO
-- ============================================================
-- 37_LabelContextAndGrid.sql
-- 1) 容器组件：新增「标签统一配置」分组（labelcontext: 宽/对齐/必填星号）
-- 2) 行容器 DynElRow：新增 cols（栅格总列数）
-- 3) 表单项：新增「栅格占据」span（供 Row 布局使用）+ labelAlign
-- ============================================================

-- ---------- 1) 容器组件加 labelcontext 分组 ----------
-- DynElDivContainer
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 100px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"基础属性","fields":[{"key":"options.itemoptions.style.padding","label":"内边距","type":"input","default":"12px"},{"key":"options.itemoptions.class","label":"CSS类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynElDivContainer';
GO

-- DynElCard
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 100px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"基础属性","fields":[{"key":"options.comoptions.title","label":"标题","type":"input","default":"卡片标题"},{"key":"options.comoptions.shadow","label":"阴影","type":"select","default":"always","options":[{"label":"总是","value":"always"},{"label":"悬停","value":"hover"},{"label":"从不","value":"never"}]}]}]}'
 WHERE ComponentName = N'DynElCard';
GO

-- DynElRow（含 cols）
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 100px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"栅格配置","fields":[{"key":"options.comoptions.cols","label":"总列数(12/24)","type":"number","default":24},{"key":"options.comoptions.gutter","label":"栅格间隔","type":"number","default":20}]},{"title":"基础属性","fields":[{"key":"options.comoptions.justify","label":"水平排列","type":"select","default":"start","options":[{"label":"左对齐","value":"start"},{"label":"居中","value":"center"},{"label":"右对齐","value":"end"},{"label":"两端","value":"space-between"},{"label":"环绕","value":"space-around"}]},{"key":"options.comoptions.align","label":"垂直排列","type":"select","default":"top","options":[{"label":"顶部","value":"top"},{"label":"居中","value":"middle"},{"label":"底部","value":"bottom"}]}]}]}'
 WHERE ComponentName = N'DynElRow';
GO

-- DynElTabs
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 100px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"基础属性","fields":[{"key":"options.comoptions.type","label":"类型","type":"select","default":"","options":[{"label":"默认","value":""},{"label":"卡片","value":"card"},{"label":"边框卡片","value":"border-card"}]},{"key":"options.comoptions.tabPosition","label":"位置","type":"select","default":"top","options":[{"label":"顶部","value":"top"},{"label":"右侧","value":"right"},{"label":"底部","value":"bottom"},{"label":"左侧","value":"left"}]}]}]}'
 WHERE ComponentName = N'DynElTabs';
GO

-- ---------- 2) 表单项：加「栅格占据」span + labelAlign ----------
-- DynElInput
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"基础属性","fields":[{"key":"options.comoptions.placeholder","label":"占位符","type":"input","default":"请输入"},{"key":"options.comoptions.type","label":"类型","type":"select","default":"text","options":[{"label":"文本","value":"text"},{"label":"密码","value":"password"},{"label":"多行","value":"textarea"},{"label":"数字","value":"number"}]},{"key":"options.comoptions.clearable","label":"可清空","type":"switch","default":false},{"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},{"key":"options.comoptions.readonly","label":"只读","type":"switch","default":false},{"key":"options.comoptions.maxlength","label":"最大长度","type":"number","default":0}]},{"title":"标签配置","fields":[{"key":"options.labeloptions.label","label":"标签文字","type":"input","default":""},{"key":"options.labeloptions.required","label":"必填","type":"switch","default":false},{"key":"options.labeloptions.show","label":"显示标签","type":"switch","default":true},{"key":"options.labeloptions.labelWidth","label":"标签宽度","type":"input","default":"","placeholder":"留空继承容器"},{"key":"options.labeloptions.labelAlign","label":"标签对齐","type":"select","default":"","options":[{"label":"继承容器","value":""},{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"}]}]},{"title":"栅格占据","fields":[{"key":"options.layout.span","label":"占据列数","type":"number","default":"","placeholder":"留空自动均分"}]},{"title":"样式","fields":[{"key":"options.itemoptions.style.width","label":"宽度","type":"input","default":"100%"},{"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynElInput';
GO

-- DynElSelect
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"基础属性","fields":[{"key":"options.comoptions.placeholder","label":"占位符","type":"input","default":"请选择"},{"key":"options.comoptions.clearable","label":"可清空","type":"switch","default":true},{"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},{"key":"options.comoptions.multiple","label":"多选","type":"switch","default":false},{"key":"options.comoptions.filterable","label":"可搜索","type":"switch","default":false},{"key":"options.optionValues","label":"选项(逗号分隔)","type":"input","default":"选项1,选项2"}]},{"title":"标签配置","fields":[{"key":"options.labeloptions.label","label":"标签文字","type":"input","default":""},{"key":"options.labeloptions.required","label":"必填","type":"switch","default":false},{"key":"options.labeloptions.show","label":"显示标签","type":"switch","default":true},{"key":"options.labeloptions.labelWidth","label":"标签宽度","type":"input","default":"","placeholder":"留空继承容器"},{"key":"options.labeloptions.labelAlign","label":"标签对齐","type":"select","default":"","options":[{"label":"继承容器","value":""},{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"}]}]},{"title":"栅格占据","fields":[{"key":"options.layout.span","label":"占据列数","type":"number","default":"","placeholder":"留空自动均分"}]},{"title":"样式","fields":[{"key":"options.itemoptions.style.width","label":"宽度","type":"input","default":"100%"},{"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynElSelect';
GO

-- DynElDatePicker
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"基础属性","fields":[{"key":"options.comoptions.type","label":"类型","type":"select","default":"date","options":[{"label":"日期","value":"date"},{"label":"日期时间","value":"datetime"},{"label":"日期范围","value":"daterange"},{"label":"月份","value":"month"}]},{"key":"options.comoptions.placeholder","label":"占位符","type":"input","default":"选择日期"},{"key":"options.comoptions.clearable","label":"可清空","type":"switch","default":true},{"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},{"key":"options.comoptions.format","label":"显示格式","type":"input","default":"YYYY-MM-DD"}]},{"title":"标签配置","fields":[{"key":"options.labeloptions.label","label":"标签文字","type":"input","default":""},{"key":"options.labeloptions.required","label":"必填","type":"switch","default":false},{"key":"options.labeloptions.show","label":"显示标签","type":"switch","default":true},{"key":"options.labeloptions.labelWidth","label":"标签宽度","type":"input","default":"","placeholder":"留空继承容器"},{"key":"options.labeloptions.labelAlign","label":"标签对齐","type":"select","default":"","options":[{"label":"继承容器","value":""},{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"}]}]},{"title":"栅格占据","fields":[{"key":"options.layout.span","label":"占据列数","type":"number","default":"","placeholder":"留空自动均分"}]},{"title":"样式","fields":[{"key":"options.itemoptions.style.width","label":"宽度","type":"input","default":"100%"},{"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynElDatePicker';
GO

-- DynElRadio
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"基础属性","fields":[{"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},{"key":"options.comoptions.size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"options.optionValues","label":"选项(逗号分隔)","type":"input","default":"选项1,选项2,选项3"}]},{"title":"标签配置","fields":[{"key":"options.labeloptions.label","label":"标签文字","type":"input","default":""},{"key":"options.labeloptions.required","label":"必填","type":"switch","default":false},{"key":"options.labeloptions.show","label":"显示标签","type":"switch","default":true},{"key":"options.labeloptions.labelWidth","label":"标签宽度","type":"input","default":"","placeholder":"留空继承容器"},{"key":"options.labeloptions.labelAlign","label":"标签对齐","type":"select","default":"","options":[{"label":"继承容器","value":""},{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"}]}]},{"title":"栅格占据","fields":[{"key":"options.layout.span","label":"占据列数","type":"number","default":"","placeholder":"留空自动均分"}]},{"title":"样式","fields":[{"key":"options.itemoptions.style.width","label":"宽度","type":"input","default":"100%"},{"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynElRadio';
GO

-- DynElSwitch
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"基础属性","fields":[{"key":"options.comoptions.disabled","label":"禁用","type":"switch","default":false},{"key":"options.comoptions.size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"options.comoptions.activeText","label":"开启文字","type":"input","default":""},{"key":"options.comoptions.inactiveText","label":"关闭文字","type":"input","default":""}]},{"title":"标签配置","fields":[{"key":"options.labeloptions.label","label":"标签文字","type":"input","default":""},{"key":"options.labeloptions.required","label":"必填","type":"switch","default":false},{"key":"options.labeloptions.show","label":"显示标签","type":"switch","default":true},{"key":"options.labeloptions.labelWidth","label":"标签宽度","type":"input","default":"","placeholder":"留空继承容器"},{"key":"options.labeloptions.labelAlign","label":"标签对齐","type":"select","default":"","options":[{"label":"继承容器","value":""},{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"}]}]},{"title":"栅格占据","fields":[{"key":"options.layout.span","label":"占据列数","type":"number","default":"","placeholder":"留空自动均分"}]},{"title":"样式","fields":[{"key":"options.itemoptions.style.width","label":"宽度","type":"input","default":"100%"},{"key":"options.itemoptions.class","label":"自定义类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynElSwitch';
GO

-- ---------- 3) Nut 容器加 labelcontext ----------
-- DynNDivContainer
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 80px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"基础属性","fields":[{"key":"options.itemoptions.style.padding","label":"内边距","type":"input","default":"12px"},{"key":"options.itemoptions.class","label":"CSS类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynNDivContainer';
GO

-- DynNForm
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 80px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"基础属性","fields":[{"key":"options.comoptions.title","label":"标题","type":"input","default":""},{"key":"options.itemoptions.class","label":"CSS类名","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynNForm';
GO

-- DynNCellGroup
UPDATE dbo.ComponentMeta SET PropertyConfigJson =
 N'{"groups":[{"title":"标签统一配置","fields":[{"key":"options.labelcontext.width","label":"标签宽度","type":"input","default":"","placeholder":"如 80px"},{"key":"options.labelcontext.align","label":"标签对齐","type":"select","default":"left","options":[{"label":"左对齐","value":"left"},{"label":"右对齐","value":"right"},{"label":"顶部","value":"top"}]}]},{"title":"基础属性","fields":[{"key":"options.comoptions.title","label":"分组标题","type":"input","default":""}]}]}'
 WHERE ComponentName = N'DynNCellGroup';
GO

PRINT '属性配置更新完成：容器 labelcontext / Row cols / 表单项 span';
GO
