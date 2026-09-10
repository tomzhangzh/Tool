using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 动作助手（actionhelper）注册表
/// —— dyn-click-{code} 动作的统一登记处：META + 脚本，可入库动态注册
/// </summary>
[SugarTable("DynActionHelper")]
public class DynActionHelper
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>显示名</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>动作名（dyn-click-{code}）</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    /// <summary>分类：flow / data / window / ui / system</summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>META：{label,doc,events,defaults,params[],i18n{}}</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? MetaJson { get; set; }

    /// <summary>JS 函数体 function(ctx){...}</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ScriptContent { get; set; }

    /// <summary>1=内置动作（只读目录），0=自定义（可编辑）</summary>
    public bool IsBuiltin { get; set; } = false;

    public bool IsEnabled { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
