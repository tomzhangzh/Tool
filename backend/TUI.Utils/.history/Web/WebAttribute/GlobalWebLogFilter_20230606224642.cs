// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace Apc.Utils;

/// <summary>
/// Web 全局调用日志记录
/// </summary>
public class GlobalWebLogFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 获取控制器/操作描述器
        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

        // 获取请求方法
        var actionMethod = controllerActionDescriptor.MethodInfo;

        // [DisplayName] 特性
        var displayNameAttribute = actionMethod.IsDefined(typeof(DisplayNameAttribute), true)
            ? actionMethod.GetCustomAttribute<DisplayNameAttribute>(true)
            : default;

        var sbInfo = context.HttpContext.GetWebInfoToString();
        sbInfo.AppendLine($"━━━━━━━━━━━━━━━  其它信息 ━━━━━━━━━━━━━━━");
        sbInfo.AppendLine($"##控制器名称## {controllerActionDescriptor.ControllerTypeInfo.Name}");
        sbInfo.AppendLine($"##操作名称## {actionMethod.Name}");
        sbInfo.AppendLine($"##显示名称## {displayNameAttribute?.DisplayName}");

        #region 拦截之前

        //提交的参数
        sbInfo.AppendLine($"参数 ActionArguments:{context.ActionArguments.ToJson()}");
        if (context.ModelState != null && context.ModelState.IsValid == false)
        {
            //验证信息
            var verifications = context.ModelState.ToDictionary(x => x.Key, v => v.Value.Errors.Select(o => o.ErrorMessage).ToList());
            sbInfo.AppendLine($"数据验证失败返回：{verifications.ToJson()}");
        }

        #endregion 拦截之前

        //执行
        var resultContext = await next();

        #region 拦截之后

        //访问返回
        if (resultContext.Result is JsonResult)
        {
            if (resultContext.Result is JsonResult jsonresult) sbInfo.AppendLine($"\r\n访问返回：{jsonresult.Value.ToJson()}");
        }

        #endregion 拦截之后

        var logFloderName = GetLogFloderName(context.HttpContext, context.RouteData);
        var _logger = AppEx.GetSeriLogger(logFloderName: logFloderName);
        // 异常拦截
        if (resultContext.Exception != null)
        {
            //操作或操作筛选器导致异常
            // Exception thrown by action or action filter.
            // 设置为null以处理异常。
            //context.Exception = null;

            sbInfo.AppendLine($"\r\n异常：{resultContext.Exception.ToStringEx()}");
            _logger?.Error(sbInfo.ToString());
        }
        else
        {
            //LogVisitEntity logVisitEntity = new();
            //context.GetWebVisitInfo(logVisitEntity);

            //var scopeFactory = App.GetService<IServiceScopeFactory>();
            //using var scope = scopeFactory.CreateScope();
            //var services = scope.ServiceProvider;
            //var _htLogVisitService = App.GetService<IHtLogVisitService>(services);

            //var input = logVisitEntity.Adapt<HtLogVisitInput>();
            //_htLogVisitService.Insert(input).GetAwaiter().GetResult();
        }
        _logger?.Debug(sbInfo.ToString());
    }

    /// <summary>
    /// 获取文件夹名称
    /// </summary>
    /// <param name="httpcontext"></param>
    /// <param name="routeData"></param>
    /// <returns></returns>
    private static string GetLogFloderName(HttpContext httpcontext, RouteData routeData)
    {
        var logFloderName = "default";

        var area = routeData.Values["area"]?.ToString() ?? "";
        var controller = routeData.Values["controller"]?.ToString() ?? "";
        var action = routeData.Values["action"]?.ToString() ?? "";

        if (!string.IsNullOrWhiteSpace(area))
        {
            logFloderName = area;
        }
        else
        {
            var path1 = $"/{area}/{controller}/{action}".Trim('/');//  /test/test
            var path = httpcontext.Request.Path.ToString(); //path:api/test/test
            if (path.Contains(path1))
            {
                area = path.RemoveLastStr(path1);
                if (!string.IsNullOrWhiteSpace(area))
                {
                    logFloderName = area.Trim("//").Trim('/');
                }
            }
        }
        if (!string.IsNullOrWhiteSpace(controller))
        {
            logFloderName += $"\\{controller}";
        }
        logFloderName = logFloderName.Trim('\\');

        return logFloderName;
    }
}