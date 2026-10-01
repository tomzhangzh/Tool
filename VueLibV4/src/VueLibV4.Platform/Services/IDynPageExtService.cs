using VueLibV4.Services.Dependency;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 页面实例扩展视图（DynWebPage.ExtViewPath）文件服务。
/// 接口定义在 Platform 层（PageGenService 经此反转依赖），实现位于 Web 层
/// （物理文件、Razor 视图目录都是 Web 层概念）。
///
/// 扩展视图约定：一个真实 cshtml，执行时把 <see cref="SlotsKey"/> 槽位字典填满——
/// HTML 片段按具名槽位（list:toolbar / list:row-actions / filter:actions / detail:footer /
/// tree:tools 等）插入内置 Block；script[tag=dynconfig-ext] 片段合并进对应 Block 的 VueApp。
/// </summary>
public interface IDynPageExtService : IScopeDependency
{
    /// <summary>ViewData 中槽位字典的键（扩展视图从此键取字典并填充）。</summary>
    const string SlotsKey = "DynExtSlots";

    /// <summary>扩展视图根目录（~/Views/DynPages/Ext/），所有扩展文件必须位于此目录下。</summary>
    string ExtRoot { get; }

    /// <summary>校验扩展视图路径：ExtRoot 前缀 + .cshtml 后缀 + 无路径穿越段。</summary>
    bool IsValidPath(string? viewPath);

    /// <summary>视图文件是否物理存在（非法路径返回 false）。</summary>
    bool ViewExists(string? viewPath);

    /// <summary>
    /// 按页面 Code 确保骨架文件存在（已存在则【不覆盖】，保护用户手工定制），返回视图路径。
    /// </summary>
    Task<string> EnsureSkeletonAsync(string code, string pageName, CancellationToken ct = default);
}
