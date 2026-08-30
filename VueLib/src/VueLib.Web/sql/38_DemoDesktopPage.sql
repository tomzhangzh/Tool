USE [VueLib];
GO
IF NOT EXISTS (SELECT 1 FROM dbo.PageSetting WHERE PageCode = N'desktop-demo')
BEGIN
INSERT INTO dbo.PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, Platform, CanvasWidth, CanvasHeight, IsEnabled, SortOrder)
VALUES (N'桌面端Demo（栅格+标签统一）', N'desktop-demo', N'示例', N'🖥️',
N'{"component":"DynElDivContainer","modelname":"","options":{"labelcontext":{"width":"120px","align":"right"},"itemoptions":{"style":{"padding":"24px"},"class":""},"comoptions":{},"comlisteners":{},"labeloptions":{}},"validators":[],"childrenctrls":[{"component":"DynElCard","modelname":"","options":{"labelcontext":{},"comoptions":{"title":"员工信息登记（标签统一右对齐 120px）","shadow":"always"},"itemoptions":{},"labeloptions":{}},"validators":[],"childrenctrls":[{"component":"DynElRow","modelname":"","options":{"labelcontext":{},"comoptions":{"cols":24,"gutter":20},"itemoptions":{},"labeloptions":{}},"validators":[],"childrenctrls":[{"component":"DynElInput","modelname":"emp.name","options":{"comoptions":{"placeholder":"请输入姓名","clearable":true},"labeloptions":{"label":"姓名","required":true,"show":true},"layout":{"span":8}},"validators":[{"type":"required","message":"请输入姓名"}],"childrenctrls":[]},{"component":"DynElInput","modelname":"emp.gender","options":{"comoptions":{"placeholder":"请输入性别"},"labeloptions":{"label":"性别","show":true},"layout":{"span":8}},"validators":[],"childrenctrls":[]},{"component":"DynElInput","modelname":"emp.age","options":{"comoptions":{"placeholder":"请输入年龄"},"labeloptions":{"label":"年龄","show":true},"layout":{"span":8}},"validators":[],"childrenctrls":[]},{"component":"DynElSelect","modelname":"emp.department","options":{"comoptions":{"placeholder":"请选择部门"},"optionValues":"技术部,市场部,财务部,人事部","labeloptions":{"label":"所属部门","show":true},"layout":{"span":12}},"validators":[],"childrenctrls":[]},{"component":"DynElDatePicker","modelname":"emp.hireDate","options":{"comoptions":{"placeholder":"选择入职日期","type":"date"},"labeloptions":{"label":"入职日期","show":true},"layout":{"span":12}},"validators":[],"childrenctrls":[]},{"component":"DynElInput","modelname":"emp.phone","options":{"comoptions":{"placeholder":"请输入手机号"},"labeloptions":{"label":"联系电话","show":true},"layout":{"span":24}},"validators":[],"childrenctrls":[]}]}]}],"extendinfo":{},"slots":{}}',
N'{"emp":{"name":"张三","gender":"男","age":30,"department":"技术部","hireDate":"2020-05-01","phone":"13800001111"}}',
N'desktop', 1366, 768, 1, 100);
END
GO
PRINT N'desktop-demo 页面创建完成';
GO
