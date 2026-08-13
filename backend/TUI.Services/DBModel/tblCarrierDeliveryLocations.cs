using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierDeliveryLocations
    {
        public int ID { get; set; }
        public string Carrier { get; set; }
        public string StartLocation { get; set; }
        public string StartAddress { get; set; }
        public string DeliveryLocation { get; set; }
        public string FullAddress { get; set; }
        public string GoogleFullAddress { get; set; }
        public string GoogleStreet { get; set; }
        public string GoogleCity { get; set; }
        public string GoogleState { get; set; }
        public int? GoogleZip { get; set; }
        public double? Distance1 { get; set; }
        public double? Distance2 { get; set; }
        public double? Distance3 { get; set; }
        public double? AverageDistance { get; set; }
    }
}
