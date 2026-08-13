using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class ScheduleTaskLog
    {
        public int ID { get; set; }
        public int? ScheduleTaskID { get; set; }
        public string Name { get; set; }
        public DateTime? BeginDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string LogInfo { get; set; }
        public string Result { get; set; }

        public virtual ScheduleTask ScheduleTask { get; set; }
    }
}
