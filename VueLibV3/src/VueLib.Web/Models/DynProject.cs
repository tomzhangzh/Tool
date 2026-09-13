using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 动态工程 —— 必须挂在 DesktopSolution 下（1 Solution → 多 Project）。
/// 每个 Project 对应一个业务数据库连接串，运行时按此连接串连接业务库并动态渲染页面。
/// </summary>
[SugarTable("DynProject")]
public class DynProject
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>所属解决方案 ID</summary>
    public int SolutionId { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DisplayName { get; set; }

    /// <summary>业务数据库连接串（运行时连接它）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ConnectionString { get; set; }

    /// <summary>业务数据库名（展示用）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DatabaseName { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Icon { get; set; } = "📦";

    [SugarColumn(Length = 20, IsNullable = true)]
    public string? Type { get; set; } = "Web";

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
