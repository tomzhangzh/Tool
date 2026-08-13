using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblConfiguration
    {
        public long ID { get; set; }
        public string Parameter { get; set; }
        public string Value { get; set; }
    }
}
