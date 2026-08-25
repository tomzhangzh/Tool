/* ============================================================
 * VueLib 低代码平台 - ElementUI 组件完整注册（续）
 * 展示类、通用类、布局类组件
 * ============================================================ */
USE VueLib;
GO

-- ============================================
-- 通用类组件
-- ============================================

-- ElButton
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElButton', 4, N'通用', N'按钮', N'🔘',
    N'{"component":"DynElButton","options":{"comoptions":{"text":"按钮","type":"primary"}}}',
    N'/ElementComponent/Common/Button', N'ElementUI 按钮', 1, 120, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"text","label":"按钮文字","type":"input","default":"按钮"},{"key":"type","label":"类型","type":"select","default":"primary","options":[{"label":"主要","value":"primary"},{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"},{"label":"信息","value":"info"},{"label":"默认","value":""}]},{"key":"size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"plain","label":"朴素","type":"switch","default":false},{"key":"round","label":"圆角","type":"switch","default":false},{"key":"circle","label":"圆形","type":"switch","default":false},{"key":"disabled","label":"禁用","type":"switch","default":false},{"key":"loading","label":"加载中","type":"switch","default":false}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ============================================
-- 展示类组件
-- ============================================

-- ElTag
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElTag', 3, N'展示', N'标签', N'🏷️',
    N'{"component":"DynElTag","options":{"comoptions":{"text":"标签","type":""}}}',
    N'/ElementComponent/Common/Tag', N'ElementUI 标签', 1, 130, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"text","label":"标签文字","type":"input","default":"标签"},{"key":"type","label":"类型","type":"select","default":"","options":[{"label":"成功","value":"success"},{"label":"信息","value":"info"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"},{"label":"默认","value":""}]},{"key":"size","label":"尺寸","type":"select","default":"default","options":[{"label":"大","value":"large"},{"label":"默认","value":"default"},{"label":"小","value":"small"}]},{"key":"effect","label":"主题","type":"select","default":"light","options":[{"label":"浅色","value":"light"},{"label":"深色","value":"dark"},{"label":"朴素","value":"plain"}]},{"key":"round","label":"圆角","type":"switch","default":false},{"key":"closable","label":"可关闭","type":"switch","default":false}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElBadge
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElBadge', 3, N'展示', N'徽章', N'🔴',
    N'{"component":"DynElBadge","options":{"comoptions":{"value":5}}}',
    N'/ElementComponent/Common/Badge', N'ElementUI 徽章', 1, 131, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"value","label":"显示值","type":"input","default":"5"},{"key":"max","label":"最大值","type":"number","default":99},{"key":"isDot","label":"小圆点","type":"switch","default":false},{"key":"hidden","label":"隐藏","type":"switch","default":false},{"key":"type","label":"类型","type":"select","default":"danger","options":[{"label":"主要","value":"primary"},{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"},{"label":"信息","value":"info"}]}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElAvatar
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElAvatar', 3, N'展示', N'头像', N'👤',
    N'{"component":"DynElAvatar","options":{"comoptions":{"text":"A","size":"large"}}}',
    N'/ElementComponent/Common/Avatar', N'ElementUI 头像', 1, 132, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"text","label":"文字","type":"input","default":"A"},{"key":"size","label":"尺寸","type":"select","default":"large","options":[{"label":"大","value":"large"},{"label":"中","value":"medium"},{"label":"小","value":"small"}]},{"key":"shape","label":"形状","type":"select","default":"circle","options":[{"label":"圆形","value":"circle"},{"label":"方形","value":"square"}]},{"key":"src","label":"图片地址","type":"input","default":""},{"key":"icon","label":"图标","type":"input","default":""}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElProgress
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElProgress', 3, N'展示', N'进度条', N'📊',
    N'{"component":"DynElProgress","modelname":"","options":{"comoptions":{"percentage":50}}}',
    N'/ElementComponent/Common/Progress', N'ElementUI 进度条', 1, 133, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"percentage","label":"百分比","type":"number","default":50},{"key":"type","label":"类型","type":"select","default":"","options":[{"label":"线条","value":""},{"label":"圆圈","value":"circle"},{"label":"仪表盘","value":"dashboard"}]},{"key":"strokeWidth","label":"宽度","type":"number","default":6},{"key":"textInside","label":"文字在内","type":"switch","default":false},{"key":"status","label":"状态","type":"select","default":"","options":[{"label":"成功","value":"success"},{"label":"异常","value":"exception"},{"label":"警告","value":"warning"},{"label":"默认","value":""}]},{"key":"color","label":"颜色","type":"color","default":""}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElAlert
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElAlert', 3, N'展示', N'警告提示', N'⚠️',
    N'{"component":"DynElAlert","options":{"comoptions":{"title":"提示信息","type":"info","showIcon":true}}}',
    N'/ElementComponent/Common/Alert', N'ElementUI 警告提示', 1, 134, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"title","label":"标题","type":"input","default":"提示信息"},{"key":"type","label":"类型","type":"select","default":"info","options":[{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"信息","value":"info"},{"label":"错误","value":"error"}]},{"key":"description","label":"描述","type":"textarea","default":""},{"key":"closable","label":"可关闭","type":"switch","default":true},{"key":"closeText","label":"关闭文字","type":"input","default":""},{"key":"center","label":"文字居中","type":"switch","default":false},{"key":"effect","label":"主题","type":"select","default":"light","options":[{"label":"浅色","value":"light"},{"label":"深色","value":"dark"}]}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElDivider
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElDivider', 3, N'展示', N'分割线', N'➖',
    N'{"component":"DynElDivider","options":{"comoptions":{"text":""}}}',
    N'/ElementComponent/Common/Divider', N'ElementUI 分割线', 1, 135, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"text","label":"分割线文字","type":"input","default":""},{"key":"direction","label":"方向","type":"select","default":"horizontal","options":[{"label":"水平","value":"horizontal"},{"label":"垂直","value":"vertical"}]},{"key":"contentPosition","label":"文字位置","type":"select","default":"center","options":[{"label":"左侧","value":"left"},{"label":"居中","value":"center"},{"label":"右侧","value":"right"}]}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElImage
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElImage', 3, N'展示', N'图片', N'🖼️',
    N'{"component":"DynElImage","options":{"comoptions":{"src":"","fit":"cover"}}}',
    N'/ElementComponent/Common/Image', N'ElementUI 图片', 1, 136, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"src","label":"图片地址","type":"input","default":""},{"key":"alt","label":"替代文字","type":"input","default":""},{"key":"fit","label":"填充方式","type":"select","default":"cover","options":[{"label":"填充","value":"fill"},{"label":"包含","value":"contain"},{"label":"覆盖","value":"cover"},{"label":"无","value":"none"},{"label":"缩放","value":"scale-down"}]},{"key":"lazy","label":"懒加载","type":"switch","default":false},{"key":"previewSrcList","label":"预览列表","type":"input","default":""},{"key":"zIndex","label":"层级","type":"number","default":2000}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ============================================
-- 布局类组件
-- ============================================

-- ElDivContainer
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElDivContainer', 2, N'布局', N'DIV容器', N'📦',
    N'{"component":"DynElDivContainer","childrenctrls":[],"options":{"itemoptions":{"style":{"padding":"12px"}}}}',
    N'/ElementComponent/Container/DivContainer', N'ElementUI DIV容器', 1, 140, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"style","label":"样式(JSON)","type":"textarea","default":"{\"padding\":\"12px\"}"},{"key":"class","label":"CSS类名","type":"input","default":""}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElCard
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElCard', 2, N'布局', N'卡片', N'🗂️',
    N'{"component":"DynElCard","childrenctrls":[],"options":{"comoptions":{"title":"卡片标题"}}}',
    N'/ElementComponent/Container/Card', N'ElementUI 卡片容器', 1, 141, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"title","label":"标题","type":"input","default":"卡片标题"},{"key":"shadow","label":"阴影","type":"select","default":"always","options":[{"label":"总是","value":"always"},{"label":"悬停","value":"hover"},{"label":"从不","value":"never"}]},{"key":"bodyStyle","label":"内容样式(JSON)","type":"textarea","default":"{\"padding\":\"20px\"}"},{"key":"border","label":"显示边框","type":"switch","default":true}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElRow
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElRow', 2, N'布局', N'行布局', N'↔️',
    N'{"component":"DynElRow","childrenctrls":[],"options":{"comoptions":{"gutter":20}}}',
    N'/ElementComponent/Container/Row', N'ElementUI 行布局', 1, 142, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"gutter","label":"栅格间隔","type":"number","default":20},{"key":"justify","label":"水平排列","type":"select","default":"start","options":[{"label":"左对齐","value":"start"},{"label":"居中","value":"center"},{"label":"右对齐","value":"end"},{"label":"两端","value":"space-between"},{"label":"环绕","value":"space-around"}]},{"key":"align","label":"垂直排列","type":"select","default":"top","options":[{"label":"顶部","value":"top"},{"label":"居中","value":"middle"},{"label":"底部","value":"bottom"}]},{"key":"type","label":"布局模式","type":"select","default":"","options":[{"label":"默认","value":""},{"label":"Flex","value":"flex"}]}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElCol
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElCol', 2, N'布局', N'列布局', N'↕️',
    N'{"component":"DynElCol","childrenctrls":[],"options":{"comoptions":{"span":12}}}',
    N'/ElementComponent/Container/Col', N'ElementUI 列布局', 1, 143, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"span","label":"栅格占据","type":"number","default":12},{"key":"offset","label":"左侧间隔","type":"number","default":0},{"key":"push","label":"向右移动","type":"number","default":0},{"key":"pull","label":"向左移动","type":"number","default":0},{"key":"xs","label":"<768px","type":"number","default":0},{"key":"sm","label":"≥768px","type":"number","default":0},{"key":"md","label":"≥992px","type":"number","default":0},{"key":"lg","label":"≥1200px","type":"number","default":0},{"key":"xl","label":"≥1920px","type":"number","default":0}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

