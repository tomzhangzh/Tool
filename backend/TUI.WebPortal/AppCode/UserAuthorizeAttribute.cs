using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using TUI.Services;
using TUI.Services.DBModel;
using TUI.Services.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TUI.WebPortal.AppCode
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class UserAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            var cad = filterContext.ActionDescriptor as ControllerActionDescriptor;
            if (cad != null && !cad.MethodInfo.GetCustomAttributes(true).Any(x => x.GetType().Equals(typeof(AllowAnonymousAttribute)))
                && !cad.ControllerTypeInfo.GetCustomAttributes(true).Any(x => x.GetType().Equals(typeof(AllowAnonymousAttribute))))
            {
               if (TUI.Services.App.CurrentUser==null )
                {
                    if (App.HttpContext.Request.Cookies["TUI_currentUser"]!=null)
                    {
                        Int32.TryParse(MD5Helper.Decrypt(App.HttpContext.Request.Cookies["TUI_currentUser"]), out int userId);
                        if (userId!=0)
                        {
                            var user = (App.RootServices.CreateScope().ServiceProvider.GetService(typeof(Services.Repository.IService<User>)) as Services.Repository.IService<User>).Get(userId);
                            if (user!=null && user.IsActive && user.IsDeleted==false)
                            {
                                App.CurrentUser =user;
                                return;
                            }
                        }
                    }
                    if (App.HttpContext.Request.IsAjaxRequest())
                    {
                        
                       filterContext.Result = new RedirectResult("/Account/RedirctSignin");
                    }
                    else
                    {
                        filterContext.Result = new RedirectResult("/Account/Signin");
                    }
                    return;
                }
            }
        }
      
    }
}
