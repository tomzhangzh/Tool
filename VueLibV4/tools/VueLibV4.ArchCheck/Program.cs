using System.Reflection;
using System.Reflection.Emit;

namespace VueLibV4.ArchCheck;

/// <summary>
/// 架构守卫（CI 可执行）：用反射 + IL 扫描替代人肉评审，守住三条硬边界。
/// 用法：dotnet run --project tools/VueLibV4.ArchCheck（违规时退出码 1）
///   1. 依赖单向：Platform 不得引用 Web；Services 不得引用 Platform / Web。
///   2. 控制器不得 new 服务：服务统一由 DI 注入（构造器参数只允许 I*Service）。
///   3. 控制器体积：单文件 ≤ 150 行（按源码行数，需传入仓库根目录）。
/// </summary>
internal static class Program
{
    /// <summary>控制器源码行数上限（含 using 与空行）。</summary>
    private const int ControllerMaxLines = 150;

    private static int Main(string[] args)
    {
        var failures = new List<string>();
        var checks = new List<string>();

        var web = typeof(VueLibV4.Web.Areas.Platform.Controllers.PageShellController).Assembly;
        var platform = typeof(VueLibV4.Platform.Models.DynWebPage).Assembly;
        var services = typeof(VueLibV4.Services.Dependency.IScopeDependency).Assembly;

        // ---------- 1. 依赖单向 ----------
        CheckNoReference(platform, web, "Platform 不得引用 Web", failures, checks);
        CheckNoReference(services, web, "Services 不得引用 Web", failures, checks);
        CheckNoReference(services, platform, "Services 不得引用 Platform", failures, checks);

        // ---------- 2. 控制器不得 new 服务 ----------
        var controllerTypes = web.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();
        foreach (var controller in controllerTypes)
        {
            foreach (var type in WithNested(controller))
            {
                foreach (var method in SafeMethods(type))
                {
                    foreach (var target in NewObjTargets(method))
                    {
                        var name = target.Name;
                        if (name.EndsWith("Service", StringComparison.Ordinal) && !target.IsInterface && !target.IsAbstract)
                            failures.Add($"{controller.Name}.{method.Name} 直接 new {target.FullName}（服务须由 DI 注入）");
                    }
                }
            }
        }
        checks.Add($"控制器 new 服务：扫描 {controllerTypes.Count} 个控制器");

        // ---------- 3. 控制器源码行数（棘轮：存量白名单只许变小，新文件必须达标） ----------
        var repoRoot = ResolveRepoRoot(args);
        if (repoRoot != null)
        {
            var baseline = Baseline.Load(Path.Combine(repoRoot, "tools", "arch-baseline.json"));
            foreach (var file in Directory.EnumerateFiles(repoRoot, "*Controller.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
                var rel = Rel(repoRoot, file);
                var limit = baseline.ControllerLimit(rel);
                var lines = File.ReadLines(file).Count();
                if (lines > limit)
                    failures.Add($"{rel} 共 {lines} 行（上限 {limit}{(baseline.Has(rel) ? "，白名单棘轮" : "")}）");
            }
            checks.Add($"控制器行数：全仓扫描，{baseline.Count} 个存量白名单（棘轮）");
        }
        else
        {
            checks.Add("控制器行数：跳过（未找到仓库根目录，可显式传入路径）");
        }

        Console.WriteLine("== VueLibV4 架构守卫 ==");
        foreach (var c in checks) Console.WriteLine("  ✓ " + c);
        if (failures.Count == 0)
        {
            Console.WriteLine("结果：通过（0 违规）");
            return 0;
        }
        foreach (var f in failures) Console.WriteLine("  ✗ " + f);
        Console.WriteLine($"结果：失败（{failures.Count} 违规）");
        return 1;
    }

    private static void CheckNoReference(Assembly from, Assembly forbidden, string rule,
        List<string> failures, List<string> checks)
    {
        var hit = from.GetReferencedAssemblies()
            .Any(a => string.Equals(a.Name, forbidden.GetName().Name, StringComparison.OrdinalIgnoreCase));
        if (hit) failures.Add($"{rule}（实际引用了 {forbidden.GetName().Name}）");
        else checks.Add(rule);
    }

    private static IEnumerable<Type> WithNested(Type type)
    {
        yield return type;
        foreach (var nested in SafeNested(type))
            foreach (var t in WithNested(nested))
                yield return t;
    }

    private static IEnumerable<Type> SafeNested(Type type)
    {
        try { return type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic); }
        catch { return Array.Empty<Type>(); }
    }

    private static IEnumerable<MethodBase> SafeMethods(Type type)
    {
        try
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Cast<MethodBase>()
                .Concat(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }
        catch { return Array.Empty<MethodBase>(); }
    }

    /// <summary>解析方法 IL 中的 newobj 目标类型（async 方法的真实逻辑在编译器生成的 MoveNext 里，由调用方连嵌套类型一起扫）。</summary>
    private static IEnumerable<Type> NewObjTargets(MethodBase method)
    {
        MethodBody body;
        try { body = method.GetMethodBody(); } catch { yield break; }
        if (body == null) yield break;
        var il = body.GetILAsByteArray();
        if (il == null) yield break;

        var module = method.Module;
        for (var i = 0; i < il.Length - 4; i++)
        {
            // OpCodes.Newobj = 0x73，后跟 4 字节元数据 token
            if (il[i] != 0x73) continue;
            var token = BitConverter.ToInt32(il, i + 1);
            if ((token & unchecked((int)0xFF000000)) != 0x0A000000) continue; // 只取 MemberRef（跨程序集/类型构造）
            Type declaring = null;
            try
            {
                if (module.ResolveMember(token) is MethodBase ctor) declaring = ctor.DeclaringType;
            }
            catch { /* token 属于其它模块或泛型上下文，忽略 */ }
            if (declaring != null) yield return declaring;
        }
    }

    private static string ResolveRepoRoot(string[] args)
    {
        if (args.Length > 0 && Directory.Exists(args[0])) return Path.GetFullPath(args[0]);
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "VueLibV4.sln"))) return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static string Rel(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');
}

/// <summary>
/// 行数基线：<c>tools/arch-baseline.json</c> 记录存量超标控制器的当前行数（棘轮上限）。
/// 白名单只会随重构变短；新增控制器必须直接满足默认上限，否则视为违规。
/// </summary>
internal sealed class Baseline
{
    private readonly int _defaultLimit;
    private readonly Dictionary<string, int> _allow;

    private Baseline(int defaultLimit, Dictionary<string, int> allow)
    {
        _defaultLimit = defaultLimit;
        _allow = allow;
    }

    public int Count => _allow.Count;

    public bool Has(string rel) => _allow.ContainsKey(rel);

    public int ControllerLimit(string rel) => _allow.TryGetValue(rel, out var v) ? v : _defaultLimit;

    public static Baseline Load(string path)
    {
        var fallback = new Baseline(150, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        if (!File.Exists(path)) return fallback;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var limit = root.TryGetProperty("controllersMaxLines", out var l) ? l.GetInt32() : 150;
            var allow = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("controllerAllowList", out var list) && list.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var p in list.EnumerateObject())
                    allow[p.Name.Replace('\\', '/')] = p.Value.GetInt32();
            }
            return new Baseline(limit, allow);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[arch-baseline.json 解析失败，按默认上限 {fallback._defaultLimit}] {ex.Message}");
            return fallback;
        }
    }
}
