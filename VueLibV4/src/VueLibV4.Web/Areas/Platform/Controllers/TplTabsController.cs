using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// tabs-basic 模板配套片段示例。
/// Action 返回的视图默认经 _ViewStart 判定：fetchPartial 拉取走 _AjaxLayout（片段），
/// 浏览器直访走 _Layout（整页）。
/// </summary>
[Area("Platform")]
public class TplTabsController : Controller
{
    /// <summary>最小片段：验证 tabs-basic 懒加载注入（无 VueApp）。</summary>
    [HttpGet("/Platform/TplTabs/Sample")]
    public IActionResult Sample()
    {
        ViewBag.ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return View("~/Views/DynTemplates/TplTabs/Sample.cshtml");
    }
}
