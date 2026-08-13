using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblOrder
    {
        public long ID { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }
        public string OrderNumber { get; set; }
        public string PONumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public string TankSize { get; set; }
        public string RegularFuel { get; set; }
        public string NonEthanolFuel { get; set; }
        public string ClearDiesel { get; set; }
        public string DyeDiesel { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string DeliveryTimeFrom { get; set; }
        public string DeliveryTimeTo { get; set; }
        public string ContactName { get; set; }
        public string DeliveryLocation { get; set; }
        public string BillTo { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string Comments { get; set; }
        public DateTime? RecordDate { get; set; }
        public string ClientIP { get; set; }
        public DateTime? EmailSent { get; set; }
    }
}
