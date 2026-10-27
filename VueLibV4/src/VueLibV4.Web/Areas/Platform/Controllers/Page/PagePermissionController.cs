using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面权限控制器：权限配置 Grid。
/// 访问示例：/Platform/Permission/Grid?roleId=1&amp;project=__platform__
/// </summary>
[Area("Platform")]
public class PagePermissionController : Controller
{
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

    /// <summary>权限配置 Grid（el-table tree）：按 roleId 加载资源树 + 已有权限回显</summary>
    [HttpGet("/Platform/Permission/Grid")]
    public IActionResult PermissionGrid(long? roleId = null, string project = "__platform__", string execId = null)
    {
        ViewBag.RoleId = roleId ?? 0;
        ViewBag.Project = project;
        ViewBag.ExecId = execId ?? ("PG-" + Guid.NewGuid().ToString("N")[..10]);
        return View(string.Format(Page,"PermissionGrid"));
    }
}
