using TUI.Services.DBModel;
using Microsoft.Extensions.Configuration;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace TUI.Services.Manager
{
    public interface ISettingService:IScopeDependency
    {
        List<tblPriceEmailRecipient> ListEmailRecipient(tblPriceBrand brand);
        List<tblPriceEmailRecipient> ListEmailRecipient(long BrandID);
        IService<tblPriceEmailRecipient> PriceEmailRecipientSevice { get; }
        IService<tblPriceSmsRecipient> PriceSmsRecipientSevice { get; }
    }
    public class SettingService : ISettingService
    {
        private readonly IConfiguration Configuration;
        private readonly IService<tblPriceEmailRecipient> tblPriceEmailRecipientSevice;
        private readonly IService<tblPriceSmsRecipient> tblPriceSmsRecipientSevice;

        public SettingService(
            IService<tblPriceEmailRecipient> tblPriceEmailRecipientSevice
            ,IService<tblPriceSmsRecipient> tblPriceSmsRecipientSevice
            , IConfiguration Configuration)
        {
            this.Configuration = Configuration;
            this.tblPriceEmailRecipientSevice = tblPriceEmailRecipientSevice;
            this.tblPriceSmsRecipientSevice = tblPriceSmsRecipientSevice;
        }

        IService<tblPriceEmailRecipient> ISettingService.PriceEmailRecipientSevice => tblPriceEmailRecipientSevice;

        IService<tblPriceSmsRecipient> ISettingService.PriceSmsRecipientSevice => tblPriceSmsRecipientSevice;

        public List<tblPriceEmailRecipient> ListEmailRecipient(tblPriceBrand brand)
        {
            var result = this.tblPriceEmailRecipientSevice.List(x => x.BrandID == brand.ID);
            return result;
        }
        public List<tblPriceEmailRecipient> ListEmailRecipient(long BrandID)
        {
            var result = this.tblPriceEmailRecipientSevice.List(x => x.BrandID == BrandID);
            return result;
        }
    }
}
