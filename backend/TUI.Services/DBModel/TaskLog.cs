using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class TaskLog
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? Status { get; set; }
        public int? RetryTime { get; set; }
        public bool Deleted { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? RunDate { get; set; }
        public string LogInfo { get; set; }
    }
}
