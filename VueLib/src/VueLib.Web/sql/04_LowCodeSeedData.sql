/* ============================================================
 * VueLib 低代码平台 - 组件元数据种子数据 (NutUI)
 * ============================================================ */
USE VueLib;
GO

SET IDENTITY_INSERT dbo.ComponentMeta ON;
GO

/* ==================== 表单项组件 (ComponentType=1) ==================== */

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (1, 'DynNInput', 1, N'表单', N'输入框', N'📝',
N'{"component":"DynNInput","modelname":"","options":{"comoptions":{"placeholder":"请输入","clearable":true},"comlisteners":{},"labeloptions":{"label":"输入框","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"placeholder":"请输入","clearable":true}',
N'/NutComponent/FormItem/Input', N'单行文本输入框', 1, 1);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (2, 'DynNTextarea', 1, N'表单', N'文本域', N'📄',
N'{"component":"DynNTextarea","modelname":"","options":{"comoptions":{"placeholder":"请输入","rows":3,"maxlength":200,"showCount":true},"comlisteners":{},"labeloptions":{"label":"文本域","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"placeholder":"请输入","rows":3}',
N'/NutComponent/FormItem/Textarea', N'多行文本输入', 1, 2);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (3, 'DynNSwitch', 1, N'表单', N'开关', N'🔘',
N'{"component":"DynNSwitch","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"开关","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{}',
N'/NutComponent/FormItem/Switch', N'开关选择器', 1, 3);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (4, 'DynNRadio', 1, N'表单', N'单选', N'🔵',
N'{"component":"DynNRadio","modelname":"","options":{"comoptions":{"direction":"horizontal"},"comlisteners":{},"labeloptions":{"label":"单选","required":false,"show":true},"itemoptions":{"style":{},"class":""},"optionValues":"选项1,选项2,选项3"},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"direction":"horizontal"}',
N'/NutComponent/FormItem/Radio', N'单选框组', 1, 4);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (5, 'DynNCheckbox', 1, N'表单', N'多选', N'☑️',
N'{"component":"DynNCheckbox","modelname":"","options":{"comoptions":{"direction":"horizontal"},"comlisteners":{},"labeloptions":{"label":"多选","required":false,"show":true},"itemoptions":{"style":{},"class":""},"optionValues":"选项1,选项2,选项3"},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"direction":"horizontal"}',
N'/NutComponent/FormItem/Checkbox', N'多选框组', 1, 5);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (6, 'DynNStepper', 1, N'表单', N'步进器', N'➕',
N'{"component":"DynNStepper","modelname":"","options":{"comoptions":{"min":0,"max":99,"step":1},"comlisteners":{},"labeloptions":{"label":"数量","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"min":0,"max":99,"step":1}',
N'/NutComponent/FormItem/Stepper', N'数字步进器', 1, 6);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (7, 'DynNRate', 1, N'表单', N'评分', N'⭐',
N'{"component":"DynNRate","modelname":"","options":{"comoptions":{"count":5},"comlisteners":{},"labeloptions":{"label":"评分","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"count":5}',
N'/NutComponent/FormItem/Rate', N'星级评分', 1, 7);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (8, 'DynNSlider', 1, N'表单', N'滑块', N'🎚️',
N'{"component":"DynNSlider","modelname":"","options":{"comoptions":{"min":0,"max":100},"comlisteners":{},"labeloptions":{"label":"滑块","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"min":0,"max":100}',
N'/NutComponent/FormItem/Slider', N'滑块选择器', 1, 8);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (9, 'DynNPicker', 1, N'表单', N'选择器', N'📋',
N'{"component":"DynNPicker","modelname":"","options":{"comoptions":{"placeholder":"请选择"},"comlisteners":{},"labeloptions":{"label":"选择器","required":false,"show":true},"itemoptions":{"style":{},"class":""},"optionValues":"选项1,选项2,选项3"},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"placeholder":"请选择"}',
N'/NutComponent/FormItem/Picker', N'弹出选择器', 1, 9);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (10, 'DynNDatePicker', 1, N'表单', N'日期选择', N'📅',
N'{"component":"DynNDatePicker","modelname":"","options":{"comoptions":{"placeholder":"请选择日期","type":"date"},"comlisteners":{},"labeloptions":{"label":"日期","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"placeholder":"请选择日期","type":"date"}',
N'/NutComponent/FormItem/DatePicker', N'日期选择器', 1, 10);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (11, 'DynNUploader', 1, N'表单', N'文件上传', N'📤',
N'{"component":"DynNUploader","modelname":"","options":{"comoptions":{"maxCount":9,"multiple":true},"comlisteners":{},"labeloptions":{"label":"上传","required":false,"show":true},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"maxCount":9,"multiple":true}',
N'/NutComponent/FormItem/Uploader', N'文件/图片上传', 1, 11);

