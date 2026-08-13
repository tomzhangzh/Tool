using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierInvoiceESP
    {
        public int ID { get; set; }
        public string Template { get; set; }
        public int? Invoice { get; set; }
        public DateTime? Date { get; set; }
        public string BillTo { get; set; }
        public string Driver { get; set; }
        public string QB_REP { get; set; }
        public string DriverID { get; set; }
        public string Site { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public int? Zip { get; set; }
        public string ShipToCityStateZip { get; set; }
        public string Truck { get; set; }
        public string Trailer { get; set; }
        public int? Miles { get; set; }
        public string TerminalID { get; set; }
        public string TerminalName { get; set; }
        public string ShipVia { get; set; }
        public string PrintInv { get; set; }
        public string PONum { get; set; }
        public string BOL_s { get; set; }
        public string ItemCode { get; set; }
        public int? Qty { get; set; }
        public string Description { get; set; }
        public double? Price { get; set; }
        public double? Total { get; set; }
        public string FileName { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
