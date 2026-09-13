using System.Text.Json;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 首启动种子数据服务 —— 内置组件元数据、内置 actionhelper 脚本、内置模板、示例工程
/// </summary>
public class SeedService
{
    private readonly AppDbContext _db;
    private readonly BusinessDbFactory _bizDb;

    public SeedService(AppDbContext db, BusinessDbFactory bizDb)
    {
        _db = db;
        _bizDb = bizDb;
    }

    public void Seed()
    {
        SeedComponentMeta();
        SeedActionHelpers();
        SeedTemplates();
        SeedDict();
        SeedDesktop();
        SeedBusinessTables();
    }

    private static string J(object o) => JsonSerializer.Serialize(o, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    });

    /// <summary>内置组件元数据</summary>
    private void SeedComponentMeta()
    {
        using var db = _db.Create();
        if (db.Queryable<ComponentMeta>().Any()) return;

        var components = new List<ComponentMeta>
        {
            // ===== 表单组件 =====
            NewComp("DynElInput", "输入框", "表单", "element", "/Areas/Components/Views/Form/DynElInput.cshtml",
                new { comoptions = new { placeholder = "请输入", clearable = true }, labeloptions = new { label = "输入框", required = false, show = true } },
                prop: new string[] { "placeholder", "clearable", "disabled", "type", "maxlength", "showwordlimit", "label", "required", "modelname" }),
            NewComp("DynElSelect", "下拉选择", "表单", "element", "/Areas/Components/Views/Form/DynElSelect.cshtml",
                new { comoptions = new { placeholder = "请选择", clearable = true, options = new object[] { } }, labeloptions = new { label = "下拉选择", required = false, show = true } },
                prop: new string[] { "placeholder", "clearable", "disabled", "multiple", "options", "dictdata", "dicttype", "label", "required", "modelname" }),
            NewComp("DynElDatePicker", "日期选择", "表单", "element", "/Areas/Components/Views/Form/DynElDatePicker.cshtml",
                new { comoptions = new { placeholder = "选择日期", clearable = true }, labeloptions = new { label = "日期", required = false, show = true } },
                prop: new string[] { "placeholder", "clearable", "disabled", "type", "format", "label", "required", "modelname" }),
            NewComp("DynElSwitch", "开关", "表单", "element", "/Areas/Components/Views/Form/DynElSwitch.cshtml",
                new { comoptions = new { }, labeloptions = new { label = "开关", required = false, show = true } },
                prop: new string[] { "label", "required", "modelname", "disabled" }),
            NewComp("DynElButton", "按钮", "表单", "element", "/Areas/Components/Views/Form/DynElButton.cshtml",
                new { comoptions = new { text = "按钮", type = "primary" }, itemoptions = new { style = new { } } },
                prop: new string[] { "text", "type", "size", "disabled", "click", "style", "class" }),
            NewComp("DynElCheckbox", "多选框", "表单", "element", "/Areas/Components/Views/Form/DynElCheckbox.cshtml",
                new { comoptions = new { options = new object[] { } }, labeloptions = new { label = "多选", required = false, show = true } },
                prop: new string[] { "options", "dictdata", "dicttype", "label", "required", "modelname" }),
            NewComp("DynElRadio", "单选框", "表单", "element", "/Areas/Components/Views/Form/DynElRadio.cshtml",
                new { comoptions = new { options = new object[] { } }, labeloptions = new { label = "单选", required = false, show = true } },
                prop: new string[] { "options", "dictdata", "dicttype", "label", "required", "modelname" }),
            NewComp("DynElInputNumber", "数字输入", "表单", "element", "/Areas/Components/Views/Form/DynElInputNumber.cshtml",
                new { comoptions = new { }, labeloptions = new { label = "数字", required = false, show = true } },
                prop: new string[] { "min", "max", "step", "disabled", "label", "required", "modelname" }),
            NewComp("DynElTextarea", "多行文本", "表单", "element", "/Areas/Components/Views/Form/DynElTextarea.cshtml",
                new { comoptions = new { placeholder = "请输入", rows = 3 }, labeloptions = new { label = "多行文本", required = false, show = true } },
                prop: new string[] { "placeholder", "rows", "maxlength", "showwordlimit", "label", "required", "modelname" }),

            // ===== 布局组件 =====
            NewComp("DynElForm", "表单容器", "布局", "element", "/Areas/Components/Views/Layout/DynElForm.cshtml",
                new { comoptions = new { labelWidth = "100px" }, itemoptions = new { style = new { } } },
                isContainer: true,
                prop: new string[] { "labelWidth", "size", "style", "class" }),
            NewComp("DynElCard", "卡片容器", "布局", "element", "/Areas/Components/Views/Layout/DynElCard.cshtml",
                new { comoptions = new { header = "卡片标题" }, itemoptions = new { style = new { } } },
                isContainer: true,
                prop: new string[] { "header", "shadow", "style", "class" }),
            NewComp("DynElRow", "栅格行", "布局", "element", "/Areas/Components/Views/Layout/DynElRow.cshtml",
                new { comoptions = new { gutter = 16 } }, isContainer: true,
                prop: new string[] { "gutter", "justify", "style", "class" }),
            NewComp("DynElCol", "栅格列", "布局", "element", "/Areas/Components/Views/Layout/DynElCol.cshtml",
                new { comoptions = new { span = 12 } }, isContainer: true,
                prop: new string[] { "span", "offset", "style", "class" }),
            NewComp("DynElDiv", "通用div容器", "布局", "native", "/Areas/Components/Views/Layout/DynElDiv.cshtml",
                new { itemoptions = new { style = new { display = "grid", gridTemplateColumns = "1fr 1fr", gap = "16px 24px" } } },
                isContainer: true,
                prop: new string[] { "style", "class", "display", "gridTemplateColumns", "gap" }),
            NewComp("DynElTabs", "标签页", "布局", "element", "/Areas/Components/Views/Layout/DynElTabs.cshtml",
                new { comoptions = new { }, slots = new { } }, isContainer: true,
                prop: new string[] { "type", "style", "class" }),
            NewComp("DynElTabPane", "标签页面板", "布局", "element", "/Areas/Components/Views/Layout/DynElTabPane.cshtml",
                new { comoptions = new { label = "标签页", name = "tab1" } }, isContainer: true,
                prop: new string[] { "label", "name", "style", "class" }),

            // ===== 展示组件 =====
            NewComp("DynElDivider", "分隔线", "展示", "element", "/Areas/Components/Views/Display/DynElDivider.cshtml",
                new { comoptions = new { content = "", direction = "horizontal" }, itemoptions = new { style = new { } } },
                prop: new string[] { "content", "direction", "style", "class" }),
            NewComp("DynElTag", "标签", "展示", "element", "/Areas/Components/Views/Display/DynElTag.cshtml",
                new { comoptions = new { text = "标签", type = "primary" }, itemoptions = new { style = new { } } },
                prop: new string[] { "text", "type", "closable", "modelname", "style", "class" }),
            NewComp("DynElImage", "图片", "展示", "element", "/Areas/Components/Views/Display/DynElImage.cshtml",
                new { comoptions = new { fit = "cover" }, itemoptions = new { style = new { width = "100px" } } },
                prop: new string[] { "src", "fit", "modelname", "style", "class" }),
            NewComp("DynElTable", "数据表格", "展示", "element", "/Areas/Components/Views/Display/DynElTable.cshtml",
                new { comoptions = new { columns = new object[] { } }, itemoptions = new { style = new { } } },
                prop: new string[] { "columns", "data", "modelname", "style", "class" }),

            // ===== 通用组件 =====
            NewComp("DynWrapper", "布局壳", "通用", "native", "/Areas/Components/Views/Common/DynWrapper.cshtml",
                new { itemoptions = new { style = new { padding = "8px" } } }, isContainer: true,
                prop: new string[] { "style", "class" }),
            NewComp("DynSlot", "插槽", "通用", "native", "/Areas/Components/Views/Common/DynSlot.cshtml",
                new { comoptions = new { name = "default" } }, isContainer: true,
                prop: new string[] { "name" }),
            NewComp("DynForEach", "循环组件", "通用", "native", "/Areas/Components/Views/Common/DynForEach.cshtml",
                new { itemoptions = new { style = new { } } }, isContainer: true,
                prop: new string[] { "modelname", "style", "class" }),
            NewComp("DynCodeMirror", "代码编辑器", "通用", "custom", "/Areas/Components/Views/Common/DynCodeMirror.cshtml",
                new { comoptions = new { mode = "json", height = "300px" } },
                prop: new string[] { "mode", "height", "theme", "modelname" }),
            NewComp("DynOpenWindow", "打开窗口组件", "通用", "custom", "/Areas/Components/Views/Common/DynOpenWindow.cshtml",
                new { comoptions = new { title = "窗口", width = "80%", text = "打开窗口" } },
                prop: new string[] { "title", "width", "url", "text", "modelname" }),
            NewComp("DynPropPanel", "属性面板", "通用", "element", "/Areas/Components/Views/System/DynPropPanel.cshtml",
                new { comoptions = new { } }, prop: new string[] { "target", "schema" }),
            NewComp("DynDesktop", "桌面组件", "通用", "custom", "/Areas/Components/Views/System/DynDesktop.cshtml",
                new { comoptions = new { } }, prop: new string[] { "solutionId" }),

            // ===== 组合组件 =====
            NewComp("DynCompositeName", "姓名组合", "组合", "element", "/Areas/Components/Views/Composite/DynCompositeName.cshtml",
                new { comoptions = new { firstPlaceholder = "名", lastPlaceholder = "姓" },
                    labeloptions = new { label = "姓名", required = false, show = true } },
                isComposite: true,
                prop: new string[] { "firstPlaceholder", "lastPlaceholder", "label", "required", "modelname" }),
        };

        db.Insertable(components).ExecuteCommand();
    }

    private static ComponentMeta NewComp(
        string name, string label, string category, string uiLib, string loadUrl,
        object defaultOptions, bool isContainer = false, bool isComposite = false,
        object? composite = null, string[]? prop = null)
    {
        return new ComponentMeta
        {
            ComponentName = name,
            Label = label,
            Category = category,
            UiLibrary = uiLib,
            LoadUrl = loadUrl,
            DefaultConfigJson = J(defaultOptions),
            IsContainer = isContainer,
            IsComposite = isComposite,
            CompositeConfigJson = composite == null ? null : J(composite),
            PropertyConfigJson = prop == null ? null : J(prop),
            SortOrder = 0,
            IsEnabled = true
        };
    }

    /// <summary>内置 actionhelper 脚本（全部入库，前端动态注册）</summary>
    private void SeedActionHelpers()
    {
        using var db = _db.Create();
        if (db.Queryable<DynActionHelper>().Any()) return;

        var actions = new List<DynActionHelper>
        {
            new() { Name = "Post 提交", Code = "post", Category = "data", IsBuiltin = true,
                Description = "把当前 model POST 到 url，返回 JSON 后提示",
                ParamsSchema = J(new object[] { new { key = "url", label = "请求地址", type = "input", required = true } }),
                ScriptContent = @"function(ctx){ return ctx.dyn.postJSON(ctx.options.url, ctx.model).then(function(res){ if(res.success!==false){ ctx.dyn.message('success','操作成功'); } else { ctx.dyn.message('error', res.message||'操作失败'); } }); }" },
            new() { Name = "Get 请求", Code = "get", Category = "data", IsBuiltin = true,
                Description = "GET 请求 url 并把结果合并到 model",
                ParamsSchema = J(new object[] { new { key = "url", label = "请求地址", type = "input", required = true } }),
                ScriptContent = @"function(ctx){ return fetch(ctx.options.url).then(function(r){return r.json();}).then(function(data){ if(data && data.success!==false){ Object.assign(ctx.model, data.data||data); ctx.dyn.message('success','加载成功'); } }); }" },
            new() { Name = "打开模态窗口", Code = "open", Category = "window", IsBuiltin = true,
                Description = "打开模态框显示内容",
                ParamsSchema = J(new object[] { new { key = "url", label = "内容地址", type = "input" }, new { key = "title", label = "标题", type = "input" }, new { key = "width", label = "宽度", type = "input", @default = "600px" } }),
                ScriptContent = @"function(ctx){ ctx.dyn.message('info', '打开窗口: ' + (ctx.options.url || '')); }" },
            new() { Name = "关闭模态窗口", Code = "close", Category = "window", IsBuiltin = true,
                Description = "关闭最近的模态框",
                ScriptContent = @"function(ctx){ ctx.dyn.message('info','关闭窗口'); }" },
            new() { Name = "刷新容器", Code = "reload", Category = "ui", IsBuiltin = true,
                Description = "重新加载 data-dyn-url 容器",
                ParamsSchema = J(new object[] { new { key = "selector", label = "选择器", type = "input" } }),
                ScriptContent = @"function(ctx){ var target = ctx.options.selector ? document.querySelector(ctx.options.selector) : ctx.element.closest('[data-dyn-url]') || ctx.element; if(target && target.getAttribute('data-dyn-url')){ return ctx.dyn.postJSON(target.getAttribute('data-dyn-url'), ctx.model).then(function(html){ if(typeof html==='string') target.innerHTML=html; }); } ctx.dyn.message('info','刷新完成'); }" },
            new() { Name = "更新元素", Code = "updateEl", Category = "ui", IsBuiltin = true,
                Description = "更新指定 DOM 元素的内容",
                ParamsSchema = J(new object[] { new { key = "selector", label = "选择器", type = "input", required = true } }),
                ScriptContent = @"function(ctx){ var el = document.querySelector(ctx.options.selector); if(el && ctx.options.html!==undefined){ el.innerHTML = ctx.options.html; } else { ctx.dyn.message('warning','元素不存在'); } }" },
            new() { Name = "设置 Vue Model", Code = "setVueModel", Category = "data", IsBuiltin = true,
                Description = "设置响应式 model 的路径值",
                ParamsSchema = J(new object[] { new { key = "path", label = "路径", type = "input", required = true }, new { key = "value", label = "值", type = "input" } }),
                ScriptContent = @"function(ctx){ ctx.dyn.setVueModel(ctx.element, ctx.options.path, ctx.options.value); }" },
            new() { Name = "执行 JS", Code = "evalJS", Category = "system", IsBuiltin = true,
                Description = "执行一段 JS 代码",
                ParamsSchema = J(new object[] { new { key = "code", label = "JS 代码", type = "textarea", required = true } }),
                ScriptContent = @"function(ctx){ try { new Function('ctx', 'return ('+ctx.options.code+')')(ctx); } catch(e) { ctx.dyn.message('error', '执行异常: '+e.message); } }" },
            new() { Name = "确认对话框", Code = "confirm", Category = "ui", IsBuiltin = true,
                Description = "确认后执行后续动作链",
                ParamsSchema = J(new object[] { new { key = "message", label = "提示", type = "input", required = true }, new { key = "then", label = "确认后动作", type = "json" } }),
                ScriptContent = @"function(ctx){ return ctx.dyn.confirmAsync(ctx.options.message||'确认？').then(function(){ if(ctx.options.then) window.dynActionHelper.runChain(ctx.element, ctx.options.then); }); }" },
            new() { Name = "消息提示", Code = "message", Category = "ui", IsBuiltin = true,
                Description = "显示 Element Plus 消息提示",
                ParamsSchema = J(new object[] { new { key = "type", label = "类型", type = "select", options = new object[] { new { label = "成功", value = "success" }, new { label = "错误", value = "error" }, new { label = "警告", value = "warning" }, new { label = "信息", value = "info" } } }, new { key = "text", label = "内容", type = "input", required = true } }),
                ScriptContent = @"function(ctx){ ctx.dyn.message(ctx.options.type||'info', ctx.options.text); }" },
            new() { Name = "动作链", Code = "chain", Category = "flow", IsBuiltin = true,
                Description = "顺序执行多个动作",
                ParamsSchema = J(new object[] { new { key = "steps", label = "动作链", type = "json", required = true } }),
                ScriptContent = @"function(ctx){ window.dynActionHelper.runChain(ctx.element, ctx.options.steps); }" },
            new() { Name = "路由跳转", Code = "navigate", Category = "window", IsBuiltin = true,
                Description = "跳转到指定地址",
                ParamsSchema = J(new object[] { new { key = "url", label = "地址", type = "input", required = true } }),
                ScriptContent = @"function(ctx){ window.location.href = ctx.options.url; }" },
            new() { Name = "保存数据", Code = "save", Category = "data", IsBuiltin = true,
                Description = "动态保存（insert/update）到业务表",
                ParamsSchema = J(new object[] { new { key = "table", label = "表名", type = "input", required = true }, new { key = "projectId", label = "项目ID", type = "input", @default = "1" } }),
                ScriptContent = @"function(ctx){ return ctx.dyn.postJSON('/Runtime/DynCrud/Save?projectId='+(ctx.options.projectId||1), { tableName: ctx.options.table, data: ctx.model }).then(function(res){ if(res.success){ ctx.dyn.message('success','保存成功'); } else { ctx.dyn.message('error', res.message||'保存失败'); } }); }" },
            new() { Name = "删除数据", Code = "delete", Category = "data", IsBuiltin = true,
                Description = "动态删除业务表记录",
                ParamsSchema = J(new object[] { new { key = "table", label = "表名", type = "input", required = true }, new { key = "id", label = "主键值", type = "input", required = true } }),
                ScriptContent = @"function(ctx){ return ctx.dyn.confirmAsync('确认删除？').then(function(){ return ctx.dyn.postJSON('/Runtime/DynCrud/Delete?projectId=1', { tableName: ctx.options.table, id: ctx.options.id }).then(function(res){ if(res.success){ ctx.dyn.message('success','删除成功'); } else { ctx.dyn.message('error', res.message||'删除失败'); } }); }); }" },
        };
        db.Insertable(actions).ExecuteCommand();
    }

    /// <summary>内置模板</summary>
    private void SeedTemplates()
    {
        using var db = _db.Create();
        if (db.Queryable<DynTemplate>().Any()) return;

        var templates = new List<DynTemplate>
        {
            new() { Name = "CRUD 列表模板", Code = "crud-list", TemplateType = "CrudList",
                RenderView = "/Areas/Runtime/Views/Templates/CrudList.cshtml",
                Description = "标准 CRUD：查询条件 + 数据表格 + 分页 + 新增/编辑/删除",
                ParamSchema = J(new object[]
                {
                    new { key = "tableName", label = "业务表名", type = "input", required = true },
                    new { key = "title", label = "页面标题", type = "input" },
                    new { key = "pageSize", label = "每页条数", type = "number", @default = 10 },
                    new { key = "orderBy", label = "默认排序列", type = "input" },
                    new { key = "orderDir", label = "排序方向", type = "select", options = new object[] { new { label = "降序", value = "desc" }, new { label = "升序", value = "asc" } } }
                }),
                DefaultConfig = J(new { pageSize = 10, orderDir = "desc" }),
                SortOrder = 1 },
            new() { Name = "左树右列表", Code = "tree-right-list", TemplateType = "TreeList",
                RenderView = "/Areas/Runtime/Views/Templates/TreeRightList.cshtml",
                Description = "左侧分类树 + 右侧数据列表",
                ParamSchema = J(new object[]
                {
                    new { key = "tableName", label = "业务表名", type = "input", required = true },
                    new { key = "treeTable", label = "树表名", type = "input", required = true },
                    new { key = "title", label = "页面标题", type = "input" }
                }),
                SortOrder = 2 },
            new() { Name = "主页九宫格", Code = "home-grid", TemplateType = "HomeGrid",
                RenderView = "/Areas/Runtime/Views/Templates/HomeGrid.cshtml",
                Description = "主页快捷入口九宫格",
                ParamSchema = J(new object[]
                {
                    new { key = "title", label = "页面标题", type = "input", @default = "欢迎" },
                    new { key = "gridItems", label = "九宫格项", type = "gridItems",
                        fields = new object[] { new { key = "name", label = "名称" }, new { key = "icon", label = "图标" }, new { key = "url", label = "链接" } } }
                }),
                SortOrder = 3 },
            new() { Name = "自定义布局", Code = "custom-layout", TemplateType = "Custom",
                RenderView = "/Areas/Runtime/Views/Templates/CustomLayout.cshtml",
                Description = "使用 dyncom 设计器自定义布局",
                ParamSchema = J(new object[]
                {
                    new { key = "layoutJson", label = "布局 JSON", type = "json", required = true }
                }),
                SortOrder = 99 },
        };
        db.Insertable(templates).ExecuteCommand();
    }

    /// <summary>Platform 词典种子</summary>
    private void SeedDict()
    {
        using var db = _db.Create();
        if (db.Queryable<DynDict>().Any()) return;
        var dict = new List<DynDict>
        {
            new() { DictType = "YesNo", DictCode = "Y", DictText = "是", DictValue = "1", SortOrder = 1 },
            new() { DictType = "YesNo", DictCode = "N", DictText = "否", DictValue = "0", SortOrder = 2 },
            new() { DictType = "Enabled", DictText = "启用", DictValue = "1", SortOrder = 1 },
            new() { DictType = "Enabled", DictText = "禁用", DictValue = "0", SortOrder = 2 },
        };
        db.Insertable(dict).ExecuteCommand();
    }

    /// <summary>桌面 + 示例工程种子</summary>
    private void SeedDesktop()
    {
        using var db = _db.Create();
        if (db.Queryable<DesktopSolution>().Any()) return;

        var sol = new DesktopSolution { Name = "默认解决方案", Icon = "🖥️", Description = "V3 默认解决方案", SortOrder = 1 };
        db.Insertable(sol).ExecuteReturnIdentity();

        var proj = new DynProject
        {
            SolutionId = sol.Id,
            Name = "DemoProject",
            DisplayName = "示例工程",
            DatabaseName = "VueLibV3_Business",
            Description = "V3 演示工程（业务库）",
            Type = "Web",
            Icon = "📦"
        };
        db.Insertable(proj).ExecuteReturnIdentity();

        var sc = new List<DesktopShortcut>
        {
            new() { Name = "设计器", Icon = "🎨", Url = "/designer", SolutionId = sol.Id, OpenType = "iframe", SortOrder = 1 },
            new() { Name = "主页", Icon = "🏠", Url = "/home", SolutionId = sol.Id, OpenType = "iframe", SortOrder = 2 },
            new() { Name = "Demo", Icon = "📋", Url = "/demo", SolutionId = sol.Id, OpenType = "iframe", SortOrder = 3 },
        };
        db.Insertable(sc).ExecuteCommand();
    }

    /// <summary>业务库示例表（学生表，用于 demo CRUD）</summary>
    private void SeedBusinessTables()
    {
        using var db = _bizDb.Create(null);
        db.DbMaintenance.CreateDatabase();

        // 确保示例表存在
        if (!db.DbMaintenance.IsAnyTable("Student", false))
        {
            db.Ado.ExecuteCommand(@"
CREATE TABLE [Student](
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] [nvarchar](100) NOT NULL,
    [Age] [int] NULL,
    [Gender] [nvarchar](10) NULL,
    [ClassName] [nvarchar](100) NULL,
    [Score] [decimal](5,2) NULL,
    [IsActive] [bit] NOT NULL DEFAULT(1),
    [CreatedAt] [datetime] NOT NULL DEFAULT(GETDATE())
)");
            db.Ado.ExecuteCommand(@"
INSERT INTO [Student](Name,Age,Gender,ClassName,Score) VALUES
('张三',18,'男','高一(1)班',95.5),
('李四',17,'女','高一(2)班',88.0),
('王五',18,'男','高一(1)班',76.5),
('赵六',17,'女','高一(3)班',92.0),
('钱七',18,'男','高一(2)班',81.5)");
        }

        // Teachers 表示例（CRUD 三屏 demo 用）
        if (!db.DbMaintenance.IsAnyTable("Teachers", false))
        {
            db.Ado.ExecuteCommand(@"
CREATE TABLE [Teachers](
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Name] [nvarchar](100) NOT NULL,
    [Gender] [nvarchar](10) NULL,
    [Age] [int] NULL,
    [Phone] [nvarchar](20) NULL,
    [Subject] [nvarchar](50) NULL,
    [Title] [nvarchar](50) NULL,
    [IsActive] [bit] NOT NULL DEFAULT(1),
    [CreatedAt] [datetime] NOT NULL DEFAULT(GETDATE())
)");
            db.Ado.ExecuteCommand(@"
INSERT INTO [Teachers](Name,Gender,Age,Phone,Subject,Title) VALUES
('王老师','男',42,'13800001111','数学','高级教师'),
('李老师','女',35,'13800002222','语文','一级教师'),
('张老师','男',50,'13800003333','英语','高级教师'),
('刘老师','女',28,'13800004444','物理','二级教师'),
('陈老师','男',45,'13800005555','化学','高级教师'),
('杨老师','女',38,'13800006666','生物','一级教师'),
('赵老师','男',33,'13800007777','历史','一级教师'),
('周老师','女',40,'13800008888','地理','高级教师')");
        }
    }
}
