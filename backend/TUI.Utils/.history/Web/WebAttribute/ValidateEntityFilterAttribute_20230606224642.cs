// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.AspNetCore.Mvc.Controllers;

namespace TUI.Utils;

//官方地址 https://docs.microsoft.com/zh-cn/aspnet/core/mvc/controllers/filters?view=aspnetcore-5.0#action-filters

/// <summary>
/// 数据验证 特性
/// </summary>
//public class ValidateEntityFilterAttribute : ActionFilterAttribute

public class ValidateEntityFilterAttribute : IAsyncActionFilter
{
    public ValidateEntityFilterAttribute()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="type">验证实体类型 ,</param>
    /// <param name="isMvcReutrnMultipleErrors">MVC 是否返回多条错误消息  默认 fasle</param>
    public ValidateEntityFilterAttribute(ValidationEntityType type = ValidationEntityType.All
, bool isMvcReutrnMultipleErrors = false)
    {
        ValidationEntityType = type;
        IsMvcReutrnMultipleErrors = isMvcReutrnMultipleErrors;
    }

    /// <summary>
    /// 验证实体类型
    /// </summary>
    public ValidationEntityType ValidationEntityType = ValidationEntityType.All;

    /// <summary>
    /// MVC 是否返回多条错误消息
    /// </summary>
    public bool IsMvcReutrnMultipleErrors { get; set; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        #region 拦截之前

        if (!context.ModelState.IsValid)
        {
            //验证未通过
            //赋值 Result 后 会中断后续操作

            var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            // 判断是否是 Mvc 控制器
            var isMvcController = typeof(Controller).IsAssignableFrom(actionDescriptor.ControllerTypeInfo);
            if (isMvcController)
            {
                //MVC
                if (ValidationEntityType == ValidationEntityType.All || ValidationEntityType == ValidationEntityType.MVC)
                {
                    context.Result = new OkObjectResult(context.ModelState);
                    var errormsg = context.ModelState.JsonResult(isReutrnMultipleErrors: IsMvcReutrnMultipleErrors); ;
                    var jsonresult = new JsonResult(errormsg);
                    context.Result = jsonresult;
                }
            }
            else
            {
                //API
                if (ValidationEntityType == ValidationEntityType.All || ValidationEntityType == ValidationEntityType.API)
                {
                    context.Result = new OkObjectResult(context.ModelState);
                    var errormsg = context.ModelState.JsonResult(isReutrnMultipleErrors: true); ;
                    var jsonresult = new JsonResult(errormsg);
                    context.Result = jsonresult;
                }
            }
        }

        #endregion 拦截之前

        //执行
        if (context.Result == null)
        {
            var resultContext = await next();

            #region 拦截之后

            var result = context.Result;

            #endregion 拦截之后

            // 异常拦截
            if (resultContext.Exception != null)
            {
                //操作或操作筛选器导致异常
                // Exception thrown by action or action filter.
                // 设置为null以处理异常。
                //context.Exception = null;
                AppEx.GetSeriLogger("ValidateEntityFilterAttribute").Error(resultContext.Exception.ToStringEx());
            }
        }
        else
        {
        }
    }
}

/// <summary>
/// 验证实体类型
/// </summary>
public enum ValidationEntityType
{
    /// <summary>
    /// 全部
    /// </summary>
    All = 0,

    MVC = 1,
    API = 2,
}