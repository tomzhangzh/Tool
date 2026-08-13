using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierInvoiceProEnergyLog
    {
        public long ID { get; set; }
        public string FileName { get; set; }
        public DateTime? FileDate { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public int Records { get; set; }
    }
}
