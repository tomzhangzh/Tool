using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 组件定义实体 - 对应数据库表 ComponentDefinitions
/// 存储 Vue 组件的 template / script / style 定义
/// </summary>
[SugarTable("ComponentDefinitions")]
public class ComponentDefinition
{
    /// <summary>主键</summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>组件名称（全局唯一，用于注册和引用）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>组件类型: 1=公共组件, 2=页面组件</summary>
    public ComponentType ComponentType { get; set; }

    /// <summary>路由路径（页面组件专用，公共组件为 null）</summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? RoutePath { get; set; }

    /// <summary>Vue template HTML 内容</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = false)]
    public string TemplateContent { get; set; } = string.Empty;

    /// <summary>Vue script JS 代码（export default {...}）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = false)]
    public string ScriptContent { get; set; } = string.Empty;

    /// <summary>组件样式 CSS（可选）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? StyleContent { get; set; }

    /// <summary>属性面板配置 JSON - 定义右侧属性面板的动态表单结构（Element Plus）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? PropertyConfigJson { get; set; }

    /// <summary>默认配置 JSON - 组件拖入画布时的初始配置</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? DefaultConfigJson { get; set; }

    /// <summary>组件描述</summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>排序号</summary>
    public int SortOrder { get; set; }

    /// <summary>创建时间（UTC）</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新时间（UTC）</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