/* ==================== 容器组件 (ComponentType=2) ==================== */

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (20, 'DynNForm', 2, N'布局', N'表单', N'📋',
N'{"component":"DynNForm","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{}',
N'/NutComponent/Container/Form', N'表单容器（验证器根节点）', 1, 1);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (21, 'DynNCellGroup', 2, N'布局', N'单元格组', N'📦',
N'{"component":"DynNCellGroup","modelname":"","options":{"comoptions":{"title":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"title":""}',
N'/NutComponent/Container/CellGroup', N'单元格分组容器', 1, 2);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (22, 'DynNDivContainer', 2, N'布局', N'布局容器', N'🗂️',
N'{"component":"DynNDivContainer","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{"padding":"10px","background":"#fff"},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{}',
N'/NutComponent/Container/DivContainer', N'通用 DIV 容器', 1, 3);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (23, 'DynNDivider', 2, N'布局', N'分割线', N'➖',
N'{"component":"DynNDivider","modelname":"","options":{"comoptions":{"content":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"content":""}',
N'/NutComponent/Container/Divider', N'分割线', 1, 4);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (24, 'DynNGrid', 2, N'布局', N'网格', N'🔲',
N'{"component":"DynNGrid","modelname":"","options":{"comoptions":{"columnNum":2},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"columnNum":2}',
N'/NutComponent/Container/Grid', N'网格布局', 1, 5);

/* ==================== 展示组件 (ComponentType=3) ==================== */

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (30, 'DynNTag', 3, N'展示', N'标签', N'🏷️',
N'{"component":"DynNTag","modelname":"","options":{"comoptions":{"text":"标签","type":"primary"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"text":"标签","type":"primary"}',
N'/NutComponent/Display/Tag', N'标签展示', 1, 1);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (31, 'DynNText', 3, N'展示', N'文本', N'📝',
N'{"component":"DynNText","modelname":"","options":{"comoptions":{"text":"文本内容","size":"base","color":""},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"text":"文本内容"}',
N'/NutComponent/Display/Text', N'文本展示', 1, 2);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (32, 'DynNNoticeBar', 3, N'展示', N'通知栏', N'📢',
N'{"component":"DynNNoticeBar","modelname":"","options":{"comoptions":{"text":"通知内容","type":"warning"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"text":"通知内容","type":"warning"}',
N'/NutComponent/Display/NoticeBar', N'滚动通知栏', 1, 3);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (33, 'DynNProgress', 3, N'展示', N'进度条', N'📊',
N'{"component":"DynNProgress","modelname":"","options":{"comoptions":{"percentage":50},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"percentage":50}',
N'/NutComponent/Display/Progress', N'进度条', 1, 4);

/* ==================== 通用组件 (ComponentType=4) ==================== */

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (40, 'DynNButton', 4, N'通用', N'按钮', N'🔘',
N'{"component":"DynNButton","modelname":"","options":{"comoptions":{"text":"按钮","type":"primary","block":false},"comlisteners":{"click":""},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"text":"按钮","type":"primary"}',
N'/NutComponent/Common/Button', N'按钮', 1, 1);

INSERT INTO dbo.ComponentMeta (Id, ComponentName, ComponentType, Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, LoadUrl, Description, IsEnabled, SortOrder)
VALUES (41, 'DynNImage', 4, N'通用', N'图片', N'🖼️',
N'{"component":"DynNImage","modelname":"","options":{"comoptions":{"src":"","width":"100px","height":"100px","fit":"cover"},"comlisteners":{},"labeloptions":{},"itemoptions":{"style":{},"class":""}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
N'{"src":"","width":"100px","height":"100px"}',
N'/NutComponent/Common/Image', N'图片展示', 1, 2);

