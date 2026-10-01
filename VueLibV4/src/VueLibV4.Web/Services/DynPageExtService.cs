using System.Text;
using System.Text.RegularExpressions;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Services;

/// <summary>
/// 页面实例扩展视图文件服务（IDynPageExtService 的 Web 层实现）。
/// 所有扩展视图限定在 ~/Views/DynPages/Ext/ 目录内（白名单 + 物理路径收敛，杜绝路径穿越）。
/// </summary>
public class DynPageExtService : IDynPageExtService
{
    private readonly IWebHostEnvironment _env;

    public DynPageExtService(IWebHostEnvironment env) => _env = env;

    public string ExtRoot => "~/Views/DynPages/Ext/";

    /// <summary>扩展视图物理根目录（ContentRoot/Views/DynPages/Ext）。</summary>
    private string PhysicalRoot
    {
        get
        {
            var rel = ExtRoot.Substring(2).Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(_env.ContentRootPath, rel));
        }
    }

    public bool IsValidPath(string? viewPath)
    {
        if (string.IsNullOrWhiteSpace(viewPath)) return false;
        var p = viewPath.Trim().Replace('\\', '/');
        if (!p.StartsWith(ExtRoot, StringComparison.OrdinalIgnoreCase)) return false;
        if (!p.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)) return false;
        var file = p.Substring(ExtRoot.Length);
        if (string.IsNullOrWhiteSpace(file) || file.Contains('/') || file.Contains("..")) return false;
        // 文件名只允许 字母数字 - _ .
        return Regex.IsMatch(file, @"^[A-Za-z0-9._-]+\.cshtml$", RegexOptions.IgnoreCase);
    }

    public bool ViewExists(string? viewPath)
    {
        if (!IsValidPath(viewPath)) return false;
        try { return File.Exists(MapPhysical(viewPath!)); }
        catch { return false; }
    }

    public async Task<string> EnsureSkeletonAsync(string code, string pageName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("缺少页面 Code");
        var file = SanitizeCode(code) + ".ext.cshtml";
        var viewPath = ExtRoot + file;
        var phys = Path.Combine(PhysicalRoot, file);
        Directory.CreateDirectory(PhysicalRoot);
        if (!File.Exists(phys))
        {
            var content = BuildSkeleton(code.Trim(), pageName ?? code);
            await File.WriteAllTextAsync(phys, content, new UTF8Encoding(true), ct);
        }
        return viewPath;
    }

    /// <summary>~/ 视图路径 → ContentRoot 下物理路径（调用前应已通过 IsValidPath）。</summary>
    private string MapPhysical(string viewPath)
    {
        var file = viewPath.Trim().Replace('\\', '/').Substring(ExtRoot.Length);
        return Path.GetFullPath(Path.Combine(PhysicalRoot, file));
    }

    /// <summary>Code 收敛为安全文件名片段（字母数字-下划线-短横，其余替换为 _）。</summary>
    private static string SanitizeCode(string code)
    {
        var s = Regex.Replace(code.Trim(), @"[^A-Za-z0-9_-]", "_");
        return string.IsNullOrWhiteSpace(s) ? "page" : s;
    }

    /// <summary>
    /// 扩展视图骨架：真实 Razor 文件，含全部内置槽位约定说明 + 可一键启用的示例。
    /// 注意：骨架内 JS 的 @ 一律写 @@（Razor 转义）；占位符 [[CODE]]/[[NAME]] 生成时替换。
    /// </summary>
    private static string BuildSkeleton(string code, string pageName)
    {
        var nameJs = (pageName ?? code).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        return """
@model VueLibV4.Platform.Models.DynWebPage
@using VueLibV4.Web.Core
@*
  ========================================================================
  页面扩展视图（DynWebPage.ExtViewPath）—— 页面 Code：[[CODE]]
  ------------------------------------------------------------------------
  · 本文件是真实 cshtml，可随意手工修改；Razor 运行时编译，保存即生效。
  · 页面生成器只在文件不存在时生成骨架，永远不会覆盖你改过的内容。
  · 原理：运行时模板先执行本视图，把 HTML/脚本片段填入 Slots 字典的具名槽位，
    再由内置 Block 在自己的锚点处输出；脚本片段在 Block 的 VueApp 创建前合并。

  可用槽位（{block角色}:{区域}）：
    list:toolbar        列表工具栏（内置「+ 新增」之后）
    list:row-actions    列表行操作列（编辑 / +子 / 删除 之后）
    list:app-ext        合并进 list Block 的脚本（script[tag=dynconfig-ext]）
    filter:actions      筛选按钮区（查询 / 重置 之后）
    filter:app-ext      合并进 filter Block 的脚本
    detail:footer       表单底部按钮区（取消 / 删除 / 保存 之后）
    detail:app-ext      合并进 detail Block 的脚本
    tree:tools          树形页左树工具区
    tree:app-ext        合并进 tree Block 的脚本

  脚本合并规则（dynconfig-ext）：
    · 声明 var dynConfigExt = { methods/computed/data/mounted... }
    · 脚本内 this 即对应 Block 的 Vue 实例：可直接访问 this.model / this.table /
      this.loadData() 等内部状态与方法；methods/computed 同名时扩展优先（可覆盖）；
      data 浅合并；created/mounted 等生命周期在 Block 自身之后串联执行。
    · cshtml 内 JS 的 @ 必须写 @@。
    · 不需要 JS 时，按钮也可直接用 data-dyn-click-open/post/... 声明式动作链（纯 HTML）。
  ========================================================================
*@
@{
    // 槽位字典由运行时注入（ViewData[DynExtSlots]）
    var Slots = ViewData["DynExtSlots"] as DynExtSlots ?? new DynExtSlots();
}

@* ===== 示例：删除外层 @if(false) 即启用 ===== *@
@if (false)
{
    @* ① 往 list Block 工具栏追加按钮（需要配合 ② 的方法） *@
    Slots["list:toolbar"] = @<button type="button"
        class="el-button el-button--success el-button--small" @@click="onExtDemo()">
        扩展按钮
    </button>;

    @* ② 脚本合并进 list Block 的 VueApp *@
    Slots["list:app-ext"] = @<script type="text/plain" tag="dynconfig-ext">
    var dynConfigExt = {
        methods: {
            onExtDemo: function () {
                // this 即 list Block 实例：this.model.rows / this.table / this.loadData() 都能用
                dyn.showMessage('[[NAME]] 扩展：当前表 ' + this.table + '，共 ' + (this.model.total || 0) + ' 条', 'success');
            }
        }
    };
    </script>;

    @* ③ 零 JS 的写法：声明式动作链按钮（打开另一个页面弹窗） *@
    @* Slots["list:toolbar"] = @<button type="button" class="el-button el-button--small"
           data-dyn-click-open="/Platform/Page/DynWebPage?id=0" data-dyn-click-mode="fragment">打开页面</button>; *@
}
""".Replace("[[CODE]]", code).Replace("[[NAME]]", nameJs);
    }
}
