using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class vw_tblESPTest_BaseView
    {
        public string Carrier { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string InvoiceNumber { get; set; }
        public int? GallonsGross { get; set; }
        public decimal GallonsNet { get; set; }
        public double? RatePerGallon { get; set; }
        public string Item { get; set; }
        public double? SplitFee { get; set; }
        public double? SurchargePercent { get; set; }
        public double? SurchargeFee { get; set; }
        public decimal SundayDeliveryFee { get; set; }
        public double? TollFee { get; set; }
        public double? DeliveryFee { get; set; }
        public double? OtherFee { get; set; }
    }
}
