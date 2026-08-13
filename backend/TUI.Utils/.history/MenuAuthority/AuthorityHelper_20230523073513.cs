// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.DynamicApiController;

using StackExchange.Profiling.Internal;
using System.Drawing;

namespace Abc.Utils;

/// <summary>
/// 授权Helper
/// </summary>
public class AuthorityHelper
{
    /// <summary>
    /// 获取菜单权限
    /// </summary>
    /// <returns></returns>
    public static List<MaMenuItem> GetMenuPermissions()
    {
        var types = App.Assemblies.SelectMany(o => o.GetTypes().AsEnumerable()
                                           .Where(type => typeof(ControllerBase).IsAssignableFrom(type)
                                           || type.GetInterfaces().Contains(typeof(IDynamicApiController)))
                                           );
        //return GetMenus(types, "Admin");

        var AllMenus = new List<MaMenuItem>();

        //遍历所有controller
        foreach (var controller in types)
        {
            //验证Controller是否贴了 AuthorityControllerAttribute
            if (controller.GetCustomAttributes().All(o => o is not MaMvcAttribute && o is not MaApiAttribute)) continue;

            var areaName = "HtAdmin";//默认为HtAdmin
            var controllerName = controller.Name.RemoveLastStr("Controller");// 移除Controller控制器名称
            var controllerDescription = "";//Controller 描述

            #region 添加菜单目录（控制器）


            #endregion 添加菜单目录（控制器）

            var menuController = new MaMenuItem();

            #region 添加菜单目录（控制器）【MVC】
            {
                if (controller.GetCustomAttributes().Any(o => o is MaMvcAttribute))
                {
                    var controllerAttribute = controller.GetCustomAttributes().First(o => o is MaMvcAttribute) as MaMvcAttribute;

                    menuController = controllerAttribute.Adapt<MaMenuItem>();
                    menuController.IsControllerCatalogue = true; // 是否控制器目录,后台菜单目录结构不考虑这层

                    if (!string.IsNullOrWhiteSpace(controllerAttribute.Controller))
                    {
                        controllerName = controllerAttribute.Controller;
                    }
                    if (!string.IsNullOrWhiteSpace(controllerAttribute.Area))
                    {
                        areaName = controllerAttribute.Area;
                    }
                    if (!string.IsNullOrWhiteSpace(controllerAttribute.ControllerDescription))
                    {
                        controllerDescription = controllerAttribute.ControllerDescription;
                    }
                    else
                    {
                        controllerDescription = controllerAttribute.Controller;
                    }

                    menuController.EntityType = controllerAttribute.EntityType;
                    menuController.MenuAreaType = controllerAttribute.MenuAreaType;
                    menuController.PCode = controllerAttribute.PCode.Trim();
                    menuController.Name = controllerDescription;
                    menuController.Area = areaName;
                    menuController.Code = $"{areaName}_{controllerName}_Controller";
                    menuController.Controller = controllerName;
                    menuController.Icon = "layui-icon-link";
                    menuController.Url = $"{areaName}/{controllerName}";

                    AllMenus.Add(menuController);
                }
            }
            #endregion

            #region 【API】
            {
                if (controller.GetCustomAttributes().Any(o => o is MaApiAttribute))
                {
                    var controllerAttribute = controller.GetCustomAttributes().First(o => o is MaApiAttribute) as MaApiAttribute;

                    menuController = controllerAttribute.Adapt<MaMenuItem>();
                    menuController.IsControllerCatalogue = true; // 是否控制器目录,后台菜单目录结构不考虑这层

                    if (!string.IsNullOrWhiteSpace(controllerAttribute.Controller))
                    {
                        controllerName = controllerAttribute.Controller;
                    }
                    if (!string.IsNullOrWhiteSpace(controllerAttribute.Area))
                    {
                        areaName = controllerAttribute.Area;
                    }
                    if (!string.IsNullOrWhiteSpace(controllerAttribute.ControllerDescription))
                    {
                        controllerDescription = controllerAttribute.ControllerDescription;
                    }
                    else
                    {
                        controllerDescription = controllerAttribute.Controller;
                    }

                    menuController.EntityType = controllerAttribute.EntityType;
                    menuController.MenuAreaType = controllerAttribute.MenuAreaType;
                    menuController.PCode = controllerAttribute.PCode.Trim();
                    menuController.Name = controllerDescription;
                    menuController.Area = areaName;
                    menuController.Code = $"{areaName}_{controllerName}_Controller";
                    menuController.Controller = controllerName;
                    menuController.Icon = "layui-icon-link";
                    menuController.Url = $"{areaName}/{controllerName}";

                    AllMenus.Add(menuController);
                }
            }
            #endregion




            #region 菜单

            #region 【菜单】 获取 控制器 上的 菜单信息

            {
                var menuAttributes = controller.GetCustomAttributes().Where(o => o is MaMenuAttribute);
                foreach (MaMenuAttribute menuAttribute in menuAttributes)
                {
                    AddMenuItem(menuAttribute);
                }
            }

            #endregion 【菜单】 获取 控制器 上的 菜单信息

            //var menus = new List<AuthorityMenuItem>();

            #region 【菜单】 获取Method上的 菜单信息

            foreach (var methodInfo in controller.GetMethods())
            {
                //if (methodInfo.GetCustomAttributes().Any(o => o is AuthorityMenuAttribute))
                //{
                //    var menuAttribute = methodInfo.GetCustomAttributes().First(o => o is AuthorityMenuAttribute) as AuthorityMenuAttribute;

                foreach (MaMenuAttribute menuAttribute in methodInfo.GetCustomAttributes().Where(o => o is MaMenuAttribute))
                {
                    AddMenuItem(menuAttribute, methodInfo.Name);
                    //var menu = menuAttribute.Adapt<AuthorityMenuItem>();
                    //menu.EntityType = controllerAttribute.EntityType;
                    //menu.MenuAreaType = controllerAttribute.MenuAreaType;
                    //menu.MenuType = MenuType.菜单;
                    //menu.Area = areaName;
                    //menu.Controller = controllerName;
                    //menu.PCode = menuController.Code;
                    //menu.Icon = "layui-icon-link";
                    ////菜单唯一ID ：area+下划线+controller+下划线+action 名
                    //if (string.IsNullOrWhiteSpace(menuAttribute.Code))
                    //{
                    //    menu.Code = $"{areaName}_{controllerName}_{methodInfo.Name}";
                    //}
                    //else
                    //{
                    //    menu.Code = $"{areaName}_{controllerName}_{menu.Code.Trim()}";
                    //}
                    ////默认url：Ht
                    //if (string.IsNullOrWhiteSpace(menuAttribute.Url))
                    //{
                    //    menu.Url = $"{areaName}/{controllerName}/{methodInfo.Name}";
                    //    //组装 url 参数
                    //    if (!string.IsNullOrWhiteSpace(menuAttribute.UrlPar))
                    //    {
                    //        menu.Url = $"{menu.Url}?{menuAttribute.UrlPar.TrimStart('?')}";
                    //    }
                    //}

                    //AllMenus.Add(menu);
                }
            }

            #endregion 【菜单】 获取Method上的 菜单信息

            void AddMenuItem(MaMenuAttribute menuAttribute, string methodName = "")
            {
                var menu = menuAttribute.Adapt<MaMenuItem>();
                menu.EntityType = menuController.EntityType;
                menu.MenuAreaType = menuController.MenuAreaType;
                menu.MenuType = MenuType.菜单;
                menu.Area = areaName;
                menu.Controller = controllerName;
                menu.PCode = menuController.Code;
                menu.Icon = "layui-icon-link";
                //菜单唯一ID ：area+下划线+controller+下划线+action 名

                if (string.IsNullOrWhiteSpace(menuAttribute.Code) && !string.IsNullOrWhiteSpace(methodName))
                {
                    menu.Code = $"{areaName}_{controllerName}_{methodName}";
                }
                else
                {
                    if (menu.Code.StartsWith($"{areaName}_{controllerName}"))
                    {
                        menu.Code = $"{menu.Code.Trim()}";
                    }
                    else
                    {
                        menu.Code = $"{areaName}_{controllerName}_{menu.Code.Trim()}";
                    }
                }
                //默认url：Ht
                if (string.IsNullOrWhiteSpace(menuAttribute.Url))
                {
                    menu.Url = $"{areaName}/{controllerName}/{methodName}";
                    //组装 url 参数
                    if (!string.IsNullOrWhiteSpace(menuAttribute.UrlPar))
                    {
                        menu.Url = $"{menu.Url}?{menuAttribute.UrlPar.TrimStart('?')}";
                    }
                }

                AllMenus.Add(menu);
            }

            #endregion 菜单

            #region 【功能方法】 action

            foreach (var methodInfo in controller.GetMethods())
            {
                //获取贴的 权限特性
                foreach (MaPermissionAttribute actionAttribute in methodInfo.GetCustomAttributes().Where(o => o is MaPermissionAttribute))
                {
                    var permissionItem = actionAttribute.Adapt<MaMenuItem>();
                    permissionItem.EntityType = menuController.EntityType;
                    permissionItem.MenuAreaType = menuController.MenuAreaType;
                    //if (menuController.MenuAreaType == MenuAreaType.API)
                    //{
                    //    permissionItem.MenuType = MenuType.API;
                    //}
                    //else
                    //{
                    permissionItem.MenuType = MenuType.功能方法;


                    permissionItem.Area = areaName;
                    permissionItem.Controller = controllerName;
                    permissionItem.PCode = menuController.Code;
                    permissionItem.Icon = "layui-icon-link";
                    if (string.IsNullOrWhiteSpace(actionAttribute.Code))
                    {
                        //页面_Action_功能
                        permissionItem.Code = $"Action_{methodInfo.Name.Trim()}";
                    }
                    if (string.IsNullOrWhiteSpace(actionAttribute.Actions))
                    {
                        permissionItem.Actions = methodInfo.Name;
                    }
                    // permissions.Add(permissionItem);

                    permissionItem.Code = $"{areaName}_{controllerName}_{permissionItem.Code}";

                    AllMenus.Add(permissionItem);
                }
            }

            #endregion 【功能方法】 action
        }
        return AllMenus;
    }

