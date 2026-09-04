using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 动态工程（低代码运行时工程）
/// 每个工程包含名称 + 数据库连接串，预览时按此连接串连接其数据库并动态渲染页面
/// </summary>
[SugarTable("DynProject")]
public class DynProject
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DisplayName { get; set; }

    /// <summary>目标数据库连接串（预览运行时连接它）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ConnectionString { get; set; }

    /// <summary>数据库名（展示用）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DatabaseName { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Icon { get; set; } = "📦";

    /// <summary>工程类型（Web / Phone / PC ...）默认一工程一数据库</summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? Type { get; set; } = "Web";

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 动态页面（汇总屏 / 细节屏 定义）
/// ColumnDefs 存页面定义的 JSON（PageDefinition 反序列化目标），预览运行时读取并动态渲染
/// </summary>
[SugarTable("DynPage")]
public class DynPage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    /// <summary>页面编码（如 CustomerList）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>页面标题</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Title { get; set; }

    /// <summary>页面类型：Summary=汇总屏 / Detail=细节屏</summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    public string PageType { get; set; } = "Summary";

    /// <summary>该页面操作的业务表名</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? TableName { get; set; }

    /// <summary>
    /// 数据源模式：Dynamic = 动态 SQL 直接查表；View = 使用真实数据库视图（ViewName）
    /// 读取（列表/详情）按此模式取数；写入（增删改）始终作用于真实表
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? DataSource { get; set; } = "Dynamic";

    /// <summary>真实视图名（DataSource = View 时用于读取）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? ViewName { get; set; }

    /// <summary>页面定义 JSON（DynPageDefinition）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ColumnDefs { get; set; }

    /// <summary>汇总屏指向的细节屏页面 Id</summary>
    public int? DetailPageId { get; set; }

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>页面定义（ColumnDefs 的反序列化目标）</summary>
public class DynPageDefinition
{
    /// <summary>主键列名</summary>
    public string PrimaryKey { get; set; } = "Id";
    public bool IsIdentity { get; set; } = true;
    /// <summary>汇总屏每页条数</summary>
    public int PageSize { get; set; } = 10;
    /// <summary>默认排序列</summary>
    public string? OrderBy { get; set; }
    /// <summary>默认排序方向 asc/desc</summary>
    public string OrderDir { get; set; } = "desc";
    /// <summary>自定义"新增"入口 url（如跳转打卡页）；空 → 默认打开详情新增表单</summary>
    public string? AddUrl { get; set; }
    public List<DynColumnDef> Columns { get; set; } = new();

    /// <summary>外键导航配置（列表/详情时按外键注入关联数据）</summary>
    public List<DynNavConfig> Navs { get; set; } = new();
}

/// <summary>外键关系类型</summary>
public enum NavRelation
{
    /// <summary>多对一：当前记录引用目标表的一条记录 → 注入 object</summary>
    ManyToOne,
    /// <summary>一对多：目标表多条记录引用当前记录 → 注入 array</summary>
    OneToMany
}

/// <summary>外键导航配置：按表外键把关联数据注入 summary 行 / detail 详情</summary>
public class DynNavConfig
{
    /// <summary>导航数据在 model 中的键（如 "Customer"、"Orders"），模型里体现为 NavKey: object|array</summary>
    public string NavKey { get; set; } = "";

    /// <summary>关联标签（前端展示标题）</summary>
    public string? Label { get; set; }

    /// <summary>关系类型</summary>
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public NavRelation Relation { get; set; } = NavRelation.ManyToOne;

    /// <summary>目标表</summary>
    public string TargetTable { get; set; } = "";

    /// <summary>ManyToOne：当前表指向目标表的外键列（如 DrawingId）</summary>
    public string? FkColumn { get; set; }

    /// <summary>OneToMany：目标表指向当前表的外键列（如 DrawingId）</summary>
    public string? TargetFkColumn { get; set; }

    /// <summary>目标表展示列（为空则取全部列，逗号分隔可指定多个）</summary>
    public List<string>? DisplayColumns { get; set; }

    /// <summary>目标表主键列（自动识别，可手工指定）</summary>
    public string? TargetPkColumn { get; set; }
}

