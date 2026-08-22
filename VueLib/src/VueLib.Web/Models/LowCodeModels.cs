using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 页面配置实体 - 存储低代码页面的组件树配置
/// </summary>
[SugarTable("PageSetting")]
public class PageSetting
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string PageName { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string PageCode { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = false)]
    public string ConfigJson { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? DefaultModelJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? ApiBaseUrl { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 组件元数据实体 - 注册可拖拽的组件
/// </summary>
[SugarTable("ComponentMeta")]
public class ComponentMeta
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>组件注册名（如 NInput）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>组件类型: 1=表单项, 2=容器, 3=展示, 4=通用</summary>
    public int ComponentType { get; set; }

    [SugarColumn(Length = 50, IsNullable = false)]
    public string Category { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Label { get; set; } = string.Empty;

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = false)]
    public string DefaultConfigJson { get; set; } = string.Empty;

    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? DefaultOptionsJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = false)]
    public string LoadUrl { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
