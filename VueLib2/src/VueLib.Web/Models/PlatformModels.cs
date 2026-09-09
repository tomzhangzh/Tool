using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>低代码组件（平台库）</summary>
[SugarTable("Components")]
public class LcComponent
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    /// <summary>组件名（唯一逻辑名，如 ElementInput）</summary>
    [SugarColumn(Length = 64)]
    public string Name { get; set; } = "";
    [SugarColumn(Length = 128)]
    public string DisplayName { get; set; } = "";
    [SugarColumn(Length = 64)]
    public string Category { get; set; } = "Common";
    [SugarColumn(Length = 64)]
    public string Icon { get; set; } = "";
    /// <summary>版本号（每次重新注册递增）</summary>
    public int Version { get; set; } = 1;
    /// <summary>是否当前生效版本</summary>
    public bool IsCurrent { get; set; } = true;
    /// <summary>来源：razor=磁盘Razor视图快照 composite=组合组件 demo=手工演示</summary>
    [SugarColumn(Length = 16)]
    public string SourceType { get; set; } = "razor";
    /// <summary>Razor 视图路径（SourceType=razor 时有效）</summary>
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? RazorPath { get; set; }
    /// <summary>组件渲染快照文本（template + script[tag=comconfig]），版本锁定的依据</summary>
    [SugarColumn(ColumnDataType = "ntext", IsNullable = true)]
    public string? ComponentText { get; set; }
    /// <summary>组合组件内部配置树 JSON（SourceType=composite 时有效）</summary>
    [SugarColumn(ColumnDataType = "ntext", IsNullable = true)]
    public string? CompositeConfigJson { get; set; }
    /// <summary>属性面板 Schema JSON（供设计器渲染属性表单）</summary>
    [SugarColumn(ColumnDataType = "ntext", IsNullable = true)]
    public string? ConfigSchemaJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

/// <summary>低代码页面（平台库）</summary>
[SugarTable("Pages")]
[SugarIndex("uk_page_code", nameof(Code), OrderByType.Asc, true)]
public class LcPage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    /// <summary>页面编码（路由 /#/page/{code}）</summary>
    [SugarColumn(Length = 64)]
    public string Code { get; set; } = "";
    [SugarColumn(Length = 128)]
    public string Name { get; set; } = "";
    /// <summary>页面组件配置树 JSON</summary>
    [SugarColumn(ColumnDataType = "ntext", IsNullable = true)]
    public string ConfigJson { get; set; } = "{}";
    /// <summary>默认模型 JSON</summary>
    [SugarColumn(ColumnDataType = "ntext", IsNullable = true)]
    public string DefaultModelJson { get; set; } = "{}";
    /// <summary>页面锁定组件版本表 JSON：{"组件名":"版本号"}</summary>
    [SugarColumn(ColumnDataType = "ntext", IsNullable = true)]
    public string ComponentVersionsJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
