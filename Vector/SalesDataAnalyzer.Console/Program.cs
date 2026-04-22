using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SalesDataAnalyzer.Data;
using SalesDataAnalyzer.Data.Repositories;
using SalesDataAnalyzer.Services;
using SalesDataAnalyzer.Services.VectorDb;

namespace SalesDataAnalyzer.Console;

class Program
{
    static async Task Main(string[] args)
    {
        var serviceProvider = ConfigureServices();
        
        System.Console.WriteLine("销售数据分析系统");
        System.Console.WriteLine("=================");
        System.Console.WriteLine();
        
        var dataImportService = serviceProvider.GetRequiredService<DataImportService>();
        
        if (args.Length > 0 && args[0] == "-import")
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("使用方式: dotnet run -- -import <xml目录路径>");
                return;
            }
            await ImportData(dataImportService, args[1]);
            return;
        }
        
        if (args.Length > 0 && args[0] == "-query")
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("使用方式: dotnet run -- -query <查询语句>");
                return;
            }
            await QueryVectorDb(dataImportService, args[1]);
            return;
        }
        
        while (true)
        {
            System.Console.WriteLine("请选择操作:");
            System.Console.WriteLine("1. 导入XML数据到数据库");
            System.Console.WriteLine("2. 查询向量数据库");
            System.Console.WriteLine("3. 退出");
            System.Console.Write("请输入选项: ");
            
            var input = System.Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    System.Console.Write("请输入XML文件目录路径: ");
                    var path = System.Console.ReadLine();
                    await ImportData(dataImportService, path);
                    break;
                case "2":
                    System.Console.Write("请输入查询语句: ");
                    var query = System.Console.ReadLine();
                    await QueryVectorDb(dataImportService, query);
                    break;
                case "3":
                    System.Console.WriteLine("退出程序...");
                    return;
                default:
                    System.Console.WriteLine("无效选项，请重新输入");
                    break;
            }
            
            System.Console.WriteLine();
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<SalesDbContext>(options =>
            options.UseSqlServer(@"Server=(LocalDB)\ProjectModels;Database=SalesDataDB;Trusted_Connection=True;"));
        
        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<IChromaService, ChromaService>();
        services.AddScoped<DataImportService>();
        
        return services.BuildServiceProvider();
    }

    private static async Task ImportData(DataImportService dataImportService, string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            System.Console.WriteLine("路径不能为空");
            return;
        }
        
        if (!Directory.Exists(directoryPath))
        {
            System.Console.WriteLine("目录不存在");
            return;
        }
        
        try
        {
            System.Console.WriteLine("开始导入数据...");
            await dataImportService.ImportAllDataAsync(directoryPath);
            System.Console.WriteLine("数据导入完成");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"导入失败: {ex.Message}");
            if (ex.InnerException != null)
            {
                System.Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                if (ex.InnerException.InnerException != null)
                {
                    System.Console.WriteLine($"详细错误: {ex.InnerException.InnerException.Message}");
                }
            }
        }
    }

    private static async Task QueryVectorDb(DataImportService dataImportService, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            System.Console.WriteLine("查询语句不能为空");
            return;
        }
        
        try
        {
            System.Console.WriteLine("正在查询向量数据库...");
            var results = await dataImportService.AnalyzeSalesDataAsync(query);
            
            if (results.Any())
            {
                System.Console.WriteLine($"找到 {results.Count} 条相似结果:");
                System.Console.WriteLine("------------------------");
                
                foreach (var (document, score, metadata) in results)
                {
                    System.Console.WriteLine($"匹配度: {score:F4}");
                    System.Console.WriteLine($"内容: {document}");
                    System.Console.WriteLine("元数据:");
                    foreach (var kvp in metadata)
                    {
                        System.Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    System.Console.WriteLine("------------------------");
                }
            }
            else
            {
                System.Console.WriteLine("未找到匹配结果");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"查询失败: {ex.Message}");
        }
    }
}