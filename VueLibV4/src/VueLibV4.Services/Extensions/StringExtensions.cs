namespace VueLibV4.Services.Extensions;

/// <summary>字符串通用扩展</summary>
public static class StringExtensions
{
    public static bool IsNullOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);

    public static bool HasValue(this string s) => !string.IsNullOrWhiteSpace(s);

    /// <summary>忽略大小写相等</summary>
    public static bool EqualsIgnoreCase(this string s, string other)
        => string.Equals(s, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>忽略大小写包含</summary>
    public static bool ContainsIgnoreCase(this string s, string value)
        => s != null && value != null && s.Contains(value, StringComparison.OrdinalIgnoreCase);

    /// <summary>安全转 int（失败返回 fallback）</summary>
    public static int ToInt(this string s, int fallback = 0)
        => int.TryParse(s, out var v) ? v : fallback;

    /// <summary>安全转 long</summary>
    public static long ToLong(this string s, long fallback = 0)
        => long.TryParse(s, out var v) ? v : fallback;

    /// <summary>安全转 decimal</summary>
    public static decimal ToDecimal(this string s, decimal fallback = 0m)
        => decimal.TryParse(s, out var v) ? v : fallback;

    /// <summary>截断到指定长度（超出加省略号）</summary>
    public static string Truncate(this string s, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(s) || maxLength <= 0 || s.Length <= maxLength) return s;
        if (maxLength <= ellipsis.Length) return s.Substring(0, maxLength);
        return s.Substring(0, maxLength - ellipsis.Length) + ellipsis;
    }
}
