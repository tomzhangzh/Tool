// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 授权菜单
/// </summary>
[SuppressSniffer, AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
public sealed class MaApiAttribute : Attribute
{
    /// <summary>
    /// 权限控制器
    /// </summary>
    /// <param name="area">权限类型代码(Admin、API、API2)</param>
    /// <param name="controller">控制器</param>
    /// <param name="controllerDescription">控制器描述符</param>
    public MaApiAttribute(string area = "API", string controller = "", string controllerDescription = "")
    {
        Area = area;
        ControllerDescription = controllerDescription;
        Controller = controller;
    }

    /// <summary>
    /// 此菜单保存的【实体】名称
    /// 【保存到那个表，就设置那个表的实体，默认 SysMenu】
    /// </summary>
    public Type EntityType { get; set; }

    /// <summary>
    /// 菜单分组（API,后台）
    /// </summary>
    public MenuAreaType MenuAreaType { get; set; } = MenuAreaType.API;

    /// <summary>
    /// 权限类型代码
    /// </summary>
    public string Area { get; set; } = "API";

    /// <summary>
    /// 控制器（默认为当前的 Controller,如果手动设置，则为手动设置）
    /// </summary>
    public string Controller { get; }

    /// <summary>
    /// Controller 名称
    /// </summary>
    public string ControllerDescription { get; set; }

    /// <summary>
    /// 父Id
    /// </summary>
    public string PCode { get; set; }

    /// <summary>
    /// 是否显示（默认：true）
    /// </summary>
    public bool IsShow { get; set; } = true;

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; } = MenuType.Controller;
}