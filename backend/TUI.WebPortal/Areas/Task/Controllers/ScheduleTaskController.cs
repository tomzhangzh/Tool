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
using TUI.Services.TaskLib;

namespace TUI.WebPortal.Controllers
{
    [Area("Task")]
    public class ScheduleTaskController : BaseController
    {
        private IService<ScheduleTask> service;
        private IExcelGenerate excelGenerate;
        private IQuartzHandleService quartzHandleService;
        public ScheduleTaskController(IService<ScheduleTask> service,
            IQuartzHandleService quartzHandleService,
            IExcelGenerate excelGenerate)
        {
            this.service = service;
            this.quartzHandleService = quartzHandleService;
            this.excelGenerate = excelGenerate;
        }
        public virtual IActionResult Index(ScheduleTask model)
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
                model = new ScheduleTask();
                this.GotoFirstPage();
            }
            return View(model);
        }
        public virtual async Task<ActionResult> Detail(ScheduleTask model)
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
        public virtual async Task<ActionResult> GetAllJobBriefInfo()
        {

            var result = await this.quartzHandleService.GetAllJobBriefInfo();
            return View(result);

        }
        public virtual async Task<ActionResult> DoAction(int ID, string Action)
        {
            var task = this.service.Get(ID);
            BaseResult result = new BaseResult();
            switch (Action)
            {
                case "Pause":
                    result= await this.quartzHandleService.Pause(task);
                    break;
                case "Resume":
                    result = await this.quartzHandleService.Resume(task);
                    break;
                case "Trigger":
                    result = await this.quartzHandleService.TriggerJob(task);
                    break;
                default:
                    break;
            }
            if (result.Code==200)
            {
                this.ExecJS(new FlashMessageJavaScript());
            }
            else
            {
                this.ExecJS(new AlertMessageJavaScript() { Message = result.Msg });
            }
            return this.EmptyView();
        }

    }
}
