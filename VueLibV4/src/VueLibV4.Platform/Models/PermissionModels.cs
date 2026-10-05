#nullable enable
using SqlSugar;

namespace VueLibV4.Platform.Models;

/// <summary>
/// 角色表
/// </summary>
[SugarTable("SysRole")]
public class SysRole
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>0=平台级角色；>0=业务项目角色</summary>
    public int ProjectId { get; set; } = 0;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 平台用户表（平台库）
/// </summary>
[SugarTable("SysUser")]
public class SysUser
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>登录名（唯一）</summary>
    [SugarColumn(Length = 100)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>显示名</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? DisplayName { get; set; }

    /// <summary>密码哈希（开发期先明文，后面改哈希）</summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Password { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 用户-角色映射
/// </summary>
[SugarTable("SysUserRole")]
public class SysUserRole
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string UserName { get; set; } = string.Empty;

    public int RoleId { get; set; }
}

/// <summary>
/// 资源目录（真树形）：所有可授权的资源节点。
/// ParentId 串成树；HasRead/HasEdit/HasDelete 标记该资源支持哪几个权限位。
/// ProjectId: 0=平台级；>0=业务项目级。
/// </summary>
[SugarTable("SysResource")]
public class SysResource
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>父节点 Id（0=根节点）</summary>
    public int ParentId { get; set; }

    /// <summary>权限编码，如 Customer.List.Export</summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Key { get; set; } = string.Empty;

    /// <summary>中文名，如"客户导出"</summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>类型：Module / Menu / Page / Operation / Field</summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    public string Type { get; set; } = "Operation";

    /// <summary>0=平台级；>0=业务项目 Id</summary>
    public int ProjectId { get; set; } = 0;

    /// <summary>是否支持 Read 权限位</summary>
    public bool HasRead { get; set; } = true;

    /// <summary>是否支持 Edit 权限位</summary>
    public bool HasEdit { get; set; } = true;

    /// <summary>是否支持 Delete 权限位</summary>
    public bool HasDelete { get; set; } = false;

    /// <summary>绑定的表名（逗号分隔），动态数据接口按表名反查资源节点做权限校验</summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    public string? TableNames { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 角色-资源授权：哪个角色对哪个资源有 Read/Edit/Delete。
/// 三个 bool 独立，不是字符串枚举。
/// </summary>
[SugarTable("SysResourcePermission")]
public class SysResourcePermission
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int ResourceId { get; set; }

    public bool Read { get; set; } = false;
    public bool Edit { get; set; } = false;
    [SugarColumn(ColumnName = "CanDelete")]
    public bool Delete { get; set; } = false;
}

/// <summary>资源类型常量</summary>
public static class ResourceNodeType
{
    public const string Module = "Module";
    public const string Menu = "Menu";
    public const string Page = "Page";
    public const string Operation = "Operation";
    public const string Field = "Field";
}
