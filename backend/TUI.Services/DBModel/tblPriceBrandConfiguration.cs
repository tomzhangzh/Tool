using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceBrandConfiguration
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public long? SupplierID { get; set; }
        public long? TerminalID { get; set; }
        public long? ProductID { get; set; }
        public long? CityID { get; set; }
        public long? AlternativeSupplierID { get; set; }
        public long? AlternativeTerminalID { get; set; }
        public long? AlternativeProductID { get; set; }
        public string ReportVariableName { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public decimal? LastUpdatedPrice { get; set; }
        public decimal? LastSentPrice { get; set; }
        public byte? SourceApiType { get; set; }
        public int? MustEffectiveTime { get; set; }
    }
}
