using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VueLibV4.Web.Services;

namespace VueLibV4.Web.Core;

/// <summary>
/// 全局权限校验过滤器：
/// 1) 带 [AllowAnonymousPermission] 的端点直接放行（登录、前端错误上报等）；
/// 2) 未登录：API(/api/) 返回 401 JSON 信封，页面 302 跳登录页（带 returnUrl）；
/// 3) 已登录再按 [ResourcePermission]/[OperationPermission] 特性做资源级校验，失败 403。
/// 认证身份由 PermissionService 统一解析（签名 cookie / API Key 服务账户 / Windows 认证）。
/// </summary>
public class PermissionFilter : IAsyncActionFilter
{
    private readonly IPermissionService _perm;

    public PermissionFilter(IPermissionService perm)
    {
        _perm = perm;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (HasAllowAnonymous(context))
        {
            await next();
            return;
        }

        // 登录拦截：未登录访问受保护端点
        var path = context.HttpContext.Request.Path.Value ?? "";
        if (!_perm.IsAuthenticated)
        {
            var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
            if (isApi)
            {
                context.Result = UnauthorizedResult();
                return;
            }
            // 仅对页面 GET 做登录跳转；其它情况（理论上不应出现）也 401
            if (HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                var returnUrl = Uri.EscapeDataString(path + context.HttpContext.Request.QueryString.Value);
                context.Result = new RedirectResult($"/Platform/Page/Login?returnUrl={returnUrl}");
                return;
            }
            context.Result = UnauthorizedResult();
            return;
        }

        var resourceAttrs = context.ActionDescriptor.EndpointMetadata.OfType<ResourcePermissionAttribute>().ToList();
        var operationAttrs = context.ActionDescriptor.EndpointMetadata.OfType<OperationPermissionAttribute>().ToList();

        foreach (var attr in resourceAttrs)
        {
            var ok = attr.Level switch
            {
                "Edit" => await _perm.CanEditAsync(attr.ResourceKey),
                "Delete" => await _perm.CanDeleteAsync(attr.ResourceKey),
                _ => await _perm.CanReadAsync(attr.ResourceKey)
            };
            if (!ok)
            {
                context.Result = ForbidResult();
                return;
            }
        }

        foreach (var attr in operationAttrs)
        {
            if (!await _perm.CanEditAsync(attr.ResourceKey))
            {
                context.Result = ForbidResult();
                return;
            }
        }

        await next();
    }

    private static bool HasAllowAnonymous(ActionExecutingContext context)
        => context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousPermissionAttribute>().Any();

    private static JsonResult UnauthorizedResult()
        => new(new { code = 401, msg = "未登录或登录已过期" }) { StatusCode = 401 };

    private static JsonResult ForbidResult()
        => new(new { code = 403, msg = "没有权限执行此操作" }) { StatusCode = 403 };
}
