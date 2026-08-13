using TUI.Services.DBModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TUI.Services.Manager;

namespace TUI.Services
{
    public static class App
    {

        private static IWebHostEnvironment webHostEnvironment;
        private static IServiceProvider rootServices;
        private static IConfiguration configuration;


        public static IApplicationBuilder UseMyApp(this IApplicationBuilder app, IWebHostEnvironment env, IConfiguration config)
        {
            rootServices = app.ApplicationServices;
            webHostEnvironment = env;
            configuration = config;
            return app;
        }
        public static IConfiguration Configuration  => configuration;
        public static HttpContext HttpContext => RootServices?.GetService<IHttpContextAccessor>()?.HttpContext;


        public static IWebHostEnvironment WebHostEnvironment => webHostEnvironment;
        public static User CurrentUser
        {
            get
            {
                return HttpContext?.Session.GetValue<User>("CurrentUser");
            }
            set
            {
                if (HttpContext!=null && HttpContext.Session != null)
                {
                    HttpContext?.Session.SetValue("CurrentUser",value);
                }
            }
        }


        public static IHostEnvironment HostEnvironment { get; set; }

        public static IServiceProvider RootServices => rootServices;
        private static ISystemSettingService systemSettingService = null;
        public static ISystemSettingService SystemSettingService
        {
            get
            {
                if (systemSettingService == null)
                {
                    ////Scope
                    //systemSettingService= HttpContext.RequestServices.GetService<ISystemSettingService>();
                    systemSettingService = rootServices.GetService<ISystemSettingService>();
                }
                return systemSettingService;

            }
        }
        public static SystemSetting SystemSetting=> SystemSettingService.GetSystemSetting();
        public static SqlSugar.ISqlSugarClient dbSqlSugar => rootServices.GetService<SqlSugar.ISqlSugarClient>();
    }
}
