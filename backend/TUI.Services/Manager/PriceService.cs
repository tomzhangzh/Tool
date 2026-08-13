using Sunshine.Services.Models;
using Sunshine.Services.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using Sunshine.Services.Extension;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Globalization;
using System.IO;
using Sunshine.Services.Properties;
using System.Diagnostics;
using System.Net.Mail;
using System.IO.Compression;
using Sunshine.Services.DBModel;
using Sunshine.Services.Model;

namespace Sunshine.Services.Manager
{
    public interface IPriceService: IScopeDependency
    {
        List<tblPriceBrandConfiguration> GetBrandConfigurations(tblPriceBrand brand);
        List<tblPriceHistory> GetByBrandAndDay(long BrandID, DateTime dateTime);
        List<tblPriceHistory> GetByGetByBrandAndRange(long BrandID, DateTime start, DateTime end);
        List<tblPriceHistory> GetByBrandAndMonth(long BrandID, DateTime dateTime);
        List<tblPriceBrand> ListActivePriceBrand();
        FuelPricingViewModel GetFuelPricingViewModel(long BrandID, DateTime date);
        FuelBrandModel GetBrandModel(long BrandID, DateTime? date = null);
        UnbrandedModel GetUnbrandedModel(DateTime? date = null);
        Object GetBrandModelObj(long BrandID, DateTime? date = null);
        string GetHtml(long BrandID, bool NoDataFile, DateTime? date = null);
        string GenerateHtmlFile(long BrandId,bool WithNoDataFile, DateTime? date = null, string dirPath = null);
        string GeneratePdf(string htmlFile);
        MailMessage GetMailMessage(tblPriceBrand brand, DateTime? date = null);
        MailMessage GetMailMessage(long BrandID, DateTime? date = null);
        void SendEmail(long BrandId, List<string> emailList, DateTime? date = null, MailMessage mailMessage = null);
        ISettingService SettingService { get; }
        IService<tblPriceBrand> PriceBrandService { get; }
        IService<tblPriceProduct> PriceProductService { get; }
        List<string> BatchGenerator(BatchGeneratorViewModel model);
    }
    public class PriceService : IPriceService
    {
        private readonly IService<tblPriceHistory> tblPriceHistoryService;
        private readonly IService<tblPriceBrandConfiguration> tblPriceBrandConfigurationSevice;
        private readonly IService<tblPriceBrand> tblPriceBrandService;
        private readonly IService<tblPriceProduct> tblPriceProductService;
        private readonly IViewRenderService viewRenderService;
        private readonly ISendEmailService sendEmailService;
        private readonly ISettingService settingService;
        private readonly SqlSugar.ISqlSugarClient dbSqlSugar;

        public static string displayPrice(FuelBrandModel model, string HasPriceField, string PriceField)
        {
            if (model.GetPropValue<bool>(HasPriceField))
            {
                return $"<td class=\"fuelPrice\">{model.GetPropertyValue(PriceField)}</td>";
            }
            if (model.LastPriceModel == null)
            {
                return " <td class=\"fuelPrice\"><span style=\"color: red\">N/A</span></td>";
            }
            if (model.LastPriceModel.GetPropValue<bool>(HasPriceField))
            {
                return $" <td class=\"fuelPrice\"><span style=\"color: red\">{model.LastPriceModel.GetPropertyValue(PriceField)} **</span></td>";
            }
            return " <td class=\"fuelPrice\"><span style=\"color: red\">N/A</span></td>";
        }
        ISettingService IPriceService.SettingService => this.settingService;

        public IService<tblPriceBrand> PriceBrandService => this.tblPriceBrandService;

        public IService<tblPriceProduct> PriceProductService => this.tblPriceProductService;

