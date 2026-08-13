using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.WebPortal.Controllers
{
    [AllowAnonymous]
    public class ErrorController : BaseController
    {
        public IActionResult Index()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = context.Error;
            return View(exception);
        }
    }
}
