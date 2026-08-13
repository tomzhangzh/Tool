using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblBouncedEmail
    {
        public long ID { get; set; }
        public string Email { get; set; }
        public string Action { get; set; }
        public string Status { get; set; }
        public string DiagnosticCode { get; set; }
        public string Source { get; set; }
        public string Subject { get; set; }
        public DateTime? RecordCreated { get; set; }
    }
}