        public PriceService(IService<tblPriceHistory> tblPriceHistoryService
            ,IService<tblPriceBrand> tblPriceBrandService
            , IService<tblPriceBrandConfiguration> tblPriceBrandConfigurationSevice
            , IService<tblPriceProduct> tblPriceProductService
            , IViewRenderService viewRenderService
            , ISendEmailService sendEmailService
            , ISettingService settingService
            , SqlSugar.ISqlSugarClient dbSqlSugar)
        {
            this.dbSqlSugar = dbSqlSugar;
            this.tblPriceHistoryService = tblPriceHistoryService;
            this.tblPriceBrandConfigurationSevice = tblPriceBrandConfigurationSevice;
            this.tblPriceBrandService = tblPriceBrandService;
            this.viewRenderService = viewRenderService;
            this.sendEmailService = sendEmailService;
            this.settingService = settingService;
            this.tblPriceProductService = tblPriceProductService;
        }
        public List<tblPriceBrandConfiguration> GetBrandConfigurations(tblPriceBrand brand)
        {
            return this.tblPriceBrandConfigurationSevice.List(x => x.BrandID == brand.ID);
        }
        public List<tblPriceHistory> GetByGetByBrandAndRange(long BrandID, DateTime start,DateTime end)
        {
            
            start = start.ToEasternStandardTimeUTC();
            end= end.ToEasternStandardTimeUTC();
            var list = this.tblPriceHistoryService.List(x => x.BrandID == BrandID && x.LastUpdated >= start && x.LastUpdated < end);
            var result = (from x in list
                          group x by new{ x.ProductID ,x.SupplierID ,x.TerminalID,x.BrandID,x.CityID, x.LastUpdated.Value.UtcDateTime.UtcToEasternStandardTime().Date} into grp
                          select grp.OrderByDescending(x => x.LastUpdated).FirstOrDefault()).ToList();
            return result;
        }
        public List<tblPriceHistory> GetByBrandAndDay(long BrandID, DateTime dateTime)
        {
            var start = dateTime.Date;
            var end = start.AddDays(1);
            var result = GetByGetByBrandAndRange(BrandID,start, end);
            return result;
        }
        public List<tblPriceHistory> GetByBrandAndMonth(long BrandID, DateTime dateTime)
        {
            var start = dateTime.FirstDayOfMonth();
            var end = start.AddMonths(1);
            var result = GetByGetByBrandAndRange(BrandID, start, end);
            return result;
        }
        public List<tblPriceBrand> ListActivePriceBrand()
        {
            var result = this.tblPriceBrandService.List(x => x.IsActive == 1);
            return result;
        }

