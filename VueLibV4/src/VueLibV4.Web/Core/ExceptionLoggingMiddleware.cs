using System.Diagnostics;
using Microsoft.Data.Sqlite;
using VueLibV4.Web.Services;

namespace VueLibV4.Web.Core;

/// <summary>
/// 全局异常日志中间件：捕获未处理异常，写入 SysLog 表。
/// - 异步写库，fire-and-forget，不阻塞请求
/// - 日志写失败只打控制台，不影响业务响应
/// - 正常请求不记 Info（量太大），只记 Error
/// </summary>
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, IConfiguration config, ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _config = config;
        _logger = logger;
    }

    // IPermissionService 是 Scoped，中间件为单例不能构造注入，只能放在 InvokeAsync 由请求容器解析。
    // 平台认证是自定义签名 cookie（不构建 ClaimsPrincipal），用户名必须从 PermissionService 取，
    // ctx.User.Identity.Name 在本项目恒为 null。
    public async Task InvokeAsync(HttpContext ctx, IPermissionService perm)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            // 用户名必须在请求线程上读取（只解析签名 cookie，不访问数据库）
            var userName = SafeUserName(perm);
            LogInBackground(ex, ctx, userName);
            throw; // 继续抛给框架处理（返回 500）
        }
    }

    private static string SafeUserName(IPermissionService perm)
    {
        try { return perm?.CurrentUserName ?? ""; }
        catch { return ""; }
    }

    private void LogInBackground(Exception ex, HttpContext ctx, string userName)
    {
        try
        {
            // 所有 HttpContext 相关读取必须在请求线程上提前拷贝：
            // Task.Run 写库时响应可能已结束，再碰 ctx.User/Request 会得到 ObjectDisposed/不一致状态
            var traceId = Activity.Current?.TraceId.ToString()?.Substring(0, 16) ?? "";
            var path = ctx.Request.Path.Value ?? "";
            var method = ctx.Request.Method;
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "";
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 异步写库，不 await
            _ = Task.Run(() =>
            {
                try
                {
                    var cs = _config.GetConnectionString("PlatformDb");
                    if (string.IsNullOrWhiteSpace(cs)) return;
                    var bld = new SqliteConnectionStringBuilder(cs);
                    if (!Path.IsPathRooted(bld.DataSource))
                        bld.DataSource = Path.Combine(AppContext.BaseDirectory, bld.DataSource.Replace('/', Path.DirectorySeparatorChar));
                    using var conn = new SqliteConnection(bld.ConnectionString);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"INSERT INTO SysLog (LogLevel,Category,Message,Exception,TraceId,UserName,Path,Method,Ip,CreateTime)
                                        VALUES ('Error',@cat,@msg,@ex,@tid,@un,@path,@m,@ip,@t)";
                    cmd.Parameters.AddWithValue("@cat", "Global");
                    var exMsg = ex.Message ?? "";
                    cmd.Parameters.AddWithValue("@msg", exMsg.Substring(0, Math.Min(2000, exMsg.Length)));
                    var exText = ex.ToString() ?? "";
                    cmd.Parameters.AddWithValue("@ex", exText.Substring(0, Math.Min(4000, exText.Length)));
                    cmd.Parameters.AddWithValue("@tid", traceId);
                    cmd.Parameters.AddWithValue("@un", userName);
                    cmd.Parameters.AddWithValue("@path", path);
                    cmd.Parameters.AddWithValue("@m", method);
                    cmd.Parameters.AddWithValue("@ip", ip);
                    cmd.Parameters.AddWithValue("@t", now);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "[SysLog] 写日志失败");
                }
            });
        }
        catch
        {
            // 日志失败绝不能影响业务
        }
    }
}
