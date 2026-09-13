using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>
/// 动作助手注册表 —— 所有 actionhelper 必须入库（包括脚本），前端启动时拉取并动态注册。
/// dyn-click-{Code} 属性触发，ScriptContent 是 function(ctx){...} 的 JS 函数体。
/// </summary>
[SugarTable("DynActionHelper")]
public class DynActionHelper
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>显示名</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>动作编码（dyn-click-{Code}），全局唯一</summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    /// <summary>分类：flow / data / window / ui / system</summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>描述</summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>参数 Schema JSON：[{key,label,type,required,default,options[]}]</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ParamsSchema { get; set; }

    /// <summary>JS 函数体 function(ctx){...}</summary>
    [SugarColumn(ColumnDataType = "nvarchar(max)", IsNullable = true)]
    public string? ScriptContent { get; set; }

    /// <summary>1=内置（只读目录），0=自定义（可编辑）</summary>
    public bool IsBuiltin { get; set; } = false;

    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
