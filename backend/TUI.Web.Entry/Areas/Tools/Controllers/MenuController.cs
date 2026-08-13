using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using TUI.Core.Models;

namespace TUI.Web.Entry.Areas.Tools.Controllers
{
    [Area("Tools")]
    public class MenuController : Controller
    {
        public IActionResult Index([FromBody] SummaryPageInfo<dynamic,dynamic> model)
        {
            return View(model);
        }
    }
}
