using Furion.DataValidation;
using Furion.UnifyResult;

namespace TUI.Utils;

/// <summary>
/// RESTful 风格返回值
/// </summary>
[SuppressSniffer, UnifyModel(typeof(RESTfulResult<>))]
public class WebRESTfulResultProvider : IUnifyResultProvider
{
    /// <summary>
    /// 异常返回值
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata)
    {
        // 解析异常信息

        //return new JsonResult(new ResultData<object>
        //{
        //    StatusCode = metadata.StatusCode,
        //    // ErrorCode = ErrorCode,
        //    Succeeded = false,
        //    Data = null,
        //    Errors = metadata.Errors,
        //    Extras = UnifyContext.Take(),
        //    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        //});
        return new JsonResult(RESTfulResult(metadata.StatusCode, errors: metadata.Errors));
    }

    /// <summary>
    /// 成功返回值
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public IActionResult OnSucceeded(ActionExecutedContext context, object data)
    {
        //object data;
        // 处理内容结果
        //if (context.Result is ContentResult contentResult) data = contentResult.Content;
        // 处理对象结果
        //else if (context.Result is ObjectResult objectResult) data = objectResult.Value;
        //else if (context.Result is EmptyResult) data = null;
        //else return null;

        //return new JsonResult(new ResultData<object>
        //{
        //    StatusCode = context.Result is EmptyResult ? StatusCodes.Status204NoContent : StatusCodes.Status200OK,  // 处理没有返回值情况 204
        //    Succeeded = true,
        //    Data = data,
        //    Errors = null,
        //    Extras = UnifyContext.Take(),
        //    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        //});

        return new JsonResult(RESTfulResult(StatusCodes.Status200OK, true, data));
    }

    /// <summary>
    /// 验证失败返回值
    /// </summary>
    /// <param name="context"></param>
    /// <param name="modelStates"></param>
    /// <param name="validationResults"></param>
    /// <param name="validateFailedMessage"></param>
    /// <returns></returns>
    public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
    {
        //return new JsonResult(new ResultData<object>
        //{
        //    StatusCode = StatusCodes.Status400BadRequest,
        //    Succeeded = false,
        //    Data = null,
        //    Errors = metadata.ValidationResult,
        //    Extras = UnifyContext.Take(),
        //    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        //});
        return new JsonResult(RESTfulResult(StatusCodes.Status400BadRequest, errors: metadata.ValidationResult));
    }

    /// <summary>
    /// 处理输出状态码
    /// </summary>
    /// <param name="context"></param>
    /// <param name="statusCode"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public async Task OnResponseStatusCodes(HttpContext context, int statusCode, UnifyResultStatusCodesOptions options = null)
    {
        // 设置响应状态码
        UnifyContext.SetResponseStatusCodes(context, statusCode, options);
        switch (statusCode)
        {
            // 处理 401 状态码
            case StatusCodes.Status401Unauthorized:
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, errors: "401 Unauthorized")
                    , App.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;
            // 处理 403 状态码
            case StatusCodes.Status403Forbidden:
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, errors: "403 Forbidden")
                    , App.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 返回 RESTful 风格结果集
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="succeeded"></param>
    /// <param name="data"></param>
    /// <param name="errors"></param>
    /// <returns></returns>
    private static ResultData<object> RESTfulResult(int statusCode, bool succeeded = default, object data = default, object errors = default)
    {
        return new ResultData<object>
        {
            StatusCode = statusCode,
            Succeeded = succeeded,
            Data = data,
            Errors = errors,
            Extras = UnifyContext.Take(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}