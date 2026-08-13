using Microsoft.AspNetCore.Mvc;

namespace TUI.Web.Entry.Controllers
{
    public class WebControlController : Controller
    {
        public IActionResult Pagination(string ModelName= "PageInfo")
        {
            this.ViewBag.ModelName = ModelName;
            return View();
        }
    }
}
