using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>桌面解决方案（开始菜单分组）</summary>
[SugarTable("DesktopSolutions")]
public class DesktopSolution
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    /// <summary>分组图标（emoji）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(255)", IsNullable = true)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;
}

/// <summary>桌面快捷方式（图标 + 打开方式 + 窗口状态记忆）</summary>
[SugarTable("DesktopShortcuts")]
public class DesktopShortcut
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Url { get; set; }

    /// <summary>打开方式：iframe / newtab / modal / fullscreen</summary>
    public string OpenType { get; set; } = "iframe";

    /// <summary>所属解决方案（null=未分组）</summary>
    [SugarColumn(IsNullable = true)]
    public int? SolutionId { get; set; }

    /// <summary>图标（emoji）</summary>
    [SugarColumn(ColumnDataType = "nvarchar(255)", IsNullable = true)]
    public string? Icon { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>窗口位置与大小记忆</summary>
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
