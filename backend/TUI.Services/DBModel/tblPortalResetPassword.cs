using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPortalResetPassword
    {
        public long ID { get; set; }
        public string Email { get; set; }
        public string HashCode { get; set; }
        public DateTime? RequestCreated { get; set; }
        public DateTime? EmailSent { get; set; }
        public DateTime? PasswordChanged { get; set; }
        public string ClientIP { get; set; }
    }
}
