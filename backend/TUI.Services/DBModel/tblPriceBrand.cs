using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceBrand
    {
        public long ID { get; set; }
        public string BrandName { get; set; }
        public string BrandCode { get; set; }
        public string EmailSubject { get; set; }
        public string EmailFrom { get; set; }
        public string EmailReplyTo { get; set; }
        public string PriceFileUrl { get; set; }
        public string AttachmentFile { get; set; }
        public byte HasAttachment { get; set; }
        public byte HasFaxNotification { get; set; }
        public byte? IsActive { get; set; }
    }
}
