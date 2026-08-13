using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblGoogleDirectionsServiceLog
    {
        public long ID { get; set; }
        public string GasStationID { get; set; }
        public string ApiRawResponse { get; set; }
        public int DistanceMeters { get; set; }
        public decimal DistanceMiles { get; set; }
        public string OriginAddress { get; set; }
        public string DestinationAddress { get; set; }
        public string RouteDistance { get; set; }
        public string StartAddress { get; set; }
        public decimal StartAddressLatitude { get; set; }
        public decimal StartAddressLongitude { get; set; }
        public string EndAddress { get; set; }
        public decimal EndAddressLatitude { get; set; }
        public decimal EndAddressLongitude { get; set; }
        public byte IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public int TakenTime { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
