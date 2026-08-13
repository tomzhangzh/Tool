using TUI.Services.DBModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace TUI.Services.Model
{
    public class GridPageExtendedBase
    {
        public GridPageExtendedBase()
        {

        }
        public static GridPageExtendedBase GetExtendProperty(GRIDPAGE item)
        {
            if (item.TEMPLATENAME == "GridWithEditBtn")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new GridWithEditBtnInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<GridWithEditBtnInfo>();
                }
            }
            else if (item.TEMPLATENAME == "GridWithEditAndDeleteBtn")
            {
                if (string.IsNullOrEmpty(item.EXTENDEDPROPERTIES) == true)
                {
                    return new GridWithEditAndDeleteBtnInfo();
                }
                else
                {
                    return item.EXTENDEDPROPERTIES.Deserialize<GridWithEditAndDeleteBtnInfo>();
                }
            }

            return new GridPageExtendedBase();
        }
        public static void SetExtendPrperty<T>(GRIDPAGE item, T extendInfo) where T : GridPageExtendedBase
        {
            item.EXTENDEDPROPERTIES = extendInfo.ToJSON();
        }
    }
    public class GridWithEditBtnInfo : GridPageExtendedBase
    {

        public string EditUrl { get; set; }

    }
    public class GridWithEditAndDeleteBtnInfo : GridPageExtendedBase
    {
        public string EditUrl { get; set; }
        public string DeleteUrl { get; set; }

    }
}

