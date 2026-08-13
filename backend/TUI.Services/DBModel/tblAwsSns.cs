using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblAwsSns
    {
        public long ID { get; set; }
        public string Notification { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
