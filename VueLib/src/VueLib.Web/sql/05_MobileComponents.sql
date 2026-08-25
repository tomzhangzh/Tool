/* ============================================================
 * VueLib 低代码平台 - 移动端业务组件元数据
 * 参考 Sunshine.WebPortal/mobile 项目
 * ============================================================ */
USE VueLib;
GO

/* ==================== 移动端业务组件 ==================== */

-- NIcon 图标
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNIcon', 4, N'通用', N'图标', N'✨',
N'{"component":"DynNIcon","modelname":"","options":{"comoptions":{"name":"home","size":"20px","color":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"name":"home","size":"20px","color":""}',
N'/NutComponent/Mobile/Icon', N'图标组件', 1, 100);

-- NEmpty 空状态
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNEmpty', 4, N'通用', N'空状态', N'📭',
N'{"component":"DynNEmpty","modelname":"","options":{"comoptions":{"icon":"image","text":"暂无数据"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"icon":"image","text":"暂无数据"}',
N'/NutComponent/Mobile/Empty', N'空状态展示', 1, 101);

-- NStatCard 统计卡片
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNStatCard', 3, N'展示', N'统计卡片', N'📊',
N'{"component":"DynNStatCard","modelname":"","options":{"comoptions":{"icon":"find","iconClass":"si-blue","value":"0","label":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"icon":"find","iconClass":"si-blue","value":"0","label":""}',
N'/NutComponent/Mobile/StatCard', N'统计数据卡片', 1, 102);

-- NMenuItem 菜单项
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNMenuItem', 4, N'通用', N'菜单项', N'📋',
N'{"component":"DynNMenuItem","modelname":"","options":{"comoptions":{"icon":"my","iconClass":"mi-blue","label":"","value":"","link":false,"switch":false,"switchValue":false},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"icon":"my","iconClass":"mi-blue","label":"","value":"","link":false,"switch":false}',
N'/NutComponent/Mobile/MenuItem', N'设置菜单项', 1, 103);

-- NNavBar 顶部导航栏
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNNavBar', 2, N'布局', N'顶部导航', N'📱',
N'{"component":"DynNNavBar","modelname":"","options":{"comoptions":{"title":"","subtitle":"","showBack":false},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"title":"","subtitle":"","showBack":false}',
N'/NutComponent/Mobile/NavBar', N'顶部导航栏', 1, 104);

-- NBottomNav 底部导航栏
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNBottomNav', 2, N'布局', N'底部导航', N'🔽',
N'{"component":"DynNBottomNav","modelname":"","options":{"comoptions":{"activePath":"/","tabs":[{"path":"/","label":"Home","icon":"home"},{"path":"/reports","label":"Reports","icon":"find"},{"path":"/price-brand","label":"Price","icon":"cart"},{"path":"/profile","label":"Profile","icon":"my"}]},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"activePath":"/","tabs":[]}',
N'/NutComponent/Mobile/BottomNav', N'底部标签导航', 1, 105);

-- NHeroBanner 首页横幅
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNHeroBanner', 3, N'展示', N'首页横幅', N'🏠',
N'{"component":"DynNHeroBanner","modelname":"","options":{"comoptions":{"greeting":"Hello","userName":"Guest","avatarIcon":"my","showBell":true,"stats":[{"value":"4","label":"Reports"},{"value":"1","label":"Price Tool"},{"value":"24/7","label":"Access"}]},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"greeting":"Hello","userName":"Guest","stats":[]}',
N'/NutComponent/Mobile/HeroBanner', N'首页用户横幅', 1, 106);

