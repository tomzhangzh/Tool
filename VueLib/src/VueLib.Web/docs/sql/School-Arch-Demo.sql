-- ============================================================
-- 学校管理（ProjectId=4）按新架构重做：三屏 PageSetting + DynWebPage 关联
-- 教师管理 /teachers：filter(姓名/性别) + list(DynElTable) + detail(表单)
-- ============================================================
USE VueLib;
GO
SET NOCOUNT ON;

DECLARE @pid INT = 4;
DECLARE @fps INT, @lps INT, @dps INT;

-- ---------- 1) Filter 屏 PageSetting ----------
IF NOT EXISTS (SELECT 1 FROM PageSetting WHERE PageCode = 'S_TEACHER_FILTER')
BEGIN
    INSERT INTO PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, ApiBaseUrl, Description, IsEnabled, SortOrder, Platform, CustomScriptJson, CanvasWidth, CanvasHeight)
    VALUES (N'教师查询屏', 'S_TEACHER_FILTER', N'Filter屏', 'el-icon-search',
        N'{"component":"DynElDivContainer","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{"display":"flex","flex-wrap":"wrap","gap":"8px","align-items":"flex-start"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[
          {"component":"DynComInput","modelname":"Filter.Name","options":{"comoptions":{"placeholder":"请输入姓名","clearable":true},"comlisteners":{},"labeloptions":{"label":"姓名","required":false,"show":true,"labelWidth":"60px"},"itemoptions":{"style":{"width":"200px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
          {"component":"DynComSelect","modelname":"Filter.Gender","options":{"comoptions":{"placeholder":"全部","clearable":true,"optionValues":"男,女"},"comlisteners":{},"labeloptions":{"label":"性别","required":false,"show":true,"labelWidth":"60px"},"itemoptions":{"style":{"width":"150px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
          {"component":"DynComSelect","modelname":"Filter.Title","options":{"comoptions":{"placeholder":"全部","clearable":true,"optionValues":"高级,特级,中级,初级"},"comlisteners":{},"labeloptions":{"label":"职称","required":false,"show":true,"labelWidth":"60px"},"itemoptions":{"style":{"width":"150px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]}
        ],"slots":{},"extendinfo":{}}',
        N'{"Filter":{}}', '/DynRun/Screen', N'教师查询：姓名/性别/职称', 1, 10, 'desktop', NULL, 1200, 800);
    SET @fps = SCOPE_IDENTITY();
END
ELSE SELECT @fps = Id FROM PageSetting WHERE PageCode = 'S_TEACHER_FILTER';

-- ---------- 2) List 屏 PageSetting（DynElTable） ----------
IF NOT EXISTS (SELECT 1 FROM PageSetting WHERE PageCode = 'S_TEACHER_LIST')
BEGIN
    INSERT INTO PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, ApiBaseUrl, Description, IsEnabled, SortOrder, Platform, CustomScriptJson, CanvasWidth, CanvasHeight)
    VALUES (N'教师列表屏', 'S_TEACHER_LIST', N'List屏', 'el-icon-data-table',
        N'{"component":"DynElTable","modelname":"","options":{"comoptions":{"dataUrl":"/DynRun/Route/List?projectId=4&route=/teachers","pk":"Id","showOps":true,"opWidth":170,"pageSizes":[10,20,50,100],"editUrl":"/DynRun/Detail?projectId=4&pageId=30&id={id}","deleteUrl":"/DynRun/Delete?projectId=4&pageId=30&id={id}","reloadSel":"#screen-list","columns":[{"prop":"TeacherNo","label":"工号","width":110},{"prop":"Name","label":"姓名","width":120},{"prop":"Gender","label":"性别","width":80},{"prop":"Title","label":"职称","width":100},{"prop":"Phone","label":"电话","width":150},{"prop":"Email","label":"邮箱","width":200},{"prop":"HireDate","label":"入职日期","width":120}]},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{},"wrapperoptions":{}},"validators":[],"childrenctrls":[],"slots":{},"extendinfo":{}}',
        N'{"Rows":[],"PageInfo":{"CurrentPage":1,"PageSize":10,"TotalCount":0,"TotalPages":0},"Filter":{}}', '/DynRun/Screen', N'教师列表：ElementUI 表格 + 分页 + 操作列', 1, 20, 'desktop', NULL, 1200, 800);
    SET @lps = SCOPE_IDENTITY();
END
ELSE SELECT @lps = Id FROM PageSetting WHERE PageCode = 'S_TEACHER_LIST';

-- ---------- 3) Detail 屏 PageSetting（表单） ----------
IF NOT EXISTS (SELECT 1 FROM PageSetting WHERE PageCode = 'S_TEACHER_DETAIL')
BEGIN
    INSERT INTO PageSetting (PageName, PageCode, Category, Icon, ConfigJson, DefaultModelJson, ApiBaseUrl, Description, IsEnabled, SortOrder, Platform, CustomScriptJson, CanvasWidth, CanvasHeight)
    VALUES (N'教师详情屏', 'S_TEACHER_DETAIL', N'Detail屏', 'el-icon-edit',
        N'{"component":"DynElDivContainer","modelname":"","options":{"comoptions":{},"comlisteners":{},"labeloptions":{"label":"","required":false,"show":false},"itemoptions":{"style":{"display":"flex","flex-wrap":"wrap","gap":"12px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[
          {"component":"DynComInput","modelname":"Row.TeacherNo","options":{"comoptions":{"placeholder":"工号"},"comlisteners":{},"labeloptions":{"label":"工号","required":true,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{"width":"240px"},"class":""},"wrapperoptions":{}},"validators":[{"type":"required","message":"工号必填"}],"childrenctrls":[]},
          {"component":"DynComInput","modelname":"Row.Name","options":{"comoptions":{"placeholder":"姓名"},"comlisteners":{},"labeloptions":{"label":"姓名","required":true,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{"width":"240px"},"class":""},"wrapperoptions":{}},"validators":[{"type":"required","message":"姓名必填"}],"childrenctrls":[]},
          {"component":"DynComSelect","modelname":"Row.Gender","options":{"comoptions":{"placeholder":"请选择","optionValues":"男,女"},"comlisteners":{},"labeloptions":{"label":"性别","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{"width":"240px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
          {"component":"DynComInput","modelname":"Row.Title","options":{"comoptions":{"placeholder":"职称"},"comlisteners":{},"labeloptions":{"label":"职称","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{"width":"240px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
          {"component":"DynComInput","modelname":"Row.Phone","options":{"comoptions":{"placeholder":"电话"},"comlisteners":{},"labeloptions":{"label":"电话","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{"width":"240px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]},
          {"component":"DynComInput","modelname":"Row.Email","options":{"comoptions":{"placeholder":"邮箱"},"comlisteners":{},"labeloptions":{"label":"邮箱","required":false,"show":true,"labelWidth":"80px"},"itemoptions":{"style":{"width":"240px"},"class":""},"wrapperoptions":{}},"validators":[],"childrenctrls":[]}
        ],"slots":{},"extendinfo":{}}',
        N'{"Row":{}}', '/DynRun/Screen', N'教师详情：表单', 1, 30, 'desktop', NULL, 1200, 800);
    SET @dps = SCOPE_IDENTITY();
END
ELSE SELECT @dps = Id FROM PageSetting WHERE PageCode = 'S_TEACHER_DETAIL';

-- ---------- 4) 更新 DynWebPage /teachers（学校管理重做） ----------
UPDATE DynWebPage
SET FilterPageSettingId = @fps, ListPageSettingId = @lps, DetailPageSettingId = @dps
WHERE ProjectId = @pid AND Route = '/teachers';

SELECT Id AS PageSettingId, PageName, PageCode, Category FROM PageSetting WHERE Id IN (@fps, @lps, @dps);
SELECT Id, Name, Route, TemplateId, FilterPageSettingId, ListPageSettingId, DetailPageSettingId FROM DynWebPage WHERE ProjectId = @pid AND Route = '/teachers';
