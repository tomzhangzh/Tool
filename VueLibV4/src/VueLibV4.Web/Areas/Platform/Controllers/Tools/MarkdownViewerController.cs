using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 工具页面：Markdown 文档查看器（docs 目录内文档）。
/// 访问示例：/Platform/Tools/MdViewer?path=README.md
/// </summary>
[Area("Platform")]
public class MarkdownViewerController : Controller
{
    private const string ToolView = "~/Areas/Platform/Views/Tools/{0}.cshtml";

    /// <summary>Markdown 文档查看器（docs 目录内文档；安全：规范化路径并限制在 docs 内）</summary>
    [HttpGet("/Platform/Tools/MdViewer")]
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
        return View(string.Format(ToolView, "MdViewer"));
    }
}
