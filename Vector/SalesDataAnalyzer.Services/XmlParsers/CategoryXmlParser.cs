using System.Xml.Linq;
using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Services.XmlParsers;

public static class CategoryXmlParser
{
    public static List<SalesCategory> Parse(string xmlContent)
    {
        var categories = new List<SalesCategory>();
        var doc = XDocument.Parse(xmlContent);
        
        XNamespace pdNs = "urn:vfi-sapphire:pd.2002-05-21";
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
        
        foreach (var categoryInfo in doc.Descendants("categoryInfo"))
        {
            var categoryBase = categoryInfo.Element(vsNs + "categoryBase");
            var netSales = categoryInfo.Element("netSales");
            
            var category = new SalesCategory
            {
                SiteId = siteId,
                CategorySysId = categoryBase?.Attribute("sysid") != null ? int.Parse(categoryBase.Attribute("sysid")!.Value) : 0,
                CategoryName = categoryBase?.Element("name")?.Value ?? string.Empty,
                NetSalesCount = netSales?.Element("count") != null ? int.Parse(netSales.Element("count")!.Value) : 0,
                NetSalesAmount = netSales?.Element("amount") != null ? decimal.Parse(netSales.Element("amount")!.Value) : 0,
                NetSalesItemCount = netSales?.Element("itemCount") != null ? decimal.Parse(netSales.Element("itemCount")!.Value) : 0,
                PercentOfSales = categoryInfo.Element("percentOfSales") != null ? decimal.Parse(categoryInfo.Element("percentOfSales")!.Value) : 0,
                PeriodBeginDate = periodBeginDate,
                PeriodEndDate = periodEndDate
            };
            
            categories.Add(category);
        }
        
        return categories;
    }
}