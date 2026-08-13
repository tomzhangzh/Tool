using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierGulfstream
    {
        public long ID { get; set; }
        public DateTime? ShipDate { get; set; }
        public string BOL { get; set; }
        public string Invoice { get; set; }
        public string Station { get; set; }
        public string DestinationStreet { get; set; }
        public string DestinationCity { get; set; }
        public string ZipCode { get; set; }
        public string Driver { get; set; }
        public string Terminal { get; set; }
        public string ProductType { get; set; }
        public double? GrossGallons { get; set; }
        public double? NetGallons { get; set; }
        public double? FreightRate { get; set; }
        public double? FreightDollar { get; set; }
        public double? SurchargePercent { get; set; }
        public double? SurchargeDollar { get; set; }
        public double? SplitFee { get; set; }
        public double? NumberSplits { get; set; }
        public double? Tolls { get; set; }
        public double? Total { get; set; }
        public string FileName { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
