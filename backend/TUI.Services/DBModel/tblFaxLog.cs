using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblFaxLog
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public string FaxNumber { get; set; }
        public string MessageID { get; set; }
        public string Status { get; set; }
        public DateTime? SentTimestamp { get; set; }
        public DateTime? LastStatusChecked { get; set; }
        public DateTime? LastStstusCheckedUtc { get; set; }
    }
}
