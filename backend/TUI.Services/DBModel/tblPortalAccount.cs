using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPortalAccount
    {
        public long ID { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public byte? IsActive { get; set; }
        public int? AccessMask { get; set; }
        public DateTime? CreatedDate { get; set; }
        public long? CreatedBy { get; set; }
        public byte? IsDeleted { get; set; }
        public DateTime? DeletedDate { get; set; }
        public long? DeletedBy { get; set; }
    }
}
