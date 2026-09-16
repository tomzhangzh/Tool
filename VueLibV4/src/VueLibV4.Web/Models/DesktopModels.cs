#nullable enable
using SqlSugar;

namespace VueLibV4.Web.Models;

/// <summary>
/// 桌面解决方案：一个解决方案可包含多个 DynProject（不同数据库）与多个快捷方式。
/// 主键 Id int 自增；Code 为业务唯一键（nvarchar(64) UNIQUE），仅用于 API 引用，不做外键。
/// </summary>
[SugarTable("DesktopSolution")]
public class DesktopSolution
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ExtJson { get; set; }
}

/// <summary>
/// 桌面快捷方式：属于某解决方案；可指向网页（page）/ 动作（action）/ 外部 URL（url）。
/// 外键一律使用 int Id（SolutionId → DesktopSolution.Id；ActionHelperId → DynActionHelper.Id）。
/// </summary>
[SugarTable("DesktopShortcut")]
public class DesktopShortcut
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    /// <summary>所属解决方案 Id（DesktopSolution.Id）</summary>
    public int? SolutionId { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Url { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    /// <summary>打开方式：page / action / url</summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? TargetType { get; set; } = "page";

    /// <summary>动作 Id（DynActionHelper.Id，TargetType=action 时使用）</summary>
    public int? ActionHelperId { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ExtJson { get; set; }
}
