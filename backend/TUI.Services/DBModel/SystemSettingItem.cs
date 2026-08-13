using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class SystemSettingItem
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Json { get; set; }
        public string TypeFullName { get; set; }
    }
}
