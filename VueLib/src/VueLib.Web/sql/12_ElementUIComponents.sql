/* ============================================================
 * VueLib 低代码平台 - ElementUI 组件注册
 * 电脑端使用的 Element Plus 组件封装
 * ============================================================ */
USE VueLib;
GO

-- ElInput
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = 'DynElInput')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary)
    VALUES ('DynElInput', 1, '表单', '输入框', '📝', '{"component":"DynElInput","modelname":"","options":{"comoptions":{"placeholder":"请输入"},"labeloptions":{"label":""}}}', '/ElementComponent/FormItem/Input', 'ElementUI 文本输入框', 1, 100, 'DynElementui');
    PRINT '已注册 ElInput';
END
GO

-- ElButton
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = 'DynElButton')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary)
    VALUES ('DynElButton', 4, '通用', '按钮', '🔘', '{"component":"DynElButton","options":{"comoptions":{"text":"按钮","type":"primary"}}}', '/ElementComponent/Common/Button', 'ElementUI 按钮', 1, 101, 'DynElementui');
    PRINT '已注册 ElButton';
END
GO

-- ElSelect
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = 'DynElSelect')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary)
    VALUES ('DynElSelect', 1, '表单', '下拉选择', '📋', '{"component":"DynElSelect","modelname":"","options":{"comoptions":{"placeholder":"请选择"},"labeloptions":{"label":""},"optionValues":"选项1,选项2"}}}', '/ElementComponent/FormItem/Select', 'ElementUI 下拉选择', 1, 102, 'DynElementui');
    PRINT '已注册 ElSelect';
END
GO

-- ElSwitch
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = 'DynElSwitch')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary)
    VALUES ('DynElSwitch', 1, '表单', '开关', '🎚️', '{"component":"DynElSwitch","modelname":"","options":{"comoptions":{},"labeloptions":{"label":""}}}', '/ElementComponent/FormItem/Switch', 'ElementUI 开关', 1, 103, 'DynElementui');
    PRINT '已注册 ElSwitch';
END
GO

-- ElDivContainer
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = 'DynElDivContainer')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary)
    VALUES ('DynElDivContainer', 2, '布局', 'DIV容器', '📦', '{"component":"DynElDivContainer","childrenctrls":[],"options":{"itemoptions":{"style":{"padding":"12px"}}}}', '/ElementComponent/Container/DivContainer', 'ElementUI DIV容器', 1, 104, 'DynElementui');
    PRINT '已注册 ElDivContainer';
END
GO

-- ElCard
IF NOT EXISTS (SELECT 1 FROM dbo.ComponentMeta WHERE ComponentName = 'DynElCard')
BEGIN
    INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary)
    VALUES ('DynElCard', 2, '布局', '卡片', '🗂️', '{"component":"DynElCard","childrenctrls":[],"options":{"comoptions":{"title":"卡片标题"}}}', '/ElementComponent/Container/Card', 'ElementUI 卡片容器', 1, 105, 'DynElementui');
    PRINT '已注册 ElCard';
END
GO

PRINT 'ElementUI 组件注册完成';
GO
