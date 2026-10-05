using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace VueLibV4.Web.Core;

/// <summary>
/// 前端 JS 错误上报 API。
/// 开关：appsettings → Dyn:Log:FrontendEnabled（默认 true）。
/// 前端 window.onerror / unhandledrejection 调用此接口，写入 SysLog 表。
/// 安全：匿名端点（ApiKey 中间件放行），因此强制同源校验 + 基于 IP 的内存限流，防止跨站刷库撑爆 SQLite。
/// </summary>
[ApiController]
[Route("api/log")]
[AllowAnonymousPermission] // 未登录页（如登录页）也允许上报；ApiKey 中间件同样放行，靠同源校验+IP 限流防刷
public class FrontendLogController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<FrontendLogController> _logger;

    // 简易内存限流：每 IP 在滚动窗口内最多写入若干条（内部工具单实例部署，无需分布式存储）
    private const int RateWindowSeconds = 60;
    private const int RateMaxRequests = 20;
    private static readonly ConcurrentDictionary<string, RateEntry> _rate = new();

    public FrontendLogController(IConfiguration config, ILogger<FrontendLogController> logger)
    {
        _config = config;
        _logger = logger;
    }

    private class RateEntry
    {
        public long WindowStartTick;
        public int Count;
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

        // 同源校验：浏览器页面脚本的 fetch/XHR 会带 Origin（旧环境至少有 Referer）；
        // 两者都没有或主机不一致，视为跨站/脚本伪造，拒绝写入。
        if (!IsSameOrigin()) return BadRequest(new { code = 1, msg = "origin denied" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-";
        if (!AllowRequest(ip)) return StatusCode(429, new { code = 1, msg = "rate limited" });

        try
        {
            var cs = _config.GetConnectionString("PlatformDb");
            if (string.IsNullOrWhiteSpace(cs)) return Ok(new { code = 0 });

            // 相对路径基于程序目录解析（与 Seeder.OpenSqlite 一致）
            var bld = new SqliteConnectionStringBuilder(cs);
            if (!Path.IsPathRooted(bld.DataSource))
                bld.DataSource = Path.Combine(AppContext.BaseDirectory, bld.DataSource.Replace('/', Path.DirectorySeparatorChar));

            using var conn = new SqliteConnection(bld.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO SysLog (LogLevel,Category,Message,Exception,Path,Method,Ip,CreateTime)
                                VALUES ('Error','Frontend',@msg,@ex,@path,'POST',@ip,@t)";
            var msg = dto.Message ?? "";
            cmd.Parameters.AddWithValue("@msg", msg.Substring(0, Math.Min(500, msg.Length)));
            var stack = dto.Stack ?? "";
            cmd.Parameters.AddWithValue("@ex", stack.Substring(0, Math.Min(2000, stack.Length)));
            cmd.Parameters.AddWithValue("@path", dto.Url ?? Request.Path.Value ?? "");
            cmd.Parameters.AddWithValue("@ip", ip);
            cmd.Parameters.AddWithValue("@t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FrontendLog] 写库失败");
        }
        return Ok(new { code = 0 });
    }

    private bool IsSameOrigin()
    {
        var host = Request.Host.Host;
        if (string.IsNullOrEmpty(host)) return false;

        string? candidate = Request.Headers.Origin.FirstOrDefault();
        if (string.IsNullOrEmpty(candidate))
            candidate = Request.Headers.Referer.FirstOrDefault();
        if (string.IsNullOrEmpty(candidate)) return false;

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase)) return false;
        // 端口均显式给出时必须一致；默认端口（缺省）放行
        if (Request.Host.Port is int reqPort && uri.Port > 0 && uri.Port != reqPort) return false;
        return true;
    }

    private static bool AllowRequest(string ip)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var entry = _rate.GetOrAdd(ip, _ => new RateEntry { WindowStartTick = now, Count = 0 });
        lock (entry)
        {
            if (now - entry.WindowStartTick >= RateWindowSeconds)
            {
                entry.WindowStartTick = now;
                entry.Count = 0;
            }
            entry.Count++;
            return entry.Count <= RateMaxRequests;
        }
    }
}
