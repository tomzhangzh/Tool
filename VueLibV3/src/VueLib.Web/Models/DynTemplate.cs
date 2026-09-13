using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 模板（重新设计）—— 预定义的页面模板：CRUD 三屏（filter/list/detail）、左树右列表、
/// Home 九宫格、登录页等。每个模板有自己的 Config Schema（JSON），模板本身是 cshtml 视图 + 占位符。
/// DynWebPage 选择模板 + 填模板参数 = 真实页面。
/// </summary>
[SugarTable("DynTemplate")]
public class DynTemplate
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>模板编码（如 crud-list / tree-right-list / home-grid / login）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    /// <summary>模板类型：CrudList / TreeList / HomeGrid / Login / Custom</summary>
    [SugarColumn(Length = 30, IsNullable = true)]
    public string TemplateType { get; set; } = "Custom";

    /// <summary>渲染视图名（cshtml），运行时按此视图渲染模板</summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? RenderView { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 模板参数定义 JSON（数组）：[{key,label,type,required,default,options[],fields[]}]
    /// type: pagePicker/input/textarea/number/switch/select/gridItems/json/comPicker
    /// </summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamSchema { get; set; }

    /// <summary>模板默认配置 JSON（DynTemplateConfig）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? DefaultConfig { get; set; }

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>模板配置（DefaultConfig 反序列化目标）</summary>
public class DynTemplateConfig
{
    /// <summary>detail 打开路径模板（支持 {projectId} {webPageId} {id} 占位）</summary>
    public string? DetailOpenPath { get; set; }
    /// <summary>detail 打开方式：modal / window / newtab</summary>
    public string DetailOpenMode { get; set; } = "modal";
    /// <summary>点击"新增"时预填参数</summary>
    public Dictionary<string, object?>? AddParams { get; set; }
    /// <summary>自定义删除 url；空 → 通用 /Runtime/DynCrud/Delete</summary>
    public string? DeleteUrl { get; set; }
    /// <summary>自定义数据 url；空 → 通用动态查询</summary>
    public string? DataUrl { get; set; }
    /// <summary>CRUD 三屏：列表操作的业务表名</summary>
    public string? TableName { get; set; }
    /// <summary>CRUD：默认排序</summary>
    public string? OrderBy { get; set; }
    public string OrderDir { get; set; } = "desc";
    public int PageSize { get; set; } = 10;
    public Dictionary<string, object?>? Extra { get; set; }
}
