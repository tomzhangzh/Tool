using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblEmailTemplate
    {
        public long ID { get; set; }
        public string TemplateType { get; set; }
        public string EmailSubject { get; set; }
        public string TemplateName { get; set; }
    }
}
