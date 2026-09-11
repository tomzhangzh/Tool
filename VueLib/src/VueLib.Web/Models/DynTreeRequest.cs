namespace VueLib.Web.Models;

/// <summary>
/// 树形数据请求（DynTree / DynTreeList 组件 → POST /DynRun/Data/Tree）
/// </summary>
public class DynTreeRequest
{
    public int ProjectId { get; set; }
    public string? Table { get; set; }
    public string? IdField { get; set; }
    public string? ParentIdField { get; set; }
    public string? NameField { get; set; }

    /// <summary>树节点 value 取值字段（如 OrgUnit 的 RefId，供右列 linkField 过滤）；空→Id</summary>
    public string? ValueField { get; set; }
    public long? RootId { get; set; }
}
