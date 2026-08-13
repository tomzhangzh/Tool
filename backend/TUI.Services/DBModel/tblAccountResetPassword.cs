using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblAccountResetPassword
    {
        public int LogID { get; set; }
        public string Email { get; set; }
        public string Guid { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? EmailSentDate { get; set; }
        public DateTime? PasswordChangedDate { get; set; }
        public string ClientIP { get; set; }
    }
}
