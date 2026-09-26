#nullable enable
using SqlSugar;

namespace VueLibV4.Platform.Models;

/// <summary>
/// 系统菜单（树形）：桌面快捷方式的数据源（IsAddToDesktop=true 时生成桌面图标），
/// 父菜单（有子菜单）显示为文件夹，点击在旁边浮动展开子菜单。
/// TargetType：FullScreen=全屏打开 / Iframe=桌面内嵌 iframe / NewWindow=新窗口。
/// PermissionCode 预留，后续对接权限模块。
/// </summary>
[SugarTable("SysMenu")]
public class SysMenu
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>父级菜单 Id（null=根菜单）</summary>
    public int? ParentId { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>业务编码（可选，API 引用用）</summary>
    [SugarColumn(Length = 64, IsNullable = true)]
    public string? Code { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Url { get; set; }

    /// <summary>打开方式：FullScreen=全屏 / Windows=桌面窗口(可配宽高) / Iframe=桌面窗口(默认尺寸) / NewWindow=新浏览器窗口</summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? TargetType { get; set; } = "Iframe";

    /// <summary>桌面窗口打开宽度（TargetType=Windows 时生效；支持像素 "800" 或百分比 "50%"）</summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? Width { get; set; }

    /// <summary>桌面窗口打开高度（TargetType=Windows 时生效；支持像素 "640" 或百分比 "60%"）</summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    public string? Height { get; set; }

    /// <summary>是否添加到桌面（桌面由此表生成快捷方式）</summary>
    public bool IsAddToDesktop { get; set; } = true;

    /// <summary>是否作为桌面根图标显示（false 时不出现在桌面根区，子菜单仍在文件夹/开始菜单中）</summary>
    public bool IsAddToDesktopRoot { get; set; } = true;

    /// <summary>是否添加到开始菜单</summary>
    public bool IsAddToStartMenu { get; set; } = true;

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    /// <summary>权限编码（预留，对接权限模块）</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? PermissionCode { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>菜单打开方式常量</summary>
public static class MenuTargetType
{
    public const string FullScreen = "FullScreen";
    public const string Windows = "Windows";
    public const string Iframe = "Iframe";
    public const string NewWindow = "NewWindow";
}
