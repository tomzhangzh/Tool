using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceBrandConfiguration
    {
        public PriceBrandConfiguration()
        {
            PriceBrandConfigurationHistory = new HashSet<PriceBrandConfigurationHistory>();
        }

        public int ID { get; set; }
        public int? PriceBrandID { get; set; }
        public int? SupplierID { get; set; }
        public int? TerminalID { get; set; }
        public int? ProductID { get; set; }
        public int? CityID { get; set; }
        public bool IsActive { get; set; }
        public string ReportVariableName { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public decimal? LastUpdatedPrice { get; set; }
        public decimal? LastSentPrice { get; set; }
        public int? SourceApiType { get; set; }
        public int? MustEffectiveTime { get; set; }
        public int? AlternativeSupplierID { get; set; }
        public int? AlternativeTerminalID { get; set; }
        public int? AlternativeProductID { get; set; }
        public DateTime? StartDate { get; set; }
        public bool? HasSms { get; set; }

        public virtual PriceBrand PriceBrand { get; set; }
        public virtual ICollection<PriceBrandConfigurationHistory> PriceBrandConfigurationHistory { get; set; }
    }
}
