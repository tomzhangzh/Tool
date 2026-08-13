// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 上传文件过滤器
/// </summary>
public class UploadFileFilterAttribute : ActionFilterAttribute
{
    /// <summary>
    /// 默认文件输出逻辑路径
    /// URL访问的逻辑路径 域名/逻辑路径/文件相对路径
    /// 这样可以隐藏真正的服务器文件路径
    /// </summary>
    public string LogicalPath { get; set; } = "/u/f";

    public UploadFileFilterAttribute(string logicalPath = "/u/f")
    {
        LogicalPath = logicalPath;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var pathurl = Path.Combine(context.HttpContext.Request.Path, context.HttpContext.Request.QueryString.Value ?? "");
        if (!pathurl.ToLower().StartsWith(LogicalPath))
        {
            //在此进行一系列访问权限验证，如果失败，返回一个默认图片，例如logo或不允许访问的提示图片

            //文件上传文件夹
            var webUploadFloder = AppEx.GetUploadDefaultFloder();
            var filepath = Path.Combine(webUploadFloder, "upload", $"{pathurl.TrimStart(LogicalPath)}");

            context.Result = new PhysicalFileResult(filepath, "application/octet-stream");
        }
        else
        {
            await next();
        }
    }
}