using TUI.Services.DBModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TUI.Services;
using TUI.Services.Models;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TUI.Services.Utility;

namespace TUI.WebPortal.Controllers
{
    [AllowAnonymous]
    public class AccountController : BaseController
    {
        private readonly IService<User> service;
        public AccountController(IService<User> service)
        {
            this.service = service;
        }
        public ActionResult Login(LoginViewModel model)
        {
            if (this.myLoadEvent == "Load")
            {
               return View(model);
            }
           
            else if (this.myLoadEvent == "Logon")
            {
                var entry = this.service.List(x =>x.UserName==model.Login && x.IsActive == true && x.IsDeleted == false).FirstOrDefault();
             
                if (entry != null
                    && model.Password==entry.Password)
                    //&& Cryptor.DecryptString(entry.Password).Equals(model.Password))
                {
                  
                    //var userAuth = new UserAuth
                    //{
                    //    AccessMask = (long)entry.AccessMask,
                    //    Login = entry.Login,
                    //    IsAuthenticated = true,
                    //    UserID = entry.ID,
                    //};
                    App.CurrentUser = entry;
                    if (model.RememberMe)
                    {
                        this.HttpContext.Response.Cookies.Append("_currentUser", MD5Helper.Encrypt(entry.ID.ToString()), new Microsoft.AspNetCore.Http.CookieOptions()
                        {
                            Expires = DateTime.Now.AddDays(30),
                        });
                    }
                    else
                    {
                        this.HttpContext.Response.Cookies.Delete("_currentUser");
                    }
                    this.ExecJS(new RedirectLocal()
                    {
                        url = @$"/Home/Index"
                    }) ;
                }
                else
                {
                    this.ExecJS(new AlertMessageJavaScript()
                    {
                        Message = @$"Invalid login or bad password"
                    });
                }
                return View(model);
            }
            else
            {
                throw new NotImplementedException();
            }
          
        }
        public ActionResult LogOff()
        {
            App.CurrentUser = null;
            this.HttpContext.Response.Cookies.Delete("_currentUser");
            return Redirect("/Account/Signin");           
        }
        public ActionResult Signin()
        {
            if (App.CurrentUser != null)
            {
                return Redirect("/Home/Index");
            }
            return View();
        }
        public ActionResult RedirctSignin()
        {
            this.ExecJS(new RedirectLocal()
            {
                url = "/Account/Signin"
            });
            return this.EmptyView();
        }
        public ActionResult KeepSessionLive()
        {
            var user = TUI.Services.App.CurrentUser;
            return this.EmptyView();
        }
    }
}
