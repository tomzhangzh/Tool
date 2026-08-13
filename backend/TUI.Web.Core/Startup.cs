using Furion;
using Furion.VirtualFileServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using NPOI.Util.Collections;
using System;
using TUI.Core;
using TUI.Utils;

namespace TUI.Web.Core
{
    public class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddConfigurableOptions<AppInfoOptions>();//选项不同于配置，需在应用启动时注册
            //services.AddConfigurableOptions<DbSettingOptions>();
            services.AddConsoleFormatter();
            services.AddControllersWithViews()
                .AddRazorRuntimeCompilation()
                        .AddInjectBase();
            services.AddMvc()
           .AddNewtonsoftJson(options => {
               options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;//指定如何处理循环引用，None--不序列化，Error-抛出异常，Serialize--仍要序列化
                                                                                               //options.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;//控制在反序列化期间如何处理缺少的成员（例如JSON包含对象上不是成员的属性）。 Ignore 忽略，error 报错
                                                                                               //ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                                                                               //options.SerializerSettings.Formatting = Formatting.Indented;//格式化 缩进
                                                                                               //options.SerializerSettings.MaxDepth = 10; //设置序列化的最大层数
                                                                                               //options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;//空值处理
                                                                                               //日期类型默认格式化处理
                                                                                               //options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat;

               options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
               options.SerializerSettings.DateParseHandling = DateParseHandling.None;
               //options.SerializerSettings.Converters.Add(new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal });//解决数据为 0001-01-01 00:00:00 的情形下 反序列化为 DateTimeOffset 格式报错问题

               options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
               options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();//json字符串大小写原样输出
               //options.SerializerSettings.Converters.Add(new NewtonsoftJsonDateTimeOffsetConverter());
           });
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60*24);
            });
            services.SqlSugarScopeConfigure();

            services.AddUnitOfWork<SqlSugarUnitOfWork>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseInjectBase();

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