        public FuelPricingViewModel GetFuelPricingViewModel(long BrandID,DateTime date)
        {
            var result = new FuelPricingViewModel();
            result.SelectedDate = date;
            result.Brand = this.dbSqlSugar.Queryable<tblPriceBrand>().Where(x => x.ID == BrandID).First();
            result.BrandID = BrandID;
            result.BrandName = result.Brand.BrandName;
            result.BrandConfiguration = this.dbSqlSugar.Queryable<tblPriceBrandConfiguration>().Where(x => x.BrandID == BrandID).ToList();
            result.Data = GetByBrandAndDay(BrandID, date);
            return result;

        }
        public FuelBrandModel GetBrandModel(long BrandID,DateTime? date = null) 
        {
           
            var dateObj = date ==null? DateTime.UtcNow.GetEasternTime() : date.Value.ToEasternStandardTimeUTC();
            
            var model = getModel((PriceBrandEnum)BrandID);
            var result = PopulateDatePrices(model, (PriceBrandEnum)BrandID, dateObj);
            APPENDEXTENDPRICE(model, (PriceBrandEnum)BrandID);
            if (model.IsPriceMissing && AppConfig.Instance.EnableConfig.PopulateDatePricesLastPrice)
            {
                var LastPriceModel = getModel((PriceBrandEnum)BrandID);
                PopulateDatePrices(LastPriceModel, (PriceBrandEnum)BrandID, dateObj, true);
                APPENDEXTENDPRICE(LastPriceModel, (PriceBrandEnum)BrandID);
                model.LastPriceModel = LastPriceModel;
            }
            if (result ==false)
            {
                return null;
            }
            model.FooterDate = dateObj.ToString("dddd, MMMM d, yyyy");
            return model;
        }
        public UnbrandedModel GetUnbrandedModel(DateTime? date=null)
        {
            var dateObj = date == null ? DateTime.UtcNow.GetEasternTime() : date.Value.ToEasternStandardTimeUTC();

            var model = new UnbrandedModel();
            var result = PopulateUnbrandedDatePrices(model, PriceBrandEnum.Unbranded, dateObj);
            if (result == false)
            {
                return null;
            }
            model.FooterDate = dateObj.ToString("dddd, MMMM d, yyyy");
            return model;
        }
        public Object GetBrandModelObj(long BrandID, DateTime? date = null)
        {
           if (BrandID== PriceBrandEnum.Unbranded.GetHashCode())
            {
                return GetUnbrandedModel(date);
            }
            else
            {
                return GetBrandModel(BrandID, date);
            }
        }
        public string GetHtml(long BrandID, bool NoDataFile, DateTime? date = null)
        {
            PriceBrandEnum PriceBrandEnum =(PriceBrandEnum)BrandID;
            var model = GetBrandModelObj(BrandID, date);
            var html = "";
            if (model!=null)
            {
                //dynamic viewBag = new System.Dynamic.ExpandoObject();
                //viewBag.AppConfig = AppConfig.Instance.EnableConfig;
                var file = Path.Combine(Settings.Default.ViewPath, $"{PriceBrandEnum.ToString()}.cshtml");
                 html = this.viewRenderService.RenderViewFromFile(file, model);
            }
            else
            {
                if (NoDataFile==false)
                {
                    return null;
                }
                var file = Path.Combine(Settings.Default.ViewPath, $"NoData.cshtml");
                html = this.viewRenderService.RenderViewFromFile(file, new { FooterDate = date??DateTime.Today});
            }
            using(StreamReader sr = new StreamReader(Path.Combine(Settings.Default.ViewPath, $"Layout.cshtml")))
            {
                var result= string.Format(sr.ReadToEnd(), html);
                return result;
            }
            
        }
        public List<string> BatchGenerator(BatchGeneratorViewModel model)
        {
            var result = new List<string>();
            var tempDir = Path.Combine(Settings.Default.RegeneratePath, Guid.NewGuid().ToString());
            if (Directory.Exists(tempDir) == false)
            {
                Directory.CreateDirectory(tempDir);
            }
            for(var date = model.Start; date <= model.End; date = date.AddDays(1))
            {
                var subTempDir = tempDir;
                if (model.ZipByDate)
                {
                    subTempDir = Path.Combine(tempDir,date.ToString("MM-dd-yyyy"));
                    if (Directory.Exists(subTempDir) == false)
                    {
                        Directory.CreateDirectory(subTempDir);
                    }
                }
                model.BrandList.ForEach(BrandId =>
                {
                    var brand = this.tblPriceBrandService.Get(BrandId);
                    if (brand.HasAttachment==1)
                    {
                        var html = GenerateHtmlFile(BrandId,Settings.Default.BatchGenerateNoData, date, subTempDir);
                        if (html.IsNullOrEmpty()==false)
                        {
                            var pdf = GeneratePdf(html);
                        }
                      
                    }
                    else 
                    {
                        var message = GetMailMessage(brand.ID, date);
                        if (message.Body.IsNullOrEmpty()==false)
                        {
                            using (System.IO.StreamWriter sw = new StreamWriter($"{subTempDir}/{brand.BrandName}_{date.ToString("MMddyyyy_HHmmss")}_{DateTime.Now.ToString("MMddyyyy_HHmmss")}.txt"))
                            {
                                sw.Write(message.Body);
                            }
                        }
                        
                    }
                });
                if (model.ZipByDate)
                {
                   var zipFilePath= Zip(subTempDir,$"{date.ToString("MM-dd-yyyy")}.zip");
                    if (zipFilePath.IsNullOrEmpty()==false)
                    {
                        result.Add(zipFilePath);
                        if (model.OnlyZipFile == false)
                        {
                            new DirectoryInfo(subTempDir).GetFiles().ToList().ForEach(file =>
                            {
                                file.MoveTo(Path.Combine(Settings.Default.RegeneratePath, file.Name), true);
                            });
                        }
                    }
                    
                    Directory.Delete(subTempDir,true);
                }
            }
            if (model.ZipByDate == false)
            {
                var zipFilePath = Zip(tempDir,$"{model.Start.ToString("MM-dd-yyyy")}-To-{model.End.ToString("MM-dd-yyyy")}.zip");
                if (zipFilePath.IsNullOrEmpty() == false)
                {
                    result.Add(zipFilePath);
                }

            }
            if (model.OnlyZipFile == false)
            {
                new DirectoryInfo(tempDir).GetFiles().ToList().ForEach(file =>
                {
                    file.MoveTo(Path.Combine(Settings.Default.RegeneratePath, file.Name), true);
                });
            }
            Directory.Delete(tempDir, true);
            return result;
        }

