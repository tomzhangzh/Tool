// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 授权功能
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MaPermissionAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">页面/功能名称</param>
    public MaPermissionAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">页面/功能名称</param>
    /// <param name="actions">action</param>
    public MaPermissionAttribute(string name, string actions)
    {
        Actions = actions;
        Name = name;
    }

    ///// <summary>
    ///// 菜单代码(不包含 Area_Controller)
    ///// </summary>
    //public string MenuCode { get; set; }

    /// <summary>
    /// 权限类型代码(默认为:area+下划线+ controller+下划线+action 例如：htadmin_user_add)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Action,多个用英文逗号隔开
    /// </summary>
    public string Actions { get; }

    /// <summary>
    /// 是否废止（默认：false，废弃后设置为true）
    /// </summary>
    public bool IsDisuse { get; set; }

    /// <summary>
    /// 是否系统菜单,系统菜单禁止删除（默认：true）
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 创建日期
    /// </summary>
    public string CreateDate { get; set; }

    /// <summary>
    /// 是否显示（默认：true）
    /// </summary>
    public bool IsShow { get; set; } = true;
}