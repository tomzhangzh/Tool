using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tbl_utest
    {
        public long? BrandID { get; set; }
        public long? ProductID { get; set; }
        public string ReportVariableName { get; set; }
        public decimal? Price { get; set; }
        public int? EffectiveTime { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
