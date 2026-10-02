using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace VueLibV4.Web.Core;

/// <summary>
/// 前端 JS 错误上报 API。
/// 开关：appsettings → Dyn:Log:FrontendEnabled（默认 true）。
/// 前端 window.onerror / unhandledrejection 调用此接口，写入 SysLog 表。
/// </summary>
[ApiController]
[Route("api/log")]
public class FrontendLogController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<FrontendLogController> _logger;

    public FrontendLogController(IConfiguration config, ILogger<FrontendLogController> logger)
    {
        _config = config;
        _logger = logger;
    }

    public class FrontendErrorDto
    {
        public string? Message { get; set; }
        public string? Source { get; set; }
        public int? Line { get; set; }
        public int? Column { get; set; }
        public string? Stack { get; set; }
        public string? Url { get; set; }
        public string? UserAgent { get; set; }
    }

    [HttpPost("frontend-error")]
    public IActionResult ReportError([FromBody] FrontendErrorDto dto)
    {
        var enabled = _config.GetValue("Dyn:Log:FrontendEnabled", true);
        if (!enabled) return Ok(new { code = 0 });

        try
        {
            var cs = _config.GetConnectionString("PlatformDb");
            if (string.IsNullOrWhiteSpace(cs)) return Ok(new { code = 0 });

            using var conn = new SqliteConnection(cs);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO SysLog (LogLevel,Category,Message,Exception,Path,Method,Ip,CreateTime)
                                VALUES ('Error','Frontend',@msg,@ex,@path,'POST',@ip,@t)";
            cmd.Parameters.AddWithValue("@msg", (dto.Message ?? "").Substring(0, Math.Min(500, dto.Message?.Length ?? 0)));
            var stack = dto.Stack ?? "";
            cmd.Parameters.AddWithValue("@ex", stack.Substring(0, Math.Min(2000, stack.Length)));
            cmd.Parameters.AddWithValue("@path", dto.Url ?? Request.Path.Value ?? "");
            cmd.Parameters.AddWithValue("@ip", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FrontendLog] 写库失败");
        }
        return Ok(new { code = 0 });
    }
}
