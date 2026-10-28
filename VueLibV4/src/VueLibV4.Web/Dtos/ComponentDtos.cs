using Newtonsoft.Json;

namespace VueLibV4.Web.Dtos;

/// <summary>组件类型（Page=页面级组件/路由；Common=通用组件）</summary>
public enum ComponentType
{
    Common = 0,
    Page = 1
}

/// <summary>组件清单项（注册表 / 设计器组件库用）</summary>
public class ComponentListItemDto
{
    [JsonProperty("componentName")] public string ComponentName { get; set; }
    [JsonProperty("label")] public string Label { get; set; }
    [JsonProperty("category")] public string Category { get; set; }
    [JsonProperty("icon")] public string Icon { get; set; }
    [JsonProperty("type")] public int ComponentType { get; set; }
    [JsonProperty("routePath")] public string RoutePath { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("sortOrder")] public int SortOrder { get; set; }
    /// <summary>组件定义加载地址（前端动态加载；为空=全局组件无需加载）</summary>
    [JsonProperty("loadUrl")] public string LoadUrl { get; set; }
    /// <summary>所属 UI 平台：Common / ElementUI / NutUI</summary>
    [JsonProperty("uiPlatform")] public string UiPlatform { get; set; }
    /// <summary>是否为组合组件（compositeComponents 表驱动）</summary>
    [JsonProperty("isComposite")] public bool IsComposite { get; set; }
    /// <summary>是否容器组件（设计器据此判定可否拖入子组件；唯一权威，前端不再按组件名猜）</summary>
    [JsonProperty("isContainer")] public bool IsContainer { get; set; }
    /// <summary>容器是否接受任意子组件（true=全接收；false=按 allowDrop 白名单）</summary>
    [JsonProperty("acceptAll")] public bool AcceptAll { get; set; }
    /// <summary>容器允许的子组件白名单（JSON 数组原文，如 ["DynTableColumn"]；"*" 表示全部）</summary>
    [JsonProperty("allowDrop")] public string? AllowDrop { get; set; }
}

/// <summary>组件完整定义（template + script + style）</summary>
public class ComponentDefineDto
{
    [JsonProperty("componentName")] public string ComponentName { get; set; }
    [JsonProperty("componentType")] public int ComponentType { get; set; }
    [JsonProperty("routePath")] public string RoutePath { get; set; }
    [JsonProperty("templateContent")] public string TemplateContent { get; set; }
    [JsonProperty("scriptContent")] public string ScriptContent { get; set; }
    [JsonProperty("styleContent")] public string StyleContent { get; set; }
}

/// <summary>Razor 组件 View 模型</summary>
public class ComponentViewModel
{
    public string ComponentName { get; set; }
}
