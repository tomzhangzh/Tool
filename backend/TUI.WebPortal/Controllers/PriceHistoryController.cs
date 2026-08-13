using TUI.Services.DBModel;
using Microsoft.AspNetCore.Mvc;
using TUI.Services.Manager;
using TUI.Services.Models;
using TUI.Services.Repository;
using TUI.Services.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TUI.WebPortal.Controllers
{
    public class PriceHistoryController : BaseController
    {
        private IService<tblPriceHistory> service;
        private IPriceService priceService;
        public PriceHistoryController(IService<tblPriceHistory> tblPricePriceHistoryervice, IPriceService priceService)
        {
            this.priceService = priceService;
            this.service = tblPricePriceHistoryervice;
        }
        public IActionResult Index(FuelPricingViewModel model)
        {
            if (this.myLoadEvent == "Load")
            {

            }
            else if (this.myLoadEvent== "BrandIDChange")
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
        public virtual ActionResult Detail(FuelPricingViewModel model)
        {
            this.ViewBag.priceService = this.priceService;
            if (this.myLoadEvent == "Load")
            {
                model = this.priceService.GetFuelPricingViewModel(model.BrandID.Value, model.SelectedDate.Value);
                return View(model);
            }
            if (this.myLoadEvent == "AddMissionProduct")
            {
                model = this.priceService.GetFuelPricingViewModel(model.BrandID.Value, model.SelectedDate.Value);
                var configs=this.priceService.GetBrandConfigurations(model.Brand);
                foreach (var config in configs)
                {
                    
                    var find = model.Data.Where(x => x.ProductID == config.ProductID && x.TerminalID == config.TerminalID && x.CityID == config.CityID && x.SupplierID == config.SupplierID).FirstOrDefault();
                    if (find == null)
                    {
                        model.Data.Add(new tblPriceHistory()
                        {
                            ProductID = config.ProductID,
                            TerminalID= config.TerminalID,
                            SupplierID=config.SupplierID,
                            CityID=config.CityID,
                            EffectiveTime=config.MustEffectiveTime,
                        });
                    }
                   
                }
                return View(model);
            }
            else if (this.myLoadEvent == "Save")
            {
                if (this.ModelState.IsValid)
                {
                    List<(decimal? oldPrice, decimal newPrice,long ProductID)> modifyList = new List<(decimal? oldPrice, decimal newPrice,long productID)>();
                    foreach (var item in model.Data)
                    {
                        if (item.ID > 0)
                        {
                            var entity=this.service.Get(item.ID);
                            if (entity.Price != item.Price)
                            {
                                modifyList.Add((entity.Price.Value, item.Price.Value, item.ProductID.Value));
                                entity.Price = item.Price;
                               
                            }
                        }
                        else
                        {
                            //var copyHistory = this.service.Queryable().Where(x => x.BrandID == model.BrandID && x.ProductID == item.ProductID).OrderByDescending(x => x.LastUpdated).FirstOrDefault();
                            //if (copyHistory == null)
                            //{
                            //    throw new Exception("Can not clone history data");
                            //}
                            var newEntity = new tblPriceHistory()
                            {
                                ProductID = item.ProductID,
                                BrandID = model.BrandID,
                                Price = item.Price,
                                RecordDateUtc = DateTime.UtcNow,
                                CityID = item.CityID,
                                SupplierID = item.SupplierID,
                                TerminalID = item.TerminalID,
                                EffectiveTime=item.EffectiveTime,
                                LastUpdated = model.SelectedDate.Value.AddHours(12).ToEasternStandardTimeUTC(),
                            };
                            
                            this.service.AddOrUpdate(newEntity);
                            modifyList.Add((null, item.Price.Value, item.ProductID.Value));
                        }
                    }
                    if (modifyList.Count > 0)
                    {
                        this.service.SaveChanges();
                        this.ExecJS(new AlertMessageJavaScript()
                        {
                            Message = string.Join("</br>", modifyList.Select(x => $"Price from {x.oldPrice} to {x.newPrice} for {this.priceService.PriceProductService.Get( x.ProductID).ProductName}"))
                        });
                    }
                    this.ExecJS(new FlashMessageJavaScript());
                    return Redirect($"/PriceHistory/Detail?BrandID={model.BrandID}&SelectedDate={model.SelectedDate.Value.ToString("yyyy/MM/dd")}");

                }
                else
                {
                    return View(model);
                }
                return View(model);
            }
            else if (this.myLoadEvent== "Regenerate")
            {
                var htmlFile = this.priceService.GenerateHtmlFile(model.BrandID.Value,false, model.SelectedDate.Value);
                var pdfFile = "";
                if (htmlFile.IsNullOrEmpty()==false)
                {
                    pdfFile = this.priceService.GeneratePdf(htmlFile);
                }
                if (htmlFile.IsNullOrEmpty()==false)
                {
                    this.ExecJS(new AlertMessageJavaScript()
                    {
                        Message = @$"Html file have been generate to :{htmlFile}<br/>Pdf file have been generate to :{pdfFile}<br/>"
                    });
                }
                else
                {
                    this.ExecJS(new AlertMessageJavaScript()
                    {
                        Message = @$"There is no data found for {model.SelectedDate.Value.ToString("yyyy/MM/dd")}<br/>"
                    });
                }
                return Redirect($"/PriceHistory/Detail?BrandID={model.BrandID}&SelectedDate={model.SelectedDate.Value.ToString("yyyy/MM/dd")}");
            }
            else
            {
                throw new NotImplementedException();
            }


        }
        public virtual ActionResult BatchGenerator(BatchGeneratorViewModel model)
        {
            this.ViewBag.priceService = this.priceService;
            
            if (this.myLoadEvent == "Load")
            {
                model.Start = DateTime.Today.FirstDayOfMonth();
                model.End = new DateTime[] { DateTime.Today, DateTime.Today.LastDayOfMonth() }.Min();
                model.BrandList = this.priceService.ListActivePriceBrand().Select(x => x.ID).ToList();
                return View(model);
            }
            else if (this.myLoadEvent == "Save")
            {
                if (this.ModelState.IsValid)
                {
                    var range = model.DateRange.Split('-').Select(x => Convert.ToDateTime(x.Trim())).ToList();
                    if (range.Count != 2)
                    {
                        throw new Exception("Please check date range");
                    }
                    model.Start = range[0];
                    model.End = range[1];
                    var result = this.priceService.BatchGenerator(model);
                    this.ExecJS(new AlertMessageJavaScript()
                    {
                        Message = string.Join("<br/>", result.Select(x=> @$"Zip file have been generated to :{x}<br/>"))
                    });
                }
                else
                {
                    return View(model);
                }
                return View(model);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public virtual ActionResult SendEmail(ResendViewModel model)
        {
            this.ViewBag.priceService = this.priceService;
            this.ViewBag.model = model;
            if (this.myLoadEvent == "Load")
            {
               var mailMessage = this.priceService.GetMailMessage(model.BrandID, model.SelectedDate);
                model.Body = mailMessage.Body;
                model.From = mailMessage.From.Address;
                model.EmailReplyTo = mailMessage.ReplyToList.First().Address;
                model.Subject = mailMessage.Subject;
                model.Body = mailMessage.Body;
                model.EmailList = this.priceService.SettingService.ListEmailRecipient(model.BrandID).OrderBy(x => x.RecipientEmail).Distinct().Select(x => x.RecipientEmail).ToList();
                model.WithAttach = this.priceService.PriceBrandService.Get(model.BrandID).HasAttachment == 1;
                //model.emailList= this.se
               return View(model);
            }

            else if (this.myLoadEvent == "Send")
            {
                var mailMessage = this.priceService.GetMailMessage(model.BrandID, model.SelectedDate);
                mailMessage.Body = model.Body;
                mailMessage.Subject = model.Subject;
                if (TUI.Services.Properties.Settings.Default.TestEmail.IsNullOrEmpty()==false)
                {
                    model.EmailList = TUI.Services.Properties.Settings.Default.TestEmail.Split(',').ToList();
                }
                
                this.priceService.SendEmail(model.BrandID, model.EmailList, model.SelectedDate, mailMessage);
                this.ExecJS(new AlertMessageJavaScript()
                {
                    Message = @$"Email have been send."
                });

                this.ExecJS(new CloseDialogJavaScript());
                return this.EmptyView();
            }
            
            else
            {
                throw new NotImplementedException();
            }
        }
        public virtual ActionResult PreviewAttachment(ResendViewModel model)
        {
            var html = this.priceService.GetHtml(model.BrandID,true, model.SelectedDate);
            this.ViewBag.html = html;
            return View();
        }
        public virtual ActionResult Delete(long ID)
        {
            this.service.Delete(ID);
            this.ExecJS(new FlashMessageJavaScript());
            return this.EmptyView();
        }
    }
}
