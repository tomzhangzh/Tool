using TUI.Services.DBModel;
using Microsoft.AspNetCore.Mvc;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUI.Services.Extension;
using TUI.Services.Models;
using CommonCore.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using TUI.Services.Manager;

namespace TUI.WebPortal.Controllers
{
    public class FuelPricingController : BaseController
    {
        private IService<tblPriceHistory> service;
        private IPriceService priceService;
        public FuelPricingController(IService<tblPriceHistory> tblPricePriceHistoryervice, IPriceService priceService)
        {
            this.priceService = priceService;
            this.service = tblPricePriceHistoryervice;
        }
        public IActionResult Index(FuelPricingViewModel model)
        {
            if (this.myLoadEvent == "Load")
            {

            }
            else if (this.myLoadEvent == "BrandIDChange")
            {
                this.ModelState.Remove("PriceBrandConfigurationID");
                model.PriceBrandConfigurationID = null;
            }
            else if (this.myLoadEvent == "Clear")
            {
                this.ModelState.Clear();
                model = new FuelPricingViewModel();
            }
            if (model.SelectedDate == null)
            {
                model.SelectedDate = DateTime.Today;
            }
            this.ViewBag.service = service;
            this.ViewBag.priceService = priceService;
            return View(model);
        }
        public virtual ActionResult ViewHtml(FuelPricingViewModel model)
        {
            this.ViewBag.service = service;
            this.ViewBag.priceService = priceService;
            
            return View(model);

        }
        

        public IActionResult PriceChart(FuelPricingViewModel model)
        {
           
            this.ViewBag.PriceModel = this.priceService.GetBrandModelObj(model.BrandID.Value, model.SelectedDate.Value); 
            return View(model);

        }
        public IActionResult PriceChart1(FuelPricingViewModel model)
        {
            this.ViewBag.PriceModel = this.priceService.GetBrandModelObj(model.BrandID.Value, model.SelectedDate.Value);
            return View(model);

        }
    }
}
