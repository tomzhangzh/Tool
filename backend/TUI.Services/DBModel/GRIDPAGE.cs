using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class GRIDPAGE
    {
        public GRIDPAGE()
        {
            GRIDITEM = new HashSet<GRIDITEM>();
        }

        public int ID { get; set; }
        public string NAME { get; set; }
        public string DESCRIPTION { get; set; }
        public string TEMPLATENAME { get; set; }
        public string CLASSNAME { get; set; }
        public string EXTENDEDPROPERTIES { get; set; }

        public virtual ICollection<GRIDITEM> GRIDITEM { get; set; }
    }
}
