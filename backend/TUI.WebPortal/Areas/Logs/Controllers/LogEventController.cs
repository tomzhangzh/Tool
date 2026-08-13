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
    [Area("Logs")]
    public class LogEventController : BaseController
    {
        private IService<LogEvents> LogEventService;
        private IExcelGenerate excelGenerate;
        public LogEventController(IService<LogEvents> LogEventService,
            IExcelGenerate excelGenerate)
        {
            this.LogEventService = LogEventService;
            this.excelGenerate = excelGenerate;
        }
        public virtual IActionResult Index(LogEventFilter model)
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
                model = new LogEventFilter();
                this.GotoFirstPage();
            }
            return View(model);
        }
        public virtual async Task<ActionResult> LogDetail(LogEvents model)
        {

            if (this.myLoadEvent == "Load")
            {
                if (model.Id != 0)
                {
                    this.ModelState.Clear();
                    model = LogEventService.Get(model.Id);
                }
                return View(model);
            }

            else
            {
                throw new NotImplementedException();
            }


        }
       
    }
}
