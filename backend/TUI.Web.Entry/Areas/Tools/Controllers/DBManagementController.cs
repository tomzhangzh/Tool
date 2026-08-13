using Furion;
using Furion.ClayObject;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NPOI.XSSF.UserModel.Helpers;
using SqlSugar;
using TUI.Core.Models;
using TUI.Web.Entry.Controllers;
using TUI.Web.Entry.ViewModels;

namespace TUI.Web.Entry.Areas.Tools.Controllers
{
    
    [Area("Tools")]
    public class DBManagementController : BaseController
    {
        private IDbMaintenance DbMaintenance { get; set; } = AppEx.dbSqlSugar.DbMaintenance;
        //[HttpGet]
        //public IActionResult Index()
        //{
        //    return View(new SummaryPageInfo<DbTableInfo>());
        //}
        //[HttpPost]
        public IActionResult Index([FromBody] SummaryPageInfo<DbTableInfo> model,string Event)
        {
            if (this.myLoadEvent == "Clear")
            {
                model = new SummaryPageInfo<DbTableInfo>();
               // return Json(model);
            }
            else if (this.myLoadEvent == "Search")
            {
                model.PageInfo.CurrentPage = 1;
                //return Json(model);
            }
            return View(model);
        }
        public IActionResult Detail([FromBody] DBTabeInfoViewModel model, string TableName)
        {
            if (this.myLoadEvent == "Load")
            {
                model = GetTableInfo(TableName);
            }
            else if (this.myLoadEvent == "Save")
            {
                if (AppEx.dbSqlSugar.DbMaintenance.IsAnyTable(TableName))
                {
                   var existTable= GetTableInfo(TableName);

                    if (TableName == model.Table.Name)
                    {
                        model.Columns.ForEach(col => {
                            var find= existTable.Columns.Where(x=>x.DbColumnName == col.DbColumnName).FirstOrDefault();
                            if (find==null)
                            {
                                this.DbMaintenance.AddColumn(TableName, col);
                            }
                            else if (find.ToJson()!=col.ToJson())
                            {
                                this.DbMaintenance.UpdateColumn(TableName, col);
                            }
                            
                           
                        });
                        if (TableName!=model.Table.Name)
                        {
                            AppEx.dbSqlSugar.DbMaintenance.RenameTable(TableName, model.Table.Name);
                        }
                        
                    }
                    else
                    {
                        this.DbMaintenance.CreateTable(TableName, model.Columns, true);
                    }
                    
                    
                }
                else
                {
                    model = new DBTabeInfoViewModel();
                }
            }
            return View(model);
        }
        public DBTabeInfoViewModel GetTableInfo(string TableName)
        {
            var result = new DBTabeInfoViewModel();
            if (AppEx.dbSqlSugar.DbMaintenance.IsAnyTable(TableName, false))
            {
                result.Table = AppEx.dbSqlSugar.DbMaintenance.GetTableInfoList(false).Where(x => x.Name == TableName).Single();
                result.TableName = TableName;
                result.Columns = this.DbMaintenance.GetColumnInfosByTableName(TableName);
            }
            else
            {
                result.Table = new DbTableInfo();
                result.TableName = "";
            }
            return result;
        }
    }
}
