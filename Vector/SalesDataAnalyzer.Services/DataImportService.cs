using SalesDataAnalyzer.Data.Repositories;
using SalesDataAnalyzer.Models;
using SalesDataAnalyzer.Services.XmlParsers;
using SalesDataAnalyzer.Services.VectorDb;
using System.Text.Json;

namespace SalesDataAnalyzer.Services;

public class DataImportService
{
    private readonly ISalesRepository _salesRepository;
    private readonly IChromaService _chromaService;
    private const string CollectionName = "sales_data_collection";

    public DataImportService(ISalesRepository salesRepository, IChromaService chromaService)
    {
        _salesRepository = salesRepository;
        _chromaService = chromaService;
    }

    public async Task ImportCategoryDataAsync(string xmlFilePath)
    {
        var xmlContent = await File.ReadAllTextAsync(xmlFilePath);
        var categories = CategoryXmlParser.Parse(xmlContent);
        await _salesRepository.AddSalesCategoriesAsync(categories);
        await _salesRepository.SaveChangesAsync();

        foreach (var category in categories)
        {
            await AddToVectorDb(category);
        }
    }

    public async Task ImportHourlyDataAsync(string xmlFilePath)
    {
        var xmlContent = await File.ReadAllTextAsync(xmlFilePath);
        var hourlySales = HourlyXmlParser.Parse(xmlContent);
        await _salesRepository.AddHourlySalesAsync(hourlySales);
        await _salesRepository.SaveChangesAsync();

        foreach (var hourly in hourlySales)
        {
            await AddToVectorDb(hourly);
        }
    }

    public async Task ImportSummaryDataAsync(string xmlFilePath)
    {
        var xmlContent = await File.ReadAllTextAsync(xmlFilePath);
        var summaries = SummaryXmlParser.Parse(xmlContent);
        await _salesRepository.AddSalesSummariesAsync(summaries);
        await _salesRepository.SaveChangesAsync();

        foreach (var summary in summaries)
        {
            await AddToVectorDb(summary);
        }
    }

    public async Task ImportPaymentMethodDataAsync(string xmlFilePath)
    {
        var xmlContent = await File.ReadAllTextAsync(xmlFilePath);
        var paymentMethods = PaymentMethodParser.Parse(xmlContent);
        await _salesRepository.AddPaymentMethodsAsync(paymentMethods);
        await _salesRepository.SaveChangesAsync();
    }

    public async Task ImportAllDataAsync(string xmlDirectoryPath)
    {
        await _chromaService.InitializeCollectionAsync(CollectionName);

        var categoryPath = Path.Combine(xmlDirectoryPath, "category.xml");
        var hourlyPath = Path.Combine(xmlDirectoryPath, "hourly.xml");
        var summaryPath = Path.Combine(xmlDirectoryPath, "summary.xml");

        if (File.Exists(categoryPath))
            await ImportCategoryDataAsync(categoryPath);

        if (File.Exists(hourlyPath))
            await ImportHourlyDataAsync(hourlyPath);

        if (File.Exists(summaryPath))
            await ImportSummaryDataAsync(summaryPath);
    }

    public async Task<List<(string Document, float Score, Dictionary<string, string> Metadata)>> AnalyzeSalesDataAsync(string query, string? siteName = null)
    {
        return await _chromaService.QuerySimilarDocumentsAsync(CollectionName, query, siteName);
    }

    public async Task<List<string>> GetAllSitesAsync()
    {
        return await _chromaService.GetAllSitesAsync(CollectionName);
    }

    public async Task<IEnumerable<SalesCategory>> GetTopSalesCategoriesAsync(int topN = 10)
    {
        return await _salesRepository.GetTopSalesCategoriesAsync(topN);
    }

    public async Task<IEnumerable<SalesCategory>> GetTopSalesCategoriesAsync(int siteId, int topN = 10)
    {
        return await _salesRepository.GetTopSalesCategoriesAsync(siteId, topN);
    }

