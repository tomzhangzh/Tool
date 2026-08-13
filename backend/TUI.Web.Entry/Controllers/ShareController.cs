using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using System.IO;
using TUI.Core.Models;

namespace TUI.Web.Entry.Controllers
{
    public class ShareController : Controller
    {
        public IActionResult Pagination()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {

            if (file != null && file.Length > 0)
            {
                // 获取文件扩展名
                var fileExtension = Path.GetExtension(file.FileName);

                // 生成唯一的文件名
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(App.WebHostEnvironment.WebRootPath, "upload", "images");
                if (Directory.Exists(filePath) == false)
                {
                    Directory.CreateDirectory(filePath);
                }

                using (var fileStream = new FileStream(Path.Combine(filePath, uniqueFileName), FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                    return Json(new { fileName = $"/upload/images/{uniqueFileName}" });
                }
            }
            return Json(new { fileName = "" } );
            
        }
        public ActionResult GetJsonBySql(string sql)
        {

            var result = AppEx.dbSqlSugar.Ado.SqlQuery<dynamic>(sql).ToList();
            return Json(result);
        }
        public ActionResult GetDict(SelectComOptions model) {
            var query = AppEx.dbSqlSugar.Queryable<DictSetting>()
                .WhereIF(model.dictTableName.IsNullOrEmpty()==false,(dict)=>dict.TableName==model.dictTableName)
                .WhereIF(model.dictType.IsNullOrEmpty() == false, (dict) => dict.Type == model.dictType)
                .Select(x=>new {x.Value,x.Text });
            var result = query.ToList();
            if (model.withEmpty)
            {
                result.Add(new {Value="",Text= model.emptyText });
            }
            return Json(result);
        }
    }

   

}
