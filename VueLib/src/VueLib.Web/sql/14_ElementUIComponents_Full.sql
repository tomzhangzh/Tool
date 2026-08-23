/* ============================================================
 * VueLib 低代码平台 - ElementUI 组件完整注册（含属性配置）
 * 24 个 Element Plus 组件，包含 PropertyConfigJson
 * ============================================================ */
USE VueLib;
GO

-- 辅助：插入或更新组件
-- ============================================
-- 表单类组件
-- ============================================

-- ElInput
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElInput', 1, N'表单', N'输入框', N'📝',
    N'{"component":"ElInput","modelname":"","options":{"comoptions":{"placeholder":"请输入"},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/Input', N'ElementUI 文本输入框', 1, 100, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"placeholder","label":"占位符","type":"input","default":"请输入"},{"key":"clearable","label":"可清空","type":"switch","default":false},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"readonly","label":"只读","type":"switch","default":false},{"key":"maxlength","label":"最大长度","type":"number","default":0},{"key":"showWordLimit","label":"显示字数","type":"switch","default":false},{"key":"type","label":"类型","type":"select","default":"text","options":[{"label":"文本","value":"text"},{"label":"密码","value":"password"},{"label":"多行","value":"textarea"},{"label":"数字","value":"number"}]}]},{"name":"前缀后缀","fields":[{"key":"prefixIcon","label":"前缀图标","type":"input","default":""},{"key":"suffixIcon","label":"后缀图标","type":"input","default":""}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElInputNumber
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElInputNumber', 1, N'表单', N'数字输入', N'🔢',
    N'{"component":"ElInputNumber","modelname":"","options":{"comoptions":{"min":0,"max":100,"step":1},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/InputNumber', N'ElementUI 数字输入框', 1, 101, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"min","label":"最小值","type":"number","default":0},{"key":"max","label":"最大值","type":"number","default":100},{"key":"step","label":"步长","type":"number","default":1},{"key":"precision","label":"精度","type":"number","default":0},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"controlsPosition","label":"按钮位置","type":"select","default":"","options":[{"label":"默认","value":""},{"label":"两侧","value":"right"}]}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElSelect
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElSelect', 1, N'表单', N'下拉选择', N'📋',
    N'{"component":"ElSelect","modelname":"","options":{"comoptions":{"placeholder":"请选择","clearable":true},"labeloptions":{"label":""},"optionValues":"选项1,选项2"}}}',
    N'/ElementComponent/FormItem/Select', N'ElementUI 下拉选择', 1, 102, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"placeholder","label":"占位符","type":"input","default":"请选择"},{"key":"clearable","label":"可清空","type":"switch","default":true},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"multiple","label":"多选","type":"switch","default":false},{"key":"filterable","label":"可搜索","type":"switch","default":false},{"key":"collapseTags","label":"折叠标签","type":"switch","default":false}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElSwitch
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElSwitch', 1, N'表单', N'开关', N'🎚️',
    N'{"component":"ElSwitch","modelname":"","options":{"comoptions":{},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/Switch', N'ElementUI 开关', 1, 103, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"loading","label":"加载中","type":"switch","default":false},{"key":"size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"activeText","label":"开启文字","type":"input","default":""},{"key":"inactiveText","label":"关闭文字","type":"input","default":""},{"key":"activeColor","label":"开启颜色","type":"color","default":"#409EFF"},{"key":"inactiveColor","label":"关闭颜色","type":"color","default":"#C0CCDA"}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElRadio
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElRadio', 1, N'表单', N'单选框组', N'🔘',
    N'{"component":"ElRadio","modelname":"","options":{"comoptions":{},"labeloptions":{"label":""},"optionValues":"选项1,选项2,选项3"}}}',
    N'/ElementComponent/FormItem/Radio', N'ElementUI 单选框组', 1, 104, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"textColor","label":"选中文字色","type":"color","default":"#ffffff"},{"key":"fill","label":"选中填充色","type":"color","default":"#409EFF"}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElCheckbox
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElCheckbox', 1, N'表单', N'多选框组', N'☑️',
    N'{"component":"ElCheckbox","modelname":"","options":{"comoptions":{},"labeloptions":{"label":""},"optionValues":"选项1,选项2,选项3"}}}',
    N'/ElementComponent/FormItem/Checkbox', N'ElementUI 多选框组', 1, 105, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"min","label":"最少选中","type":"number","default":0},{"key":"max","label":"最多选中","type":"number","default":0},{"key":"textColor","label":"选中文字色","type":"color","default":"#ffffff"},{"key":"fill","label":"选中填充色","type":"color","default":"#409EFF"}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElDatePicker
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElDatePicker', 1, N'表单', N'日期选择', N'📅',
    N'{"component":"ElDatePicker","modelname":"","options":{"comoptions":{"type":"date","placeholder":"选择日期"},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/DatePicker', N'ElementUI 日期选择器', 1, 106, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"type","label":"类型","type":"select","default":"date","options":[{"label":"日期","value":"date"},{"label":"日期时间","value":"datetime"},{"label":"日期范围","value":"daterange"},{"label":"月份","value":"month"},{"label":"年份","value":"year"},{"label":"周","value":"week"}]},{"key":"placeholder","label":"占位符","type":"input","default":"选择日期"},{"key":"clearable","label":"可清空","type":"switch","default":true},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"format","label":"显示格式","type":"input","default":"YYYY-MM-DD"},{"key":"valueFormat","label":"值格式","type":"input","default":"YYYY-MM-DD"}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElTimePicker
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElTimePicker', 1, N'表单', N'时间选择', N'⏰',
    N'{"component":"ElTimePicker","modelname":"","options":{"comoptions":{"placeholder":"选择时间"},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/TimePicker', N'ElementUI 时间选择器', 1, 107, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"placeholder","label":"占位符","type":"input","default":"选择时间"},{"key":"clearable","label":"可清空","type":"switch","default":true},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"format","label":"格式","type":"input","default":"HH:mm:ss"},{"key":"isRange","label":"范围选择","type":"switch","default":false}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElSlider
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElSlider', 1, N'表单', N'滑块', N'🎚️',
    N'{"component":"ElSlider","modelname":"","options":{"comoptions":{"min":0,"max":100},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/Slider', N'ElementUI 滑块', 1, 108, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"min","label":"最小值","type":"number","default":0},{"key":"max","label":"最大值","type":"number","default":100},{"key":"step","label":"步长","type":"number","default":1},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"showInput","label":"显示输入框","type":"switch","default":false},{"key":"showStops","label":"显示间断点","type":"switch","default":false},{"key":"range","label":"范围选择","type":"switch","default":false},{"key":"vertical","label":"竖向","type":"switch","default":false}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElRate
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElRate', 1, N'表单', N'评分', N'⭐',
    N'{"component":"ElRate","modelname":"","options":{"comoptions":{"max":5},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/Rate', N'ElementUI 评分', 1, 109, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"max","label":"最大分值","type":"number","default":5},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"allowHalf","label":"允许半选","type":"switch","default":false},{"key":"showText","label":"显示文字","type":"switch","default":false},{"key":"showScore","label":"显示分数","type":"switch","default":false},{"key":"textColor","label":"文字颜色","type":"color","default":"#ff9900"},{"key":"voidColor","label":"未选中颜色","type":"color","default":"#C6D1DE"}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElColorPicker
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'ElColorPicker', 1, N'表单', N'颜色选择', N'🎨',
    N'{"component":"ElColorPicker","modelname":"","options":{"comoptions":{},"labeloptions":{"label":""}}}',
    N'/ElementComponent/FormItem/ColorPicker', N'ElementUI 颜色选择器', 1, 110, N'elementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"showAlpha","label":"显示透明度","type":"switch","default":false},{"key":"colorFormat","label":"颜色格式","type":"select","default":"hex","options":[{"label":"HEX","value":"hex"},{"label":"RGB","value":"rgb"},{"label":"HSL","value":"hsl"}]},{"key":"predefine","label":"预定义颜色","type":"input","default":""}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

PRINT '表单类组件注册完成（11个）';
GO
