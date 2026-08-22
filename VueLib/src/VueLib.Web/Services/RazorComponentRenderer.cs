using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using VueLib.Web.Dtos;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// Razor 组件渲染器 - 将 .cshtml 组件定义渲染并解析为 ComponentDefineDto
///
/// 组件 View 约定:
///   - 路径: Views/Shared/Components/{ComponentName}.cshtml
///   - 模型: ComponentViewModel
///   - View 中设置 Model.Template / Model.Script / Model.Style
///   - 输出格式: 用 <!--TEMPLATE_START-->...<!--TEMPLATE_END--> 等标记包裹
/// </summary>
public class RazorComponentRenderer
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RazorComponentRenderer> _logger;

    public RazorComponentRenderer(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider,
        ILogger<RazorComponentRenderer> logger)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// 检查指定名称的 Razor 组件是否存在
    /// </summary>
    public bool Exists(string componentName)
    {
        var viewPath = $"/Views/Shared/Components/{componentName}.cshtml";
        var result = _viewEngine.GetView(null, viewPath, false);
        return result.Success;
    }

    /// <summary>
    /// 渲染指定 Razor 组件并解析为 ComponentDefineDto
    /// </summary>
    public async Task<ComponentDefineDto?> RenderAsync(string componentName)
    {
        var viewPath = $"/Views/Shared/Components/{componentName}.cshtml";
        var viewResult = _viewEngine.GetView(null, viewPath, false);

        if (!viewResult.Success)
        {
            _logger.LogDebug("Razor 组件不存在: {ViewPath}", viewPath);
            return null;
        }

        try
        {
            var html = await RenderViewToStringAsync(viewResult.View, componentName);
            var define = ParseComponentHtml(html, componentName);
            if (define != null)
            {
                _logger.LogInformation("从 Razor View 加载组件: {ComponentName}", componentName);
            }
            return define;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "渲染 Razor 组件失败: {ComponentName}", componentName);
            return null;
        }
    }

    /// <summary>
    /// 渲染 View 为字符串
    /// </summary>
    private async Task<string> RenderViewToStringAsync(IView view, string componentName)
    {
        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        await using var sw = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            view,
            new ViewDataDictionary<ComponentViewModel>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = new ComponentViewModel { ComponentName = componentName }
            },
            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
            sw,
            new HtmlHelperOptions());

        await view.RenderAsync(viewContext);
        return sw.ToString();
    }

    /// <summary>
    /// 解析渲染后的 HTML，提取 template/script/style
    /// 约定标记:
    ///   <!--TEMPLATE_START--> ... <!--TEMPLATE_END-->
    ///   <!--SCRIPT_START--> ... <!--SCRIPT_END-->
    ///   <!--STYLE_START--> ... <!--STYLE_END-->
    /// </summary>
    private static ComponentDefineDto? ParseComponentHtml(string html, string componentName)
    {
        var template = ExtractSection(html, "TEMPLATE");
        var script = ExtractSection(html, "SCRIPT");
        var style = ExtractSection(html, "STYLE");

        if (string.IsNullOrWhiteSpace(template) && string.IsNullOrWhiteSpace(script))
        {
            return null;
        }

        // 从 HTML 中尝试提取组件类型和路由
        var typeMatch = System.Text.RegularExpressions.Regex.Match(
            html, @"<!--COMPONENT_TYPE:(\d+)-->");
        var componentType = typeMatch.Success && int.TryParse(typeMatch.Groups[1].Value, out var ct)
            ? (ComponentType)ct
            : ComponentType.Common;

        var routeMatch = System.Text.RegularExpressions.Regex.Match(
            html, @"<!--ROUTE_PATH:([^-->]+)-->");
        var routePath = routeMatch.Success ? routeMatch.Groups[1].Value.Trim() : null;

        return new ComponentDefineDto
        {
            ComponentName = componentName,
            ComponentType = componentType,
            RoutePath = routePath,
            TemplateContent = template ?? string.Empty,
            ScriptContent = script ?? string.Empty,
            StyleContent = style
        };
    }

    /// <summary>
    /// 提取标记之间的内容
    /// </summary>
    private static string? ExtractSection(string html, string sectionName)
    {
        var pattern = $"<!--{sectionName}_START-->([\\s\\S]*?)<!--{sectionName}_END-->";
        var match = System.Text.RegularExpressions.Regex.Match(html, pattern);
        if (!match.Success) return null;
        return match.Groups[1].Value.Trim();
    }
}
