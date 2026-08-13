using TUI.Services.DBModel;
using TUI.Services.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.WebPortal.Areas.Tools.Models
{
    public class DETAILITEMViewModel
    {
        public DETAILITEMViewModel()
        { }
        public DETAILITEMViewModel(DETAILITEM item)
        {
            this.DETAILITEM = item;
            this.initExtendPropery();
        }
        public DETAILITEM DETAILITEM { get; set; }
        public ItemExtendedBase ExtendPropery { get; set; }
        public void initExtendPropery()
        {
            this.ExtendPropery = ItemExtendedBase.GetExtendProperty(this.DETAILITEM);
        }
    }
}
