using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblOrderFuelDetails
    {
        public long ID { get; set; }
        public long? OrderID { get; set; }
        public byte? FuelType { get; set; }
        public int? FuelQuantity { get; set; }
        public int? TankSize { get; set; }
        public string DeliveryLocation { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string DeliveryTimeFrom { get; set; }
        public string DeliveryTimeTo { get; set; }
    }
}
