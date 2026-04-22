using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Data.Repositories;

public interface ISalesRepository
{
    Task AddSalesCategoriesAsync(IEnumerable<SalesCategory> categories);
    Task AddHourlySalesAsync(IEnumerable<HourlySales> hourlySales);
    Task AddSalesSummariesAsync(IEnumerable<SalesSummary> summaries);
    Task AddPaymentMethodsAsync(IEnumerable<PaymentMethod> paymentMethods);
    Task AddVectorDataAsync(VectorData vectorData);
    Task<IEnumerable<SalesCategory>> GetSalesCategoriesAsync(int siteId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<HourlySales>> GetHourlySalesAsync(int siteId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<SalesSummary>> GetSalesSummariesAsync(int siteId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<SalesCategory>> GetTopSalesCategoriesAsync(int topN = 10);
    Task<IEnumerable<SalesCategory>> GetTopSalesCategoriesAsync(int siteId, int topN = 10);
    Task<int> SaveChangesAsync();
}