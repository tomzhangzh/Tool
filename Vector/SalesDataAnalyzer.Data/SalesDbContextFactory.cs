using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SalesDataAnalyzer.Data;

public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();
        optionsBuilder.UseSqlServer(@"Server=(LocalDB)\ProjectModels;Database=SalesDataDB;Trusted_Connection=True;");

        return new SalesDbContext(optionsBuilder.Options);
    }
}