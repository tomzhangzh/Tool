using System.Xml.Linq;
using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Services.XmlParsers;

public static class SummaryXmlParser
{
    public static List<SalesSummary> Parse(string xmlContent)
    {
        var summaries = new List<SalesSummary>();
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
        
        var totalsElement = doc.Descendants("totals").FirstOrDefault();
        if (totalsElement != null)
        {
            var summary = ParseSummaryElement(totalsElement, siteId, periodBeginDate, periodEndDate, null, null, null);
            if (summary != null)
                summaries.Add(summary);
        }
        
        foreach (var byRegister in doc.Descendants("byRegister"))
        {
            var registerId = byRegister.Element(vsNs + "register")?.Attribute("sysid") != null 
                ? int.Parse(byRegister.Element(vsNs + "register")!.Attribute("sysid")!.Value) 
                : (int?)null;
            
            var summaryInfo = byRegister.Descendants("summaryInfo").FirstOrDefault();
            if (summaryInfo != null)
            {
                var summary = ParseSummaryElement(byRegister, siteId, periodBeginDate, periodEndDate, registerId, null, null);
                if (summary != null)
                    summaries.Add(summary);
            }
        }
        
        return summaries;
    }
    
    private static SalesSummary? ParseSummaryElement(XElement element, int siteId, DateTime periodBeginDate, DateTime periodEndDate, int? registerId, int? cashierId, string? cashierName)
    {
        var totalizers = element.Descendants("totalizers").FirstOrDefault();
        var summaryInfo = element.Descendants("summaryInfo").FirstOrDefault();
        
        if (summaryInfo == null)
            return null;
        
        var startElement = totalizers?.Element("start");
        var endElement = totalizers?.Element("end");
        var diffElement = totalizers?.Element("difference");
        
        return new SalesSummary
        {
            SiteId = siteId,
            InsideGrandStart = startElement?.Element("insideGrand") != null ? decimal.Parse(startElement.Element("insideGrand")!.Value) : 0,
            InsideSalesStart = startElement?.Element("insideSales") != null ? decimal.Parse(startElement.Element("insideSales")!.Value) : 0,
            OutsideGrandStart = startElement?.Element("outsideGrand") != null ? decimal.Parse(startElement.Element("outsideGrand")!.Value) : 0,
            OutsideSalesStart = startElement?.Element("outsideSales") != null ? decimal.Parse(startElement.Element("outsideSales")!.Value) : 0,
            InsideGrandEnd = endElement?.Element("insideGrand") != null ? decimal.Parse(endElement.Element("insideGrand")!.Value) : 0,
            InsideSalesEnd = endElement?.Element("insideSales") != null ? decimal.Parse(endElement.Element("insideSales")!.Value) : 0,
            OutsideGrandEnd = endElement?.Element("outsideGrand") != null ? decimal.Parse(endElement.Element("outsideGrand")!.Value) : 0,
            OutsideSalesEnd = endElement?.Element("outsideSales") != null ? decimal.Parse(endElement.Element("outsideSales")!.Value) : 0,
            InsideGrandDifference = diffElement?.Element("insideGrand") != null ? decimal.Parse(diffElement.Element("insideGrand")!.Value) : 0,
            InsideSalesDifference = diffElement?.Element("insideSales") != null ? decimal.Parse(diffElement.Element("insideSales")!.Value) : 0,
            OutsideGrandDifference = diffElement?.Element("outsideGrand") != null ? decimal.Parse(diffElement.Element("outsideGrand")!.Value) : 0,
            OutsideSalesDifference = diffElement?.Element("outsideSales") != null ? decimal.Parse(diffElement.Element("outsideSales")!.Value) : 0,
            NetSales = summaryInfo.Element("netSales") != null ? decimal.Parse(summaryInfo.Element("netSales")!.Value) : 0,
            ItemCount = summaryInfo.Element("itemCount") != null ? int.Parse(summaryInfo.Element("itemCount")!.Value) : 0,
            CustomerCount = summaryInfo.Element("customerCount") != null ? int.Parse(summaryInfo.Element("customerCount")!.Value) : 0,
            NoSaleCount = summaryInfo.Element("noSaleCount") != null ? int.Parse(summaryInfo.Element("noSaleCount")!.Value) : 0,
            FuelSales = summaryInfo.Element("fuelSales") != null ? decimal.Parse(summaryInfo.Element("fuelSales")!.Value) : 0,
            MerchSales = summaryInfo.Element("merchSales") != null ? decimal.Parse(summaryInfo.Element("merchSales")!.Value) : 0,
            RegisterId = registerId,
            CashierId = cashierId,
            CashierName = cashierName,
            PeriodBeginDate = periodBeginDate,
            PeriodEndDate = periodEndDate
        };
    }
}