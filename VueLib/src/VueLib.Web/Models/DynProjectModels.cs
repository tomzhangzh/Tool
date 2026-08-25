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
    public List<DynColumnDef> Columns { get; set; } = new();
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
