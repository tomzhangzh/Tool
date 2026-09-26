using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VueLibV4.Services.Data.Sugar;
using VueLibV4.Services.Dependency;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// VueLibV4 依赖注入自动注册：
/// 启动时扫描所有名称以 VueLibV4 开头的程序集，凡实现 IDependency（含 IScopeDependency /
/// ISingletonDependency / ITransientDependency）标记接口的类，按其业务接口自动注册到容器。
/// 用法：builder.Services.AddVueLibPlatform();（内含本扫描）
/// </summary>
public static class VueLibServiceCollectionExtensions
{
    /// <summary>扫描注册时匹配的程序集名前缀</summary>
    public const string AssemblyPrefix = "VueLibV4";

    /// <summary>
    /// 注册通用 open generic 数据服务基类 + 扫描所有标记接口实现。
    /// 平台库 ISqlSugarClient 由 VueLibV4.Platform 的 AddVueLibPlatform 负责注册。
    /// </summary>
    public static IServiceCollection AddVueLibCore(this IServiceCollection services)
    {
        // open generic 基类：未定义专属业务接口时也可直接注入 ISugarService&lt;TEntity&gt;
        services.TryAddScoped(typeof(ISugarService<>), typeof(SugarService<>));

        var assemblies = CollectAssemblies();
        var markerType = typeof(IDependency);
        var scopeType = typeof(IScopeDependency);
        var singletonType = typeof(ISingletonDependency);
        var transientType = typeof(ITransientDependency);

        var implementTypes = assemblies
            .SelectMany(SafeGetTypes)
            .Where(t => t.IsClass && !t.IsAbstract && markerType.IsAssignableFrom(t))
            .ToList();

        foreach (var implType in implementTypes)
        {
            // 业务接口：实现类继承的、属于 IDependency 体系但非三个标记接口本身的"最具体"接口；
            // 若类只直接实现标记接口（无业务接口），则注册具体类自身。
            var serviceType = ResolveBusinessInterface(implType, markerType, scopeType, singletonType, transientType)
                              ?? implType;

            if (singletonType.IsAssignableFrom(implType))
                services.TryAddSingleton(serviceType, implType);
            else if (transientType.IsAssignableFrom(implType))
                services.TryAddTransient(serviceType, implType);
            else
                services.TryAddScoped(serviceType, implType); // 默认/Scoped
        }

        return services;
    }

    /// <summary>收集入口程序集及其引用链上所有 VueLibV4.* 程序集（含尚未加载的引用，按需 Load）</summary>
    private static List<Assembly> CollectAssemblies()
    {
        var result = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        var entry = Assembly.GetEntryAssembly();

        // 已加载到当前 AppDomain 的 VueLibV4 程序集（覆盖从 bin 外宿主启动等场景）
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var n = a.GetName().Name ?? "";
            if (n.StartsWith(AssemblyPrefix, StringComparison.OrdinalIgnoreCase))
                result[n] = a;
        }

        // 从入口程序集沿引用链补加载（Razor 视图等延迟加载程序集也会被引用关系覆盖）
        if (entry != null)
        {
            var queue = new Queue<Assembly>();
            queue.Enqueue(entry);
            result[entry.GetName().Name] = entry;
            while (queue.Count > 0)
            {
                var asm = queue.Dequeue();
                AssemblyName[] refs;
                try { refs = asm.GetReferencedAssemblies(); }
                catch { continue; }
                foreach (var rn in refs)
                {
                    if (rn.Name is null || !rn.Name.StartsWith(AssemblyPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                    if (result.ContainsKey(rn.Name)) continue;
                    try
                    {
                        var loaded = Assembly.Load(rn);
                        result[rn.Name] = loaded;
                        queue.Enqueue(loaded);
                    }
                    catch { /* 引用但运行期缺失的程序集跳过 */ }
                }
            }
        }

        return result.Values.ToList();
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
    }

    /// <summary>选取实现类的最具体业务接口；多个业务接口时选不被其它候选继承的那个，避免注册到父接口</summary>
    private static Type ResolveBusinessInterface(Type implType, Type marker, Type scope, Type singleton, Type transient)
    {
        bool IsMarkerItself(Type t) => t == marker || t == scope || t == singleton || t == transient;

        var candidates = implType.GetInterfaces()
            .Where(i => marker.IsAssignableFrom(i) && !IsMarkerItself(i))
            .ToList();
        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        // 最具体 = 不被候选集合中任何其它接口继承
        var mostConcrete = candidates
            .FirstOrDefault(c => !candidates.Any(other => other != c && c.IsAssignableFrom(other)));
        return mostConcrete ?? candidates[0];
    }
}
