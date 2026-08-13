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
    public class ComController : BaseController
    {
        public IActionResult DynamicCom()
        {
            return View();
        }
        public IActionResult LabelWrapper()
        {
            return View();
        }
        public IActionResult Combine()
        {
            return View();
        }
        public IActionResult Slot()
        {
            return View();
        }
        public IActionResult HtmlSlots()
        {
            return View();
        }
    }
}
