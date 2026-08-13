using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblAccount
    {
        public tblAccount()
        {
            tblAccountAccessList = new HashSet<tblAccountAccessList>();
        }

        public int AccountID { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public int? AccountRole { get; set; }
        public byte IsActive { get; set; }
        public byte IsDeleted { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }

        public virtual ICollection<tblAccountAccessList> tblAccountAccessList { get; set; }
    }
}
