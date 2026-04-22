using System.Xml.Linq;
using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Services.XmlParsers;

public static class PaymentMethodParser
{
    public static List<PaymentMethod> Parse(string xmlContent)
    {
        var paymentMethods = new List<PaymentMethod>();
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
        
        foreach (var mopInfo in doc.Descendants("mopInfo"))
        {
            var payment = new PaymentMethod
            {
                SiteId = siteId,
                PaymentSysId = mopInfo.Attribute("sysid") != null ? int.Parse(mopInfo.Attribute("sysid")!.Value) : 0,
                PaymentName = mopInfo.Element("name")?.Value ?? string.Empty,
                IsCardBased = mopInfo.Attribute("isCardBased") != null && bool.Parse(mopInfo.Attribute("isCardBased")!.Value),
                SaleCount = mopInfo.Element("count") != null ? int.Parse(mopInfo.Element("count")!.Value) : 0,
                SaleAmount = mopInfo.Element("amount") != null ? decimal.Parse(mopInfo.Element("amount")!.Value) : 0,
                PeriodBeginDate = periodBeginDate,
                PeriodEndDate = periodEndDate
            };
            
            paymentMethods.Add(payment);
        }
        
        return paymentMethods;
    }
}