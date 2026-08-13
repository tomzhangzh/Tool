using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblGasType
    {
        public long ID { get; set; }
        public long? Code { get; set; }
        public string Name { get; set; }
        public string TaxScheduleReceipts { get; set; }
        public string TaxScheduleDisbursement { get; set; }
    }
}
