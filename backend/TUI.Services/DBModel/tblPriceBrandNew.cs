using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceBrandNew
    {
        public long ID { get; set; }
        public string BrandName { get; set; }
        public string EmailSubject { get; set; }
        public string EmailFrom { get; set; }
        public string EmailReplyTo { get; set; }
        public string PriceFileUrl { get; set; }
        public string AttachmentFile { get; set; }
    }
}
