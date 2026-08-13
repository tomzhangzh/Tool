using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class ScheduleTask
    {
        public ScheduleTask()
        {
            ScheduleTaskLog = new HashSet<ScheduleTaskLog>();
        }

        public int ID { get; set; }
        public string GroupName { get; set; }
        public string TaskName { get; set; }
        public string Interval { get; set; }
        public string Parameter { get; set; }
        public string Description { get; set; }
        public DateTime? LastRunTime { get; set; }
        public int? Status { get; set; }
        public string ClassName { get; set; }
        public string Result { get; set; }

        public virtual ICollection<ScheduleTaskLog> ScheduleTaskLog { get; set; }
    }
}
