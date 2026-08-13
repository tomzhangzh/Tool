using TUI.Services.DBModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TUI.Services;
using TUI.Services.Extension;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TUI.Services.TaskLib;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TUI.WebPortal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Startup.Configuration = configuration;
        }
        public static IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            //services.AddRazorPages();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(Properties.Settings.Default.SessionTimeOutMinutes);
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            //services.AddScoped<ITUIContext, TUIContext>();
            services.AddDbContext<TUIDbContext>(options => {
                //options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()));
                options.UseSqlServer(Configuration.GetConnectionString("TUIDatabase"));
                options.UseLazyLoadingProxies();
            });
            //services.AddDistributedSqlServerCache(o =>
            //{
            //    o.ConnectionString = Configuration.GetConnectionString("TUISession");
            //    o.SchemaName = "dbo";
            //    o.TableName = "Sessions";
            //});
            services.AddMvc()
             .AddNewtonsoftJson(options => {
                 options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                 options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                 options.SerializerSettings.ContractResolver = new IngoreLazyLoadResolver();
             }); 
            
            services.AddSqlsugarSetup(Configuration);
            services.AddDataService();
            services.AddLocalization();
            services.AddQuartz();
            #region Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "API", Version = "v1" });
               // c.DocumentFilter<SwaggerIgnoreFilter>();
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";//����xmlע��
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            #endregion
            services.ConfigureApplicationCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(Properties.Settings.Default.CookieTimeOutMinutes);
                options.SlidingExpiration = true;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error/Index");
            }
            else
            {
                app.UseExceptionHandler("/Error/Index");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            var supportedCultures = new[]{
                new CultureInfo("en-US")
            };
            
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                FallBackToParentCultures = false
            });
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CreateSpecificCulture("en-US");
            app.UseMyApp(env, Configuration);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSerilogRequestLogging();
            app.UseSession();
            app.UseRouting();
            app.UseQuartz();
            app.UseAuthentication();
            app.UseAuthorization();
            #region Swagger
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API");
               
            });
            #endregion
            //CultureInfo.CurrentCulture = new CultureInfo("en-US");
            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapControllers();
                endpoints.MapControllerRoute(
                name: "Areas_Admin",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                //endpoints.MapRazorPages();
            });

        }
    }
}
