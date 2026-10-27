using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 基于 OpenAI 兼容协议的 AI 服务实现。
/// 豆包（火山方舟）、DeepSeek、通义等均兼容此协议，只需改 BaseUrl + Model。
/// </summary>
public class AiService : IAiService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AiService> _logger;

    public AiService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<AiService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<AiChatResult> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var baseUrl = _config["Ai:BaseUrl"]?.TrimEnd('/') ?? "https://ark.cn-beijing.volces.com/api/v3";
        var apiKey = _config["Ai:ApiKey"] ?? "";
        var model = _config["Ai:Model"] ?? "doubao-1-5-lite-32k-250115";
        var timeoutSec = int.TryParse(_config["Ai:TimeoutSeconds"], out var ts) ? ts : 60;

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("未配置 Ai:ApiKey，请在 appsettings.json 中设置豆包 API Key");

        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(timeoutSec);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new JObject
        {
            ["model"] = model,
            ["messages"] = new JArray
            {
                new JObject { ["role"] = "system", ["content"] = systemPrompt },
                new JObject { ["role"] = "user", ["content"] = userPrompt }
            },
            ["temperature"] = 0.3,
            ["stream"] = false
        };

        var url = $"{baseUrl}/chat/completions";
        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        _logger.LogInformation("[AiService] 调用 {Url} model={Model} promptLen={Len}", url, model, userPrompt.Length);

        var resp = await client.PostAsync(url, content, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("[AiService] HTTP {Status}: {Body}", (int)resp.StatusCode, body);
            throw new HttpRequestException($"AI 接口返回 {(int)resp.StatusCode}: {body}");
        }

        var json = JObject.Parse(body);
        var msg = json["choices"]?[0]?["message"] as JObject;
        var text = msg?["content"]?.ToString() ?? "";
        var reasoning = msg?["reasoning_content"]?.ToString();
        return new AiChatResult { Content = text, Reasoning = reasoning };
    }

    public async Task<(JObject Json, string? Reasoning)> ChatJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var result = await ChatAsync(systemPrompt, userPrompt, ct);
        return (ParseJsonLoose(result.Content), result.Reasoning);
    }

    public async Task ChatStreamAsync(List<(string Role, string Content)> messages, Action<(string? ReasoningDelta, string? ContentDelta)> onDelta, CancellationToken ct = default)
    {
        var baseUrl = _config["Ai:BaseUrl"]?.TrimEnd('/') ?? "https://ark.cn-beijing.volces.com/api/v3";
        var apiKey = _config["Ai:ApiKey"] ?? "";
        var model = _config["Ai:Model"] ?? "doubao-1-5-lite-32k-250115";
        var timeoutSec = int.TryParse(_config["Ai:TimeoutSeconds"], out var ts) ? ts : 120;

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("未配置 Ai:ApiKey");

        using var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(timeoutSec);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new JObject
        {
            ["model"] = model,
            ["stream"] = true,
            ["thinking"] = new JObject { ["type"] = "enabled" }, // 开启深度思考，返回reasoning_content
            ["temperature"] = 0.3,
            ["messages"] = new JArray(messages.Select(m => new JObject { ["role"] = m.Role, ["content"] = m.Content }))
        };

        var url = $"{baseUrl}/chat/completions";
        var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

        _logger.LogInformation("[AiService.Stream] 调用 {Url} model={Model} msgCount={Count}", url, model, messages.Count);

        var resp = await client.PostAsync(url, content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var errBody = await resp.Content.ReadAsStringAsync(ct);
            _logger.LogError("[AiService.Stream] HTTP {Status}: {Body}", (int)resp.StatusCode, errBody);
            throw new HttpRequestException($"AI 接口返回 {(int)resp.StatusCode}: {errBody}");
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;
            var data = line.Substring(6).Trim();
            if (data == "[DONE]") break;

            try
            {
                var json = JObject.Parse(data);
                var delta = json["choices"]?[0]?["delta"] as JObject;
                if (delta == null) continue;
                var reasoning = delta["reasoning_content"]?.ToString();
                var contentDelta = delta["content"]?.ToString();
                if (!string.IsNullOrEmpty(reasoning) || !string.IsNullOrEmpty(contentDelta))
                {
                    onDelta((string.IsNullOrEmpty(reasoning) ? null : reasoning, 
                             string.IsNullOrEmpty(contentDelta) ? null : contentDelta));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AiService.Stream] 解析SSE行失败: {Line}", line);
            }
        }
    }

    /// <summary>
    /// 宽松解析 JSON：模型可能在 ```json 代码块里或带解释文字，尽量提取出 JSON 对象。
    /// </summary>
    private static JObject ParseJsonLoose(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new FormatException("AI 返回为空");

        // 去掉 ```json ... ``` 包裹
        var cleaned = text.Trim();
        var fenceStart = cleaned.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var contentStart = cleaned.IndexOf('\n', fenceStart);
            if (contentStart >= 0)
            {
                var fenceEnd = cleaned.IndexOf("```", contentStart, StringComparison.Ordinal);
                if (fenceEnd > contentStart)
                {
                    cleaned = cleaned.Substring(contentStart + 1, fenceEnd - contentStart - 1).Trim();
                }
            }
        }

        // 找第一个 { 到最后一个 }
        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        try
        {
            return JObject.Parse(cleaned);
        }
        catch (Exception ex)
        {
            throw new FormatException($"AI 返回无法解析为 JSON: {ex.Message}\n原始内容: {text}", ex);
        }
    }
}
