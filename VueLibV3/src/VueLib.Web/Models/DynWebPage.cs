using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 动态页面 —— 选择模板 + 填模板参数 = 真实页面。
/// 模板参数页本身用 dyncom 渲染（comPicker 选组件 JSON 作为 filter/list/detail 配置）。
/// </summary>
[SugarTable("DynWebPage")]
public class DynWebPage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    /// <summary>路由路径（如 /home /customer-list）</summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Route { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Title { get; set; }

    /// <summary>使用的模板 Id</summary>
    public int TemplateId { get; set; }

    /// <summary>页面配置 JSON（覆盖模板默认配置）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? Config { get; set; }

    /// <summary>按模板 ParamSchema 填写的参数值 JSON</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? Params { get; set; }

    /// <summary>页面 dyncom 布局 JSON（设计器保存的组件树，用于自定义模板）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? LayoutJson { get; set; }

    public bool IsHome { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ==================== 共享 DTO ====================

public class OpResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }

    public static OpResult Ok(object? data = null, string? msg = null) => new() { Success = true, Data = data, Message = msg };
    public static OpResult Fail(string msg) => new() { Success = false, Message = msg };
}

public class PagedResult<T>
{
    public List<T> Rows { get; set; } = new();
    public long TotalCount { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
}

/// <summary>动态 CRUD 请求体（无需 model，直接传 table 名 + 条件 + 分页排序）</summary>
public class DynCrudQuery
{
    public string TableName { get; set; } = string.Empty;
    public Dictionary<string, object?>? Filter { get; set; }
    public string? OrderBy { get; set; }
    public string OrderDir { get; set; } = "desc";
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Keyword { get; set; }
    public List<string>? KeywordColumns { get; set; }
}

/// <summary>动态保存请求（insert 或 update，按主键自动判断）</summary>
public class DynCrudSave
{
    public string TableName { get; set; } = string.Empty;
    public Dictionary<string, object?> Data { get; set; } = new();
    /// <summary>主键列名（不传则自动探测）</summary>
    public string? PrimaryKey { get; set; }
}

/// <summary>动态删除请求</summary>
public class DynCrudDelete
{
    public string TableName { get; set; } = string.Empty;
    public object Id { get; set; } = null!;
    public string? PrimaryKey { get; set; }
}
