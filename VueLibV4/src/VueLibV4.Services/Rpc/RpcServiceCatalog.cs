using System.Reflection;
using VueLibV4.Services.Data.Sugar;

namespace VueLibV4.Services.Rpc;

/// <summary>
/// RPC 服务目录条目：一个被 IDependency 扫描器注册的可调用服务。
/// ServiceType 是该服务实际注册到 DI 容器的类型（业务接口或实现类自身）。
/// </summary>
public class RpcServiceEntry
{
    public string Name { get; set; }
    public Type ServiceType { get; set; }
    public Type ImplType { get; set; }
    public List<string> Aliases { get; set; } = new();
}

/// <summary>
/// RPC 服务目录：启动时随 IDependency 扫描一起构建（单例）。
/// 名字解析不区分大小写，支持实现类名（DesktopShortcutService）与业务接口短名（IDesktopShortcutService）。
/// </summary>
public class RpcServiceCatalog
{
    private readonly List<RpcServiceEntry> _entries = new();
    private readonly Dictionary<string, RpcServiceEntry> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _entities = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>eval 脚本中保留的内置变量名，服务名不得占用</summary>
    public static readonly string[] ReservedNames = { "Crud", "Biz", "Platform" };

    public void Add(Type implType, Type serviceType)
    {
        if (implType == null || serviceType == null) return;

        var entry = new RpcServiceEntry
        {
            Name = implType.Name,
            ImplType = implType,
            ServiceType = serviceType
        };

        // 业务接口短名作为别名（IDesktopShortcutService）：排除标记接口自身
        var marker = typeof(Dependency.IDependency);
        var markers = new[]
        {
            marker,
            typeof(Dependency.IScopeDependency),
            typeof(Dependency.ISingletonDependency),
            typeof(Dependency.ITransientDependency)
        };
        // 收集强类型实体：服务实现了 ISugarService<TEntity>（平台库实体），供 eval 实体门面变量使用
        var sugarIf = typeof(ISugarService<>);
        foreach (var i in implType.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == sugarIf)
            {
                var entity = i.GetGenericArguments()[0];
                if (entity.IsClass && !entity.IsAbstract)
                    _entities.TryAdd(entity.Name, entity);
            }
        }

        foreach (var i in implType.GetInterfaces())
        {
            if (!marker.IsAssignableFrom(i)) continue;
            if (markers.Contains(i)) continue;
            if (!entry.Aliases.Contains(i.Name)) entry.Aliases.Add(i.Name);
        }

        _entries.Add(entry);
        TryRegister(entry.Name, entry);
        foreach (var a in entry.Aliases) TryRegister(a, entry);
    }

    private void TryRegister(string name, RpcServiceEntry entry)
    {
        if (ReservedNames.Contains(name, StringComparer.OrdinalIgnoreCase)) return;
        _byName.TryAdd(name, entry);
    }

    /// <summary>按实现类名/接口名查找（不区分大小写）</summary>
    public RpcServiceEntry Find(string name)
        => string.IsNullOrWhiteSpace(name) ? null
           : _byName.TryGetValue(name.Trim(), out var e) ? e : null;

    public IReadOnlyList<RpcServiceEntry> Entries => _entries;

    /// <summary>扫描到的平台库强类型实体（实体类名 → 类型，不区分大小写），供 eval 实体查询门面使用</summary>
    public IReadOnlyDictionary<string, Type> Entities => _entities;

    /// <summary>
    /// 列出服务元信息（给设计器服务浏览面板用）：服务名、别名、公共方法签名。
    /// </summary>
    public List<object> Describe()
    {
        return _entries.Select(e => (object)new
        {
            name = e.Name,
            aliases = e.Aliases,
            serviceType = e.ServiceType.Name,
            methods = GetCallableMethods(e.ImplType).Select(m => new
            {
                name = m.Name,
                returnType = FriendlyName(m.ReturnType),
                parameters = m.GetParameters().Select(p => new
                {
                    name = p.Name,
                    type = FriendlyName(p.ParameterType),
                    optional = p.IsOptional
                }).ToList()
            }).ToList()
        }).ToList();
    }

    /// <summary>公共实例方法（排除 System.Object 与特殊名称），用于方法目录展示与反射调用</summary>
    public static MethodInfo[] GetCallableMethods(Type implType)
        => implType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(object) && !m.IsSpecialName)
            .ToArray();

    private static string FriendlyName(Type t)
    {
        if (t == null) return "void";
        if (t.IsGenericType)
        {
            var name = t.Name.Contains('`') ? t.Name[..t.Name.IndexOf('`')] : t.Name;
            var args = t.GetGenericArguments().Select(FriendlyName);
            return $"{name}<{string.Join(", ", args)}>";
        }
        return t.Name;
    }
}
