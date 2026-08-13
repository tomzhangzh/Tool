using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class vwCarrierUniqueDeliveryLocations
    {
        public string Carrier { get; set; }
        public string DeliveryLocation { get; set; }
        public double? AverageDistance { get; set; }
        public string FullAddress { get; set; }
    }
}
