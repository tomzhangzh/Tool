using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceEmailRecipientLog
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public string RecipientEmail { get; set; }
        public byte? EmailStatus { get; set; }
    }
}
