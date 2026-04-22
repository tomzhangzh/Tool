using Microsoft.EntityFrameworkCore;
using SalesDataAnalyzer.Models;

namespace SalesDataAnalyzer.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }
    
    public DbSet<SalesCategory> SalesCategories { get; set; }
    public DbSet<HourlySales> HourlySales { get; set; }
    public DbSet<SalesSummary> SalesSummaries { get; set; }
    public DbSet<PaymentMethod> PaymentMethods { get; set; }
    public DbSet<VectorData> VectorData { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesCategory>()
            .HasIndex(sc => new { sc.SiteId, sc.CategorySysId, sc.PeriodBeginDate });
        
        modelBuilder.Entity<HourlySales>()
            .HasIndex(hs => new { hs.SiteId, hs.Hour, hs.PeriodBeginDate });
        
        modelBuilder.Entity<SalesSummary>()
            .HasIndex(ss => new { ss.SiteId, ss.PeriodBeginDate });
        
        modelBuilder.Entity<PaymentMethod>()
            .HasIndex(pm => new { pm.SiteId, pm.PaymentSysId, pm.PeriodBeginDate });
        
        modelBuilder.Entity<VectorData>()
            .HasIndex(vd => new { vd.SiteId, vd.PeriodDate, vd.DataType });
    }
}