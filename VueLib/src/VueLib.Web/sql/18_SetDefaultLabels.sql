/* ============================================================
 * VueLib 低代码平台 - ElementUI 表单组件设置默认标签文字
 * 更新 DefaultConfigJson 中的 labeloptions.label
 * ============================================================ */
USE VueLib;
GO

UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElInput","modelname":"","options":{"comoptions":{"placeholder":"请输入"},"labeloptions":{"label":"输入框","required":false,"show":true}}}' WHERE ComponentName = N'DynElInput';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElInputNumber","modelname":"","options":{"comoptions":{"min":0,"max":100,"step":1},"labeloptions":{"label":"数字输入","required":false,"show":true}}}' WHERE ComponentName = N'DynElInputNumber';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElSelect","modelname":"","options":{"comoptions":{"placeholder":"请选择","clearable":true},"labeloptions":{"label":"下拉选择","required":false,"show":true},"optionValues":"选项1,选项2"}}' WHERE ComponentName = N'DynElSelect';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElSwitch","modelname":"","options":{"comoptions":{},"labeloptions":{"label":"开关","required":false,"show":true}}}' WHERE ComponentName = N'DynElSwitch';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElRadio","modelname":"","options":{"comoptions":{},"labeloptions":{"label":"单选框","required":false,"show":true},"optionValues":"选项1,选项2,选项3"}}' WHERE ComponentName = N'DynElRadio';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElCheckbox","modelname":"","options":{"comoptions":{},"labeloptions":{"label":"多选框","required":false,"show":true},"optionValues":"选项1,选项2,选项3"}}' WHERE ComponentName = N'DynElCheckbox';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElDatePicker","modelname":"","options":{"comoptions":{"type":"date","placeholder":"选择日期"},"labeloptions":{"label":"日期选择","required":false,"show":true}}}' WHERE ComponentName = N'DynElDatePicker';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElTimePicker","modelname":"","options":{"comoptions":{"placeholder":"选择时间"},"labeloptions":{"label":"时间选择","required":false,"show":true}}}' WHERE ComponentName = N'DynElTimePicker';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElSlider","modelname":"","options":{"comoptions":{"min":0,"max":100},"labeloptions":{"label":"滑块","required":false,"show":true}}}' WHERE ComponentName = N'DynElSlider';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElRate","modelname":"","options":{"comoptions":{"max":5},"labeloptions":{"label":"评分","required":false,"show":true}}}' WHERE ComponentName = N'DynElRate';
UPDATE dbo.ComponentMeta SET DefaultConfigJson = N'{"component":"DynElColorPicker","modelname":"","options":{"comoptions":{},"labeloptions":{"label":"颜色选择","required":false,"show":true}}}' WHERE ComponentName = N'DynElColorPicker';

PRINT N'ElementUI 表单组件默认标签设置完成（11个）';
GO
