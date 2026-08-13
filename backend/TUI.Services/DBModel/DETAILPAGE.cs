using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class DETAILPAGE
    {
        public DETAILPAGE()
        {
            DETAILITEM = new HashSet<DETAILITEM>();
        }

        public int ID { get; set; }
        public string NAME { get; set; }
        public string DESCRIPTION { get; set; }
        public int PAGECOLUMNCOUNT { get; set; }
        public string CLASSNAME { get; set; }

        public virtual ICollection<DETAILITEM> DETAILITEM { get; set; }
    }
}
