using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class GRIDITEM
    {
        public int ID { get; set; }
        public int GRIDPAGEID { get; set; }
        public int SORTID { get; set; }
        public string COLUMNNAME { get; set; }
        public string LABEL { get; set; }
        public string SORTNAME { get; set; }
        public string FORMAT { get; set; }
        public bool? ISSHOW { get; set; }
        public string WIDTH { get; set; }
        public string HTMLATTRIBUTES { get; set; }
        public string EXTENDEDPROPERTIES { get; set; }

        public virtual GRIDPAGE GRIDPAGE { get; set; }
    }
}