    private async Task AddToVectorDb(SalesCategory category)
    {
        var document = BuildDocumentText(category);
        var metadata = new Dictionary<string, string>
        {
            { "site_id", category.SiteId.ToString() },
            { "site_name", category.SiteName ?? "" },
            { "category_name", category.CategoryName },
            { "data_type", "SalesCategory" },
            { "period_begin_date", category.PeriodBeginDate.ToString("o") },
            { "net_sales_amount", category.NetSalesAmount.ToString() }
        };

        var documentId = await _chromaService.AddDocumentAsync(CollectionName, document, metadata);

        var vectorData = new VectorData
        {
            SiteId = category.SiteId,
            SiteName = category.SiteName,
            PeriodDate = category.PeriodBeginDate.Date,
            DataType = "SalesCategory",
            MetadataJson = JsonSerializer.Serialize(metadata),
            ChromaDocumentId = documentId
        };

        await _salesRepository.AddVectorDataAsync(vectorData);
        await _salesRepository.SaveChangesAsync();
    }

    private async Task AddToVectorDb(HourlySales hourly)
    {
        var document = BuildDocumentText(hourly);
        var metadata = new Dictionary<string, string>
        {
            { "site_id", hourly.SiteId.ToString() },
            { "site_name", hourly.SiteName ?? "" },
            { "hour", hourly.Hour.ToString() },
            { "data_type", "HourlySales" },
            { "period_begin_date", hourly.PeriodBeginDate.ToString("o") },
            { "total_amount", (hourly.MerchOnlyAmount + hourly.MerchFuelAmount + hourly.FuelOnlyAmount).ToString() }
        };

        var documentId = await _chromaService.AddDocumentAsync(CollectionName, document, metadata);

        var vectorData = new VectorData
        {
            SiteId = hourly.SiteId,
            SiteName = hourly.SiteName,
            PeriodDate = hourly.PeriodBeginDate.Date,
            DataType = "HourlySales",
            MetadataJson = JsonSerializer.Serialize(metadata),
            ChromaDocumentId = documentId
        };

        await _salesRepository.AddVectorDataAsync(vectorData);
        await _salesRepository.SaveChangesAsync();
    }

    private async Task AddToVectorDb(SalesSummary summary)
    {
        var document = BuildDocumentText(summary);
        var metadata = new Dictionary<string, string>
        {
            { "site_id", summary.SiteId.ToString() },
            { "site_name", summary.SiteName ?? "" },
            { "data_type", "SalesSummary" },
            { "period_begin_date", summary.PeriodBeginDate.ToString("o") },
            { "net_sales", summary.NetSales.ToString() },
            { "fuel_sales", summary.FuelSales.ToString() },
            { "merch_sales", summary.MerchSales.ToString() }
        };

        var documentId = await _chromaService.AddDocumentAsync(CollectionName, document, metadata);

        var vectorData = new VectorData
        {
            SiteId = summary.SiteId,
            SiteName = summary.SiteName,
            PeriodDate = summary.PeriodBeginDate.Date,
            DataType = "SalesSummary",
            MetadataJson = JsonSerializer.Serialize(metadata),
            ChromaDocumentId = documentId
        };

        await _salesRepository.AddVectorDataAsync(vectorData);
        await _salesRepository.SaveChangesAsync();
    }

    private string BuildDocumentText(SalesCategory category)
    {
        // 门店: ...，销售类别: ...，销售额: ...，销售数量: ...，占比: ...
        return $"Site: {category.SiteName ?? category.SiteId.ToString()}, Sales Category: {category.CategoryName}, Sales Amount: {category.NetSalesAmount:C}, Sales Count: {category.NetSalesCount}, Percentage: {category.PercentOfSales}%";
    }

    private string BuildDocumentText(HourlySales hourly)
    {
        var totalAmount = hourly.MerchOnlyAmount + hourly.MerchFuelAmount + hourly.FuelOnlyAmount;
        // 门店: ...，时段: ...点，商品销售: ...，商品加油: ...，纯加油: ...，总计: ...
        return $"Site: {hourly.SiteName ?? hourly.SiteId.ToString()}, Period: {hourly.Hour} o'clock, Merch Sales: {hourly.MerchOnlyAmount:C}, Merch+Fuel: {hourly.MerchFuelAmount:C}, Fuel Only: {hourly.FuelOnlyAmount:C}, Total: {totalAmount:C}";
    }

    private string BuildDocumentText(SalesSummary summary)
    {
        // 门店: ...，销售汇总: 净销售额 ...，商品销售 ...，加油销售 ...，顾客数 ...，商品数 ...
        return $"Site: {summary.SiteName ?? summary.SiteId.ToString()}, Sales Summary: Net Sales {summary.NetSales:C}, Merch Sales {summary.MerchSales:C}, Fuel Sales {summary.FuelSales:C}, Customers {summary.CustomerCount}, Items {summary.ItemCount}";
    }
}