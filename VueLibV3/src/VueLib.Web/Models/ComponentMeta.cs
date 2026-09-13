using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 组件元数据 —— dynCom 唯一内核的组件注册表
/// 组件实现优先从数据库读取；若 Area 中存在同名 cshtml 视图则用视图渲染（fallback）。
/// </summary>
[SugarTable("ComponentMeta")]
public class ComponentMeta
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>组件名（全局唯一，如 DynNInput / DynElCard / DynCompositeDemo）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>显示名（设计器面板上的标签）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Label { get; set; }

    /// <summary>分类：data / container / wrapper / slot / composite / validator / property / system</summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>UI 库：element / layui / native / custom</summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? UiLibrary { get; set; } = "element";

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    /// <summary>组件的 cshtml 视图路径（如 /Areas/Components/Views/Data/DynNInput.cshtml），空则用 ComponentName 约定查找</summary>
    [SugarColumn(Length = 300, IsNullable = true)]
    public string? LoadUrl { get; set; }

    /// <summary>默认配置 JSON（新建组件实例时的初始 options）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? DefaultConfigJson { get; set; }

    /// <summary>属性配置 Schema JSON（右侧属性框渲染依据）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? PropertyConfigJson { get; set; }

    /// <summary>是否组合组件</summary>
    public bool IsComposite { get; set; } = false;

    /// <summary>组合组件配置 JSON（开放属性路径、开放容器路径、开放 slot 名）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? CompositeConfigJson { get; set; }

    /// <summary>是否容器组件（可拖拽子组件）</summary>
    public bool IsContainer { get; set; } = false;

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
