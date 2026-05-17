using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SalesDataAnalyzer.Data;
using SalesDataAnalyzer.Data.Repositories;
using SalesDataAnalyzer.Services;
using SalesDataAnalyzer.Services.AI;
using SalesDataAnalyzer.Services.VectorDb;

namespace SalesDataAnalyzer.Console;

class Program
{
    private static ILLMService? _llmService;
    private static RAGQuestionAnsweringService? _ragService;

    static async Task Main(string[] args)
    {
        var serviceProvider = ConfigureServices();

        // 销售数据分析系统
        System.Console.WriteLine("Sales Data Analysis System");
        System.Console.WriteLine("=================");
        System.Console.WriteLine();

        var dataImportService = serviceProvider.GetRequiredService<DataImportService>();
        var chromaService = serviceProvider.GetRequiredService<IChromaService>();

        await ConfigureAIServiceAsync(chromaService);

        if (args.Length > 0 && args[0] == "-import")
        {
            if (args.Length < 2)
            {
                // 使用方式: dotnet run -- -import <xml目录路径>
                System.Console.WriteLine("Usage: dotnet run -- -import <xml directory path>");
                return;
            }
            await ImportData(dataImportService, args[1]);
            return;
        }

        if (args.Length > 0 && args[0] == "-query")
        {
            if (args.Length < 2)
            {
                // 使用方式: dotnet run -- -query <查询语句> [site名称]
                System.Console.WriteLine("Usage: dotnet run -- -query <query statement> [site name]");
                return;
            }
            var siteName = args.Length > 2 ? args[2] : null;
            await QueryVectorDb(dataImportService, args[1], siteName);
            return;
        }

        if (args.Length > 0 && args[0] == "-ai")
        {
            if (args.Length < 2)
            {
                // 使用方式: dotnet run -- -ai <问题> [site名称]
                System.Console.WriteLine("Usage: dotnet run -- -ai <question> [site name]");
                return;
            }
            var siteName = args.Length > 2 ? args[2] : null;
            var question = string.Join(" ", args.Skip(1).Take(args.Length > 2 ? args.Length - 2 : 1));
            await AskAI(question, siteName);
            return;
        }

        while (true)
        {
            // 请选择操作:
            System.Console.WriteLine("Please select an operation:");
            // 1. 导入XML数据到数据库
            System.Console.WriteLine("1. Import XML data to database");
            // 2. 查询向量数据库（全部门店）
            System.Console.WriteLine("2. Query vector database (all sites)");
            // 3. 查询向量数据库（指定门店）
            System.Console.WriteLine("3. Query vector database (specific site)");
            // 4. 查看所有门店
            System.Console.WriteLine("4. View all sites");
            // 5. AI智能问答
            System.Console.WriteLine("5. AI intelligent QA");
            // 6. AI智能问答（指定门店）
            System.Console.WriteLine("6. AI intelligent QA (specific site)");
            // 7. 退出
            System.Console.WriteLine("7. Exit");
            // 请输入选项:
            System.Console.Write("Please enter your choice: ");

            var input = System.Console.ReadLine();

            switch (input)
            {
                case "1":
                    // 请输入XML文件目录路径:
                    System.Console.Write("Please enter XML file directory path: ");
                    var path = System.Console.ReadLine();
                    await ImportData(dataImportService, path);
                    break;
                case "2":
                    // 请输入查询语句:
                    System.Console.Write("Please enter query statement: ");
                    var queryAll = System.Console.ReadLine();
                    await QueryVectorDb(dataImportService, queryAll, null);
                    break;
                case "3":
                    await ListSitesAsync(dataImportService);
                    // 请输入门店名称:
                    System.Console.Write("Please enter site name: ");
                    var siteName = System.Console.ReadLine();
                    // 请输入查询语句:
                    System.Console.Write("Please enter query statement: ");
                    var querySite = System.Console.ReadLine();
                    await QueryVectorDb(dataImportService, querySite, siteName);
                    break;
                case "4":
                    await ListSitesAsync(dataImportService);
                    break;
                case "5":
                    // 请输入您的问题:
                    System.Console.Write("Please enter your question: ");
                    var questionAll = System.Console.ReadLine();
                    await AskAI(questionAll, null);
                    break;
                case "6":
                    await ListSitesAsync(dataImportService);
                    // 请输入门店名称:
                    System.Console.Write("Please enter site name: ");
                    var siteForAI = System.Console.ReadLine();
                    // 请输入您的问题:
                    System.Console.Write("Please enter your question: ");
                    var questionSite = System.Console.ReadLine();
                    await AskAI(questionSite, siteForAI);
                    break;
                case "7":
                    // 退出程序...
                    System.Console.WriteLine("Exiting program...");
                    return;
                default:
                    // 无效选项，请重新输入
                    System.Console.WriteLine("Invalid option, please re-enter");
                    break;
            }

            System.Console.WriteLine();
        }
    }

