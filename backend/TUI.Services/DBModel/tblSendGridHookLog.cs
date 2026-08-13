using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblSendGridHookLog
    {
        public long ID { get; set; }
        public string Email { get; set; }
        public string Event { get; set; }
        public string Response { get; set; }
        public string IP { get; set; }
        public int? Timestamp { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
