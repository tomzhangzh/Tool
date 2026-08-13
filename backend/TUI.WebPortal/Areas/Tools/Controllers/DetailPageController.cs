using TUI.Services.DBModel;
using Microsoft.AspNetCore.Mvc;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUI.Services.Extension;
using SqlSugar;
using TUI.Services.Models;
using TUI.Services.Manager;
using System.IO;
using TUI.WebPortal.Areas.Logs.Models;
using TUI.WebPortal.Areas.Tools.Models;
using TUI.Services.Model;
using System.Reflection;
using TUI.Services;
using System.Net;

namespace TUI.WebPortal.Controllers
{
    [Area("Tools")]
    public class DetailPageController : BaseController
    {
        private IService<DETAILPAGE> service;
        private IService<DETAILITEM> DETAILITEMService;
        public DetailPageController(IService<DETAILPAGE> service,
            IService<DETAILITEM> DETAILITEMServic,
            IExcelGenerate excelGenerate)
        {
            this.service = service;
            this.DETAILITEMService = DETAILITEMServic;

        }
        public virtual IActionResult Index(DETAILPAGE model)
        {
            this.GetPager();
            if (this.myLoadEvent == "Load")
            {

            }
            else if (this.myLoadEvent == "Search")
            {
                this.GotoFirstPage();
            }
            else if (this.myLoadEvent == "Clear")
            {
                this.ModelState.Clear();
                model = new DETAILPAGE();
                this.GotoFirstPage();
            }
            return View(model);
        }
        public virtual async Task<ActionResult> General(DETAILPAGE model)
        {
           
            switch (this.myLoadEvent)
            {
                case "Load":
                    var obj = service.GetOrNew(model.ID);
                    if (obj.ID == 0)
                    {
                        obj.PAGECOLUMNCOUNT = 1;
                        obj.CLASSNAME = "TUI.Services.DBModel";
                    }
                    return View(obj);
                case "Save":
                    if (this.ModelState.IsValid)
                    {

                        using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                        {

                            var entity = service.GetOrNew(model);

                            await TryUpdateModelAsync(entity);
                            service.AddOrUpdate(entity);
                            ts.Complete();
                            this.ExecJS(new FlashMessageJavaScript());
                            this.ExecJS(new CloseDialogJavaScript());
                            return Redirect($"/Tools/DetailPage/General?ID={entity.ID}");
                        }

                    }
                    else
                    {
                        return View(model);
                    }


                default:
                    throw new NotImplementedException();
            }
        }

