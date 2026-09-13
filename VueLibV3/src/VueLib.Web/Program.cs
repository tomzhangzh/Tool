using VueLib.Web.Data;
using VueLib.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- 服务注册 ----------
builder.Services.AddControllersWithViews();

// SqlSugar Platform 上下文（元数据库）
builder.Services.AddSingleton<AppDbContext>();
// Business 库工厂（按 Project 连接串动态创建）
builder.Services.AddSingleton<BusinessDbFactory>();
// 种子数据
builder.Services.AddScoped<SeedService>();
// 动态 CRUD 服务
builder.Services.AddScoped<DynCrudService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ---------- 首启动：建库建表 + 种子数据 ----------
using (var scope = app.Services.CreateScope())
{
    var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbCtx.EnsureInit();
    var seed = scope.ServiceProvider.GetRequiredService<SeedService>();
    seed.Seed();
}

// ---------- 中间件管道 ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("DevCors");
app.UseAuthorization();

// Area 路由（Platform / Runtime / Components）
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 默认路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
