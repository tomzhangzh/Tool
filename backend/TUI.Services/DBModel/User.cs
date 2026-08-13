using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class User
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public int Status { get; set; }
        public int? AttachmentID { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
