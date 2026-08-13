using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class SendLog
    {
        public int ID { get; set; }
        public int? PriceBrandID { get; set; }
        public string Type { get; set; }
        public DateTime? RunTime { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string AttachmentFile { get; set; }
        public string AttachmentHtml { get; set; }
        public string Info { get; set; }
        public string Result { get; set; }
    }
}
