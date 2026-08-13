using Furion;
using Furion.ClayObject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NPOI.XSSF.UserModel.Helpers;
using SqlSugar;
using System.Reflection;
using TUI.Core.Models;
using TUI.Web.Entry.Controllers;
using TUI.Web.Entry.ViewModels;

namespace TUI.Web.Entry.Areas.Tools.Controllers
{
    
    [Area("VueComponent")]
    public class ContainerController : BaseController
    {
        public IActionResult Card()
        {
            return View();
        }
        public IActionResult Container()
        {
            return View();
        }
        public IActionResult DivContainer()
        {
            return View();
        }
        public IActionResult RepeatTagContainer()
        {
            return View();
        }
        
        public IActionResult Collapse() { return View(); }
        public IActionResult Panel() { return View(); }

        public IActionResult Table() { return View(); }
    }
}
