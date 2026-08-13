using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.DBModel
{
    public class PriceEntry
    {
        public long BrandID { get; set; }
        public long ProductID { get; set; }
        public decimal Price { get; set; }
        public DateTimeOffset Updated { get; set; }
        public long EffectiveTime { get; set; }
        public string ReportVariableName { get; set; }
    }

    public enum PriceBrandEnum
    {
        Chevron = 1,
        Citgo = 2,
        Marathon = 3,
        MarathonDTW = 4,
        Shell = 5,
        ExxonMobil = 6,
        MarathonRecAndDyed = 7,
        ChevronDyed = 8,
        ChevronRec = 9,
        ShellRec = 10,
        Texaco = 11,
        BpPort = 12,
        BpTaft = 13,
        BpTampa = 14,
        ChevronTaft = 15,
        ChevronTampa = 16,
        ChevronPort = 17,
        Unbranded = 18
    }
    public static class DateTimeExtension
    {
        public static DateTime GetEasternTime()
        {
            string easternZoneId = "Eastern Standard Time";
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById(easternZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
        }

        public static DateTime GetEasternTime(this DateTime utcTimestamp)
        {
            string easternZoneId = "Eastern Standard Time";
            var easternZone = TimeZoneInfo.FindSystemTimeZoneById(easternZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, easternZone);
        }
    }
}
