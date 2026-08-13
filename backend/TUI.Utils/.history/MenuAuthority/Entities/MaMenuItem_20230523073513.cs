// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

public class MaMenuItem
{
    /// <summary>
    /// 此菜单保存的【实体】名称
    /// 【保存到那个表，就设置那个表的实体，默认 SysMenu】
    /// </summary>
    public Type EntityType { get; set; }

    /// <summary>
    /// 菜单分组（API,后台）
    /// </summary>
    public MenuAreaType MenuAreaType { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 唯一表示代码(默认为:area+下划线+ controller+下划线+页面名字 例如：htadmin_user_index)
    /// </summary>
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 父Id
    /// 上级目录
    /// </summary>
    public string PCode { get; set; }

    /// <summary>
    ///  区域
    /// </summary>
    public string Area { get; set; }

    /// <summary>
    /// URL 地址（例如：htadmin/user/index）
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 控制器
    /// </summary>
    public string Controller { get; set; }

    /// <summary>
    /// 是否废止，接口废弃后，设置为true
    /// </summary>
    public bool IsDisuse { get; set; }

    /// <summary>
    /// 是否系统菜单,系统菜单禁止删除
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// 是否显示菜单（默认：true）
    /// </summary>
    public bool IsShow { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }

    /// <summary>
    /// 创建日期
    /// </summary>
    public string CreateDate { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; }

    /// <summary>
    /// 是否控制器目录
    /// 注意：控制器目录不生成为后台菜单层次机构中去，不然多出来一层，要把这个换成菜单
    /// </summary>
    public bool IsControllerCatalogue { get; set; }

    /// <summary>
    /// 方法/Action名称,多个用英文逗号隔开
    /// </summary>
    public string Actions { get; set; }

    /// <summary>
    /// 顺序(按从小到大排序)
    /// </summary>
    public int Sort { get; set; }

    ///// <summary>
    ///// 权限功能
    ///// </summary>
    //public List<AuthorityPermissionItem> Permissions { get; set; } = new List<AuthorityPermissionItem>();
}