using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Services.Model
{
    public class GenerateCVModel
    {
        public GenerateCVModel()
        {
            this.TemplateClassName = "Client";
           
        }
        public string ClassName { get; set; }
        public string TemplateClassName { get; set; }
        public bool GenerateDetailSelect { get; set; }

    }
}
