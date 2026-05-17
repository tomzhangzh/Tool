using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SalesDataAnalyzer.Services.AI;

public class OpenAITextService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAITextService(string apiKey, string model = "gpt-3.5-turbo")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/")
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> GenerateResponseAsync(string userMessage, string context)
    {
        var systemPrompt = @"你是一个专业的销售数据分析助手。请根据提供的上下文信息（来自向量数据库的检索结果）回答用户的问题。

规则：
1. 如果上下文中有相关信息，请基于上下文回答
2. 如果上下文中没有相关信息，请说明无法从现有数据中找到答案
3. 回答要简洁、准确、专业
4. 可以进行数据分析、趋势判断、对比等

上下文信息（来自向量数据库检索）：
" + context;

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 1000
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await _httpClient.PostAsync("v1/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"API调用失败: {response.StatusCode} - {responseBody}";
            }

            using var doc = JsonDocument.Parse(responseBody);
            var answer = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return answer ?? "无法生成回答";
        }
        catch (Exception ex)
        {
            return $"生成回答时出错: {ex.Message}";
        }
    }
}

public class AzureOpenAITextService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;

    public AzureOpenAITextService(string endpoint, string apiKey, string deploymentName)
    {
        _endpoint = endpoint;
        _apiKey = apiKey;
        _deploymentName = deploymentName;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_endpoint)
        };
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task<string> GenerateResponseAsync(string userMessage, string context)
    {
        var systemPrompt = @"你是一个专业的销售数据分析助手。请根据提供的上下文信息（来自向量数据库的检索结果）回答用户的问题。

规则：
1. 如果上下文中有相关信息，请基于上下文回答
2. 如果上下文中没有相关信息，请说明无法从现有数据中找到答案
3. 回答要简洁、准确、专业
4. 可以进行数据分析、趋势判断、对比等

上下文信息（来自向量数据库检索）：
" + context;

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 1000
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        try
        {
            var url = $"/openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-01";
            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"API调用失败: {response.StatusCode} - {responseBody}";
            }

            using var doc = JsonDocument.Parse(responseBody);
            var answer = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return answer ?? "无法生成回答";
        }
        catch (Exception ex)
        {
            return $"生成回答时出错: {ex.Message}";
        }
    }
}