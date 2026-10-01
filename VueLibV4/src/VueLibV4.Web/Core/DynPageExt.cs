using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Core;

/// <summary>
/// 页面扩展槽位字典：key = {block角色}:{区域}（忽略大小写，如 list:toolbar），
/// value = 扩展视图中用 Razor 模板委托（@&lt;button&gt;…&lt;/button&gt;）提供的 HTML/脚本片段。
/// </summary>
public sealed class DynExtSlots : Dictionary<string, Func<object?, IHtmlContent>>
{
    public DynExtSlots() : base(StringComparer.OrdinalIgnoreCase) { }

    /// <summary>输出槽位内容；槽位不存在返回 null（Razor 中即不输出任何内容）。</summary>
    public IHtmlContent? Render(string key)
        => TryGetValue(key, out var f) ? f(null) : null;
}

/// <summary>Razor 中加载页面扩展：@inject 不需要，模板里直接 await Html.LoadPageExtAsync(Model)。</summary>
public static class DynPageExtHtmlHelperExtensions
{
    /// <summary>
    /// 执行页面实例扩展视图（DynWebPage.ExtViewPath）收集槽位片段。
    /// 扩展视图只负责往 ViewData[DynExtSlots] 字典里填模板委托，其自身标记输出为空（丢弃）。
    /// 未配置 / 路径非法 / 文件不存在时返回空字典，对现有页面零影响。
    /// </summary>
    public static async Task<DynExtSlots> LoadPageExtAsync(this IHtmlHelper html, DynWebPage? page)
    {
        var slots = new DynExtSlots();
        var path = page?.ExtViewPath;
        if (page is null || string.IsNullOrWhiteSpace(path)) return slots;

        var ctx = html.ViewContext.HttpContext;
        var svc = ctx.RequestServices.GetService<IDynPageExtService>();
        if (svc is null || !svc.IsValidPath(path))
        {
            if (svc is not null) ctx.RequestServices.GetService<ILoggerFactory>()?
                .CreateLogger("DynPageExt").LogWarning("扩展视图路径非法，已忽略：{Path}", path);
            return slots;
        }
        if (!svc.ViewExists(path))
        {
            ctx.RequestServices.GetService<ILoggerFactory>()?
                .CreateLogger("DynPageExt").LogWarning("扩展视图文件不存在，已忽略：{Path}", path);
            return slots;
        }

        // 克隆当前 ViewData（保留下发参数），Model 显式设为页面实例，注入槽位字典
        var vd = new ViewDataDictionary(html.ViewData) { Model = page };
        vd[IDynPageExtService.SlotsKey] = slots;
        await html.PartialAsync(path, page, vd);
        return slots;
    }
}
