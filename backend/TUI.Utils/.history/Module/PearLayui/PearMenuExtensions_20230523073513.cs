// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// Pear 菜单扩展
/// </summary>
public static class PearMenuExtensions
{
    /// <summary>
    /// 获取Pear 后台菜单
    /// </summary>
    /// <param name="menus"></param>
    /// <param name="pid">父级ID</param>
    /// <returns></returns>
    public static List<PearMenuItemOut> GetMenus(this List<PearMenuItem> menus, string pid = "")
    {
        List<PearMenuItemOut> menuItems = new List<PearMenuItemOut>();
        foreach (var menu in menus.Where(o => o.PCode != null && o.PCode.ToLower() == pid.ToLower()).OrderBy(o => o.Sort))
        {
            //var type = menus.Any(o => o.ParentId == menu.Id) ? 0 : 1;
            if ((menu.MenuType == MenuType.目录 && menu.IsControllerCatalogue == false) || menu.MenuType == MenuType.Controller && menu.IsControllerCatalogue == false) //目录
            {
                //MVC 目录，
                PearMenuItemOut menuitem = new PearMenuItemOut()
                {
                    id = menu.Code,
                    title = menu.Name,
                    icon = menu.Icon,
                    type = (int)menu.MenuType,
                };
                if (!string.IsNullOrWhiteSpace(menu.Url)) menuitem.href = $"/{menu.Url.TrimStart('/')}";

                menuitem.children = GetMenus(menus, menu.Code);
                menuItems.Add(menuitem);
            }
            else if ((menu.MenuType == MenuType.目录 && menu.IsControllerCatalogue == true) || menu.MenuType == MenuType.Controller && menu.IsControllerCatalogue == true) //目录
            {
                //处理MVC 有Controller 目录的情况，防止生成多余的空菜单目录
                foreach (var menuitem in menus.Where(o => o.PCode != null && o.PCode.ToLower() == menu.Code.ToLower() && o.MenuType == MenuType.菜单).OrderBy(o => o.Sort))
                {
                    AddMenu(menuitem);
                }
            }
            else if (menu.MenuType == MenuType.菜单)
            {
                //菜单
                AddMenu(menu);
            }
            void AddMenu(PearMenuItem menu)
            {
                PearMenuItemOut menuitem = new PearMenuItemOut()
                {
                    id = menu.Code,
                    title = menu.Name,
                    icon = menu.Icon,
                    type = (int)menu.MenuType,
                    openType = "_iframe"
                };
                if (!string.IsNullOrWhiteSpace(menu.Url)) menuitem.href = $"/{menu.Url.TrimStart('/')}";
                menuItems.Add(menuitem);
            }

            //}
        }

        return menuItems;
    }
}

public class PearMenuItem
{
    /// <summary>
    /// 节点类型(菜单、工具栏)
    /// </summary>
    public MenuType MenuType { get; set; }

    /// <summary>
    /// 菜单ID，全局唯一标识
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 父Id
    /// </summary>
    public string PCode { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string Icon { get; set; }

    /// <summary>
    /// 顺序(按从小到大排序)
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// Url
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 是否控制器目录
    /// 注意：控制器目录不生成为后台菜单层次机构中去，不然多出来一层，要把这个换成菜单
    /// </summary>
    public bool IsControllerCatalogue { get; set; }
}

/// <summary>
/// 菜单类型
/// </summary>
public enum MenuType
{
    /// <summary>
    /// 目录
    /// </summary>
    目录 = 0,

    /// <summary>
    /// 菜单
    /// </summary>
    菜单 = 1,

    /// <summary>
    /// API 接口
    /// </summary>
    API = 2,

    功能方法 = 3,
    /// <summary>
    /// 控制器
    /// </summary>
    Controller = 4,
}