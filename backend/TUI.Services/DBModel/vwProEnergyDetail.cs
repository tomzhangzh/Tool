using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class vwProEnergyDetail
    {
        public string Carrier { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string InvoiceNumber { get; set; }
        public int? GallonsGross { get; set; }
        public int? GallonsNet { get; set; }
        public decimal? RatePerGallon { get; set; }
        public string Item { get; set; }
        public decimal? SplitFee { get; set; }
        public decimal? SundayDeliveryFee { get; set; }
        public decimal? TollFee { get; set; }
        public decimal? DeliveryFee { get; set; }
        public int? SurchargePercent { get; set; }
        public decimal? SurchargeFee { get; set; }
        public decimal OtherFee { get; set; }
        public decimal? TotalFee { get; set; }
        public decimal? TotalWithSurcharge { get; set; }
    }
}
