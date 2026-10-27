using Microsoft.AspNetCore.Mvc;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Controllers;

/// <summary>
/// AI 流式对话接口：和豆包官方对话体验一致，实时返回思考过程+回答内容。
/// </summary>
[Route("api/ai")]
[ApiController]
public class AiChatController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(IAiService aiService, ILogger<AiChatController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public class ChatStreamRequest
    {
        public List<ChatMessage> Messages { get; set; } = new();
    }
    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }

    /// <summary>
    /// 流式对话接口：返回 text/event-stream，每条data是 {reasoning, content} 增量。
    /// </summary>
    [HttpPost("chat/stream")]
    public async Task ChatStream([FromBody] ChatStreamRequest req, CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // 关Nginx缓冲

        try
        {
            var messages = req.Messages.Select(m => (m.Role, m.Content)).ToList();
            await _aiService.ChatStreamAsync(messages, delta =>
            {
                var ssePayload = System.Text.Json.JsonSerializer.Serialize(new
                {
                    reasoning = delta.ReasoningDelta,
                    content = delta.ContentDelta
                });
                var line = $"data: {ssePayload}\n\n";
                var bytes = System.Text.Encoding.UTF8.GetBytes(line);
                Response.Body.Write(bytes, 0, bytes.Length);
                Response.Body.Flush();
            }, ct);

            // 结束标记
            var doneBytes = System.Text.Encoding.UTF8.GetBytes("data: [DONE]\n\n");
            await Response.Body.WriteAsync(doneBytes, ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiChat] 流式对话出错");
            var errLine = $"data: {{\"error\": \"{ex.Message.Replace("\"", "'")}\"}}\n\n";
            var errBytes = System.Text.Encoding.UTF8.GetBytes(errLine);
            await Response.Body.WriteAsync(errBytes, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
