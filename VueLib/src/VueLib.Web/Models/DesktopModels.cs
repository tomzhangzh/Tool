using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 桌面快捷方式
/// </summary>
[SugarTable("DesktopShortcut")]
public class DesktopShortcut
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(Length = 500, IsNullable = false)]
    public string Url { get; set; } = string.Empty;

    /// <summary>打开方式: iframe / newtab / window</summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? OpenType { get; set; } = "iframe";

    /// <summary>所属解决方案ID</summary>
    public int? SolutionId { get; set; }

    /// <summary>桌面位置: x, y（像素）或网格位置</summary>
    public int PosX { get; set; } = 0;
    public int PosY { get; set; } = 0;

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 解决方案（快捷方式分组）
/// </summary>
[SugarTable("DesktopSolution")]
public class DesktopSolution
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
