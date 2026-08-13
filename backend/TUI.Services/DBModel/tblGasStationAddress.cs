using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblGasStationAddress
    {
        public string StationID { get; set; }
        public string StationName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public string Manager { get; set; }
        public string CarWashBrand { get; set; }
        public string Carrier { get; set; }
    }
}
