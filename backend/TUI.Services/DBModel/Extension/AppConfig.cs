using System;
using System.Collections.Generic;
using System.Text;

namespace Sunshine.Services.DBModel
{
    public class EnableConfig
    {
        public bool PopulateDatePricesLastPrice { get; set; } = true;
        public bool WarehousesMapping { get; set; } = true;
        public bool DisablefCitgoClearDiesel { get; set; } = false;
        public string CancelUpdateList { get; set; } = "fChevronClearDiesel,fChevronDyedDiesel";
    }
    public sealed class AppConfig
    {
       
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static AppConfig Instance { get; private set; }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUser { get; set; }
        public string SmtpUserPassword { get; set; }
        public bool SmtpUseSSL { get; set; }
        public string SmtpMailFrom { get; set; }
        public string NewOrderEmail { get; set; }
        public string NewOrderEmailInternal { get; set; }
        public string PortalUrl { get; set; }
        public string FuelPriceKey { get; set; }
        public string TwilioAccountSID { get; set; }
        public string TwilioAccountToken { get; set; }
        public string TwilioOutgoingNumber { get; set; }
        public string MainConnectionString { get; set; }
        public Dictionary<string, string> WarehousesMapping
        {
            get;set;
        }
        public Dictionary<string, int> ForceEffectiveTime
        {
            get;set;
        }
        public EnableConfig EnableConfig
        {
            get;set;
        }

    }
}
