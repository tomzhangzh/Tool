using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// Platform 数据词典（平台级）。Business 的词典放入各自业务库（表名约定 Dict_xxx）。
/// </summary>
[SugarTable("DynDict")]
public class DynDict
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string DictType { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DictCode { get; set; }

    [SugarColumn(Length = 200, IsNullable = false)]
    public string DictText { get; set; } = string.Empty;

    [SugarColumn(Length = 200, IsNullable = false)]
    public string DictValue { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;
    public bool IsEnabled { get; set; } = true;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
