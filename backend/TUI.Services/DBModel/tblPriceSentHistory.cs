using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceSentHistory
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public long? ProductID { get; set; }
        public decimal? Price { get; set; }
        public DateTime? LastSentUtc { get; set; }
        public DateTime? RecordDate { get; set; }
    }
}
