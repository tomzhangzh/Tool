using Newtonsoft.Json.Linq;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Platform.Services;

/// <summary>AI 对话返回结果：正文 + 推理过程（推理模型才有）</summary>
public class AiChatResult
{
    /// <summary>模型最终输出文本</summary>
    public string Content { get; set; } = "";
    /// <summary>模型推理/思考过程（reasoning_content，非推理模型为空）</summary>
    public string? Reasoning { get; set; }
}

/// <summary>
/// AI 对话服务抽象：对接 OpenAI 兼容协议（豆包/DeepSeek/通义等）。
/// 通过 appsettings.json 的 "Ai" 节点配置 BaseUrl / ApiKey / Model。
/// </summary>
public interface IAiService : IScopeDependency
{
    /// <summary>
    /// 调用大模型对话，返回正文 + 推理过程。
    /// </summary>
    Task<AiChatResult> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);

    /// <summary>
    /// 调用大模型并把回复正文解析为 JSON 对象（适合要求模型输出结构化 JSON 的场景）。
    /// 若解析失败会抛出异常，由调用方决定是否重试。
    /// </summary>
    Task<(JObject Json, string? Reasoning)> ChatJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
    Task ChatStreamAsync(List<(string Role, string Content)> messages, Action<(string? ReasoningDelta, string? ContentDelta)> onDelta, CancellationToken ct = default);
}
