using System.Net;

namespace VueLibV4.Web.Core;

/// <summary>
/// 最小侵入的 API Key 鉴权中间件。
/// 配置（appsettings.json → Dyn:Auth）：
///   Enabled      默认 false（不影响 localhost 开发）；生产置 true 才校验。
///   ApiKey       约定的密钥；为空时即使 Enabled=true 也视为未配置、放行（避免把自己锁外面）。
///   AllowLocal   默认 true：本机回环请求（127.0.0.1/::1）直接放行，便于开发调试。
/// 请求方式：其它环境在 header 带  X-Api-Key: <ApiKey>。
/// 说明：当前是内网/无登录体系平台的轻量补丁；后续接 SSO/JWT 时替换本中间件即可。
/// </summary>
public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext ctx, IConfiguration cfg)
    {
        var enabled = cfg.GetValue("Dyn:Auth:Enabled", false);
        if (!enabled)
        {
            await _next(ctx);
            return;
        }

        var configuredKey = cfg["Dyn:Auth:ApiKey"];
        // 没配密钥等于没开，避免误锁
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            await _next(ctx);
            return;
        }

        // 本机回环放行（可关）
        var allowLocal = cfg.GetValue("Dyn:Auth:AllowLocal", true);
        if (allowLocal && IsLocal(ctx))
        {
            await _next(ctx);
            return;
        }

        // 浏览器静态资源与设计器页不卡，只拦 API（/api/ 前缀）
        var path = ctx.Request.Path.Value ?? "";
        // 前端错误上报放行（无登录态，浏览器无法带 API Key）
        if (path.StartsWith("/api/log", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var headerKey = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.Equals(headerKey, configuredKey, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            await ctx.Response.WriteAsync("{\"code\":401,\"msg\":\"未授权：缺少或错误的 X-Api-Key\"}");
            return;
        }

        await _next(ctx);
    }

    private static bool IsLocal(HttpContext ctx)
    {
        var remote = ctx.Connection.RemoteIpAddress;
        if (remote == null) return true;
        if (IPAddress.IsLoopback(remote)) return true;
        // 容器/反向代理常见回环
        return remote.Equals(IPAddress.Loopback) || remote.Equals(IPAddress.IPv6Loopback);
    }
}
