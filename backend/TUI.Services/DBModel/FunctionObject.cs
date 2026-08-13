using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class FunctionObject
    {
        public FunctionObject()
        {
            InverseParentFunctionObject = new HashSet<FunctionObject>();
            RolePermission = new HashSet<RolePermission>();
        }

        public int ID { get; set; }
        public string PermissonTag { get; set; }
        public string FunctionObjectName { get; set; }
        public string FunctionObjectName_En { get; set; }
        public string Description { get; set; }
        public int? ParentFunctionObjectId { get; set; }
        public bool FunctionAvailable { get; set; }
        public bool EditPermissionAvailable { get; set; }
        public bool DeletePermissionAvailable { get; set; }

        public virtual FunctionObject ParentFunctionObject { get; set; }
        public virtual ICollection<FunctionObject> InverseParentFunctionObject { get; set; }
        public virtual ICollection<RolePermission> RolePermission { get; set; }
    }
}
