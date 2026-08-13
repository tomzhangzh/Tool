using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TUI.Services.DBModel
{
    public partial class tblPriceHistory
    {
        [NotMapped]
        public DateTime? Ext_EST_LastUpdated
        {
            get
            {
                if (this.LastUpdated == null) return null;
                return this.LastUpdated.Value.UtcDateTime.GetEasternTime();
            }
        }
        //[NotMapped]
        //public DateTime? Ext_EST_ReportDate
        //{
        //    get
        //    {
        //        return this.Ext_EST_LastUpdated?.Date;
        //    }
        //}
    }
    public partial class PriceBrandConfigurationHistory
    {
        [SqlSugar.SugarColumn(IsIgnore = true)]
        [NotMapped]
        public string DisplayPrice { get; set; }
    }
}
