using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class RolePermission
    {
        public int ID { get; set; }
        public int FunctionObjectId { get; set; }
        public bool Enable { get; set; }
        public bool Editable { get; set; }
        public bool Deleteable { get; set; }
        public int? RoleId { get; set; }

        public virtual FunctionObject FunctionObject { get; set; }
        public virtual Role Role { get; set; }
    }
}
