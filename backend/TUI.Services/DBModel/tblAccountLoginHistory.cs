using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblAccountLoginHistory
    {
        public int LogID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public byte IsSuccess { get; set; }
        public string ClientIP { get; set; }
        public DateTime RecordDate { get; set; }
    }
}
