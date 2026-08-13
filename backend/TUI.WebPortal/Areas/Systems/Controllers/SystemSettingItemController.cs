using TUI.Services.DBModel;
using Microsoft.AspNetCore.Mvc;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUI.Services.Extension;
using SqlSugar;
using TUI.Services.Models;
using TUI.Services.Manager;
using System.IO;
using TUI.WebPortal.Areas.Logs.Models;
using TUI.Services;
namespace TUI.WebPortal.Controllers
{
    [Area("Systems")]
    public class SystemSettingItemController : BaseController
    {
        private IService<SystemSettingItem> service;
        private ISystemSettingService SystemSettingService;
        private IExcelGenerate excelGenerate;
        public SystemSettingItemController(IService<SystemSettingItem> service,
            ISystemSettingService SystemSettingService,
            IExcelGenerate excelGenerate)
        {
            this.service = service;
            this.SystemSettingService = SystemSettingService;
            this.excelGenerate = excelGenerate;
        }
        public virtual IActionResult Index(SystemSettingItem model)
        {
            this.GetPager();
            if (this.myLoadEvent == "Load")
            {

            }
            else if (this.myLoadEvent == "Search")
            {
                this.GotoFirstPage();
            }
            else if (this.myLoadEvent == "Clear")
            {
                this.ModelState.Clear();
                model = new SystemSettingItem();
                this.GotoFirstPage();
            }
            return View(model);
        }
        public virtual async Task<ActionResult> Detail(SystemSettingItem model)
        {

            if (this.myLoadEvent == "Load")
            {
                if (model.ID != 0)
                {
                    this.ModelState.Clear();
                    model = service.Get(model.ID);
                    model.Json = model.Json.DeserializeObject().ToJSON(format: true);
                }
                return View(model);
            }

            else if (this.myLoadEvent == "Save")
            {


                if (this.ModelState.IsValid)
                {

                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {

                        var obj = service.GetOrNew(model);

                        await TryUpdateModelAsync(obj);
                        service.AddOrUpdate(obj);
                        ts.Complete();
                        this.ExecJS(new FlashMessageJavaScript());
                        this.ExecJS(new CloseDialogJavaScript());
                        return EmptyView();
                    }

                }
                else
                {
                    return View(model);
                }

            }

            else
            {
                throw new NotImplementedException();
            }


        }

        public virtual ActionResult PreviewSystemSetting()
        {

            if (this.myLoadEvent == "Load")
            {
               
                return View();
            }

            else if (this.myLoadEvent == "Refresh")
            {
                this.SystemSettingService.RefreshSystemSetting();
                return View();

            }

            else
            {
                throw new NotImplementedException();
            }


        }
        public virtual ActionResult Delete(int ID)
        {
            this.service.Delete(ID);
            this.ExecJS(new FlashMessageJavaScript());
            return this.EmptyView();
        }

    }
}
