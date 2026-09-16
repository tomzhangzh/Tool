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

// 基础设施：数据库工厂 + 免模型动态 CRUD
builder.Services.AddSingleton<DbFactory>();
builder.Services.AddScoped<DynamicCrudService>();

// 组件双定义源：Razor View 渲染器 + 组件服务（View 代码优先，DB 回退）
builder.Services.AddScoped<RazorComponentRenderer>();
builder.Services.AddScoped<ComponentService>();

var app = builder.Build();

// 启动建库 + 种子
if (builder.Configuration.GetValue<bool>("Seed:RunOnStartup", true))
{
    try
    {
        var seeder = new Seeder(builder.Configuration, app.Logger);
        seeder.Run();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "种子数据初始化失败，请检查 SQL Server 连接字符串");
    }
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
