using Microsoft.EntityFrameworkCore;
using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Data.Repositories;

public class SalesRepository : ISalesRepository
{
    private readonly SalesDbContext _dbContext;

    public SalesRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddSalesCategoriesAsync(IEnumerable<SalesCategory> categories)
    {
        await _dbContext.SalesCategories.AddRangeAsync(categories);
    }

    public async Task AddHourlySalesAsync(IEnumerable<HourlySales> hourlySales)
    {
        await _dbContext.HourlySales.AddRangeAsync(hourlySales);
    }

    public async Task AddSalesSummariesAsync(IEnumerable<SalesSummary> summaries)
    {
        await _dbContext.SalesSummaries.AddRangeAsync(summaries);
    }

    public async Task AddPaymentMethodsAsync(IEnumerable<PaymentMethod> paymentMethods)
    {
        await _dbContext.PaymentMethods.AddRangeAsync(paymentMethods);
    }

    public async Task AddVectorDataAsync(VectorData vectorData)
    {
        await _dbContext.VectorData.AddAsync(vectorData);
    }

    public async Task<IEnumerable<SalesCategory>> GetSalesCategoriesAsync(int siteId, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.SalesCategories
            .Where(sc => sc.SiteId == siteId && sc.PeriodBeginDate >= startDate && sc.PeriodEndDate <= endDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<HourlySales>> GetHourlySalesAsync(int siteId, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.HourlySales
            .Where(hs => hs.SiteId == siteId && hs.PeriodBeginDate >= startDate && hs.PeriodEndDate <= endDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalesSummary>> GetSalesSummariesAsync(int siteId, DateTime startDate, DateTime endDate)
    {
        return await _dbContext.SalesSummaries
            .Where(ss => ss.SiteId == siteId && ss.PeriodBeginDate >= startDate && ss.PeriodEndDate <= endDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalesCategory>> GetTopSalesCategoriesAsync(int topN = 10)
    {
        return await _dbContext.SalesCategories
            .OrderByDescending(sc => sc.NetSalesAmount)
            .Take(topN)
            .ToListAsync();
    }

    public async Task<IEnumerable<SalesCategory>> GetTopSalesCategoriesAsync(int siteId, int topN = 10)
    {
        return await _dbContext.SalesCategories
            .Where(sc => sc.SiteId == siteId)
            .OrderByDescending(sc => sc.NetSalesAmount)
            .Take(topN)
            .ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync();
    }
}