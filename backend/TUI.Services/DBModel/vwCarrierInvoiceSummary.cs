using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class vwCarrierInvoiceSummary
    {
        public string Carrier { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string InvoiceNumber { get; set; }
        public double? GallonsGross { get; set; }
        public double? GallonsNet { get; set; }
        public double EffectiveRatePerGallon { get; set; }
        public string Item { get; set; }
        public double? SplitFee { get; set; }
        public double? SurchargePercent { get; set; }
        public double? SurchargeFee { get; set; }
        public decimal? SundayDeliveryFee { get; set; }
        public double? TollFee { get; set; }
        public double? DeliveryFee { get; set; }
        public double? OtherFee { get; set; }
        public double? TotalFee { get; set; }
        public double? TotalWithSurcharge { get; set; }
    }
}
