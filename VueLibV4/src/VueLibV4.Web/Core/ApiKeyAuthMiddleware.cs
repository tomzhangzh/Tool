using System.Net;
using VueLibV4.Web.Services;

namespace VueLibV4.Web.Core;

/// <summary>
/// 最小侵入的 API Key 鉴权中间件（机器/服务间调用通道）。
/// 配置（appsettings.json → Dyn:Auth）：
///   Enabled      默认 false（不影响 localhost 开发）；生产置 true 才校验。
///   ApiKey       约定的密钥；为空时即使 Enabled=true 也视为未配置、放行（避免把自己锁外面）。
///   AllowLocal   默认 true：本机回环请求（127.0.0.1/::1）直接放行，便于开发调试。
/// 请求方式：其它环境在 header 带  X-Api-Key: <ApiKey>。
/// 反向代理：Program 已启用 ForwardedHeaders（默认只信任回环代理），此处 RemoteIpAddress
/// 已是真实客户端 IP，同机 nginx 反代的外部请求不会再被 AllowLocal 误放行。
/// 与登录体系关系：X-Api-Key 校验通过后在 HttpContext.Items 打 Dyn.ApiAuthenticated 标记，
/// PermissionFilter 据此把请求识别为"apikey 服务账户"（已认证、非 admin、无 UI 角色）。
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

        // 通知 PermissionFilter：本次请求已通过 API Key 认证（apikey 服务账户）
        ctx.Items[PermissionIdentity.ApiAuthenticatedItem] = true;
        await _next(ctx);
    }

    private static bool IsLocal(HttpContext ctx)
    {
        var remote = ctx.Connection.RemoteIpAddress;
        // RemoteIpAddress 已经过 ForwardedHeaders 中间件按受信代理重写
        return remote == null || IPAddress.IsLoopback(remote);
    }
}
