using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>桌面解决方案（1 个 Solution → 多个 Project，每个 Project 对应一个业务数据库）</summary>
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

/// <summary>桌面快捷方式</summary>
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

    /// <summary>所属解决方案 ID</summary>
    public int? SolutionId { get; set; }

    public int PosX { get; set; } = 0;
    public int PosY { get; set; } = 0;
    public int Width { get; set; } = 0;
    public int Height { get; set; } = 0;

    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
