using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class Attachment
    {
        public int ID { get; set; }
        public string FileName { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int? CreateUserId { get; set; }
        public string ObjType { get; set; }
        public int? ObjID { get; set; }
        public string TempIdForNew { get; set; }
        public string FileFormat { get; set; }
        public string FileType { get; set; }
    }
}
