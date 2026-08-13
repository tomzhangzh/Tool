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

namespace TUI.WebPortal.Controllers
{
    [Area("Task")]
    public class ScheduleTaskLogController : BaseController
    {
        private IService<ScheduleTaskLog> service;
        private IExcelGenerate excelGenerate;
        public ScheduleTaskLogController(IService<ScheduleTaskLog> service,
            IExcelGenerate excelGenerate)
        {
            this.service = service;
            this.excelGenerate = excelGenerate;
        }
        public virtual IActionResult Index(ScheduleTaskLog model)
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
                model = new ScheduleTaskLog();
                this.GotoFirstPage();
            }
            return View(model);
        }
        public virtual async Task<ActionResult> Detail(ScheduleTaskLog model)
        {

            if (this.myLoadEvent == "Load")
            {
                if (model.ID != 0)
                {
                    this.ModelState.Clear();
                    model = service.Get(model.ID);
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
        public virtual ActionResult Delete(int ID)
        {
            this.service.Delete(ID);
            this.ExecJS(new FlashMessageJavaScript());
            return this.EmptyView();
        }
      

    }
}
