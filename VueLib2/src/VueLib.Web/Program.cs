using SqlSugar;
using VueLib.Web.Models;
using VueLib.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- 数据库：平台库 + 业务库（SQL Server + SqlSugar 双库，多租户单例） ----------
var platformCs = builder.Configuration.GetConnectionString("PlatformDb")
    ?? "Server=localhost;Database=VueLib2_Platform;Trusted_Connection=True;TrustServerCertificate=True";
var businessCs = builder.Configuration.GetConnectionString("BusinessDb")
    ?? "Server=localhost;Database=VueLib2_Business;Trusted_Connection=True;TrustServerCertificate=True";

var db = new SqlSugarScope(new List<ConnectionConfig>
{
    new ConnectionConfig
    {
        ConfigId = "platform",
        ConnectionString = platformCs,
        DbType = DbType.SqlServer,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    },
    new ConnectionConfig
    {
        ConfigId = "business",
        ConnectionString = businessCs,
        DbType = DbType.SqlServer,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    }
});
builder.Services.AddSingleton<ISqlSugarClient>(db);

// ---------- MVC + Razor 运行时编译（组件 cshtml 改动即时生效） ----------
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();
builder.Services.AddHttpContextAccessor();

// ---------- 业务服务 ----------
builder.Services.AddScoped<ViewRenderService>();
builder.Services.AddScoped<ComponentRegistryService>();
builder.Services.AddScoped<OptionsService>();
builder.Services.AddScoped<DemoPageFactory>();

var app = builder.Build();

// ---------- 初始化：建表 + 注册组件 + 种子数据 ----------
using (var scope = app.Services.CreateScope())
{
    var sugar = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
    var platform = sugar.AsTenant().GetConnectionScope("platform");
    var business = sugar.AsTenant().GetConnectionScope("business");

    // 自动建库（SqlServer：连接 master 执行 CREATE DATABASE；已存在则跳过）
    try { platform.DbMaintenance.CreateDatabase(); } catch (Exception ex) { Console.WriteLine($"[VueLib2] 平台库创建失败: {ex.Message}"); }
    try { business.DbMaintenance.CreateDatabase(); } catch (Exception ex) { Console.WriteLine($"[VueLib2] 业务库创建失败: {ex.Message}"); }

    // CodeFirst 建表（平台库 + 业务库）
    platform.CodeFirst.InitTables(typeof(LcComponent), typeof(LcPage), typeof(DesktopSolution), typeof(DesktopShortcut));
    business.CodeFirst.InitTables(typeof(DictType), typeof(DictItem), typeof(Product), typeof(Customer));

    // 复合唯一索引（SugarIndex 特性只支持单字段，复合索引用 Ado 补）
    EnsureIndex(platform, "Components", "uk_name_version", "Name, Version");
    EnsureIndex(platform, "Pages", "uk_page_code", "Code");
    EnsureIndex(business, "DictTypes", "uk_dict_type_code", "TypeCode");

    var registry = scope.ServiceProvider.GetRequiredService<ComponentRegistryService>();
    await registry.RefreshRazorComponentsAsync();   // Razor 组件渲染快照入库（带版本）

    var demo = scope.ServiceProvider.GetRequiredService<DemoPageFactory>();
    await demo.SeedAsync(platform);                 // demo 组件 + 页面种子

    await SeedDesktopAsync(platform);               // 桌面快捷方式 + 解决方案种子
    await SeedBusinessAsync(business);              // 业务库 demo 数据
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();
app.MapControllerRoute(name: "designer", pattern: "designer", defaults: new { controller = "Home", action = "Designer" });
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Common}/{action=Index}/{id?}");

Console.WriteLine("[VueLib2] http://localhost:5198/  设计器: /designer");
await app.RunAsync();

// 确保唯一索引存在（幂等）
static void EnsureIndex(ISqlSugarClient db, string table, string indexName, string columns)
{
    var sql = $"""
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = '{indexName}' AND object_id = OBJECT_ID('{table}'))
BEGIN
  CREATE UNIQUE INDEX [{indexName}] ON [{table}]({columns});
END
""";
    try { db.Ado.ExecuteCommand(sql); }
    catch (Exception ex) { Console.WriteLine($"[VueLib2] 索引 {indexName} 创建失败: {ex.Message}"); }
}

