using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class DICTSETTING
    {
        public int ID { get; set; }
        public string TABLENAME { get; set; }
        public string TYPE { get; set; }
        public string NAME { get; set; }
        public string VALUE { get; set; }
    }
}
