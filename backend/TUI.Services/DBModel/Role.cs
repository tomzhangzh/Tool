using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class Role
    {
        public Role()
        {
            RolePermission = new HashSet<RolePermission>();
            UsersInRole = new HashSet<UsersInRole>();
        }

        public int RoleId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ICollection<RolePermission> RolePermission { get; set; }
        public virtual ICollection<UsersInRole> UsersInRole { get; set; }
    }
}
