// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
///  DateTime 扩展
/// </summary>
public static class DateTimeExtension
{
    /// <summary>
    /// 返回格式为(yyyy-MM-dd HH:mm:ss)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringLongDate(this DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd HH:mm:ss)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringLongDate(this DateTime? dt)
    {
        return dt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringDate(this DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringDate(this DateTime? dt)
    {
        return dt?.ToString("yyyy-MM-dd") ?? "";
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd HH:mm:ss)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringLongDate(this DateTimeOffset dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd HH:mm:ss)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringLongDate(this DateTimeOffset? dt)
    {
        return dt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringDate(this DateTimeOffset dt)
    {
        return dt.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// 返回格式为(yyyy-MM-dd)的时间字符串
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static string ToStringDate(this DateTimeOffset? dt)
    {
        return dt?.ToString("yyyy-MM-dd") ?? "";
    }

    /// <summary>
    /// 获取时间戳  13位
    /// </summary>
    /// <returns></returns>
    public static long ToTimeStamp(this DateTime dateTime)
    {
        var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds * 1000);
    }

    /// <summary>
    /// 获取时间戳  13位
    /// </summary>
    /// <returns></returns>
    public static long ToTimeStamp(this DateTimeOffset dateTime)
    {
        var ts = dateTime.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds * 1000);
    }

    /// <summary>
    /// 获取时间戳 10位
    /// </summary>
    /// <returns></returns>
    public static long ToTimeStampTen()
    {
        return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
    }

    /// <summary>
    /// 将时间戳转换为日期类型，并格式化
    /// </summary>
    /// <param name="longDateTime"></param>
    /// <returns>yyyy-MM-dd HH:mm:ss 的字符串</returns>
    private static string LongDateTimeToDateTimeString(this string longDateTime)
    {
        //用来格式化long类型时间的,声明的变量
        long unixDate;
        DateTime start;
        DateTime date;
        //ENd

        unixDate = long.Parse(longDateTime);
        start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        date = start.AddMilliseconds(unixDate).ToLocalTime();

        return date.ToString("yyyy-MM-dd HH:mm:ss");
    }

    #region ToDayEndTime DateTimeOffset

    /// <summary>
    /// 一天的结束时间  23:59:59
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTimeOffset ToDayEndTime(this DateTimeOffset dt)
    {
        return DateTimeOffset.Parse($"{dt:yyyy-MM-dd} 23:59:59");
    }

    /// <summary>
    /// 一天的结束时间  23:59:59
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTimeOffset ToDayEndTime(this DateTimeOffset? dt)
    {
        return DateTimeOffset.Parse($"{dt:yyyy-MM-dd} 23:59:59");
    }

    /// <summary>
    /// 一天的开始时间  00:00:00
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTimeOffset ToDayStartTime(this DateTimeOffset dt)
    {
        return DateTimeOffset.Parse($"{dt:yyyy-MM-dd} 00:00:00");
    }

    /// <summary>
    /// 一天的开始时间  00:00:00
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTimeOffset ToDayStartTime(this DateTimeOffset? dt)
    {
        return DateTimeOffset.Parse($"{dt:yyyy-MM-dd} 00:00:00");
    }

    #endregion ToDayEndTime DateTimeOffset

    #region ToDayEndTime DateTime

    /// <summary>
    /// 一天的结束时间  23:59:59
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTime ToDayEndTime(this DateTime dt)
    {
        return Convert.ToDateTime($"{dt:yyyy-MM-dd} 23:59:59");
    }

    /// <summary>
    /// 一天的结束时间  23:59:59
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTime ToDayEndTime(this DateTime? dt)
    {
        return Convert.ToDateTime($"{dt:yyyy-MM-dd} 23:59:59");
    }

    /// <summary>
    /// 一天的开始时间  00:00:00
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTime ToDayStartTime(this DateTime dt)
    {
        return Convert.ToDateTime($"{dt:yyyy-MM-dd} 00:00:00");
    }

    /// <summary>
    /// 一天的开始时间  00:00:00
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static DateTime ToDayStartTime(this DateTime? dt)
    {
        return Convert.ToDateTime($"{dt:yyyy-MM-dd} 00:00:00");
    }

    #endregion ToDayEndTime DateTime
}