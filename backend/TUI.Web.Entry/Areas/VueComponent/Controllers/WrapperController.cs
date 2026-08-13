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
    public class WrapperController : BaseController
    {
        public IActionResult BadgeWrapper()
        {
            return View();
        }
        public IActionResult HtmlWrapper()
        {
            return View();
        }
    }
}
