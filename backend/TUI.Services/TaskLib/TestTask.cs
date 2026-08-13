using TUI.Services.DBModel;
using TUI.Services.Manager;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.TaskLib
{
    public class TestTask : IJobService
    {
        public string Execute(ScheduleTask sTask, ScheduleTaskLog Log, string parameter)
        {
            throw new NotImplementedException();
        }
    }
}
