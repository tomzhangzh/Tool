namespace VueLib.Web.Models;

/// <summary>
/// Razor 组件视图模型 - 用于在 .cshtml 中定义组件
/// 每个组件 View 强类型为此模型，设置 Template/Script/Style 属性
/// </summary>
public class ComponentViewModel
{
    /// <summary>组件名称</summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>组件类型</summary>
    public ComponentType ComponentType { get; set; } = ComponentType.Common;

    /// <summary>路由路径（页面组件）</summary>
    public string? RoutePath { get; set; }

    /// <summary>Vue template HTML</summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>Vue script (export default {...})</summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>组件样式 CSS</summary>
    public string? Style { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }
}