// 桌面种子：解决方案分组 + 快捷方式（开发工具 / 演示应用）
static async Task SeedDesktopAsync(ISqlSugarClient platform)
{
    if (await platform.Queryable<DesktopShortcut>().AnyAsync()) return;

    var devTools = new DesktopSolution { Name = "开发工具", Icon = "🛠️", SortOrder = 1, IsEnabled = true };
    var demos = new DesktopSolution { Name = "演示应用", Icon = "📱", SortOrder = 2, IsEnabled = true };
    devTools.Id = await platform.Insertable(devTools).ExecuteReturnIdentityAsync();
    demos.Id = await platform.Insertable(demos).ExecuteReturnIdentityAsync();

    var shortcuts = new List<DesktopShortcut>
    {
        new() { Name = "首页", Icon = "🏠", Url = "/", OpenType = "iframe", SortOrder = 1, IsEnabled = true },
        new() { Name = "页面列表", Icon = "📄", Url = "/#/pages", OpenType = "iframe", SortOrder = 2, IsEnabled = true },
        new() { Name = "设计器", Icon = "🎨", Url = "/designer", OpenType = "iframe", SolutionId = devTools.Id, SortOrder = 1, IsEnabled = true },
        new() { Name = "组件管理", Icon = "🧩", Url = "/component-manage", OpenType = "iframe", SolutionId = devTools.Id, SortOrder = 2, IsEnabled = true },
        new() { Name = "快捷方式管理", Icon = "🖥️", Url = "/desktop/manage?type=shortcut", OpenType = "modal", SolutionId = devTools.Id, SortOrder = 3, IsEnabled = true },
        new() { Name = "解决方案管理", Icon = "📁", Url = "/desktop/manage?type=solution", OpenType = "modal", SolutionId = devTools.Id, SortOrder = 4, IsEnabled = true },
        new() { Name = "表单 Demo", Icon = "📋", Url = "/#/page/demo-form", OpenType = "iframe", SolutionId = demos.Id, SortOrder = 1, IsEnabled = true },
        new() { Name = "三屏管理 Demo", Icon = "📊", Url = "/#/page/demo-grid3", OpenType = "iframe", SolutionId = demos.Id, SortOrder = 2, IsEnabled = true },
        new() { Name = "窗口 Demo", Icon = "🪟", Url = "/#/page/demo-window", OpenType = "iframe", SolutionId = demos.Id, SortOrder = 3, IsEnabled = true },
        new() { Name = "组合组件 Demo", Icon = "🧱", Url = "/#/page/demo-composite", OpenType = "iframe", SolutionId = demos.Id, SortOrder = 4, IsEnabled = true },
        new() { Name = "动作链 Demo", Icon = "⚡", Url = "/#/page/demo-actions", OpenType = "iframe", SolutionId = demos.Id, SortOrder = 5, IsEnabled = true },
        new() { Name = "Flex 布局 Demo", Icon = "📐", Url = "/#/page/demo-flex", OpenType = "iframe", SolutionId = demos.Id, SortOrder = 6, IsEnabled = true }
    };
    await platform.Insertable(shortcuts).ExecuteCommandAsync();
    Console.WriteLine("[VueLib2] 桌面快捷方式种子完成");
}

// 业务库 demo 种子：字典 + 客户 + 商品（demo 下拉/列表/窗口数据源）
static async Task SeedBusinessAsync(ISqlSugarClient business)
{
    if (await business.Queryable<DictItem>().AnyAsync()) return;

    var types = new[] { "customer_industry", "product_category", "order_status" };
    await business.Insertable(types.Select(t => new DictType { TypeCode = t, TypeName = t }).ToList())
        .ExecuteCommandAsync();

    var items = new List<DictItem>
    {
        new() { TypeCode = "customer_industry", Name = "制造业", Value = "1", Sort = 1 },
        new() { TypeCode = "customer_industry", Name = "互联网", Value = "2", Sort = 2 },
        new() { TypeCode = "customer_industry", Name = "金融", Value = "3", Sort = 3 },
        new() { TypeCode = "customer_industry", Name = "零售", Value = "4", Sort = 4 },
        new() { TypeCode = "product_category", Name = "电子产品", Value = "电子产品", Sort = 1 },
        new() { TypeCode = "product_category", Name = "办公用品", Value = "办公用品", Sort = 2 },
        new() { TypeCode = "product_category", Name = "生活家电", Value = "生活家电", Sort = 3 },
        new() { TypeCode = "order_status", Name = "待处理", Value = "0", Sort = 1 },
        new() { TypeCode = "order_status", Name = "已发货", Value = "1", Sort = 2 },
        new() { TypeCode = "order_status", Name = "已完成", Value = "2", Sort = 3 }
    };
    await business.Insertable(items).ExecuteCommandAsync();

    var customers = new List<Customer>
    {
        new() { Name = "张伟", Phone = "13800001111", Company = "北京华信科技有限公司" },
        new() { Name = "李娜", Phone = "13800002222", Company = "上海精工制造集团" },
        new() { Name = "王强", Phone = "13800003333", Company = "深圳星辰金融" },
        new() { Name = "陈静", Phone = "13800004444", Company = "广州优选零售连锁" },
        new() { Name = "刘洋", Phone = "13800005555", Company = "杭州云端软件" }
    };
    await business.Insertable(customers).ExecuteCommandAsync();

    var products = new List<Product>();
    var names = new[] { "机械键盘", "无线鼠标", "27寸显示器", "USB-C 扩展坞", "人体工学椅", "激光打印机", "A4 复印纸", "签字笔套装", "空气净化器", "扫地机器人", "智能音箱", "4K 投影仪" };
    var cats = new[] { "电子产品", "电子产品", "电子产品", "电子产品", "办公用品", "办公用品", "办公用品", "办公用品", "生活家电", "生活家电", "生活家电", "生活家电" };
    var prices = new[] { 399m, 129m, 1899m, 259m, 899m, 1599m, 49m, 25m, 1299m, 2299m, 349m, 4599m };
    var rnd = new Random(42);
    for (var i = 0; i < names.Length; i++)
    {
        products.Add(new Product
        {
            Name = names[i], Category = cats[i], Price = prices[i],
            Stock = rnd.Next(10, 500), Status = i % 3 == 0 ? 0 : 1,
            Remark = "VueLib2 demo 数据 " + (i + 1)
        });
    }
    await business.Insertable(products).ExecuteCommandAsync();
    Console.WriteLine($"[VueLib2] 业务库种子完成: {types.Length} 字典 / {customers.Count} 客户 / {products.Count} 商品");
}