    /// <summary>
    /// 获取API菜单
    /// </summary>
    /// <returns></returns>
    [Obsolete("启用，过时")]
    public static List<MaMenuItem> GetApiPermissions()
    {
        var types = App.Assemblies.SelectMany(a => a.GetTypes()
                       .Where(t => t.GetInterfaces().Contains(typeof(IDynamicApiController))));
        //return GetItems(types, "Api");
        var menus = new List<MaMenuItem>();

        foreach (var controller in types)
        {
            //验证Controller是否贴了 AuthorityControllerAttribute
            if (controller.GetCustomAttributes().All(o => o is not MaMvcAttribute)) continue;

            var areaName = "Api";//默认为HtAdmin
            var controllerName = controller.Name.RemoveLastStr("Controller").RemoveLastStr("Service");// 移除Controller控制器名称
            var controllerDescription = "";//Controller 描述

            var controllerAttribute = controller.GetCustomAttributes().First(o => o is MaMvcAttribute) as MaMvcAttribute;
            if (!string.IsNullOrWhiteSpace(controllerAttribute.Controller))
            {
                controllerName = controllerAttribute.Controller;
            }
            if (!string.IsNullOrWhiteSpace(controllerAttribute.Area))
            {
                areaName = controllerAttribute.Area;
            }
            if (!string.IsNullOrWhiteSpace(controllerAttribute.ControllerDescription))
            {
                controllerDescription = controllerAttribute.ControllerDescription;
            }
            else
            {
                controllerDescription = controllerAttribute.Controller;
            }

            var menu = new MaMenuItem
            {
                MenuAreaType = MenuAreaType.API,
                MenuType = MenuType.目录,
                Area = areaName,
                PCode = areaName,
                Name = controllerDescription,
                Code = $"{areaName}_{controllerName}",
                Controller = controllerName,
                Icon = "layui-icon-link",
                Url = $"{areaName}/{controllerName}"
            };
            menus.Add(menu);
            //if (menu.Permissions == null) menu.Permissions = new List<AuthorityPermissionItem>();

            foreach (var methodInfo in controller.GetMethods())
            {
                //获取贴的 权限特性
                foreach (MaPermissionAttribute actionAttribute in methodInfo.GetCustomAttributes().Where(o => o is MaPermissionAttribute))
                {
                    var permissionItem = actionAttribute.Adapt<MaMenuItem>();
                    permissionItem.MenuType = MenuType.目录;
                    permissionItem.MenuType = MenuType.API;
                    permissionItem.Area = areaName;
                    permissionItem.PCode = menu.Code;
                    permissionItem.Controller = controllerName;
                    if (string.IsNullOrWhiteSpace(actionAttribute.Code))
                    {
                        permissionItem.Code = $"{areaName}_{controllerName}_{methodInfo.Name}";
                    }
                    if (string.IsNullOrWhiteSpace(actionAttribute.Actions))
                    {
                        permissionItem.Actions = methodInfo.Name;
                    }
                    permissionItem.Url = $"{menu.Url}/{permissionItem.Actions}";

                    //menu.Permissions.Add(permissionItem);
                    menus.Add(menu);
                }
            }
        }
        return menus;
    }
}