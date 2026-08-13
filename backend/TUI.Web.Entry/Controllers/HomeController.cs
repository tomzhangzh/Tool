using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TUI.Application;

namespace TUI.Web.Entry.Controllers
{
    [AllowAnonymous]
    public class HomeController : BaseController
    {
        private readonly ISystemService _systemService;

        public HomeController(ISystemService systemService)
        {
            _systemService = systemService;
        }

        public IActionResult Index()
        {
            ViewBag.Description = _systemService.GetDescription();

            return View();
        }
        public IActionResult KeepSessionLive()
        {
            return this.EmptyView();
        }
        public IActionResult Test()
        {
            ViewBag.Description = _systemService.GetDescription();

            return View();
        }
    }
}