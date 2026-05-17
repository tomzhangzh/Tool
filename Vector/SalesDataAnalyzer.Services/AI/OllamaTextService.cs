using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SalesDataAnalyzer.Services.AI;

public class OllamaTextService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaTextService(string baseUrl = "http://localhost:11434", string model = "llama3")
    {
        _baseUrl = baseUrl;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<string> GenerateResponseAsync(string userMessage, string context)
    {
        // 你是一个专业的销售数据分析助手。请根据提供的上下文信息（来自向量数据库的检索结果）回答用户的问题。
        //
        // 规则：
        // 1. 如果上下文中有相关信息，请基于上下文回答
        // 2. 如果上下文中没有相关信息，请说明无法从现有数据中找到答案
        // 3. 回答要简洁、准确、专业
        // 4. 可以进行数据分析、趋势判断、对比等
        // 5. 用中文回答
        //
        // 上下文信息（来自向量数据库检索）：
        var systemPrompt = @"You are a professional sales data analysis assistant. Please answer the user's questions based on the provided context information (from the vector database search results).

Rules:
1. If there is relevant information in the context, answer based on the context
2. If there is no relevant information in the context, explain that you cannot find an answer from the existing data
3. Keep answers concise, accurate, and professional
4. You may perform data analysis, trend analysis, comparisons, etc.
5. Answer in Chinese

Context information (from vector database search):
" + context;

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            stream = false,
            options = new
            {
                temperature = 0.7,
                num_predict = 1000
            }
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/chat", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Ollama调用失败:
                return $"Ollama call failed: {response.StatusCode}\n{responseBody}";
            }

            using var doc = JsonDocument.Parse(responseBody);
            
            if (doc.RootElement.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var contentProp))
            {
                // 无法生成回答
                return contentProp.GetString() ?? "Cannot generate response";
            }

            // 无法解析Ollama响应
            return "Cannot parse Ollama response";
        }
        catch (Exception ex)
        {
            // 连接Ollama失败: ...请确保Ollama已启动并正在运行。启动命令: ollama serve
            return $"Ollama connection failed: {ex.Message}\n\nPlease ensure Ollama is started and running.\nStart command: ollama serve";
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>();
                return json?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
            }
        }
        catch { }
        return new List<string>();
    }
}

public class OllamaTagsResponse
{
    public List<OllamaModel>? Models { get; set; }
}

public class OllamaModel
{
    public string Name { get; set; } = string.Empty;
}

public interface IEmbeddingService
{
    Task<List<float>> GenerateEmbeddingsAsync(string text);
    Task<bool> IsAvailableAsync();
}

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaEmbeddingService(string baseUrl = "http://localhost:11434", string model = "nomic-embed-text")
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
        _model = model;
    }

    public async Task<List<float>> GenerateEmbeddingsAsync(string text)
    {
        try
        {
            var requestBody = new
            {
                model = _model,
                prompt = text
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/embeddings", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Embedding failed: {response.StatusCode} - {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("embedding", out var embeddingProp))
            {
                var embeddingArray = embeddingProp.EnumerateArray();
                return embeddingArray.Select(e => e.GetSingle()).ToList();
            }

            throw new Exception("No embedding in response");
        }
        catch (Exception ex)
        {
            throw new Exception($"Embedding generation failed: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}