// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 授权菜单，可适用于控制器或方法
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class MaMenuAttribute : Attribute
{
    /// <summary>
    /// 构造函数，主要是MVC下使用
    /// </summary>
    /// <param name="name">页面/功能名称</param>
    public MaMenuAttribute(string name)
    {
        Name = name;

        //this.Args = args?.ToList(new char[] { ',' }) ?? new List<string>();
        ////去除 开始的 问号 ？
        //for (int i = 0; i < this.Args.Count; i++)
        //{
        //    this.Args[i] = this.Args[i].TrimStart('?');
        //}
    }

    /// <summary>
    /// 构造函数，主要给API情况下使用
    /// </summary>
    /// <param name="name">页面/功能名称</param>
    /// <param name="code">菜单代码,只填写菜单唯一代码就可以（组成规则={areaName}_{controllerName}_{menu.Code}）</param>
    /// <param name="url">url地址，前端的路由</param>
    /// <param name="sort">排序，默认为0</param>
    /// <param name="icon">图标</param>
    public MaMenuAttribute(string name, string code, string url, int sort = 0, string icon = "iconfont icon-dian")
    {
        this.Name = name;
        this.Code = code;
        this.Url = url;
        this.Icon = icon;
        this.Sort = sort;
    }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string Name { get; }

    ///// <summary>
    ///// 参数
    ///// </summary>
    //public List<string> Args = new List<string>();

    /// <summary>
    /// 唯一表示代码(默认为:area+下划线+ controller+下划线+页面名字 例如：htadmin_user_index)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// URL 地址（例如：htadmin/user/index）
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// URL 地址参数(例如：htadmin/user/index?参数,只是参数，不包含url部分)
    /// </summary>
    public string UrlPar { get; set; }

    /// <summary>
    /// 是否废止（默认：false）
    /// </summary>
    public bool IsDisuse { get; set; }

    /// <summary>
    /// 是否系统菜单,系统菜单禁止删除（默认：true）
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
    /// 创建日期(为空，不更新数据库，创建默认为新增入库时间)
    /// </summary>
    public string CreateDate { get; set; }

    /// <summary>
    /// 图标（默认图标="layui-icon-component"）
    /// </summary>
    public string Icon { get; set; } = "layui-icon layui-icon-face-smile-fine";

    /// <summary>
    /// 顺序(按从小到大排序)
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 权限功能
    /// </summary>
    public List<MaPermissionAttribute> Permissions { get; set; }
}