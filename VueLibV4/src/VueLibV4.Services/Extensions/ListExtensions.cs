namespace VueLibV4.Services.Extensions;

/// <summary>集合通用扩展</summary>
public static class ListExtensions
{
    /// <summary>条件成立才添加项（链式）</summary>
    public static ICollection<T> AddIf<T>(this ICollection<T> source, bool condition, T item)
    {
        if (condition) source.Add(item);
        return source;
    }

    /// <summary>不存在时才添加（按相等比较器/默认相等）</summary>
    public static bool AddIfNotContains<T>(this ICollection<T> source, T item)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source.Contains(item)) return false;
        source.Add(item);
        return true;
    }

    /// <summary>批量添加（null 集合安全）</summary>
    public static void AddRangeIfNotNull<T>(this ICollection<T> source, IEnumerable<T> items)
    {
        if (source == null || items == null) return;
        foreach (var i in items) source.Add(i);
    }

    /// <summary>按块切分（大数据分批处理）</summary>
    public static IEnumerable<List<T>> ChunkBy<T>(this IEnumerable<T> source, int chunkSize)
    {
        if (chunkSize < 1) throw new ArgumentOutOfRangeException(nameof(chunkSize), "块大小必须大于 0");
        var bucket = new List<T>(chunkSize);
        foreach (var item in source)
        {
            bucket.Add(item);
            if (bucket.Count == chunkSize)
            {
                yield return bucket;
                bucket = new List<T>(chunkSize);
            }
        }
        if (bucket.Count > 0) yield return bucket;
    }

    /// <summary>对每个元素执行动作（流畅式 foreach）</summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null || action == null) return;
        foreach (var item in source) action(item);
    }

    /// <summary>序列化为去重后的数组（null 安全）</summary>
    public static List<T> DistinctToList<T>(this IEnumerable<T> source)
        => source == null ? new List<T>() : source.Distinct().ToList();
}
