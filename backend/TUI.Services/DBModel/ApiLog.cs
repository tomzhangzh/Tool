using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class ApiLog
    {
        public int ID { get; set; }
        public string ApiType { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
