using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceFaxRecipient
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public string RecipientFax { get; set; }
        public string RecipientName { get; set; }
    }
}
