using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace TUI.Services.DBModel
{
    public enum PriceTaskEnum
    {
        PrepareData = 1,
        EmailNotification = 2,
        FaxNotification = 3,
        SmsNotification = 4,
        //PriceUpdateNotification = 5
    }
    public partial class PriceTask
    {
        [NotMapped]
        public PriceTaskEnum PriceTaskEnum
        {
            get
            {
                return (PriceTaskEnum)this.TaskID.Value;
            }
            set
            {
                this.TaskID = value.GetHashCode();
            }
        }
    }
    
}
