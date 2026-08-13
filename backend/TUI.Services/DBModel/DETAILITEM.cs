using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class DETAILITEM
    {
        public int ID { get; set; }
        public int DETAILPAGEID { get; set; }
        public int SORTID { get; set; }
        public string COLUMNNAME { get; set; }
        public string LABEL { get; set; }
        public string COLUMNTYPE { get; set; }
        public bool AS1COLUMN { get; set; }
        public bool? ISSHOW { get; set; }
        public bool ISREQUIRED { get; set; }
        public string VALIDATETYPE { get; set; }
        public string HTMLATTRIBUTES { get; set; }
        public string EXTENDEDPROPERTIES { get; set; }
        public bool Disabled { get; set; }

        public virtual DETAILPAGE DETAILPAGE { get; set; }
    }
}
