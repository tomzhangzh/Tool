using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Core.Models
{
    public class ValueText
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
    public class SelectComOptions
    {
        public string dictTableName { get; set; }
        public string dictType { get; set; }
        public string ajaxUrl { get; set; } = "/share/GetJsonBySql?sql=select Id as Value, name as Text from ComponentSetting";
        public string optionValues { get; set; }
        public bool withEmpty { get; set; }
        
        public string emptyText { get; set; } = "请选择";
    }
}
