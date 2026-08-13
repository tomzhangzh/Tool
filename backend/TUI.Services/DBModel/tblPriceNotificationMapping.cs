using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class tblPriceNotificationMapping
    {
        public long ID { get; set; }
        public long? NotificationEmailID { get; set; }
        public long? NotificationEmailTypeID { get; set; }
    }
}
