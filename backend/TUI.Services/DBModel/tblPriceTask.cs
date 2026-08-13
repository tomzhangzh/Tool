using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceTask
    {
        public long ID { get; set; }
        public long? BrandID { get; set; }
        public long? TaskID { get; set; }
        public DateTime? TaskCompletedUtc { get; set; }
    }
}
