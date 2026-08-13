using Furion.JsonSerialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Text.Json.Serialization;
using TUI.Core.Models;
using TUI.Utils.Extensions;
using TUI.Web.Entry.Controllers;
using TUI.Web.Entry.ViewModels;

namespace TUI.Web.Entry.Areas.Tools.Controllers
{
    [Area("Tools")]
    public class ComponentSettingController : BaseController
    {
        private IService<ComponentSetting> Service =  App.GetRequiredService<IService<ComponentSetting>>();
     
        public IActionResult Index([FromBody] SummaryPageInfo<ComponentSetting> model)
        {
            if (this.myLoadEvent == "Clear")
            {
                model = new SummaryPageInfo<ComponentSetting>();
                // return Json(model);
            }
            else if (this.myLoadEvent == "Search")
            {
                model.PageInfo.CurrentPage = 1;
                //return Json(model);
            }
            return View(model);
           
        }
        public IActionResult Detail([FromBody] ComponentSetting model, int  ID)
        {
            if (this.myLoadEvent == "Load")
            {
                model = Service.GetOrNew(ID);
            }
            else if (this.myLoadEvent == "Save")
            {
                //var find= Service.GetOrNew(model.Id);

                //// 将两个对象合并为一个新的对象
                //var json = Request.Body.ReadToEnd();
                this.Service.AddOrUpdate(model);
                this.ExecJS(new AlertMessageJavaScript() { });
            }
            else if (this.myLoadEvent == "Copy")
            {
                model.Id = 0;
                model.Name = $"{model.Name}-Copy";
                model.Type = $"{model.Type}";
                this.Service.AddOrUpdate(model);
                this.ExecJS(new AlertMessageJavaScript() { });
            }
            return View(model);
        }
        public IActionResult Delete(int Id)
        {
           
            Service.Delete(Id);
            this.ExecJS(new AlertMessageJavaScript() { });
            return this.EmptyView();

        }
        public IActionResult GetComponentSettingNodes()
        {
            List<ComponentSettingNode> result= AppEx.ManagerService.GetComponentSettingNodes();
            return Json(result);
        }
        public IActionResult ListComponents()
        {
            List<ComponentSetting> result = this.Service.Queryable().ToList();
            return Json(result);
        }
        public IActionResult GetJson(int Id)
        {
            var result = this.Service.GetOrNew(Id);
            return Json(result);
        }
    }

}
