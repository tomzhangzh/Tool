using Furion.ConfigurableOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Core
{
    public class WebsiteOptions : IConfigurableOptions
    {
        public int SessionTimeOutMinutes { get; set; } = 45;
        public int KeepSessionLiveMinutes { get; set; } = 3;
        public int CookieTimeOutMinutes { get; set; } = 40;
    }
}
