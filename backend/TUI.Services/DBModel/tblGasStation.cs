using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblGasStation
    {
        public long ID { get; set; }
        public long? DEP { get; set; }
        public string Name { get; set; }
        public long? FEIN { get; set; }
    }
}
