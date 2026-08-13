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
using TUI.Services.Extension;
using TUI.Services;

namespace TUI.WebPortal.Controllers
{
    [Area("Systems")]
    public class EmailTemplateVersionController : BaseController
    {
        private IService<EmailTemplate> service;
        private IExcelGenerate excelGenerate;
        public EmailTemplateVersionController(IService<EmailTemplate> service,
            IExcelGenerate excelGenerate)
        {
            this.service = service;
            this.excelGenerate = excelGenerate;
        }
        public virtual IActionResult Index(EmailTemplateFilter model)
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
                model = new EmailTemplateFilter();
                this.GotoFirstPage();
            }
            else if (this.myLoadEvent == "Copy")
            {
                var parent = service.Get(model.ParentID.Value);
                var copy = parent.Clone();
                copy.ID = 0;
                copy.ParentID=model.ParentID.Value;
                this.service.AddOrUpdate(copy);
            }
            return View(model);
        }
        public virtual async Task<ActionResult> Detail(EmailTemplate model)
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
            //else if (this.myLoadEvent == "Copy")
            //{
            //    model = service.Get(model.ID);
            //    var copy = model.Clone();
            //    copy.ID = 0;
            //    copy.TemplateName = $"{model.TemplateName}-copy";
            //    this.service.AddOrUpdate(copy);
            //    this.ModelState.Clear();
            //    return View(copy);
            //}
            else if (this.myLoadEvent == "Save")
            {


                if (this.ModelState.IsValid)
                {

                    using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                    {
                        var parent = this.service.Get(model.ParentID);

                        var obj = service.GetOrNew(model);
                        obj.TemplateName = parent.TemplateName;
                        obj.Category = parent.Category;
                        await TryUpdateModelAsync(obj);
                        service.AddOrUpdate(obj);
                        ts.Complete();
                        this.ExecJS(new FlashMessageJavaScript());
                        this.ExecJS(new CloseDialogJavaScript());
                        return this.EmptyView();
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
