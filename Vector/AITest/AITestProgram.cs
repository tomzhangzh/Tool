using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== AI 智能问答测试 ===\n");

        var baseUrl = "http://localhost:11434";
        var model = "llama3";

        Console.WriteLine("测试 1：检查 Ollama 服务...");
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(2) };
        
        try
        {
            var response = await httpClient.GetAsync("/api/tags");
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("   ✓ Ollama 服务正常！");
                var tagsContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   响应：{tagsContent.Substring(0, Math.Min(100, tagsContent.Length))}...\n");
            }
            else
            {
                Console.WriteLine($"   ✗ Ollama 服务不可用：{response.StatusCode}");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ 连接失败：{ex.Message}");
            return;
        }

        Console.WriteLine("测试 2：测试 Embedding 服务...");
        try
        {
            var embeddingRequest = new { model = "nomic-embed-text", prompt = "Hello world!" };
            var content = new StringContent(JsonSerializer.Serialize(embeddingRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/api/embeddings", content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<EmbeddingResponse>(jsonString);
                Console.WriteLine($"   ✓ Embedding 生成成功，维度：{json?.embedding?.Count ?? 0}\n");
            }
            else
            {
                Console.WriteLine($"   ✗ Embedding 失败：{response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Embedding 测试失败：{ex.Message}\n");
        }

        Console.WriteLine("测试 3：测试 LLM 问答...");
        var systemPrompt = @"你是一个专业的销售数据分析助手。请根据提供的上下文信息回答用户的问题。
规则：
1. 如果上下文中有相关信息，请基于上下文回答
2. 如果上下文中没有相关信息，请说明无法从现有数据中找到答案
3. 回答要简洁、准确、专业
4. 用中文回答

上下文信息：
这是一些测试销售数据：
- 2026年5月16日：销售额 150,000 元
- 2026年5月15日：销售额 120,000 元
- 2026年5月14日：销售额 180,000 元";

        var userQuestion = "最近三天的销售情况如何？哪天销售额最高？";
        Console.WriteLine($"   问题：{userQuestion}");
        Console.WriteLine("   正在思考...\n");

        try
        {
            var chatRequest = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userQuestion }
                },
                stream = false,
                options = new { temperature = 0.7, num_predict = 1000 }
            };

            var content = new StringContent(JsonSerializer.Serialize(chatRequest), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("/api/chat", content);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<ChatResponse>(jsonString);
                Console.WriteLine("   AI 回答：");
                Console.WriteLine("   " + (json?.message?.content ?? "无内容").Replace("\n", "\n   "));
                Console.WriteLine("\n   ✓ LLM 问答正常！\n");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   ✗ LLM 失败：{response.StatusCode}\n   {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ LLM 测试失败：{ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"   内部错误：{ex.InnerException.Message}");
        }

        Console.WriteLine("=== 测试完成 ===");
    }
}

class EmbeddingResponse
{
    public List<float>? embedding { get; set; }
}

class ChatResponse
{
    public ChatMessage? message { get; set; }
}

class ChatMessage
{
    public string? content { get; set; }
}
