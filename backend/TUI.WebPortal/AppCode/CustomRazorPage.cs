using Microsoft.AspNetCore.Mvc.Razor;
using SqlSugar;
using TUI.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.WebPortal.AppCode
{
    public abstract class CustomRazorPage<TModel> : RazorPage<TModel>
    {
        
        public PaginationInfo PaginationInfo => this.ViewBag.__PageInfo;
     

    }
}
