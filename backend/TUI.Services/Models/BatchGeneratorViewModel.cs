using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.Models
{
    public class BatchGeneratorViewModel
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<long> BrandList { get; set; }
        //public bool WithEmailBody { get; set; }
        public bool ZipByDate { get; set; } = false;
        public bool OnlyZipFile { get; set; } = true;
        public string DateRange { get; set; }
    }
}
