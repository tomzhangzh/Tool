using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 工具页面：组件检查、ER 图、页面图谱、Markdown 查看器
/// </summary>
[Area("Platform")]
public class ToolController : Controller
{
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;
    private readonly IDynTemplateBlockService _tplBlocks;
    private readonly IDynBlockService _blocks;

    public ToolController(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings,
        IDynTemplateBlockService tplBlocks,
        IDynBlockService blocks)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
        _tplBlocks = tplBlocks;
        _blocks = blocks;
    }

    /// <summary>组件检查：在线测试组件</summary>
    [HttpGet("/Platform/Page/ComponentCheck")]
    public IActionResult ComponentCheck()
    {
        ViewData["Title"] = "组件检查 - VueLibV4";
        return View(string.Format(Page, "ComponentCheck"));
    }

    [HttpGet("/Platform/Page/PageGraphDemo")]
    public IActionResult PageGraphDemo(int id = 34)
    {
        ViewData["Title"] = "页面组成图 Demo - VueLibV4";
        var wp = _webPages.GetById(id);
        if (wp == null) return Content($"WebPage {id} 不存在");

        var tpl = wp.TemplateId > 0 ? _templates.GetById(wp.TemplateId) : null;
        var param = string.IsNullOrEmpty(wp.ParamsJson) ? new JObject() : JObject.Parse(wp.ParamsJson);
        var blocksNode = param["blocks"] as JObject ?? new JObject();

        // 取 Template 槽位定义
        var slots = tpl != null
            ? _tplBlocks.Query(x => x.TemplateId == tpl.Id).OrderBy(x => x.SortNo).ToList()
            : new List<VueLibV4.Platform.Models.DynTemplateBlock>();

        // 拼 Mermaid
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("flowchart LR");
        // WebPage 节点
        sb.AppendLine($"    WP[\"📄 {wp.Name}<br/><small>id={wp.Id}</small>\"]:::wp");
        // Template
        if (tpl != null)
        {
            sb.AppendLine($"    T[\"📐 {tpl.Name}<br/><small>{tpl.Code}</small>\"]:::tpl");
            sb.AppendLine("    WP --- T");
        }
        // 每个槽位一个 Block 节点 + 一个 PageSetting 节点
        int idx = 0;
        foreach (var s in slots)
        {
            var slot = s.Slot;
            var blk = _blocks.GetById(s.BlockId);
            var blkName = blk != null ? blk.Name : $"Block#{s.BlockId}";
            var role = blk != null ? (blk.ImplementsRole ?? "") : "";

            var bId = $"B{idx}";
            var psId = $"PS{idx}";
            var icon = role == "filter" ? "🔍" : role == "list" ? "📋" : role == "detail" ? "📝" : "🧩";
            sb.AppendLine($"    {bId}[\"{icon} {blkName}<br/><small>slot={slot}</small>\"]:::blk");

            // PageSetting
            int settingId = 0;
            if (blocksNode[slot] != null) int.TryParse(blocksNode[slot]["settingId"]?.ToString(), out settingId);
            if (settingId > 0)
            {
                var ps = _settings.GetById(settingId);
                if (ps != null)
                {
                    sb.AppendLine($"    {psId}[\"📄 {ps.Name}<br/><small>{ps.Code}</small>\"]:::ps");
                    sb.AppendLine($"    {bId} -.->|配置| {psId}");
                }
            }
            idx++;
        }
        // 内置消息流（filter->list）
        if (slots.Count >= 2)
        {
            sb.AppendLine("    B0 ==>|筛选条件| B1");
        }
        sb.AppendLine("    classDef wp fill:#eaf2ff,stroke:#409eff,stroke-width:2px,color:#1f3a68");
        sb.AppendLine("    classDef tpl fill:#fdf6ec,stroke:#e6a23c,stroke-width:2px,color:#7a5b17");
        sb.AppendLine("    classDef blk fill:#ecf5ff,stroke:#409eff,stroke-width:1.5px,color:#1f3a68");
        sb.AppendLine("    classDef ps fill:#f4f0fa,stroke:#9b59b6,stroke-width:1.5px,color:#5b2c6f");

        ViewData["Mermaid"] = sb.ToString();
        ViewData["WpName"] = wp.Name;
        return View(string.Format(Page, "PageGraphDemo"));
    }

    [HttpGet("/Platform/Page/DbErGraph")]
    public IActionResult DbErGraph()
    {
        ViewData["Title"] = "数据库 ER 关系图 - VueLibV4";
        return View(string.Format(Page, "DbErGraph"));
    }

    /// <summary>Markdown 文档查看器（docs 目录内文档；安全：规范化路径并限制在 docs 内）</summary>
    [HttpGet("/Platform/Page/MdViewer")]
    public IActionResult MdViewer(string path)
    {
        ViewData["Title"] = "文档 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        // 解决方案根 docs 目录（AppContext.BaseDirectory = src/VueLibV4.Web/bin/Debug/net8.0/）
        var docsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs"));
        if (!Directory.Exists(docsRoot)) return Content("文档目录不存在：" + docsRoot);
        var safe = Path.GetFullPath(Path.Combine(docsRoot, path ?? ""));
        if (!safe.StartsWith(docsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !safe.Equals(docsRoot, StringComparison.OrdinalIgnoreCase))
            return Content("无效文档路径");
        if (!System.IO.File.Exists(safe)) return Content("文档不存在：" + path);
        ViewBag.MdPath = path;
        ViewBag.MdContent = System.IO.File.ReadAllText(safe, System.Text.Encoding.UTF8);
        return View(string.Format(Page, "MdViewer"));
    }
}
