using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceBrandConfigurationHistory
    {
        public int ID { get; set; }
        public int? PriceBrandConfigurationID { get; set; }
        public decimal? Price { get; set; }
        public decimal? Move { get; set; }
        public int? EffectiveTime { get; set; }
        public DateTime? LastUpdated { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? RecordDateUtc { get; set; }
        [SqlSugar.SugarColumn(IsIgnore = true)]
        public virtual PriceBrandConfiguration PriceBrandConfiguration { get; set; }
    }
}
