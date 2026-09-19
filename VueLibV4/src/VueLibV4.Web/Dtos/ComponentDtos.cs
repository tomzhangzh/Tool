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
    /// <summary>组件定义加载地址（前端 vueLoadCom 拉取：Razor 优先 / DB 回退）</summary>
    [JsonProperty("loadUrl")] public string LoadUrl { get; set; }
    /// <summary>组件版本（如 ElementPlus 3.x / 1.0.0）</summary>
    [JsonProperty("version")] public string Version { get; set; }
    /// <summary>是否为组合组件（compositeComponents 表驱动）</summary>
    [JsonProperty("isComposite")] public bool IsComposite { get; set; }
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
