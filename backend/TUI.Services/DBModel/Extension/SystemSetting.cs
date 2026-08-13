using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace TUI.Services.DBModel
{
    //      Fuel Buyer Price Automation Web Service
    //      https://app.swaggerhub.com/apis-docs/DTN-FuelSuite/fuel-buyer_price_automation_web_service/1.0#/
    public class DTN_API_Setting
    {
        public string apiUrl { get; set; } = "https://api.dtn.com/fuelsuite/fuelbuyer/prices";
        public string userApi { get; set; } = "dtn@TUIgasoline.com";
        public string serviceKey { get; set; } = "TUILIVE";
        public string apiKey { get; set; } = "IFkKjOEGTmDETwPB2F6AlcEoYK8TiDbA";
    }
    public class Opis_API_Setting
    {
        public string apiUrl { get; set; } = "https://rackapi.opisnet.com/api/v1";
        public string apiUser { get; set; } = "Iovana@webhosting.net";
        public string apiPass { get; set; } = "TUI@GD1";
        public string summaryUrl { get; set; } = "summary?timing=Live&reportType=1&priceType=1&carNormalizationType=0&includepremium=false&benchmarkTypes=293";
        public bool PullProduct { get; set; } = true;
    }
    public class SendFax_Setting
    {
        public string apiUrl { get; set; } = "https://api.documo.com/v1/";
        public string apiKey { get; set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiJmOGJhZTNiYi04NmUwLTQ1Y2MtYjllNS1jMDBkNmQyNjViMDYiLCJpYXQiOjE1ODI1NjUxOTl9.Y2DTxpBMPy5HkaP0D3Z_KO5v6Q7Pv_HFXLaKg2h7518";
        //ToDo
        public bool SimulatSend = true;
        public int checkMins = -30;
        public bool SendOtherAttchement { get; set; } = true;
    }
    public class Smtp_Setting
    {
        public string SmtpServer { get; set; } = "smtp.sendgrid.net";
        public int SmtpPort { get; set; } = 25;
        public string SmtpUser { get; set; } = "apikey";
        public string SmtpUserPassword { get; set; } = "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";
        public bool SmtpUseSSL { get; set; } = true;
        public string SmtpMailFrom { get; set; } = "dispatch-orders@TUIgasoline.com";
        //ToDo
        public bool UseTestEmail { get; set; } = true;
        //ToDo
        public string TestEmail { get; set; } = "tom_zhangzh@aliyun.com";


    }
    public class Twilio_Setting
    {
        public string TwilioAccountSID { get; set; } = "AC8c6df73ca3ddb153ebbfd57d6ea694b9";
        public string TwilioAccountToken { get; set; } = "2291cb0e4262c520742f91884382c725";
        public string TwilioOutgoingNumber { get; set; } = "+1 786 650 0236";
}
    public class ExcelGenerate_Setting
    {
        public string ExcelProcessedFolder { get; set; } = @"wwwroot\Generate\";
        public string ExcelTemplatePath { get; set; } = @"wwwroot\Templates\";
    }
    public class AuditEntitySetList
    {
        public List<string> List { get; set; }=
        new List<string>()
            {
                "PriceBrand",
                "EmailTemplate",
                "PriceBrandConfiguration",
                "PriceBrandFormula",
                "Role",
                "RolePermission",
                "SystemSettingItem",
                "User",
                "UsersInRole",
               // "ScheduleTask",
                "xxxxx",

            };
}
    public class EnableConfig
    {
        public bool PopulateDatePricesLastPrice { get; set; } = true;
        public int PopulateDatePricesLastPrice_PreDays { get; set; } = 7;
        public bool WarehousesMapping { get; set; } = true;
        public string CancelUpdateList { get; set; } = "fChevronClearDiesel,fChevronDyedDiesel";
    }
    public class ReportPriceUpdateFailureSetting
    {
        public int FailureDays { get; set; } = -4;
        public string From { get; set; } = "dailyfuelpricing@TUIgasoline.com";
        public string Bcc { get; set; } = "vcio@webhosting.net,iovana@webhosting.net";
        public string EmailTemplateName { get; set; } = "Task.ReportPriceFailureTemplate";
    }
   
    public class DailyProcess_Setting
    {
        public int CheckingTimeFrame_StartHour { get; set; } = 17;
        public int CheckingTimeFrame_EndHour { get; set; } = 22;
        public int EmailThrottleValue { get; set; } = 50;
        public int AfterSendEmailThreadSleep { get; set; } = 2000;
        public string EmailFrom { get; set; } = "dailyfuelpricing@TUIgasoline.com";
        public string DefaultEmail { get; set; } = "serg@webhosting.net,iovana@webhosting.net,Max@TUIgasoline.com" ;
        public string DefaultFax { get; set; } = "13054777049,18662914906,17865630355";
    }
    public class SendDailySms_Setting
    {
        public int IgnoredHours = 12;
        public int MissingPricesTotalHours = 5;
    }
    public class CarrierInvoices_Setting
    {
        public string ftpHost { get; set; } = "ftp://ftpcarriers.TUIgasoline.com";
        public string ftpUser { get; set; } = "ftpadmin";
        public string ftpPassword { get; set; } = "Lt4*VzEx_qntXuYo";
        public string EmailFrom { get; set; } = "carriers@TUIgasoline.com";
        public string EmailTo { get; set; } = "serg@webhosting.net";
    }
    public class EspCarrier_Setting
    {
        public string LocalFileLocationPath{get;set;}= "C:\\TUIFTP\\LocalUser\\Esp";
        public string EmailFrom { get; set; } = "carriers@TUIgasoline.com";
        public string EmailTo { get; set; } = "serg@webhosting.net";
        public string MoveTo { get; set; } = "Archive";
    }
    public class Gulfstream_SendFile_Setting
    {
        public string To { get; set; } = "jmor@TUIgasoline.com,julym@TUIgasoline.com";
        public string Bcc { get; set; } = "serg@webhosting.net,Iovana@webhosting.net,james@webhosting.net";
        public string FromEmail { get; set; } = "noreply@TUIgasoline.com";
        public string FromDisplayName { get; set; } = "GulfStream Carrier File";
        public string ReplyToEmail { get; set; } = "ivonnegulfstream@yahoo.com";
        public string ReplyToDisplayName { get; set; } = "Ivonne Prieto";
    }
    public class Gulfstream_Setting
    {
        public string LocalFileLocationPath { get; set; } = "C:\\TUIFTP\\LocalUser\\GulfStream";
        public string EmailFrom { get; set; } = "carriers@TUIgasoline.com";
        public string EmailTo { get; set; } = "serg@webhosting.net";
        public string MoveTo { get; set; } = "Archive";
        public Gulfstream_SendFile_Setting Gulfstream_SendFile_Setting { get; set; } = new Gulfstream_SendFile_Setting();
    }
    public class ErroLogJob_Setting
    {
        public string To { get; set; } = "tom_zhangzh@aliyun.com,Iovana@webhosting.net,james@webhosting.net";
        public string From { get; set; } = "dailyfuelpricing@TUIgasoline.com";
        public string EmailTemplateName { get; set; } = "Task.ErrorLogJob";

    }
    public class SystemSetting
    {
        public DTN_API_Setting DTN_API_Setting { get; set; } = new DTN_API_Setting();
        public Opis_API_Setting Opis_API_Setting { get; set; } = new Opis_API_Setting();
        public SendFax_Setting SendFax_Setting { get; set; } = new SendFax_Setting();
        public Smtp_Setting Smtp_Setting { get; set; } = new Smtp_Setting();
        public Twilio_Setting Twilio_Setting { get; set; } = new Twilio_Setting();
        public AuditEntitySetList AuditEntitySetList { get; set; } = new AuditEntitySetList();
        public EnableConfig EnableConfig { get; set; } = new EnableConfig();
        //public Dictionary<string, int> ForceEffectiveTime { get; set; }=new Dictionary<string, int> { { "fCitgoUnleaded", 1800 }, { "fCitgoClearDiesel", 1800 } };
        public Dictionary<string, string> WarehousesMapping
        {
            get; set;
        }= new Dictionary<string, string> { { "P02", "448626406" }, { "P03", "449103294" } };
        public ReportPriceUpdateFailureSetting ReportPriceUpdateFailureSetting { get; set; } = new ReportPriceUpdateFailureSetting();
        public DailyProcess_Setting DailyProcess_Setting { get; set; } = new DailyProcess_Setting();
        public ExcelGenerate_Setting ExcelGenerate_Setting { get; set; } = new ExcelGenerate_Setting();
        public SendDailySms_Setting SendDailySms_Setting { get; set; } = new SendDailySms_Setting();
        public CarrierInvoices_Setting CarrierInvoices_Setting { get; set; } = new CarrierInvoices_Setting();
        public EspCarrier_Setting EspCarrier_Setting { get; set; } = new EspCarrier_Setting();
        public Gulfstream_Setting Gulfstream_Setting { get; set; } = new Gulfstream_Setting();
        public ErroLogJob_Setting ErroLogJob_Setting { get; set; }= new ErroLogJob_Setting();
    }
}
