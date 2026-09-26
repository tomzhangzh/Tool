using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Services.Data;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// M5 平台管理页视图控制器：
/// 每个管理页 = 列表页（_Layout 全页，data-dyn-init-createapp + script[tag=dynconfig]）
///           + 表单片段（PartialView，layer fragment 弹窗，服务端预载行 JSON）。
/// 页面范式：ActionHelper 动作管道 + DynTable 免模型表格，数据走 /api/platform/dyndata，页面零手写业务请求。
/// </summary>
[Area("Platform")]
public class MgmtController : Controller
{
    private const string Page = "~/Views/Platform/Mgmt/{0}.cshtml";
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public MgmtController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    /// <summary>桌面快捷方式管理（列表）</summary>
    [HttpGet("/Platform/Mgmt/DesktopShortcut")]
    public IActionResult DesktopShortcut()
    {
        ViewData["Title"] = "桌面快捷方式管理 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Page, "DesktopShortcut"));
    }

    /// <summary>快捷方式表单片段（layer fragment 加载；id 为空=新增，注入默认值）</summary>
    [HttpGet("/Platform/Mgmt/DesktopShortcutForm")]
    public IActionResult DesktopShortcutForm(string id = null)
    {
        ViewBag.FormUrl = "/api/platform/dyndata/save?table=DesktopShortcut&gridId=grid_shortcut";
        var row = new JObject
        {
            ["Code"] = "",
            ["Name"] = "",
            ["SolutionId"] = null,
            ["Url"] = "",
            ["Icon"] = "",
            ["TargetType"] = "page",
            ["ActionHelperId"] = null,
            ["SortNo"] = 0,
            ["IsActive"] = 1
        };
        using var db = _dbs.PlatformDb();
        if (!string.IsNullOrWhiteSpace(id))
        {
            var pks = _svc.PrimaryKeys(db, "DesktopShortcut");
            var found = pks.Count > 0
                ? _svc.First(db, "DesktopShortcut", $"[{pks[0]}]=@v", new { v = id })
                : null;
            if (found == null) return Content("记录不存在");
            row = found;
        }
        // 解决方案下拉：服务端一次性渲染（IsActive=1），页面无需再走 ajax
        var sol = _svc.Page(db, "DesktopSolution", 1, 100, new JObject { ["IsActive"] = 1 });
        ViewBag.Solutions = sol?.Rows ?? new List<JObject>();
        ViewBag.RowJson = row.ToString(Newtonsoft.Json.Formatting.None);
        return PartialView(string.Format(Page, "DesktopShortcutForm"));
    }
}
