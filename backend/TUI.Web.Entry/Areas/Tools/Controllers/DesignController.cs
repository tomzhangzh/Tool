using Microsoft.AspNetCore.Mvc;

namespace TUI.Web.Entry.Areas.Tools.Controllers
{
    [Area("Tools")]
    public class DesignController : Controller
    {
        private IService<ComponentSetting> Service = App.GetRequiredService<IService<ComponentSetting>>();
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult ListComponents()
        {
            List<ComponentSetting> result = this.Service.Queryable().ToList();
            return Json(result);
        }
    }
}
