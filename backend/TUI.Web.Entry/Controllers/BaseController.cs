using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.Web.Entry.Controllers
{
    //[Authorize]
    public class BaseController : Controller
    {
        protected string myLoadEvent
        {
            get
            {
                return HttpContext.Request.GetValue("_Event") ?? "Load";
            }
        }
        //public PaginationInfo  GetPager()
        //{
        //    var pager = new PaginationInfo();
            
        //    this.TryUpdateModelAsync(pager, "__PageInfo").Wait();
        //    this.ViewBag.__PageInfo = pager;
        //    return pager;

        //}
        //public PaginationInfo GotoFirstPage()
        //{
        //    PaginationInfo pager = this.ViewBag.__PageInfo?? new PaginationInfo();
        //    pager.CurrentPage = 1;
        //    this.ViewBag.__PageInfo = pager;
            
        //    return pager;

        //}
        public ActionResult EmptyView()
        {
            return View("/Views/Shared/Empty.cshtml", null);

        }
        public void ExecJS(BaseJavaScript script)
        {
            List<string> scripts = HttpContext.Session.GetValue<List<string>>(CommonConst.CUSTOM_SCRIPTS);
            if (scripts == null)
            {
                scripts = new List<string>();
            }
            scripts.Add(script.Script);
            HttpContext.Session.SetValue(CommonConst.CUSTOM_SCRIPTS, scripts);

        }
    }
}