        public virtual ActionResult Detail(int ID)
        {

            return View(ID);
        }
        public virtual ActionResult Delete(long ID)
        {
            this.service.Delete(ID);
            this.ExecJS(new FlashMessageJavaScript());
            return this.EmptyView();
        }

     
        public virtual async Task<ActionResult> DetailItem(DETAILITEMViewModel item,int? DETAILPAGEID, int ID)
        {
           
            if (this.myLoadEvent== "Load")
            {
                var obj = this.DETAILITEMService.GetOrNew(ID);
                if (obj.ID==0)
                {
                    obj.DETAILPAGEID = DETAILPAGEID.Value;
                }
              
                return View(new DETAILITEMViewModel(obj));
            }
            else if (this.myLoadEvent == "Save")
            {
                var obj = this.DETAILITEMService.GetOrNew(item.DETAILITEM.ID);
                await TryUpdateModelAsync(obj, "DETAILITEM");
                item.initExtendPropery();
                await TryUpdateModelAsync(item.ExtendPropery, "ExtendPropery");
                ItemExtendedBase.SetExtendPrperty(obj, item.ExtendPropery);
                DETAILITEMService.AddOrUpdate(obj);
                return Redirect($"/Tools/DetailPage/DetailItem?ID={obj.ID}");
            }
            else if  (this.myLoadEvent == "Changed")
            {
                item.initExtendPropery();
                return View(item);
            }
            else
            {
                throw new NotImplementedException();
            }
                    
            

        }
        public virtual ActionResult Perview(int DETAILPAGEID)
        {
            var dPage = this.service.Get(DETAILPAGEID);
            this.ViewBag.DETAILPAGE = dPage;

            Assembly SampleAssembly;
            SampleAssembly = Assembly.Load("TUI.Services.DBModel");
            var obj = SampleAssembly.CreateInstance(dPage.CLASSNAME);
            return View(obj);
        }
        public virtual ActionResult Items(int ID)
        {
              if (this.myLoadEvent == "Generate")
            {

                using (System.Transactions.TransactionScope ts = new System.Transactions.TransactionScope())
                {
                    var page = this.service.Get(ID);
                    var list = page.DETAILITEM.ToList();
                    Assembly SampleAssembly;
                    SampleAssembly = Assembly.Load("TUI.Services.DBModel");
                    var itemObj = SampleAssembly.CreateInstance(page.CLASSNAME);
                    int i = 0;

                    foreach (var p in itemObj.GetType().GetProperties())
                    {

                        var findItem = list.FirstOrDefault(x => x.COLUMNNAME == p.Name);

                        if (findItem == null)
                        {
                            i++;
                            var detailItem = new DETAILITEM()
                            {
                                COLUMNNAME = p.Name,
                                COLUMNTYPE = "TextBox",
                                ISREQUIRED = false,
                                DETAILPAGEID = page.ID,
                                ISSHOW = true,
                                LABEL = p.Name,
                                SORTID = i * 100,

                            };
                            if (p.Name.Contains("身份证"))
                            {
                                detailItem.VALIDATETYPE = "isIdCardNo";
                                detailItem.EXTENDEDPROPERTIES = (new TextBoxSettingInfo()
                                {
                                    AddonIconClass = "icon-credit-card",
                                }).ToJSONWithoutNull();
                            }
                            if (p.Name.Contains("邮箱"))
                            {
                                detailItem.VALIDATETYPE = "email";
                                detailItem.EXTENDEDPROPERTIES = (new TextBoxSettingInfo()
                                {
                                    AddonIconClass = "icon-envelope",
                                }).ToJSONWithoutNull();
                            }
                            if (p.Name.Contains("电话"))
                            {
                                detailItem.EXTENDEDPROPERTIES = (new TextBoxSettingInfo()
                                {
                                    AddonIconClass = "icon-phone",
                                }).ToJSONWithoutNull();
                            }
                            if (p.Name.Contains("面积"))
                            {
                                detailItem.EXTENDEDPROPERTIES = (new TextBoxSettingInfo()
                                {
                                    AddonText = "M<sup>2</sup>",
                                }).ToJSONWithoutNull();
                            }
                            if (p.Name.Contains("金额") || p.Name.Contains("价"))
                            {
                                detailItem.EXTENDEDPROPERTIES = (new TextBoxSettingInfo()
                                {
                                    AddonText = "￥",
                                }).ToJSONWithoutNull();
                            }
                            if (p.Name == "描述" || p.Name == "备注")
                            {
                                detailItem.COLUMNTYPE = "TextArea";
                                detailItem.AS1COLUMN = true;
                            }

                            if (p.Name.Contains("密码"))
                            {
                                detailItem.COLUMNTYPE = "Password";

                                detailItem.EXTENDEDPROPERTIES = (new PasswordSettingInfo()
                                {
                                    AddonIconClass = "icon-lock",

                                }).ToJSONWithoutNull();
                            }
                            if (p.Name == "重复密码")
                            {
                                detailItem.HTMLATTRIBUTES = "data-rule-equalTo=#密码";
                            }
                            if (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?))
                            {
                                detailItem.COLUMNTYPE = "DatePicker";
                            }
                            if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?))
                            {
                                detailItem.VALIDATETYPE = "number";
                            }
                            if (p.Name == "ID" || p.Name.ToLower() == (itemObj.GetType().Name + "ID").ToLower())
                            {
                                detailItem.COLUMNTYPE = "Hidden";
                            }
                            this.DETAILITEMService.AddOrUpdate(detailItem);
                        }
                    }
                    ts.Complete();

                }

            }
            if (this.myLoadEvent == "GenerateFile")
            {
                var page = this.service.Get(ID);
                //WebRequest req = WebRequest.Create(string.Format("http://{0}{1}",
                //    this.Request.Url
                //    Url.Action(MVC.DetailPage.Generate(ID))));
                //WebResponse resp = req.GetResponse();
                //var file = App.WebHostEnvironment.row(string.Format("~/Views/AutoGenerate/Detail/{0}.cshtml", page.NAME));
                //var dir = Path.GetDirectoryName(file);
                //if (Directory.Exists(dir) == false)
                //{
                //    Directory.CreateDirectory(dir);
                //}
                //using (StreamWriter sw = new StreamWriter(file, false, System.Text.Encoding.UTF8))
                //{
                //    var sr = new StreamReader(resp.GetResponseStream());
                //    sw.Write(sr.ReadToEnd());
                //}

               

            }
            return Redirect($"/Tools/DetailPage/Items?ID={ID}");
        }
        public virtual ActionResult Generate(int ID)
        {
            var page = this.DETAILITEMService.Get(ID);
            return View("~/Views/GeneratePage/DetailGenerate.cshtml", page);

        }
        public virtual ActionResult DeleteItem(int ID)
        {

            this.DETAILITEMService.Delete(ID);
            this.ExecJS(new FlashMessageJavaScript());
            return this.EmptyView();
        }
    }
}
