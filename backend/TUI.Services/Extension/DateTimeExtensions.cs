using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.Extension
{
    public static class DateTimeExtensions
    {
       

        public static DateTime FirstDayOfMonth(this DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }

        public static DateTime LastDayOfMonth(this DateTime date)
        {
            return date.FirstDayOfMonth().AddMonths(1).AddDays(-1);
        }

        public static int NrOfDaysInMonth(this DateTime date)
        {
            return date.LastDayOfMonth().Day;
        }

        public static bool IsLastDayOfMonth(this DateTime date)
        {
            return date == date.LastDayOfMonth();
        }

        public static DateTime FirstDayOfYear(this DateTime date)
        {
            return new DateTime(date.Year, 1, 1);
        }

        public static DateTime LastDayOfYear(this DateTime date)
        {
            return new DateTime(date.Year, 12, 31);
        }

        public static DayOfWeek FirstWeekDayOfMonth(this DateTime date)
        {
            var firstDay = date.FirstDayOfMonth();
            return firstDay.DayOfWeek;
        }
        public static int DaysInMonth(this DateTime date)
        {
            int days = DateTime.DaysInMonth(date.Year, date.Month);
            return days;
        }
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
        public static string GetJDEDate(this DateTime date)
        {
            return string.Format("{0:000}{1:000}", date.Year - 1900, date.DayOfYear);
        }
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
        public static DateTime ToEasternStandardTimeUTC (this DateTime date)
        {
            var clientZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(date, clientZone);
            return utcTime;
        }
        public static DateTime UtcToEasternStandardTime (this DateTime utcTimestamp)
        {
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, easternZone);
        }
    }
}
