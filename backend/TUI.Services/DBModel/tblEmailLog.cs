using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblEmailLog
    {
        public long ID { get; set; }
        public long? EmailType { get; set; }
        public string EmailRecipient { get; set; }
        public byte? Status { get; set; }
        public DateTime? RecordDate { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
    }
}
