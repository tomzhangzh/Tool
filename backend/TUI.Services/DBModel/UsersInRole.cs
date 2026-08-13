using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class UsersInRole
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        public virtual Role Role { get; set; }
    }
}
