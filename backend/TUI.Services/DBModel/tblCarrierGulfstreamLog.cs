using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierGulfstreamLog
    {
        public long ID { get; set; }
        public string FileName { get; set; }
        public byte FileProcessed { get; set; }
        public DateTime? FileProcessedDate { get; set; }
        public byte FileSent { get; set; }
        public DateTime? FileSentDate { get; set; }
        public int Records { get; set; }
    }
}