        private string Zip(string subTempDir, string zipFile)
        {
            string filename = Path.Combine(Settings.Default.RegeneratePath, zipFile);
            if (new DirectoryInfo(subTempDir).GetFiles().Length == 0)
            {
                return null;
            }
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            ZipFile.CreateFromDirectory(subTempDir, filename);
            return filename;
        }

        public string GenerateHtmlFile(long BrandId, bool WithNoDataFile,DateTime? date=null,string dirPath=null)
        {
            var brand = this.tblPriceBrandService.Get(BrandId);
            var attachmentsPath = dirPath?? Path.Combine(Settings.Default.PricePullSchedulePath, "attachments");
            if (Directory.Exists(attachmentsPath) ==false)
            {
                Directory.CreateDirectory(attachmentsPath);
            }
            var htmlFile = Path.Combine(attachmentsPath, String.Format("{0}_{1}.html", brand.BrandName, DateTime.Now.ToString("MMddyyyy_HHmmss")));
            if (date!=null)
            {
                htmlFile = Path.Combine(attachmentsPath, String.Format("{0}_{1}_{2}.html", brand.BrandName, date.Value.ToString("MMddyyyy_HHmmss"), DateTime.Now.ToString("MMddyyyy_HHmmss")));
            }
            var htmlString = GetHtml(BrandId, WithNoDataFile, date);
            if (htmlString.IsNullOrEmpty())
            {
                return null;
            }
            using (StreamWriter sw = new StreamWriter(htmlFile))
            {
               
                sw.Write(htmlString);
            }
            return htmlFile;
        }
        public string GeneratePdf(string htmlFile)
        {
            var pdfFile = htmlFile.Replace(".html", ".pdf");

            var si = new ProcessStartInfo(Path.Combine(Settings.Default.PricePullSchedulePath, "wkhtmltopdf.exe"), String.Format("\"{0}\" \"{1}\"", htmlFile, pdfFile));
            var ps = Process.Start(si);
            ps.WaitForExit();
            return pdfFile;
        }
        public MailMessage GetMailMessage(long BrandID, DateTime? date = null)
        {
            var brand = this.tblPriceBrandService.Get(BrandID);
            var result = GetMailMessage(brand, date);
            return result;
        }
        public  MailMessage GetMailMessage(tblPriceBrand brand,DateTime? date=null)
        {
            if (date == null)
            {
                date = DateTime.Now;
            }
            var message = new MailMessage();
            message.From = new MailAddress(brand.EmailFrom);
            message.ReplyToList.Add(brand.EmailReplyTo);
            message.Subject = brand.EmailSubject;
            if (brand.HasAttachment==1)
            {
                message.Body = $"Please see attached file for the most current prices {date.Value.ToString("dddd, MMMM d, yyyy")}\n\n{Settings.Default.PriceSendEmailBody}";
            }
            else
            {
                var list = GetByBrandAndDay(brand.ID, date.Value);
                var body=$"{date.Value.ToString("dddd, MMMM d, yyyy")}\n\n";
                foreach (var config in this.GetBrandConfigurations(brand))
                {
                    var find = list.Where(x => x.ProductID == config.ProductID && x.TerminalID==config.TerminalID && x.SupplierID==config.SupplierID && x.CityID==config.CityID).OrderByDescending(x => x.LastUpdated).FirstOrDefault();
                    if (find != null)
                    {
                        body += $"{config.ReportVariableName}\n${find.Price}\n\n\n";
                    }
                    else
                    {
                        //No data find.
                        message.Body = null;
                        return message;
                    }
                }
                message.Body = $"{body}\n\n{Settings.Default.PriceSendEmailBody}"; ;
            }
            
            return message;
        }
        public void SendEmail(long BrandId,List<string> emailList,DateTime? date=null, MailMessage mailMessage=null)
        {
            emailList = emailList.Distinct().ToList();
            var brand = this.tblPriceBrandService.Get(BrandId);
            //if (brand.HasAttachment == 1)//Todo no used from old code.
            var pdfFile = GeneratePdf(GenerateHtmlFile(BrandId,true, date));
            mailMessage = mailMessage?? this.GetMailMessage(brand,date);
            if (mailMessage.Body.IsNullOrEmpty())
            {
                return;
            }
            mailMessage.Attachments.Add(new System.Net.Mail.Attachment(pdfFile));
            var sendGroupList = emailList.Chunks(Settings.Default.EmailThrottleValue);
            foreach (var list in sendGroupList)
            {
                mailMessage.Bcc.Clear();
                var emails = list.ToList();
                emails.ForEach(x =>
                {
                    mailMessage.Bcc.Add(x);
                });
                this.sendEmailService.Send(mailMessage);
                System.Threading.Thread.Sleep(2000);
            }
            
        }
        #region private
        void appendExtendPRICE(FuelBrandModel fuelBrandModel,decimal price)
        {
            if (fuelBrandModel.RackPriceUnleaded > 0)
                fuelBrandModel.RackPriceUnleaded += price;

            if (fuelBrandModel.RackPricePlus > 0)
                fuelBrandModel.RackPricePlus += price;

            if (fuelBrandModel.RackPricePremium > 0)
                fuelBrandModel.RackPricePremium += price;

            if (fuelBrandModel.RackPriceDiesel > 0)
                fuelBrandModel.RackPriceDiesel += price;
        }
        void APPENDEXTENDPRICE(FuelBrandModel fuelBrandModel, PriceBrandEnum brandEnum)
        {
            switch (brandEnum)
            {
                case PriceBrandEnum.MarathonDTW:
                    appendExtendPRICE(fuelBrandModel, 0.02M);
                    break;
                case PriceBrandEnum.Shell:
                    appendExtendPRICE(fuelBrandModel, 0.015M);
                    break;
                case PriceBrandEnum.ExxonMobil:
                    appendExtendPRICE(fuelBrandModel, 0.02M);
                    break;
            }
        }
        private FuelBrandModel getModel(PriceBrandEnum brandEnum)
        {
            var result = new FuelBrandModel();
            switch (brandEnum)
            {
                case PriceBrandEnum.Chevron:
                    result= new ChevronModel();
                    break;
                case PriceBrandEnum.Citgo:
                     result= new CitgoModel(); break;
                case PriceBrandEnum.Marathon:
                     result= new MarathonModel(); break;
                case PriceBrandEnum.MarathonDTW:
                     result= new MarathonModel(); break;
                case PriceBrandEnum.Shell:
                     result= new ShellModel(); break;
                case PriceBrandEnum.ExxonMobil:
                     result= new MobilModel(); break;
                case PriceBrandEnum.MarathonRecAndDyed:
                case PriceBrandEnum.ChevronDyed:
                case PriceBrandEnum.ChevronRec:
                case PriceBrandEnum.ShellRec:
                     result= new FuelBrandModel(); break;

                case PriceBrandEnum.Texaco:
                     result= new TexacoModel(); break;
                case PriceBrandEnum.BpPort:
                     result= new BpModel(); break;
                case PriceBrandEnum.BpTaft:
                     result= new BpModel(); break;
                case PriceBrandEnum.BpTampa:
                     result= new BpModel(); break;
                case PriceBrandEnum.ChevronTaft:
                     result= new ChevronModel(); break;
                case PriceBrandEnum.ChevronTampa:
                     result= new ChevronModel(); break;
                case PriceBrandEnum.ChevronPort:
                     result= new ChevronModel(); break;
                default:
                    throw new NotImplementedException($"{brandEnum}");
                    //case PriceBrandEnum.Unbranded:
                    //    return new UnbrandedModel();
                   
            }
            return result;
        }
        private bool PopulateDatePrices(FuelBrandModel brandModel, PriceBrandEnum brand, DateTime date, bool lastPrice = false)
        {
            using var conn = new SqlConnection(this.dbSqlSugar.CurrentConnectionConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand
            {
                Connection = conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = lastPrice ? "spGetBrandDatePrices_LastPrices" : "spGetBrandDatePricesNew"
            };
            cmd.Parameters.AddWithValue("@BrandID", (long)brand);
            cmd.Parameters.AddWithValue("@Date", date);
            using var adapter = new SqlDataAdapter(cmd);
            using var dataTable = new DataTable();
            var records = adapter.Fill(dataTable);

            if (records == 0)
                return false;

            var prices = dataTable.AsEnumerable().Select(n => new PriceEntry { BrandID = Convert.ToInt64(n["BrandID"]), ProductID = Convert.ToInt64(n["ProductID"]), Price = Convert.ToDecimal(n["Price"]), EffectiveTime = Convert.ToInt64(n["EffectiveTime"]), ReportVariableName = n["ReportVariableName"].ToString() }).ToList();
            if (AppConfig.Instance.EnableConfig.CancelUpdateList.IsNullOrEmpty() == false)
            {
                prices = prices.Where(x => AppConfig.Instance.EnableConfig.CancelUpdateList.Split(',').Contains(x.ReportVariableName) == false).ToList();
            }
            if (prices.Where(n => n.Price > 0).Count() == 0)
                return false;

            brandModel.RackPriceUnleaded = prices.FirstOrDefault(n => n.ReportVariableName == "fUnleaded")?.Price ?? 0;
            brandModel.RackPricePlus = prices.FirstOrDefault(n => n.ReportVariableName == "fPlus")?.Price ?? 0;
            brandModel.RackPricePremium = prices.FirstOrDefault(n => n.ReportVariableName == "fPremium")?.Price ?? 0;
            brandModel.RackPriceDiesel = prices.FirstOrDefault(n => n.ReportVariableName == "fDiesel")?.Price ?? 0;
            brandModel.PriceEntries = prices;
            brandModel.EffectiveTime = prices.FirstOrDefault(n => n.ReportVariableName == "fUnleaded")?.EffectiveTime ?? 0;
            return true;
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private bool PopulateUnbrandedDatePrices(UnbrandedModel brandModel, PriceBrandEnum brand, DateTime date)
        {
            using var conn = new SqlConnection(this.dbSqlSugar.CurrentConnectionConfig.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand
            {
                Connection = conn,
                CommandType = CommandType.StoredProcedure,
                CommandText = "spGetUnbrandedDatePrices"
            };

            cmd.Parameters.AddWithValue("@BrandID", (long)brand);
            cmd.Parameters.AddWithValue("@Date", date);
            using var adapter = new SqlDataAdapter(cmd);
            using var dataTable = new DataTable();
            var records = adapter.Fill(dataTable);

            if (records == 0)
                return false;

            var prices = dataTable.AsEnumerable().Select(n => new PriceEntry { BrandID = Convert.ToInt64(n["BrandID"]), ProductID = Convert.ToInt64(n["ProductID"]), Price = Convert.ToDecimal(n["Price"]), EffectiveTime = Convert.ToInt64(n["EffectiveTime"]), ReportVariableName = n["ReportVariableName"].ToString() }).ToList();
            if(AppConfig.Instance.EnableConfig.CancelUpdateList.IsNullOrEmpty()==false)
            {
                prices = prices.Where(x => AppConfig.Instance.EnableConfig.CancelUpdateList.Split(',').Contains(x.ReportVariableName) == false).ToList();
            }
            if (prices.Where(n => n.Price > 0).Count() == 0)
                return false;

            brandModel.CitgoRackUnleaded = prices.FirstOrDefault(n => n.ReportVariableName == "fCitgoUnleaded")?.Price ?? 0;
            brandModel.CitgoRackClearDiesel = prices.FirstOrDefault(n => n.ReportVariableName == "fCitgoClearDiesel")?.Price ?? 0;

            brandModel.ChevronRackUnleaded = prices.FirstOrDefault(n => n.ReportVariableName == "fChevronUnleaded")?.Price ?? 0;
            brandModel.ChevronRackClearDiesel = prices.FirstOrDefault(n => n.ReportVariableName == "fChevronClearDiesel")?.Price ?? 0;
            brandModel.ChevronRackDyedDiesel = prices.FirstOrDefault(n => n.ReportVariableName == "fChevronDyedDiesel")?.Price ?? 0;

            brandModel.MotivaRackUnleaded = prices.FirstOrDefault(n => n.ReportVariableName == "fMotivaUnleaded")?.Price ?? 0;
            brandModel.MotivaRackRec90 = prices.FirstOrDefault(n => n.ReportVariableName == "fMotivaRec90")?.Price ?? 0;
            brandModel.MotivaRackClearDiesel = prices.FirstOrDefault(n => n.ReportVariableName == "fMotivaClearDiesel")?.Price ?? 0;
            brandModel.MotivaRackDyedDiesel = prices.FirstOrDefault(n => n.ReportVariableName == "fMotivaDyedDiesel")?.Price ?? 0;
            brandModel.PriceEntries = prices;
            brandModel.EffectiveTime = prices.FirstOrDefault(n => n.ReportVariableName == "fCitgoUnleaded")?.EffectiveTime ?? 0;
            return true;
        }
        #endregion

    }
}