    private static async Task ConfigureAIServiceAsync(IChromaService chromaService)
    {
        var ollama = new OllamaTextService("http://localhost:11434", "llama3");

        // 正在检查AI服务...
        System.Console.WriteLine("Checking AI service...");

        if (await ollama.IsAvailableAsync())
        {
            // ✓ 检测到本地Ollama服务
            System.Console.WriteLine("✓ Detected local Ollama service");
            var models = await ollama.GetAvailableModelsAsync();
            if (models.Any())
            {
                // 可用模型:
                System.Console.WriteLine($"  Available models: {string.Join(", ", models)}");
                // 使用模型: llama3
                System.Console.WriteLine("  Using model: llama3");
            }
            _llmService = ollama;
            _ragService = new RAGQuestionAnsweringService(chromaService, _llmService);
            // AI服务(Ollama)已配置完成
            System.Console.WriteLine("AI service (Ollama) configured successfully");
            System.Console.WriteLine();
            return;
        }

        // ✗ 未检测到本地Ollama服务
        System.Console.WriteLine("✗ Local Ollama service not detected");

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            // 尝试使用OpenAI API...
            System.Console.WriteLine("Attempting to use OpenAI API...");
            _llmService = new OpenAITextService(apiKey, "gpt-3.5-turbo");
            _ragService = new RAGQuestionAnsweringService(chromaService, _llmService);
            // AI服务(OpenAI)已配置完成
            System.Console.WriteLine("AI service (OpenAI) configured successfully");
        }
        else
        {
            // 提示: AI问答功能不可用
            System.Console.WriteLine("Note: AI QA functionality not available");
            System.Console.WriteLine();
            // 要启用AI功能，请选择以下方式之一:
            System.Console.WriteLine("To enable AI functionality, please choose one of the following:");
            // 1. 安装Ollama (推荐，完全免费):
            System.Console.WriteLine("1. Install Ollama (Recommended, completely free):");
            System.Console.WriteLine("   - Download: https://ollama.com/download");
            System.Console.WriteLine("   - Install model: ollama pull llama3");
            System.Console.WriteLine("   - Start service: ollama serve");
            System.Console.WriteLine();
            // 2. 使用OpenAI API:
            System.Console.WriteLine("2. Use OpenAI API:");
            System.Console.WriteLine("   - Set environment variable: setx OPENAI_API_KEY \"your sk-xxx key\"");
        }

        System.Console.WriteLine();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<SalesDbContext>(options =>
            options.UseSqlServer(@"Server=(LocalDB)\ProjectModels;Database=SalesDataDB;Trusted_Connection=True;"));

        services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        services.AddSingleton<IChromaService, ChromaService>();

        services.AddScoped<ISalesRepository, SalesRepository>();
        services.AddScoped<DataImportService>();

        return services.BuildServiceProvider();
    }

    private static async Task ImportData(DataImportService dataImportService, string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            // 路径不能为空
            System.Console.WriteLine("Path cannot be empty");
            return;
        }

        if (!Directory.Exists(directoryPath))
        {
            // 目录不存在
            System.Console.WriteLine("Directory does not exist");
            return;
        }

        try
        {
            // 开始导入数据...
            System.Console.WriteLine("Starting data import...");
            await dataImportService.ImportAllDataAsync(directoryPath);
            // 数据导入完成
            System.Console.WriteLine("Data import completed");
        }
        catch (Exception ex)
        {
            // 导入失败:
            System.Console.WriteLine($"Import failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                // 内部错误:
                System.Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                if (ex.InnerException.InnerException != null)
                {
                    // 详细错误:
                    System.Console.WriteLine($"Detailed error: {ex.InnerException.InnerException.Message}");
                }
            }
        }
    }

    private static async Task QueryVectorDb(DataImportService dataImportService, string? query, string? siteName)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // 查询语句不能为空
            System.Console.WriteLine("Query statement cannot be empty");
            return;
        }

        try
        {
            // 正在查询向量数据库...
            System.Console.WriteLine($"Querying vector database{(siteName != null ? $" (site: {siteName})" : " (all sites)")}...");
            var results = await dataImportService.AnalyzeSalesDataAsync(query, siteName);

            if (results.Any())
            {
                // 找到 {results.Count} 条相似结果:
                System.Console.WriteLine($"Found {results.Count} similar results:");
                System.Console.WriteLine("------------------------");

                foreach (var (document, score, metadata) in results)
                {
                    // 匹配度:
                    System.Console.WriteLine($"Match Score: {score:F4}");
                    // 内容:
                    System.Console.WriteLine($"Content: {document}");
                    // 元数据:
                    System.Console.WriteLine("Metadata:");
                    foreach (var kvp in metadata)
                    {
                        System.Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                    }
                    System.Console.WriteLine("------------------------");
                }
            }
            else
            {
                // 未找到匹配结果
                System.Console.WriteLine("No matching results found");
            }
        }
        catch (Exception ex)
        {
            // 查询失败:
            System.Console.WriteLine($"Query failed: {ex.Message}");
        }
    }

    private static async Task ListSitesAsync(DataImportService dataImportService)
    {
        try
        {
            var sites = await dataImportService.GetAllSitesAsync();
            if (sites.Any())
            {
                // 可用门店:
                System.Console.WriteLine("Available sites:");
                foreach (var site in sites)
                {
                    System.Console.WriteLine($"  - {site}");
                }
            }
            else
            {
                // 暂无门店数据，请先导入数据
                System.Console.WriteLine("No site data available, please import data first");
            }
        }
        catch (Exception ex)
        {
            // 获取门店列表失败:
            System.Console.WriteLine($"Failed to get site list: {ex.Message}");
        }
    }

    private static async Task AskAI(string? question, string? siteName)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            // 问题不能为空
            System.Console.WriteLine("Question cannot be empty");
            return;
        }

        if (_ragService == null)
        {
            // AI服务未配置
            System.Console.WriteLine("AI service not configured");
            return;
        }

        try
        {
            // 正在思考...
            System.Console.WriteLine($"Thinking{(siteName != null ? $" (site: {siteName})" : "")}...");
            var answer = await _ragService.AskAsync(question, siteName);
            System.Console.WriteLine();
            // === AI回答 ===
            System.Console.WriteLine("=== AI Answer ===");
            System.Console.WriteLine(answer);
        }
        catch (Exception ex)
        {
            // AI回答失败:
            System.Console.WriteLine($"AI answer failed: {ex.Message}");
        }
    }
}