using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceHistory
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public long? ProductID { get; set; }
        public long? SupplierID { get; set; }
        public long? TerminalID { get; set; }
        public long? CityID { get; set; }
        public decimal? Price { get; set; }
        public decimal? Move { get; set; }
        public int? EffectiveTime { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? RecordDateUtc { get; set; }
    }
}
