// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// Dtree 扩展
/// </summary>
public static class DtreeExtensions
{
    /// <summary>
    ///  集合转换为 Dtree 树
    /// </summary>
    /// <param name="DtreeEntities"></param>
    /// <param name="pid"></param>
    /// <returns></returns>
    public static Dtree GetDtree(this IEnumerable<DtreeEntity> DtreeEntities, string pid)
    {
        var dtree = new Dtree
        {
            data = GeDtreeItems(DtreeEntities, pid)
        };
        return dtree;
    }

    private static List<DtreeItem> GeDtreeItems(IEnumerable<DtreeEntity> entities, string pid)
    {
        var dtreeItems = new List<DtreeItem>();
        foreach (var dataconfig in entities.Where(o => (o.pid ?? "") == pid).OrderBy(o => o.sort))
        {
            var dtreeitem = new DtreeItem()
            {
                id = dataconfig.id,
                title = dataconfig.name.Trim(),
                parentId = pid
            };

            if (entities.Any(o => o.pid == dataconfig.id))
            {
                dtreeitem.last = false;
                dtreeitem.children = GeDtreeItems(entities, dataconfig.id);
            }
            else
            {
                dtreeitem.last = true;
            }
            dtreeItems.Add(dtreeitem);
        }
        return dtreeItems;
    }

    public static Dtree GetDtreeCheck(this IEnumerable<DtreeEntity> DtreeEntities, string pid, IEnumerable<DtreeEntity> selectEntitites)
    {
        var dtree = new Dtree();
        var dtreeitems = GeDtreeCheckItems(DtreeEntities, pid, selectEntitites);
        HandleChekedSelectState(dtreeitems);
        dtree.data = dtreeitems;
        return dtree;
    }

    private static List<DtreeCheckItem> GeDtreeCheckItems(IEnumerable<DtreeEntity> entities, string pid, IEnumerable<DtreeEntity> selectEntitites)
    {
        var dtreeItems = new List<DtreeCheckItem>();
        foreach (var dtreeentity in entities.Where(o => (o.pid ?? "") == pid).OrderBy(o => o.sort))
        {
            var dtreeitem = new DtreeCheckItem()
            {
                id = dtreeentity.id,
                title = dtreeentity.name.Trim(),
                parentId = pid
            };
            if (selectEntitites.Any(o => o.id == dtreeentity.id))
            {
                dtreeitem.checkArr = new Dictionary<string, string>()
                {
                   { "type","0"},
                   { "checked",$"1" }
                };
            }

            if (entities.Any(o => o.pid == dtreeentity.id))
            {
                dtreeitem.last = false;
                dtreeitem.children = GeDtreeCheckItems(entities, dtreeentity.id, selectEntitites);
            }
            else
            {
                dtreeitem.last = true;
            }
            dtreeItems.Add(dtreeitem);
        }
        return dtreeItems;
    }

    /// <summary>
    /// 处理上级cheked选中状态
    /// </summary>
    /// <param name="dtreeCheckItems"></param>
    /// <returns></returns>
    private static bool HandleChekedSelectState(List<DtreeCheckItem> dtreeCheckItems)
    {
        //是否有选中的权限
        var IsCheckedPermission = false;
        foreach (var dtreeCheckItem in dtreeCheckItems)
        {
            if (dtreeCheckItem.children.Any())
            {
                //还有下级

                IsCheckedPermission = HandleChekedSelectState(dtreeCheckItem.children);
                if (IsCheckedPermission)
                {
                    dtreeCheckItem.checkArr = new Dictionary<string, string>()
                    {
                       { "type","0"},
                       { "checked",$"1" }
                    };
                }
            }
            else
            {
                //没有下级了、到权限了
                //if (!dtreeCheckItem.id.StartsWith("m_"))
                //{
                //    if (dtreeCheckItem.checkArr.ContainsKey("checked"))
                //    {
                //        if (dtreeCheckItem.checkArr["checked"] == "1")
                //        {
                //            IsCheckedPermission = true;
                //        }
                //    }
                //}

                if ((dtreeCheckItem.children == null || dtreeCheckItem.children.Count <= 0) && dtreeCheckItem.checkArr.ContainsKey("checked"))
                {
                    if (dtreeCheckItem.checkArr["checked"] == "1")
                    {
                        IsCheckedPermission = true;
                    }
                }

                //if (IsCheckedPermission) break;//只要有一个权限选中即为true
            }
        }

        return IsCheckedPermission;
    }
}