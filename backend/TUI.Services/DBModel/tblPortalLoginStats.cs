using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPortalLoginStats
    {
        public long ID { get; set; }
        public string LoginName { get; set; }
        public byte? IsSuccess { get; set; }
        public string ClientIP { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
