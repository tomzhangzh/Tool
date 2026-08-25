using VueLib.Web.Data;
using VueLib.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- 服务注册 ----------
builder.Services.AddControllersWithViews();

// SqlSugar 数据库上下文
builder.Services.AddSingleton<AppDbContext>();

// Razor 组件渲染器（从 .cshtml 渲染组件定义）
builder.Services.AddSingleton<RazorComponentRenderer>();

// 组件服务（数据库优先 + Razor 回退）
builder.Services.AddScoped<ComponentService>();

// 低代码服务
builder.Services.AddScoped<PageSettingService>();
builder.Services.AddScoped<ComponentMetaService>();

// 动态工程 / 动态运行时
builder.Services.AddScoped<DynCrudService>();
builder.Services.AddScoped<DynProjectService>();

// 跨域（开发环境前端调试用）
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- 中间件管道 ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
}

app.UseAuthorization();

// Area 路由（NutUI/ElementUI 组件加载）- 必须在 default 前面
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 默认路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
