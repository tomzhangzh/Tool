using System.Xml.Linq;
using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Services.XmlParsers;

public static class HourlyXmlParser
{
    public static List<HourlySales> Parse(string xmlContent)
    {
        var hourlySalesList = new List<HourlySales>();
        var doc = XDocument.Parse(xmlContent);
        
        XNamespace vsNs = "urn:vfi-sapphire:vs.2001-10-01";
        
        var periodBeginDate = DateTime.MinValue;
        var periodEndDate = DateTime.MinValue;
        var siteId = 0;
        
        var periodElement = doc.Descendants(vsNs + "period").FirstOrDefault(p => p.Attribute("periodType")?.Value == "day");
        if (periodElement != null)
        {
            if (DateTime.TryParse(periodElement.Attribute("periodBeginDate")?.Value, out var beginDate))
                periodBeginDate = beginDate;
            if (DateTime.TryParse(periodElement.Attribute("periodEndDate")?.Value, out var endDate))
                periodEndDate = endDate;
        }
        
        var siteElement = doc.Descendants(vsNs + "site").FirstOrDefault();
        if (siteElement != null && int.TryParse(siteElement.Value, out var site))
            siteId = site;
        
        foreach (var hourlyInfo in doc.Descendants("hourlyInfo"))
        {
            var merchOnly = hourlyInfo.Element("merchOnly");
            var merchFuel = hourlyInfo.Element("merchFuel");
            var fuelOnly = hourlyInfo.Element("fuelOnly");
            
            var hourly = new HourlySales
            {
                SiteId = siteId,
                Hour = hourlyInfo.Element("hour") != null ? int.Parse(hourlyInfo.Element("hour")!.Value) : 0,
                ItemCount = hourlyInfo.Element("itemCount") != null ? decimal.Parse(hourlyInfo.Element("itemCount")!.Value) : 0,
                MerchOnlyCount = merchOnly?.Element("count") != null ? int.Parse(merchOnly.Element("count")!.Value) : 0,
                MerchOnlyAmount = merchOnly?.Element("amount") != null ? decimal.Parse(merchOnly.Element("amount")!.Value) : 0,
                MerchFuelCount = merchFuel?.Element("count") != null ? int.Parse(merchFuel.Element("count")!.Value) : 0,
                MerchFuelAmount = merchFuel?.Element("amount") != null ? decimal.Parse(merchFuel.Element("amount")!.Value) : 0,
                FuelOnlyCount = fuelOnly?.Element("count") != null ? int.Parse(fuelOnly.Element("count")!.Value) : 0,
                FuelOnlyAmount = fuelOnly?.Element("amount") != null ? decimal.Parse(fuelOnly.Element("amount")!.Value) : 0,
                PeriodBeginDate = periodBeginDate,
                PeriodEndDate = periodEndDate
            };
            
            hourlySalesList.Add(hourly);
        }
        
        return hourlySalesList;
    }
}