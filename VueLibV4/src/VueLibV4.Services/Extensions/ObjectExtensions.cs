using System.ComponentModel;
using System.Reflection;

namespace VueLibV4.Services.Extensions;

/// <summary>对象通用扩展：深/浅拷贝、类型转换、属性读写、字符串修剪等</summary>
public static class ObjectExtensions
{
    /// <summary>JSON 深拷贝（跨实体/DTO 复制最稳妥，要求对象可 JSON 往返）</summary>
    public static T Clone<T>(this T source)
    {
        if (source == null) return default;
        return source.ToJson().FromJson<T>();
    }

    /// <summary>浅拷贝：逐属性复制同名列（含值类型/字符串/引用赋值，嵌套对象不复制）</summary>
    public static T ShallowCopy<T>(this T source) where T : class, new()
    {
        if (source == null) return null;
        var target = new T();
        foreach (var p in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.CanRead && p.CanWrite)
                p.SetValue(target, p.GetValue(source, null), null);
        }
        return target;
    }

    /// <summary>将同名字段从 source 复制到已存在的 target（用于编辑前合并提交数据）</summary>
    public static TTarget CopyTo<TSource, TTarget>(this TSource source, TTarget target)
        where TSource : class where TTarget : class
    {
        if (source == null || target == null) return target;
        var sourceProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var tp in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!tp.CanWrite || !sourceProps.TryGetValue(tp.Name, out var sp) || !sp.CanRead) continue;
            if (!tp.PropertyType.IsAssignableFrom(sp.PropertyType)) continue;
            tp.SetValue(target, sp.GetValue(source, null), null);
        }
        return target;
    }

    /// <summary>转换为目标类型（自动处理 Nullable），转换失败抛异常</summary>
    public static object ChangeType(this object value, Type conversionType)
    {
        if (conversionType == null) throw new ArgumentNullException(nameof(conversionType));
        if (value == null) return null;
        if (conversionType == typeof(object)) return value;
        if (conversionType.IsInstanceOfType(value)) return value;

        var t = conversionType;
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            if (value is string s && string.IsNullOrWhiteSpace(s)) return null;
            t = Nullable.GetUnderlyingType(t);
        }

        if (t.IsEnum)
        {
            if (value is int ei) return Enum.ToObject(t, ei);
            return Enum.Parse(t, value.ToString(), true);
        }

        if (t == typeof(Guid) && value is string gs) return Guid.Parse(gs);

        var converter = TypeDescriptor.GetConverter(t);
        if (converter != null && converter.CanConvertFrom(value.GetType()))
            return converter.ConvertFrom(value);
        return Convert.ChangeType(value, t);
    }

    /// <summary>转换为目标类型；失败返回 default(T) 而不抛异常</summary>
    public static T ChangeType<T>(this object value)
    {
        try { return (T)value.ChangeType(typeof(T)); }
        catch { return default; }
    }

    /// <summary>按 "A.B.C" 路径读取嵌套属性</summary>
    public static object GetPropertyValue(this object obj, string path)
    {
        if (obj == null || string.IsNullOrWhiteSpace(path)) return null;
        var current = obj;
        foreach (var name in path.Split('.'))
        {
            if (current == null) return null;
            var prop = current.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) return null;
            current = prop.GetValue(current, null);
        }
        return current;
    }

    /// <summary>按属性名写入值（自动类型转换）</summary>
    public static void SetPropertyValue(this object obj, string propName, object value)
    {
        if (obj == null || string.IsNullOrWhiteSpace(propName)) return;
        var prop = obj.GetType().GetProperty(propName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null || !prop.CanWrite) return;
        prop.SetValue(obj, value.ChangeType(prop.PropertyType), null);
    }

    /// <summary>修剪对象所有可写字符串属性（提交前规整）</summary>
    public static void TrimStringProperties(this object obj)
    {
        if (obj == null) return;
        foreach (var p in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.PropertyType != typeof(string) || !p.CanWrite || !p.CanRead) continue;
            if (p.GetValue(obj, null) is string s) p.SetValue(obj, s.Trim(), null);
        }
    }
}
