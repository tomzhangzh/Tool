using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 类模式输入源：把 C# 类（实体 / DTO / record）反射成与「数据库表 schema」同构的列元数据，
/// 从而复用同一套字段推断与三屏组装规则（<see cref="PageFieldInference.BuildColumns"/>）。
///
/// 与表名模式的区别只在「元数据来源」：
///   - 表名：读 SQLite schema（列类型/可空/默认值/注释）
///   - 类名：读属性类型 + 数据注解（[Display(Name)] / [Required] / [Key]）
/// 两者最终都产出 List&lt;ColumnMeta&gt;，生成逻辑只写一遍。
///
/// 约定：只生成页面配置，不建表、不做 DDL；建表仍由用户自己的 SQL / 迁移负责。
/// </summary>
internal static class ClassModelReader
{
    /// <summary>候选类（供向导下拉选择）。</summary>
    public sealed class ClassOption
    {
        public string FullName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Namespace { get; set; } = "";
        public int PropCount { get; set; }
    }

    /// <summary>类 → 列元数据；类不存在或无可用属性时返回空列表。</summary>
    public static List<ColumnMeta> ReadSchema(string className)
    {
        var type = ResolveType(className);
        if (type == null) return new List<ColumnMeta>();

        var list = new List<ColumnMeta>();
        var simple = type.Name;
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length > 0) continue;      // 索引器
            if (p.GetMethod == null) continue;                    // 只写属性
            if (p.Name == "Item") continue;
            var dbType = MapType(p.PropertyType);
            if (dbType == null) continue;                         // 导航属性/集合/复杂对象：不进三屏

            var label = ReadLabel(p);
            var required = p.GetCustomAttribute<RequiredAttribute>() != null;
            var isValueType = p.PropertyType.IsValueType;
            var isNullable = isValueType && Nullable.GetUnderlyingType(p.PropertyType) != null;
            var notNull = required || (isValueType && !isNullable);

            list.Add(new ColumnMeta
            {
                Name = p.Name,
                Label = label,
                Type = dbType,
                NotNull = notNull,
                IsPrimaryKey = IsPrimaryKey(p, simple),
                HasDefault = false,
                FkTable = ReadFkTable(p, simple),
                DefaultValue = null
            });
        }
        return list;
    }

    /// <summary>列出可选的类（仅 VueLibV4 自有程序集，避免扫描系统类型）。</summary>
    public static List<ClassOption> ListClasses(string? keyword)
    {
        var kw = (keyword ?? "").Trim();
        var result = new List<ClassOption>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            var an = asm.GetName().Name ?? "";
            if (!an.StartsWith("VueLibV4", StringComparison.OrdinalIgnoreCase)) continue;
            Type[] types;
            try { types = asm.GetTypes(); } catch { continue; }
            foreach (var t in types)
            {
                if (!t.IsClass || !t.IsPublic || t.IsAbstract || t.IsGenericTypeDefinition) continue;
                if (t.Namespace == null) continue;
                if (kw.Length > 0 && t.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0
                    && t.FullName!.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var usable = props.Count(p => p.GetMethod != null && MapType(p.PropertyType) != null);
                if (usable == 0) continue;
                result.Add(new ClassOption
                {
                    FullName = t.FullName ?? t.Name,
                    Name = t.Name,
                    Namespace = t.Namespace,
                    PropCount = usable
                });
                if (result.Count >= 500) return result;
            }
        }
        return result.OrderBy(x => x.Namespace).ThenBy(x => x.Name).ToList();
    }

    /// <summary>取类的简单名（类模式未指定表名时用作 TableName 兜底）。</summary>
    public static string SimpleName(string className)
    {
        var t = ResolveType(className);
        return t?.Name ?? className.Split('.').LastOrDefault() ?? className;
    }

    /// <summary>按全名或简单名解析类型（简单名重名时优先 Models 命名空间）。</summary>
    public static Type? ResolveType(string className)
    {
        var name = (className ?? "").Trim();
        if (name.Length == 0) return null;
        var all = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            Type[] types;
            try { types = asm.GetTypes(); } catch { continue; }
            foreach (var t in types)
            {
                if (!t.IsClass) continue;
                if (t.FullName == name) return t;
                if (t.Name == name) all.Add(t);
            }
        }
        return all.FirstOrDefault(t => (t.Namespace ?? "").Contains("Model", StringComparison.OrdinalIgnoreCase))
               ?? all.FirstOrDefault();
    }

    /// <summary>属性 → 库列类型（与 BuildColumns 的类型关键字约定一致：TEXT/INTEGER/REAL/DATETIME）。
    /// 返回 null 表示该属性不参与生成（导航属性、集合、byte[] 等）。</summary>
    private static string? MapType(Type t)
    {
        if (t.IsEnum) return "INTEGER";
        var u = Nullable.GetUnderlyingType(t) ?? t;
        if (u == typeof(string) || u == typeof(char) || u == typeof(Guid)) return "TEXT";
        if (u == typeof(bool)) return "INTEGER";
        if (u == typeof(byte) || u == typeof(sbyte) || u == typeof(short) || u == typeof(ushort)
            || u == typeof(int) || u == typeof(uint) || u == typeof(long) || u == typeof(ulong)) return "INTEGER";
        if (u == typeof(decimal) || u == typeof(double) || u == typeof(float)) return "REAL";
        if (u == typeof(DateTime) || u == typeof(DateTimeOffset) || u == typeof(TimeSpan)
            || u.Name == "DateOnly" || u.Name == "TimeOnly") return "DATETIME";
        return null;   // 集合 / 复杂对象 / 文件流：跳过
    }

    /// <summary>[Display(Name)] → [Description] → 空（由 NiceLabel 拆词兜底）。</summary>
    private static string ReadLabel(PropertyInfo p)
    {
        var display = p.GetCustomAttribute<DisplayAttribute>();
        if (!string.IsNullOrWhiteSpace(display?.Name)) return display!.Name!.Trim();
        var desc = p.GetCustomAttribute<DescriptionAttribute>();
        if (!string.IsNullOrWhiteSpace(desc?.Description)) return desc!.Description!.Trim();
        return "";
    }

    private static bool IsPrimaryKey(PropertyInfo p, string simpleName)
    {
        if (p.GetCustomAttribute<KeyAttribute>() != null) return true;
        return p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || p.Name.Equals(simpleName + "Id", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>外键推断：XxxId 形式且不是自身主键 → 目标表名存疑，返回 null 交由用户指定。</summary>
    private static string? ReadFkTable(PropertyInfo p, string simpleName)
    {
        _ = p; _ = simpleName;
        return null;
    }
}
