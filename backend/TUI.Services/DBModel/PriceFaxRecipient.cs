using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceFaxRecipient
    {
        public int ID { get; set; }
        public int PriceBrandID { get; set; }
        public string RecipientFax { get; set; }
        public string RecipientName { get; set; }

        public virtual PriceBrand PriceBrand { get; set; }
    }
}
