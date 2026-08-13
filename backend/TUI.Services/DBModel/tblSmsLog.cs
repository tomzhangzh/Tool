using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblSmsLog
    {
        public long ID { get; set; }
        public byte? Status { get; set; }
        public byte? SmsType { get; set; }
        public string RecipientPhone { get; set; }
        public DateTime? RecordDate { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
        public string MessageSID { get; set; }
    }
}
