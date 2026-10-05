using Microsoft.AspNetCore.HttpOverrides;
using VueLibV4.Web.Core;
using VueLibV4.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 反向代理（nginx/IIS 同机反代）转发头：默认仅信任回环代理，转发后 RemoteIpAddress 为真实客户端 IP，
// ApiKeyAuthMiddleware 的本机回环放行才不会被代理请求绕过。
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// MVC + Areas + Newtonsoft JSON（支持 JObject 绑定与序列化）
// AddRazorRuntimeCompilation：开发期改 .cshtml 即时生效，视图不编译进 dll（配合 csproj 中 RazorCompileOnBuild=false）
builder.Services.AddControllersWithViews(o =>
    {
        // 全局权限过滤器：未登录 API 返回 401、页面跳登录；资源级特性校验在登录之后
        o.Filters.Add<PermissionFilter>();
    })
    .AddRazorRuntimeCompilation()
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

// 权限模块
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPermissionService, PermissionService>();
// 行级数据权限示例（业务项目改成自己的实现）
builder.Services.AddScoped<IPermissionHook, DemoRowLevelHook>();

var app = builder.Build();

// 启动初始化平台 SQLite 库（建表 + 种子 SQL）。
// 失败必须终止启动：Seeder 内部已用事务保证不会留下半迁移库，带病启动只会让系统静默跑在残缺数据上。
var seeder = new Seeder(builder.Configuration, app.Logger);
seeder.Run();

// 页面全部由 Razor View 返回
app.UseStaticFiles();
// 最早处理转发头，保证后续鉴权中间件拿到的 RemoteIpAddress 是真实客户端 IP
app.UseForwardedHeaders();
app.UseRouting();

// API Key 鉴权（默认关闭；appsettings → Dyn:Auth 开启后校验 /api/ 的 X-Api-Key header）
app.UseMiddleware<ApiKeyAuthMiddleware>();

// 全局异常日志（写 SysLog 表）
app.UseMiddleware<ExceptionLoggingMiddleware>();

app.MapGet("/", () => Results.Redirect("/Platform/Page/Desktop"));

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
