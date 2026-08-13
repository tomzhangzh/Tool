using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierInvoiceProEnergy
    {
        public long ID { get; set; }
        public string Carrier { get; set; }
        public long? InvoiceID { get; set; }
        public string StationName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public DateTime? Date { get; set; }
        public long? DriverNumber { get; set; }
        public string BOL { get; set; }
        public string Terminal { get; set; }
        public int? GallonsGross { get; set; }
        public int? GallonsNet { get; set; }
        public decimal? Rate { get; set; }
        public string Type { get; set; }
        public decimal? SplitFee { get; set; }
        public int? NumberOfSplits { get; set; }
        public string MinimumGallons { get; set; }
        public decimal? SundayCharge { get; set; }
        public decimal? Tolls { get; set; }
        public decimal? HoursDumurrage { get; set; }
        public decimal? Misc { get; set; }
        public int? SurchargePercent { get; set; }
        public decimal? SurchargeAmount { get; set; }
        public decimal? Total { get; set; }
        public decimal? TotalAndSurcharge { get; set; }
    }
}
