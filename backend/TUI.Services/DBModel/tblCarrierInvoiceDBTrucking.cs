using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierInvoiceDBTrucking
    {
        public long ID { get; set; }
        public DateTime? ShipDate { get; set; }
        public string BOL { get; set; }
        public string Invoice { get; set; }
        public string MasterPro { get; set; }
        public string DetailPro { get; set; }
        public string BillTo { get; set; }
        public string StationCode { get; set; }
        public string StationName { get; set; }
        public string DestinationStreet { get; set; }
        public string DestinationCity { get; set; }
        public string Product { get; set; }
        public int? Gallons { get; set; }
        public decimal? FreightRate { get; set; }
        public string FreightPrice { get; set; }
        public string SurchargeFee { get; set; }
        public decimal? SurchargePercent { get; set; }
        public decimal? OtherFee { get; set; }
        public decimal? Tolls { get; set; }
        public decimal? SplitLoad { get; set; }
        public decimal? Total { get; set; }
        public string FileName { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