-- ElTabs
MERGE dbo.ComponentMeta AS target
USING (VALUES (N'DynElTabs', 2, N'布局', N'标签页', N'📑',
    N'{"component":"DynElTabs","childrenctrls":[],"options":{"comoptions":{"type":""}}}',
    N'/ElementComponent/Container/Tabs', N'ElementUI 标签页', 1, 144, N'DynElementui',
    N'{"groups":[{"name":"基础属性","fields":[{"key":"type","label":"类型","type":"select","default":"","options":[{"label":"默认","value":""},{"label":"卡片","value":"card"},{"label":"边框卡片","value":"border-card"}]},{"key":"tabPosition","label":"位置","type":"select","default":"top","options":[{"label":"顶部","value":"top"},{"label":"右侧","value":"right"},{"label":"底部","value":"bottom"},{"label":"左侧","value":"left"}]},{"key":"stretch","label":"自动拉伸","type":"switch","default":false},{"key":"closable","label":"可关闭","type":"switch","default":false},{"key":"addable","label":"可新增","type":"switch","default":false}]}]}')
) AS source (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
ON target.ComponentName = source.ComponentName
WHEN MATCHED THEN UPDATE SET target.DefaultConfigJson = source.DefaultConfigJson, target.PropertyConfigJson = source.PropertyConfigJson, target.UiLibrary = source.UiLibrary
WHEN NOT MATCHED THEN INSERT (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, LoadUrl, Description, IsEnabled, SortOrder, UiLibrary, PropertyConfigJson)
VALUES (source.ComponentName, source.ComponentType, source.Category, source.Label, source.Icon, source.DefaultConfigJson, source.LoadUrl, source.Description, source.IsEnabled, source.SortOrder, source.UiLibrary, source.PropertyConfigJson);
GO

PRINT '展示类、通用类、布局类组件注册完成（13个）';
PRINT '共计 24 个 ElementUI 组件';
GO
