using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierLoadDBTruckingLog
    {
        public long ID { get; set; }
        public string FileName { get; set; }
        public DateTime FileProcessedDate { get; set; }
        public int Records { get; set; }
    }
}
