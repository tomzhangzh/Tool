using VueLibV4.Web.Core;
using VueLibV4.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC + Areas + Newtonsoft JSON（支持 JObject 绑定与序列化）
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    });

// 平台层一站式装配：DbFactory/免模型CRUD/项目库解析 + 平台库 ISqlSugarClient
// + 扫描 VueLibV4.* 程序集自动注册 IDependency（IScopeDependency/ISingletonDependency/ITransientDependency）
builder.Services.AddVueLibPlatform();

// 组件双定义源：Razor View 渲染器 + 组件服务（View 代码优先，DB 回退）
builder.Services.AddScoped<RazorComponentRenderer>();
builder.Services.AddScoped<ComponentService>();

var app = builder.Build();

// 启动初始化平台 SQLite 库（建表 + 种子 SQL）
try
{
    var seeder = new Seeder(builder.Configuration, app.Logger);
    seeder.Run();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "平台库初始化失败");
}

// 页面全部由 Razor View 返回
app.UseStaticFiles();
app.UseRouting();

app.MapGet("/", () => Results.Redirect("/Platform/Page/Desktop"));

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