/// <summary>单个列的页面配置</summary>
public class DynColumnDef
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>CLR 类型：string/int/long/decimal/datetime/bool/guid</summary>
    public string DbType { get; set; } = "string";
    /// <summary>数据库原始类型（nvarchar/int/...）</summary>
    public string SqlType { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsNullable { get; set; } = true;

    /// <summary>是否作为查询条件</summary>
    public bool IsFilter { get; set; }
    /// <summary>筛选方式 eq/like/start/end/gt/ge/lt/le</summary>
    public string FilterOp { get; set; } = "eq";
    /// <summary>是否在汇总屏表格中显示</summary>
    public bool IsGrid { get; set; }
    /// <summary>是否在细节屏表单中显示</summary>
    public bool IsForm { get; set; }
    /// <summary>控件类型：input/input-number/date/datetime/select/switch</summary>
    public string Control { get; set; } = "input";
    /// <summary>select 的选项</summary>
    public List<DynOption> Options { get; set; } = new();
    /// <summary>是否必填</summary>
    public bool Required { get; set; }
    /// <summary>表格列宽</summary>
    public int Width { get; set; }
    /// <summary>是否只读（表单不编辑）</summary>
    public bool IsReadOnly { get; set; }
    public int Order { get; set; }
}

public class DynOption
{
    public DynOption() { }
    public DynOption(string label, string value) { Label = label; Value = value; }
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>通用分页结果</summary>
public class PagedResult<T>
{
    public List<T> Rows { get; set; } = new();
    public long TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
}

/// <summary>统一操作结果</summary>
public class OpResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}

// ==================== 设计层：模板管理 + 路由页面管理 ====================

/// <summary>模板：把 Filter 屏 + Summary 屏 + Detail 屏组装成一个可复用模板（如 List 模板 / 主页模板）</summary>
public class DynTemplate
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = "";

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Code { get; set; } = "";

    /// <summary>模板类型：List / Home / Custom</summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string TemplateType { get; set; } = "List";

    /// <summary>模板统一渲染视图名（如 RouteList / RouteHome），运行时按此渲染</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? RenderView { get; set; }

    /// <summary>模板参数定义 JSON（DynTemplateParam 数组）：描述模板需要哪些参数及控件</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamSchema { get; set; }

    /// <summary>组装的 Filter 屏 Id</summary>
    public int? FilterPageId { get; set; }
    /// <summary>组装的 Summary 屏 Id</summary>
    public int? SummaryPageId { get; set; }
    /// <summary>组装的 Detail 屏 Id</summary>
    public int? DetailPageId { get; set; }

    /// <summary>模板配置 JSON（DynTemplateConfig）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? Config { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>模板配置：detail 打开路径、新增参数、deleteUrl、自定义数据 url 等</summary>
public class DynTemplateConfig
{
    /// <summary>detail 打开路径模板（支持 {projectId} {pageId} {id} 占位）；空 → 用默认 Detail 路径</summary>
    public string? DetailOpenPath { get; set; }
    /// <summary>detail 打开方式：modal(默认) / window / newtab</summary>
    public string DetailOpenMode { get; set; } = "modal";
    /// <summary>点击"新增"时预填到详情表单的参数</summary>
    public Dictionary<string, object?>? AddParams { get; set; }
    /// <summary>自定义删除 url；空 → 通用 /DynRun/Delete</summary>
    public string? DeleteUrl { get; set; }
    /// <summary>自定义数据 url；空 → 通用后端（按模板 summary 屏定义查询）</summary>
    public string? DataUrl { get; set; }
    /// <summary>预留扩展</summary>
    public Dictionary<string, object?>? Extra { get; set; }
}