-- NGridMenu 九宫格菜单
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNGridMenu', 2, N'布局', N'九宫格', N'🔲',
N'{"component":"DynNGridMenu","modelname":"","options":{"comoptions":{"items":[{"label":"Price Brand","icon":"cart","iconClass":"gi-blue","path":"/price-brand"},{"label":"Category","icon":"find","iconClass":"gi-cyan","path":"/report/category"},{"label":"Hourly","icon":"clock","iconClass":"gi-orange","path":"/report/hourly"},{"label":"Reports","icon":"checklist","iconClass":"gi-indigo","path":"/reports"},{"label":"Profile","icon":"my","iconClass":"gi-teal","path":"/profile"},{"label":"More","icon":"more","iconClass":"gi-gray","placeholder":true}]},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"items":[]}',
N'/NutComponent/Mobile/GridMenu', N'九宫格快捷入口', 1, 107);

-- NReportCard 报表卡片
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNReportCard', 3, N'展示', N'报表卡片', N'📈',
N'{"component":"DynNReportCard","modelname":"","options":{"comoptions":{"title":"","desc":"","icon":"find","gradient":"linear-gradient(135deg,#4A90D9,#6BA3E0)","path":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"title":"","desc":"","icon":"find","path":""}',
N'/NutComponent/Mobile/ReportCard', N'报表入口卡片', 1, 108);

-- NReportFilter 报表筛选栏
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNReportFilter', 1, N'表单', N'报表筛选', N'🔍',
N'{"component":"DynNReportFilter","modelname":"","options":{"comoptions":{"stationId":0,"topN":10,"startDate":"","endDate":"","stations":[]},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"stationId":0,"topN":10,"startDate":"","endDate":""}',
N'/NutComponent/Mobile/ReportFilter', N'报表筛选条件栏', 1, 109);

-- NEChart ECharts图表
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNEChart', 3, N'展示', N'ECharts图表', N'📊',
N'{"component":"DynNEChart","modelname":"","options":{"comoptions":{"height":"300px","chartType":"bar","color":"#4A90D9","chartField":"","valueField":"","apiUrl":"","staticData":[],"autoLoad":true},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"height":"300px","chartType":"bar","color":"#4A90D9"}',
N'/NutComponent/Mobile/EChart', N'ECharts 图表组件', 1, 110);

-- NDataTable 数据表格
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNDataTable', 3, N'展示', N'数据表格', N'📋',
N'{"component":"DynNDataTable","modelname":"","options":{"comoptions":{"columns":[],"pageSize":20,"reportType":"","stationId":0,"startDate":"","endDate":"","apiUrl":"","staticData":[],"autoLoad":true},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"columns":[],"pageSize":20}',
N'/NutComponent/Mobile/DataTable', N'分页数据表格', 1, 111);

-- NViewToggle 视图切换
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNViewToggle', 4, N'通用', N'视图切换', N'🔄',
N'{"component":"DynNViewToggle","modelname":"","options":{"comoptions":{"defaultMode":"chart"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"defaultMode":"chart"}',
N'/NutComponent/Mobile/ViewToggle', N'Chart/Data 视图切换', 1, 112);

-- NProfileHeader 个人中心头部
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNProfileHeader', 3, N'展示', N'个人中心头', N'👤',
N'{"component":"DynNProfileHeader","modelname":"","options":{"comoptions":{"userName":"User","role":"Verified User","userId":"-","avatarIcon":"my"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"userName":"User","role":"Verified User","userId":"-"}',
N'/NutComponent/Mobile/ProfileHeader', N'个人中心头部卡片', 1, 113);

-- NLoginCard 登录卡片
INSERT INTO dbo.ComponentMeta (ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES ('DynNLoginCard', 1, N'表单', N'登录卡片', N'🔐',
N'{"component":"DynNLoginCard","modelname":"","options":{"comoptions":{"title":"Sign In","usernamePlaceholder":"Username","passwordPlaceholder":"Password","buttonText":"Sign In","loadingText":"Signing in...","rememberText":"Remember me","showRemember":true},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"title":"Sign In","showRemember":true}',
N'/NutComponent/Mobile/LoginCard', N'登录表单卡片', 1, 114);

PRINT '移动端业务组件元数据插入完成 (15 个)';
GO
