using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace TUI.WebPortal.Controllers
{
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            
            return View();
        }
        public IActionResult ComingSoon()
        {

            return View();
        }
        [AllowAnonymousAttribute]
        public IActionResult DownLoadFile(string fileName, string downloadName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                fileStream.Close();
                return File(bytes, System.Net.Mime.MediaTypeNames.Application.Octet, downloadName ?? new System.IO.FileInfo(fileName).Name);
            }
        }
    }
}
