using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 数据字典（常用组件下拉/单选数据源，替代 SunnySystem DICTSETTING）
/// </summary>
[SugarTable("DynDict")]
public class DynDict
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>字典类型（Gender / Status / YesNo ...）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string DictType { get; set; } = string.Empty;

    /// <summary>编码（可空）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DictCode { get; set; }

    /// <summary>显示文本</summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string DictText { get; set; } = string.Empty;

    /// <summary>值</summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string DictValue { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
