using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceSmsRecipient
    {
        public int ID { get; set; }
        public string PhoneNumber { get; set; }
        public string RecipientName { get; set; }
    }
}
