using TUI.Services.DBModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.WebPortal.Areas.Logs.Models
{
    public class LogEventFilter : LogEvents
    {
        public List<DateTime?> Date_Range
        {
            get; set;

        } = new List<DateTime?> { DateTime.Today.AddDays(-7), DateTime.Today };
    }
}
