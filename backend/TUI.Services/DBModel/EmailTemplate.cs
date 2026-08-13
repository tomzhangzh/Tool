using System;
using System.Collections.Generic;

namespace TUI.Services.DBModel
{
    public partial class EmailTemplate
    {
        public EmailTemplate()
        {
            InverseBodyFooterTemplate = new HashSet<EmailTemplate>();
            PriceBrandAttachmentEmailTemlate = new HashSet<PriceBrand>();
            PriceBrandEmailTemplate = new HashSet<PriceBrand>();
        }

        public int ID { get; set; }
        public string Category { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Cc { get; set; }
        public string Bcc { get; set; }
        public string Subject { get; set; }
        public string MailBody { get; set; }
        public bool IsBodyHtml { get; set; }
        public string ModelName { get; set; }
        public int? BodyFooterTemplateID { get; set; }
        public int? ParentID { get; set; }
        public DateTime? StartDate { get; set; }

        public string Attachments { get; set; }
        public string AttachmentsCondition { get; set; }

        public virtual EmailTemplate BodyFooterTemplate { get; set; }
        public virtual ICollection<EmailTemplate> InverseBodyFooterTemplate { get; set; }
        public virtual ICollection<PriceBrand> PriceBrandAttachmentEmailTemlate { get; set; }
        public virtual ICollection<PriceBrand> PriceBrandEmailTemplate { get; set; }
    }
}
