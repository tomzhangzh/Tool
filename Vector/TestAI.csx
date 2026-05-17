using SalesDataAnalyzer.Services.AI;

Console.WriteLine("=== AI 智能问答测试 ===\n");

// 测试 Embedding 服务
Console.WriteLine("1. 测试 Embedding 服务...");
var embeddingService = new OllamaEmbeddingService();

try
{
    var embedding = await embeddingService.GenerateEmbeddingsAsync("测试文本");
    Console.WriteLine($"   ✓ Embedding 生成成功，维度：{embedding.Count}");
}
catch (Exception ex)
{
    Console.WriteLine($"   ✗ Embedding 失败：{ex.Message}");
}

// 测试 LLM 服务
Console.WriteLine("\n2. 测试 LLM 服务...");
var llmService = new OllamaTextService();

var testContext = @"
这是一些测试销售数据：
- 2026年5月16日：销售额 150,000 元
- 2026年5月15日：销售额 120,000 元
- 2026年5月14日：销售额 180,000 元
";

var testQuestion = "最近三天的销售情况如何？哪天销售额最高？";

Console.WriteLine($"   问题：{testQuestion}");
Console.WriteLine($"   正在思考...\n");

try
{
    var response = await llmService.GenerateResponseAsync(testQuestion, testContext);
    Console.WriteLine("   AI 回答：");
    Console.WriteLine("   " + response.Replace("\n", "\n   "));
    Console.WriteLine("\n   ✓ LLM 服务工作正常！");
}
catch (Exception ex)
{
    Console.WriteLine($"   ✗ LLM 测试失败：{ex.Message}");
}

Console.WriteLine("\n=== 测试完成 ===");
