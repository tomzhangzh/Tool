using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblSmsTemplate
    {
        public long ID { get; set; }
        public string TemplateName { get; set; }
        public string SmsBody { get; set; }
    }
}