SET IDENTITY_INSERT dbo.ComponentMeta OFF;
GO

/* ==================== 示例页面配置 ==================== */
INSERT INTO dbo.PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, Description, IsEnabled, SortOrder)
VALUES (N'用户注册表单', N'user-register', N'示例', N'📝',
N'{"component":"DynNForm","modelname":"","options":{"comoptions":{},"labeloptions":{},"itemoptions":{"style":{}}},"childrenctrls":[{"component":"DynNCellGroup","modelname":"","options":{"comoptions":{"title":"基本信息"},"itemoptions":{"style":{}}},"childrenctrls":[{"component":"DynNInput","modelname":"username","options":{"comoptions":{"placeholder":"请输入用户名","clearable":true},"labeloptions":{"label":"用户名","required":true,"show":true},"itemoptions":{"style":{}}},"validators":[{"type":"required","message":"用户名不能为空"},{"type":"minLength","value":3,"message":"用户名至少3个字符"}]},{"component":"DynNInput","modelname":"phone","options":{"comoptions":{"placeholder":"请输入手机号","type":"tel"},"labeloptions":{"label":"手机号","required":true,"show":true},"itemoptions":{"style":{}}},"validators":[{"type":"required","message":"手机号不能为空"},{"type":"phone","message":"手机号格式不正确"}]},{"component":"DynNPicker","modelname":"gender","options":{"comoptions":{"placeholder":"请选择性别"},"labeloptions":{"label":"性别","required":true,"show":true},"itemoptions":{"style":{}},"optionValues":"男,女,保密"},"validators":[{"type":"required","message":"请选择性别"}]},{"component":"DynNSwitch","modelname":"agree","options":{"comoptions":{},"labeloptions":{"label":"同意协议","required":true,"show":true},"itemoptions":{"style":{}}},"validators":[{"type":"requiredTrue","message":"请同意用户协议"}]}]},{"component":"DynNDivider","modelname":"","options":{"comoptions":{"content":""},"itemoptions":{"style":{}}}},{"component":"DynNButton","modelname":"","options":{"comoptions":{"text":"提交注册","type":"primary","block":true},"comlisteners":{"click":"handleSubmit"},"itemoptions":{"style":{"marginTop":"16px"}}}}]}',
N'{"username":"","phone":"","gender":"","agree":false}',
N'用户注册表单示例，包含验证器', 1, 1);

INSERT INTO dbo.PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, Description, IsEnabled, SortOrder)
VALUES (N'商品评价', N'product-review', N'示例', N'⭐',
N'{"component":"DynNForm","modelname":"","options":{"comoptions":{},"labeloptions":{},"itemoptions":{"style":{}}},"childrenctrls":[{"component":"DynNCellGroup","modelname":"","options":{"comoptions":{"title":"评价商品"},"itemoptions":{"style":{}}},"childrenctrls":[{"component":"DynNRate","modelname":"rating","options":{"comoptions":{"count":5},"labeloptions":{"label":"评分","required":true,"show":true},"itemoptions":{"style":{}}},"validators":[{"type":"required","message":"请评分"}]},{"component":"DynNTextarea","modelname":"content","options":{"comoptions":{"placeholder":"分享你的使用体验...","rows":4,"maxlength":500,"showCount":true},"labeloptions":{"label":"评价内容","required":false,"show":true},"itemoptions":{"style":{}}},"validators":[{"type":"maxLength","value":500,"message":"评价不能超过500字"}]},{"component":"DynNUploader","modelname":"images","options":{"comoptions":{"maxCount":6,"multiple":true},"labeloptions":{"label":"上传图片","required":false,"show":true},"itemoptions":{"style":{}}}}]},{"component":"DynNButton","modelname":"","options":{"comoptions":{"text":"提交评价","type":"primary","block":true},"comlisteners":{"click":"handleSubmit"},"itemoptions":{"style":{"marginTop":"16px"}}}}]}',
N'{"rating":0,"content":"","images":[]}',
N'商品评价表单示例', 1, 2);

PRINT '低代码平台种子数据插入完成。';
GO

-- 验证
SELECT 'ComponentMeta' AS TableName, COUNT(*) AS Count FROM dbo.ComponentMeta
UNION ALL
SELECT 'PageSetting', COUNT(*) FROM dbo.PageSetting;
GO
