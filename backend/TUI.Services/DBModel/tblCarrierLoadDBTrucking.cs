using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblCarrierLoadDBTrucking
    {
        public long ID { get; set; }
        public string OrderLine { get; set; }
        public string TruckID { get; set; }
        public string DriverID { get; set; }
        public string BusinessUnit { get; set; }
        public string SalesOrderNumber { get; set; }
        public string CustomerNo { get; set; }
        public string CustomerLocNo { get; set; }
        public string CustomerAddrDescription { get; set; }
        public string CustomerAddrLine1 { get; set; }
        public string CustomerAddrLine2 { get; set; }
        public string CustomerAddrCity { get; set; }
        public string CustomerAddrState { get; set; }
        public string CustomerAddrZip { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string DeliveryTimeStart { get; set; }
        public string DeliveryTimeEnd { get; set; }
        public string SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string TerminalID { get; set; }
        public string TerminalName { get; set; }
        public string ProductCode { get; set; }
        public string FuelGradeCode { get; set; }
        public string ProductDescription { get; set; }
        public decimal? QuantityOrdered { get; set; }
        public string TankSerialNumber { get; set; }
        public string TankDescription { get; set; }
        public string TankSize { get; set; }
        public string IsUndergroundTankIND { get; set; }
        public string NeedPumpIND { get; set; }
        public string DispatchNotes { get; set; }
        public string CustomerPO { get; set; }
        public string CarrierOrderID { get; set; }
        public string FTPSCAC { get; set; }
        public string RackUpdateCode { get; set; }
        public string RackUpdateField { get; set; }
        public string RackUpdateValue { get; set; }
        public decimal? NetGallons { get; set; }
        public decimal? GrossGallons { get; set; }
        public string LoadingBOL { get; set; }
        public DateTime? AtRack { get; set; }
        public DateTime? LeftRack { get; set; }
        public DateTime? LiftedStartTime { get; set; }
        public DateTime? LiftedEndTime { get; set; }
        public DateTime? AtLocationTime { get; set; }
        public DateTime? LeftLocationTime { get; set; }
        public string CustomField1 { get; set; }
        public DateTime? CustomField2 { get; set; }
        public string CustomField3 { get; set; }
        public string CustomField4 { get; set; }
        public string CustomField5 { get; set; }
        public string TimeZoneGMTOffset { get; set; }
        public string LoadingNotes { get; set; }
        public string FileName { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