/// <summary>路由页面：把"路由 ↔ 模板"关联起来，配置模板所需数据即可显示页面</summary>
public class DynWebPage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    public int ProjectId { get; set; }

    /// <summary>路由路径（如 /home /drawings）</summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Route { get; set; } = "";

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = "";

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Title { get; set; }

    /// <summary>使用的模板 Id</summary>
    public int TemplateId { get; set; }

    /// <summary>页面配置 JSON（DynWebPageConfig）：可覆盖模板的 filter/summary/detail + 模板实例数据</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? Config { get; set; }

    /// <summary>按模板 ParamSchema 填写的参数值 JSON（如 {"summaryPageId":6,"filterPageId":17,"detailPageId":5}）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? Params { get; set; }

    /// <summary>是否主页（首页路由）</summary>
    public bool IsHome { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>路由页面配置：覆盖模板的三屏 pageId + 模板实例数据（Params）</summary>
public class DynWebPageConfig
{
    /// <summary>覆盖模板的 Filter 屏 Id（空 → 用模板的）</summary>
    public int? FilterPageId { get; set; }
    /// <summary>覆盖模板的 Summary 屏 Id</summary>
    public int? SummaryPageId { get; set; }
    /// <summary>覆盖模板的 Detail 屏 Id</summary>
    public int? DetailPageId { get; set; }
    /// <summary>模板实例数据（如主页展示内容 / 默认查询参数）</summary>
    public Dictionary<string, object?>? Params { get; set; }
    /// <summary>预留扩展</summary>
    public Dictionary<string, object?>? Extra { get; set; }
}

/// <summary>模板参数定义（ParamSchema 数组项）：描述模板需要哪些参数及控件</summary>
public class DynTemplateParam
{
    public string Key { get; set; } = "";
    public string? Label { get; set; }
    /// <summary>控件类型：pagePicker 页面选择 / input / textarea / number / switch / select / gridItems 九宫格数组 / json</summary>
    public string Type { get; set; } = "input";
    /// <summary>pagePicker 过滤的页面类型（Filter/Summary/Detail）</summary>
    public string? PageType { get; set; }
    public bool Required { get; set; }
    public object? Default { get; set; }
    /// <summary>select 选项</summary>
    public List<DynOption>? Options { get; set; }
    /// <summary>gridItems 子字段定义</summary>
    public List<DynTemplateParamField>? Fields { get; set; }
}

public class DynTemplateParamField
{
    public string Key { get; set; } = "";
    public string? Label { get; set; }
}

/// <summary>路由页面运行时模型（List 模板：Filter 屏 + Summary 屏 + Detail 屏 组合）</summary>
public class DynRouteListModel
{
    public DynProject? Project { get; set; }
    public DynWebPage? WebPage { get; set; }
    public DynTemplate? Template { get; set; }
    public DynPage? FilterPage { get; set; }
    public DynPage? SummaryPage { get; set; }
    public DynPage? DetailPage { get; set; }
    public DynPageDefinition? FilterDef { get; set; }
    public DynPageDefinition? SummaryDef { get; set; }
    public DynTemplateConfig? TemplateConfig { get; set; }
    public DynWebPageConfig? PageConfig { get; set; }
    public Dictionary<string, object?>? Filter { get; set; }
    public PagedResult<Dictionary<string, object?>>? Result { get; set; }
    /// <summary>按模板 ParamSchema 解析后的页面实例参数</summary>
    public Dictionary<string, object?>? Params { get; set; }
}

/// <summary>路由页面运行时模型（Home 模板：主页）</summary>
public class DynRouteHomeModel
{
    public DynProject? Project { get; set; }
    public DynWebPage? WebPage { get; set; }
    public DynTemplate? Template { get; set; }
    public DynWebPageConfig? PageConfig { get; set; }
    public DynPageDefinition? HomeSummaryDef { get; set; }
    public PagedResult<Dictionary<string, object?>>? HomeResult { get; set; }
    /// <summary>主页快捷入口（其它路由页面）</summary>
    public List<DynWebPage>? Pages { get; set; }
    /// <summary>按模板 ParamSchema 解析后的页面实例参数（banner/gridItems 等）</summary>
    public Dictionary<string, object?>? Params { get; set; }
}

/// <summary>路由页面运行时模型（通用模板：按 Params 动态渲染）</summary>
public class DynRouteCustomModel
{
    public DynProject? Project { get; set; }
    public DynWebPage? WebPage { get; set; }
    public DynTemplate? Template { get; set; }
    public Dictionary<string, object?>? Params { get; set; }
    /// <summary>模板参数定义（用于按类型渲染参数值）</summary>
    public List<DynTemplateParam>? Schema { get; set; }
    /// <summary>工程页面列表（pagePicker 参数值转页面名称）</summary>
    public List<DynPage>? Pages { get; set; }
}

// ==================== 预览运行时视图模型 ====================

public class DynRunSummaryModel
{
    public DynProject? Project { get; set; }
    public DynPage? Page { get; set; }
    public DynPageDefinition? Def { get; set; }
    /// <summary>汇总屏实际使用的细节屏页面 Id（用于编辑/新增按钮）</summary>
    public int DetailPageId { get; set; }
    public Dictionary<string, object?> Filter { get; set; } = new();
    public PagedResult<Dictionary<string, object?>>? Result { get; set; }
}

public class DynRunDetailModel
{
    public DynProject? Project { get; set; }
    public DynPage? Page { get; set; }
    public DynPageDefinition? Def { get; set; }
    /// <summary>当前行数据（新增时为按列定义生成的空模板）</summary>
    public Dictionary<string, object?> Row { get; set; } = new();
}

/// <summary>汇总屏 POST 提交体（dyn-lib 提交整个 model）</summary>
public class DynSummaryPost
{
    public Dictionary<string, object?>? Filter { get; set; }
    public DynPageInfoPost? PageInfo { get; set; }
}

public class DynPageInfoPost
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; }
}
