using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class AuditData
    {
        public int ID { get; set; }
        public string DBName { get; set; }
        public string TableName { get; set; }
        public string Keys { get; set; }
        public int? UserID { get; set; }
        public string LoginName { get; set; }
        public DateTime ExecuteTime { get; set; }
        public DateTime ExecuteTimeUtc { get; set; }
        public string Server { get; set; }
        public int? AuditType { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
    }
}
