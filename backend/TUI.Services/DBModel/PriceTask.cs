using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceTask
    {
        public int ID { get; set; }
        public int? PriceBrandID { get; set; }
        public int? TaskID { get; set; }
        public DateTime? TaskCompletedUtc { get; set; }

        public virtual PriceBrand PriceBrand { get; set; }
    }
}
