using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPortalApiLog
    {
        public long ID { get; set; }
        public string Action { get; set; }
        public long BrandID { get; set; }
        public long EntryID { get; set; }
        public string Recipient { get; set; }
        public long AccountID { get; set; }
        public string IPAddress { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
