#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using VueLibV4.Web.Dtos;

namespace VueLibV4.Web.Services;

/// <summary>
/// Razor 组件渲染器 - 将 .cshtml 组件定义渲染并解析为 ComponentDefineDto
///
/// 组件 View 约定（两种格式均支持）:
///   1. V4 标记格式: Views/Shared/Components/{ComponentName}.cshtml
///       用 <!--TEMPLATE_START-->...<!--TEMPLATE_END--> 等标记包裹
///   2. V1 Area 格式: Areas/ElementComponent/Views/{Container|Common|FormItem}/{ComponentName}.cshtml
///       输出 <template>...</template> + <script tag='comconfig'>var comConfig = {...}</script>
///       其中 comconfig script 即 Vue 组件选项（setup 内使用全局 Vue / inject 依赖注入）
///
/// 文件头元数据注释:
///   @* ComponentType: Page *@
///   @* RoutePath: /about *@
///   @* Description: 关于页面 *@
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

    /// <summary>Area 组件搜索目录（对应 V1 ElementComponent 三个控制器）</summary>
    private static readonly string[] AreaViewDirs = { "Container", "Common", "FormItem" };

    /// <summary>检查指定名称的 Razor 组件是否存在</summary>
    public bool Exists(string componentName)
    {
        return FindViewPath(componentName) != null;
    }

    /// <summary>查找组件 View 路径（V4 标记格式优先，V1 Area 格式回退）</summary>
    private string? FindViewPath(string componentName)
    {
        var v4Path = $"/Views/Shared/Components/{componentName}.cshtml";
        if (_viewEngine.GetView(null, v4Path, false).Success) return v4Path;

        foreach (var dir in AreaViewDirs)
        {
            var areaPath = $"/Areas/ElementComponent/Views/{dir}/{componentName}.cshtml";
            if (_viewEngine.GetView(null, areaPath, false).Success) return areaPath;
        }
        return null;
    }

    /// <summary>渲染指定 Razor 组件并解析为 ComponentDefineDto</summary>
    public async Task<ComponentDefineDto?> RenderAsync(string componentName)
    {
        var viewPath = FindViewPath(componentName);
        if (viewPath == null)
        {
            _logger.LogDebug("Razor 组件不存在: {ComponentName}", componentName);
            return null;
        }

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
                _logger.LogInformation("从 Razor View 加载组件: {ComponentName} ({ViewPath})", componentName, viewPath);
            }
            return define;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "渲染 Razor 组件失败: {ComponentName}", componentName);
            return null;
        }
    }

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
    /// 支持两种格式：
    ///   A. V4 标记: <!--TEMPLATE_START-->...<!--TEMPLATE_END-->
    ///   B. V1 Area: <template>...</template> + <script tag='comconfig'>var comConfig = {...}</script>
    /// </summary>
    private static ComponentDefineDto? ParseComponentHtml(string html, string componentName)
    {
        // A. V4 标记格式
        var template = ExtractSection(html, "TEMPLATE");
        var script = ExtractSection(html, "SCRIPT");
        var style = ExtractSection(html, "STYLE");

        if (string.IsNullOrWhiteSpace(template) && string.IsNullOrWhiteSpace(script))
        {
            // B. V1 Area 格式：<template> + <script tag='comconfig'>
            template = ExtractV1Template(html);
            script = ExtractV1ComConfig(html);
            style = null;
        }

        if (string.IsNullOrWhiteSpace(template) && string.IsNullOrWhiteSpace(script))
        {
            return null;
        }

        var typeMatch = System.Text.RegularExpressions.Regex.Match(
            html, @"<!--COMPONENT_TYPE:(\d+)-->");
        var componentType = typeMatch.Success && int.TryParse(typeMatch.Groups[1].Value, out var ct)
            ? ct
            : (int)ComponentType.Common;

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

    /// <summary>提取 V1 格式的 &lt;template&gt; 内 HTML（深度计数，支持内嵌 &lt;template #xxx&gt;）</summary>
    private static string? ExtractV1Template(string html)
    {
        var startIdx = html.IndexOf("<template>", StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0) return null;
        startIdx += "<template>".Length;

        int depth = 0;
        int i = startIdx;
        int scanStart = startIdx;
        while (i < html.Length)
        {
            var open = html.IndexOf("<template", i, StringComparison.OrdinalIgnoreCase);
            var close = html.IndexOf("</template>", i, StringComparison.OrdinalIgnoreCase);
            if (close < 0) break;

            if (open >= 0 && open < close)
            {
                // 遇到 <template ...>（含 <template> / <template #xxx> / <template v-slot:xxx>）
                depth++;
                i = open + "<template".Length;
                // 跳过该标签到 '>'
                var gt = html.IndexOf('>', i);
                i = (gt < 0) ? html.Length : gt + 1;
            }
            else
            {
                if (depth == 0)
                {
                    // 第一个闭合即外层模板结束
                    return html.Substring(scanStart, close - scanStart).Trim();
                }
                depth--;
                i = close + "</template>".Length;
            }
        }
        // 兜底：未找到闭合，返回起点到末尾
        return html.Substring(scanStart).Trim();
    }

    /// <summary>
    /// 提取 V1 格式的 comconfig script（<script tag='comconfig'>var comConfig = {...}</script>），
    /// 转为 vue-loader 可执行的组件选项表达式（return {...}）。
    /// 与 V1 nut-runtime 行为一致：执行 script 后取 comConfig 对象作为组件选项。
    /// </summary>
    private static string? ExtractV1ComConfig(string html)
    {
        var m = System.Text.RegularExpressions.Regex.Match(html,
            @"<script[^>]*tag\s*=\s*['""]comconfig['""][^>]*>([\s\S]*?)</script>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!m.Success) return null;

        var text = m.Groups[1].Value.Trim();
        // 去掉 "var comConfig =" 前缀，保留对象字面量
        var obj = System.Text.RegularExpressions.Regex.Replace(text,
            @"^\s*var\s+comConfig\s*=\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        obj = obj.Trim().TrimEnd(';').Trim();
        // 组件选项表达式（与 V1 相同：comConfig 对象 + 注入 template）
        return "return " + obj + ";";
    }

    private static string? ExtractSection(string html, string sectionName)
    {
        var pattern = $"<!--{sectionName}_START-->([\\s\\S]*?)<!--{sectionName}_END-->";
        var match = System.Text.RegularExpressions.Regex.Match(html, pattern);
        if (!match.Success) return null;
        return match.Groups[1].Value.Trim();
    }
}
