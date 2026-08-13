// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Furion.Localization;
using Furion.UnifyResult;

using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Abc.Utils;

/// <summary>
/// 防止重复提交 特性
/// </summary>
//public class PreventSpamFilterAttribute : ActionFilterAttribute, IActionFilter
public class PreventSpamFilterAttribute : IAsyncActionFilter
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="limitAreas">限制的 区域名字，默认* ，表示全部都要限制,多个area用英文逗号隔开</param>
    /// <param name="preventSpamHttpMethodType">防止重复提交Http类型</param>
    /// <param name="httpAccessInterval">Http 访问间隔(毫秒),默认 10毫秒</param>
    public PreventSpamFilterAttribute(string limitAreas = "*",
                                PreventSpamHttpMethodType preventSpamHttpMethodType = PreventSpamHttpMethodType.All,
                                int httpAccessInterval = 10)
    {
        LimitAreas = limitAreas;
        PreventSpamHttpMethodType = preventSpamHttpMethodType;
        HttpAccessInterval = httpAccessInterval;
    }

    ///// <summary>
    ///// 防止重复提交类型
    ///// </summary>
    //public PreventSpamType PreventSpamType = PreventSpamType.All;

    /// <summary>
    /// 限制的 区域名字，默认* ，表示全部都要限制,多个area用英文逗号隔开
    /// </summary>
    public string LimitAreas { get; set; } = "*";

    /// <summary>
    /// 防止重复提交类型
    /// </summary>
    public PreventSpamHttpMethodType PreventSpamHttpMethodType = PreventSpamHttpMethodType.All;

    /// <summary>
    /// Http 访问间隔(毫秒),默认 10毫秒
    /// </summary>
    public int HttpAccessInterval = 10;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        //// 判断是否是 Mvc 控制器
        //var isMvcController = typeof(Controller).IsAssignableFrom(actionDescriptor.ControllerTypeInfo);

        var area = context.RouteData.Values["area"]?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(area))
        {
            await Next(context, next);
        }
        //else if (this.PreventSpamType == PreventSpamType.All)
        //{
        //    await Ver(context, next);
        //}
        //else if (this.PreventSpamType == PreventSpamType.API && area.ToLower() == "api")
        //{
        //    await Ver(context, next);
        //}
        //else if (this.PreventSpamType == PreventSpamType.MVC && area.ToLower() != "api")
        //{
        //    await Ver(context, next);
        //}
        else if (LimitAreas.ToLower().Contains(area.ToLower()))
        {
            await Ver(context, next);
        }
        else
        {
            await Next(context, next);
        }
    }

    private async Task Ver(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method.ToLower() == "get")
        {
            // 所有或者 GET方式
            if (PreventSpamHttpMethodType == PreventSpamHttpMethodType.All || PreventSpamHttpMethodType == PreventSpamHttpMethodType.Query)
            {
                ValidationRepeatSubmit(context);
            }
        }
        else
        {
            // 所有或者 除 GET以外
            if (PreventSpamHttpMethodType == PreventSpamHttpMethodType.All || PreventSpamHttpMethodType == PreventSpamHttpMethodType.Submit)
            {
                ValidationRepeatSubmit(context);
            }
        }
        await Next(context, next);
    }

    private static async Task Next(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Result == null)
        {
            //执行
            var resultContext = await next();

            // 异常拦截
            if (resultContext.Exception != null)
            {
                //操作或操作筛选器导致异常
                // Exception thrown by action or action filter.
                // 设置为null以处理异常。
                //context.Exception = null;
                var _logger = AppEx.GetSeriLogger("PreventSpamFilterAttribute");
                _logger.Error(resultContext.Exception.ToStringEx());
            }
        }
    }

    /// <summary>
    /// 验证重复提交
    /// </summary>
    /// <param name="context"></param>
    private void ValidationRepeatSubmit(ActionExecutingContext context)
    {
        var memoryCache = App.GetService<IMemoryCache>();
        var DataId = ($"{context.HttpContext.Request.Path}_{context.HttpContext.Request.QueryString}_{context.ActionArguments.ToJson()}").ToMd5();
        Debug.WriteLine($"ID:{DataId}");
        var dic = context.ActionArguments.ToDictionary(k => k.Key, v => v.Value);
        if (memoryCache.TryGetValue(DataId, out object _))
        {
            var resultdata = new RESTfulResult<string>
            {
                Errors = L.Text["请不要重复提交"]
            };
            context.Result = new JsonResult(resultdata);
        }
        else
        {
            memoryCache.Set(DataId, context.ActionDescriptor.Id, DateTimeOffset.Now.AddMilliseconds(HttpAccessInterval));
        }
    }
}

///// <summary>
///// 防止重复提交类型
///// </summary>
//public enum PreventSpamType
//{
//    /// <summary>
//    /// 全部
//    /// </summary>
//    All = 0,

//    MVC = 1,
//    API = 2,
//}

/// <summary>
/// 防止重复提交Http类型
/// </summary>
public enum PreventSpamHttpMethodType
{
    /// <summary>
    /// 全部
    /// </summary>
    All = 0,

    /// <summary>
    /// 查询（GET 方式）
    /// </summary>
    Query = 1,

    /// <summary>
    /// 提交(除GET以外)
    /// </summary>
    Submit = 2,
}