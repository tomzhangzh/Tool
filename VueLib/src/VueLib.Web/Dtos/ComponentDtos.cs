using VueLib.Web.Models;

namespace VueLib.Web.Dtos;

/// <summary>
/// 组件清单 DTO - 用于前端启动时获取组件列表（不含完整内容，减少传输量）
/// </summary>
public class ComponentListItemDto
{
    public string ComponentName { get; set; } = string.Empty;
    public ComponentType ComponentType { get; set; }
    public string? RoutePath { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// 组件完整定义 DTO - 返回给前端用于动态创建 Vue 组件
/// </summary>
public class ComponentDefineDto
{
    public string ComponentName { get; set; } = string.Empty;
    public ComponentType ComponentType { get; set; }
    public string? RoutePath { get; set; }
    public string TemplateContent { get; set; } = string.Empty;
    public string ScriptContent { get; set; } = string.Empty;
    public string? StyleContent { get; set; }
}

/// <summary>
/// 通用 API 响应包装
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}
