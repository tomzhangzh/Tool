using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceBrandFormula
    {
        public int ID { get; set; }
        public int? PriceBrandID { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string GroupName { get; set; }
        public string Formula { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public string BasePrice { get; set; }
        public decimal? FederalStateRate { get; set; }
        public decimal? CountyRate { get; set; }
        public decimal? Frt { get; set; }
        public decimal? FrtSurch { get; set; }
        public decimal? Markup { get; set; }
        public decimal? Other { get; set; }

        public virtual PriceBrand PriceBrand { get; set; }
    }
}
