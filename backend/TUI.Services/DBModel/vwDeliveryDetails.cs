using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class vwDeliveryDetails
    {
        public string Carrier { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string InvoiceNumber { get; set; }
        public string TerminalID { get; set; }
        public string BOL { get; set; }
        public string DeliveryLocation { get; set; }
        public string FullAddress { get; set; }
    }
}
