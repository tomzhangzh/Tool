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

    public async Task<List<(string Document, float Score, Dictionary<string, string> Metadata)>> AnalyzeSalesDataAsync(string query)
    {
        return await _chromaService.QuerySimilarDocumentsAsync(CollectionName, query);
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
            { "SiteId", category.SiteId.ToString() },
            { "CategoryName", category.CategoryName },
            { "DataType", "SalesCategory" },
            { "PeriodBeginDate", category.PeriodBeginDate.ToString("o") },
            { "NetSalesAmount", category.NetSalesAmount.ToString() }
        };

        var documentId = await _chromaService.AddDocumentAsync(CollectionName, document, metadata);
        
        var vectorData = new VectorData
        {
            SiteId = category.SiteId,
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
            { "SiteId", hourly.SiteId.ToString() },
            { "Hour", hourly.Hour.ToString() },
            { "DataType", "HourlySales" },
            { "PeriodBeginDate", hourly.PeriodBeginDate.ToString("o") },
            { "TotalAmount", (hourly.MerchOnlyAmount + hourly.MerchFuelAmount + hourly.FuelOnlyAmount).ToString() }
        };

        var documentId = await _chromaService.AddDocumentAsync(CollectionName, document, metadata);
        
        var vectorData = new VectorData
        {
            SiteId = hourly.SiteId,
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
            { "SiteId", summary.SiteId.ToString() },
            { "DataType", "SalesSummary" },
            { "PeriodBeginDate", summary.PeriodBeginDate.ToString("o") },
            { "NetSales", summary.NetSales.ToString() },
            { "FuelSales", summary.FuelSales.ToString() },
            { "MerchSales", summary.MerchSales.ToString() }
        };

        var documentId = await _chromaService.AddDocumentAsync(CollectionName, document, metadata);
        
        var vectorData = new VectorData
        {
            SiteId = summary.SiteId,
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
        return $"销售类别: {category.CategoryName}，销售额: {category.NetSalesAmount:C}，销售数量: {category.NetSalesCount}，占比: {category.PercentOfSales}%";
    }

    private string BuildDocumentText(HourlySales hourly)
    {
        var totalAmount = hourly.MerchOnlyAmount + hourly.MerchFuelAmount + hourly.FuelOnlyAmount;
        return $"时段: {hourly.Hour}点，商品销售: {hourly.MerchOnlyAmount:C}，商品加油: {hourly.MerchFuelAmount:C}，纯加油: {hourly.FuelOnlyAmount:C}，总计: {totalAmount:C}";
    }

    private string BuildDocumentText(SalesSummary summary)
    {
        return $"销售汇总: 净销售额 {summary.NetSales:C}，商品销售 {summary.MerchSales:C}，加油销售 {summary.FuelSales:C}，顾客数 {summary.CustomerCount}，商品数 {summary.ItemCount}";
    }
}