
// DateTime扩展类
namespace TUI.Utils.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// 获取当前月份的第一天
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份的第一天</returns>
        public static DateTime FirstDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);



        }

        /// <summary>
        /// 获取当前月份的最后一天
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份的最后一天</returns>
        public static DateTime LastDayOfMonth(this DateTime date)
        {


            
            return date.FirstDayOfMonth().AddMonths(1).AddDays(-1);
        }

        /// <summary>
        /// 获取当前月份的天数
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份的天数</returns>
        public static int NrOfDaysInMonth(this DateTime date)
        {
            return date.LastDayOfMonth().Day;
        }

        /// <summary>
        /// 判断当前日期是否为当前月份的最后一天
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>是否为当前月份的最后一天</returns>
        public static bool IsLastDayOfMonth(this DateTime date)
        {
            return date == date.LastDayOfMonth();
        }

        /// <summary>
        /// 获取当前年份的第一天
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前年份的第一天</returns>
        public static DateTime FirstDayOfYear(this DateTime date)
        {
            return new DateTime(date.Year, 1, 1);
        }

        /// <summary>
        /// 获取当前年份的最后一天
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前年份的最后一天</returns>
        public static DateTime LastDayOfYear(this DateTime date)
        {
            return new DateTime(date.Year, 12, 31);
        }

        /// <summary>
        /// 获取当前月份的第一天是星期几
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份的第一天是星期几</returns>
        public static DayOfWeek FirstWeekDayOfMonth(this DateTime date)
        {
            var firstDay = date.FirstDayOfMonth();
            return firstDay.DayOfWeek;
        }

        /// <summary>
        /// 获取当前月份的天数
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份的天数</returns>
        public static int DaysInMonth(this DateTime date)
        {
            int days = DateTime.DaysInMonth(date.Year, date.Month);
            return days;
        }

        /// <summary>
        /// 获取当前日期所在季度
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前日期所在季度</returns>
        public static int GetQuarter(this DateTime date)
        {
            if (date.Month >= 1 && date.Month <= 3)
                return 1;
            else if (date.Month >= 4 && date.Month <= 6)
                return 2;
            else if (date.Month >= 7 && date.Month <= 9)
                return 3;
            else
                return 4;
        }

        /// <summary>
        /// 获取当前月份每周的起始日期和结束日期
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份每周的起始日期和结束日期</returns>
        public static Dictionary<DateTime, DateTime> WeekdaysOfMonth(this DateTime date)
        {
            var weeks = new Dictionary<DateTime, DateTime>();
            DateTime weekStart = date.FirstDayOfMonth();
            DateTime lastDayOfMonth = date.LastDayOfMonth();
            int dayOfWeek = weekStart.FirstWeekDayOfMonth().GetHashCode();
            DateTime weekEnd = weekStart.AddDays(6 - dayOfWeek);
            weeks.Add(weekStart, weekEnd);
            while (weekEnd.AddDays(1) <= lastDayOfMonth)
            {
                weekStart = weekEnd.AddDays(1);
                weekEnd = weekEnd.AddDays(7) > lastDayOfMonth ? lastDayOfMonth : weekEnd.AddDays(7);
                weeks.Add(weekStart, weekEnd);
            }
            return weeks;
        }

        /// <summary>
        /// 获取当前月份每周的星期几的天数
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>当前月份每周的星期几的天数</returns>
        public static Dictionary<DayOfWeek, int> WeekDaysOfMonth(this DateTime date)
        {
            var weeks = new Dictionary<DayOfWeek, int>();
            weeks.Add(DayOfWeek.Sunday, 0);
            weeks.Add(DayOfWeek.Monday, 0);
            weeks.Add(DayOfWeek.Tuesday, 0);
            weeks.Add(DayOfWeek.Wednesday, 0);
            weeks.Add(DayOfWeek.Thursday, 0);
            weeks.Add(DayOfWeek.Friday, 0);
            weeks.Add(DayOfWeek.Saturday, 0);
            DateTime firstDayOfMonth = date.FirstDayOfMonth();
            DateTime lastDayOfMonth = date.LastDayOfMonth();
            DateTime day = firstDayOfMonth;
            while (day <= lastDayOfMonth)
            {
                weeks[day.DayOfWeek]++;
                day = day.AddDays(1);
            }
            return weeks;
        }

        /// <summary>
        /// 将日期转换为东部标准时间的UTC时间
        /// </summary>
        /// <param name="date">当前日期</param>
        /// <returns>东部标准时间的UTC时间</returns>
        public static DateTime ToEasternStandardTimeUTC(this DateTime date)
        {
            var clientZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(date, clientZone);
            return utcTime;
        }

        /// <summary>
        /// 将UTC时间转换为东部标准时间
        /// </summary>
        /// <param name="utcTimestamp">UTC时间戳</param>
        /// <returns>东部标准时间</returns>
        public static DateTime UtcToEasternStandardTime(this DateTime utcTimestamp)
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, easternZone);
        }

    }
}

