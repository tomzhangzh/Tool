using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUI.Core.Entities;

namespace TUI.Core.Models
{
    public class ComponentSettingNode
    {
      
        public string value { get; set; }
        public string label { get; set; }
   
        public List<ComponentSettingNode> children { get; set; } = new List<ComponentSettingNode>();
    }
}
