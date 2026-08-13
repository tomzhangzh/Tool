using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblESPTest
    {
        public string Template { get; set; }
        public int? Invoice { get; set; }
        public DateTime? Date { get; set; }
        public string BillTo { get; set; }
        public string Driver { get; set; }
        public string QB_REP { get; set; }
        public string Driver_ { get; set; }
        public string Site { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int? Zip { get; set; }
        public string ShipToCityStateZip { get; set; }
        public string Truck { get; set; }
        public int? Trailer { get; set; }
        public int? Miles { get; set; }
        public string Terminal_ID { get; set; }
        public string Terminal_Name { get; set; }
        public string ShipVia { get; set; }
        public string PrintInv { get; set; }
        public int? PONum { get; set; }
        public int? BOL_s_ { get; set; }
        public string ItemCode { get; set; }
        public int? Qty { get; set; }
        public string Description { get; set; }
        public double? Price { get; set; }
        public double? Total { get; set; }
    }
}
