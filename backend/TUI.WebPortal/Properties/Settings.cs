using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.WebPortal.Properties
{
    public partial class Settings
    {

        private static Settings defaultInstance;
        public static Settings Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");
                    var configuration = builder.Build();
                    defaultInstance = new Settings();
                    configuration.GetSection("WebsiteSettings").Bind(defaultInstance);
                }
                return defaultInstance;
            }
        }
        public int SessionTimeOutMinutes { get; set; } = 45;
        public int KeepSessionLiveMinutes { get; set; } = 3;
        public int CookieTimeOutMinutes { get; set; } = 40;

    }
}
