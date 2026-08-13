using TUI.Services.DBModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.TaskLib
{
   
    public interface IJobService:IScopeDependency
    {
        string Execute(ScheduleTask sTask, ScheduleTaskLog Log, string parameter);
    }
}
