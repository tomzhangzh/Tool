using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class PriceBrand
    {
        public PriceBrand()
        {
            PriceBrandConfiguration = new HashSet<PriceBrandConfiguration>();
            PriceBrandFormula = new HashSet<PriceBrandFormula>();
            PriceEmailRecipient = new HashSet<PriceEmailRecipient>();
            PriceFaxRecipient = new HashSet<PriceFaxRecipient>();
            PriceTask = new HashSet<PriceTask>();
        }

        public int ID { get; set; }
        public string BrandName { get; set; }
        public string BrandCode { get; set; }
        public string EmailSubject { get; set; }
        public bool HasAttachment { get; set; }
        public bool HasFaxNotification { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? EmailTemplateID { get; set; }
        public int? AttachmentEmailTemlateID { get; set; }
        public string AttachmentFile { get; set; }

        public virtual EmailTemplate AttachmentEmailTemlate { get; set; }
        public virtual EmailTemplate EmailTemplate { get; set; }
        public virtual ICollection<PriceBrandConfiguration> PriceBrandConfiguration { get; set; }
        public virtual ICollection<PriceBrandFormula> PriceBrandFormula { get; set; }
        public virtual ICollection<PriceEmailRecipient> PriceEmailRecipient { get; set; }
        public virtual ICollection<PriceFaxRecipient> PriceFaxRecipient { get; set; }
        public virtual ICollection<PriceTask> PriceTask { get; set; }
    }
}
