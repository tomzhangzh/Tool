using Microsoft.Extensions.DependencyInjection.Extensions;
using SqlSugar;
using VueLibV4.Services.Data;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// VueLibV4 平台层装配（Web 启动调用一次：builder.Services.AddVueLibPlatform()）：
/// 1) DbFactory 单例、免模型 DynamicCrudService / ProjectDbResolver 为 Scoped；
/// 2) ISqlSugarClient 绑定平台库（Scoped，强类型 SugarService&lt;T&gt; 由此注入）；
/// 3) 触发 AddVueLibCore() 扫描注册所有 IDependency 标记实现（含本工程 10 个平台服务）。
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddVueLibPlatform(this IServiceCollection services)
    {
        // 多库工厂（单例：只持有 IConfiguration，无线程安全问题）
        services.TryAddSingleton<DbFactory>();
        // 免模型动态 CRUD 与业务库解析（Scoped）
        services.TryAddScoped<DynamicCrudService>();
        services.TryAddScoped<ProjectDbResolver>();

        // 强类型服务使用的平台库客户端：每个请求一个实例，IsAutoCloseConnection 保证连接释放
        services.AddScoped<ISqlSugarClient>(sp => sp.GetRequiredService<DbFactory>().PlatformDb());

        // 扫描注册 VueLibV4.* 程序集中的标记接口实现
        services.AddVueLibCore();

        return services;
    }
}
