using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblAccountAccessList
    {
        public int EntryID { get; set; }
        public int? AccountID { get; set; }
        public int? AccessEntryID { get; set; }

        public virtual tblAccount Account { get; set; }
    }
}
