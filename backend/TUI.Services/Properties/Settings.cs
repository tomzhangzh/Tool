using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TUI.Services.Properties
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
                    configuration.GetSection("TUI.Services").Bind(defaultInstance);
                }
                return defaultInstance;
            }
        }
        public string PricePullSchedulePath { get; set; } = @"C:\Tools\PricePullScheduleNew\";
        public string ViewPath { get; set; } = @"C:\Tools\PricePullScheduleNew\Views";
        public string PriceSendEmailBody = @"Please DO NOT reply to this e-mail.  This e-mail is being sent out from an automated and unmonitored account.  If you have any questions or concerns regarding these price notifications, please contact the TUI Gasoline main office at (305) 477-5800 or at reception@TUIgasoline.com.  The content of this message is confidential. If you have received it by mistake, please inform us and then delete the message.  The integrity and security of this email cannot be guaranteed over the Internet. Therefore, the sender will not be held liable for any damage caused by the message.


Por favor NO responder a este email. Este correo electrónico se envía desde una cuenta automatizada y no supervisada. Si tiene alguna pregunta o inquietud con respecto a estas notificaciones de precios, comuníquese con la oficina principal de TUI Gasoline al (305) 477-5800 o en reception@TUIgasoline.com. El contenido de este mensaje es confidencial. Si lo recibió por error, infórmenos y luego elimine el mensaje. La integridad y seguridad de este correo electrónico no se puede garantizar a través de Internet. Por lo tanto, el remitente no será responsable de ningún daño causado por el mensaje.


TUI Pricing Department
TUI Gasoline Distributors, Inc.
1650 NW 87th Ave
Doral, FL 33172
Fax:(305) 477-7049
Ph:(305) 477-5800
reception@TUIgasoline.com";
        public int EmailThrottleValue { get; set; } = 50;
        public string TestEmail { get; set; }
        public string RegeneratePath { get; set; }= @"C:\Tools\Regenerate\";
        public bool BatchGenerateNoData { get; set; } = false;
        public string ExcelProcessedFolder { get; set; } = @"Generate\";
        public string ExcelTemplatePath { get; set; }=@"Templates\";
    }
  
}
