SET NOCOUNT ON;
-- ===== 1033 Detail：容器改 grid 两列；Gender/Title 改 dict 下拉；Email 跨两列 =====
DECLARE @cfg NVARCHAR(MAX) = N'{"component":"DynElDivContainer","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{"display":"grid","grid-template-columns":"1fr 1fr","gap":"16px 24px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[
{"component":"DynComInput","modelname":"Row.TeacherNo","options":{"comoptions":{"placeholder":"工号"},"comlisteners":{},"labeloptions":{"label":"工号","required":true,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[{"type":"required","message":"工号必填"}],"childrenctrls":[]},
{"component":"DynComInput","modelname":"Row.Name","options":{"comoptions":{"placeholder":"姓名"},"comlisteners":{},"labeloptions":{"label":"姓名","required":true,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[{"type":"required","message":"姓名必填"}],"childrenctrls":[]},
{"component":"DynComSelect","modelname":"Row.Gender","options":{"comoptions":{"placeholder":"请选择","sourceType":"dict","dictType":"Gender"},"comlisteners":{},"labeloptions":{"label":"性别","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
{"component":"DynComSelect","modelname":"Row.Title","options":{"comoptions":{"placeholder":"请选择","sourceType":"dict","dictType":"TeacherTitle"},"comlisteners":{},"labeloptions":{"label":"职称","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
{"component":"DynComInput","modelname":"Row.Phone","options":{"comoptions":{"placeholder":"电话"},"comlisteners":{},"labeloptions":{"label":"电话","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
{"component":"DynComInput","modelname":"Row.Email","options":{"comoptions":{"placeholder":"邮箱"},"comlisteners":{},"labeloptions":{"label":"邮箱","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{},"class":"col-span-2"},"wrapperoptions":{}},"validators":[],"childrenctrls":[]}
],"slots":{},"extendinfo":{}}';
UPDATE PageSetting SET ConfigJson = @cfg WHERE Id = 1033;

-- ===== 1031 Filter：Gender/Title 下拉改 dict =====
UPDATE PageSetting SET ConfigJson = REPLACE(ConfigJson,
  N'"optionValues":"男,女"', N'"sourceType":"dict","dictType":"Gender"')
WHERE Id = 1031 AND ConfigJson LIKE '%optionValues":"男,女"%';
UPDATE PageSetting SET ConfigJson = REPLACE(ConfigJson,
  N'"optionValues":"高级,特级,中级,初级"', N'"sourceType":"dict","dictType":"TeacherTitle"')
WHERE Id = 1031 AND ConfigJson LIKE '%optionValues":"高级,特级,中级,初级"%';

SELECT Id, ConfigJson FROM PageSetting WHERE Id IN (1031, 1033);
