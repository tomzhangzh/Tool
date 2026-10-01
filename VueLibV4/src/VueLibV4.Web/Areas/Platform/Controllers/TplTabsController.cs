using Microsoft.AspNetCore.Mvc;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// tabs-basic 模板配套片段示例（实战：DynTemplate 管理）。
/// 这些 Action 返回的视图默认经 _ViewStart 判定：fetchPartial 拉取走 _AjaxLayout（片段），
/// 浏览器直访走 _Layout（整页）。
/// </summary>
[Area("Platform")]
public class TplTabsController : Controller
{
    private readonly IDynTemplateService _templates;
    private readonly IDynWebPageService _webPages;

    public TplTabsController(IDynTemplateService templates, IDynWebPageService webPages)
    {
        _templates = templates;
        _webPages = webPages;
    }

    /// <summary>最小片段：验证 tabs-basic 懒加载注入（无 VueApp）。</summary>
    [HttpGet("/Platform/TplTabs/Sample")]
    public IActionResult Sample()
    {
        ViewBag.ServerTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return View("~/Views/DynTemplates/TplTabs/Sample.cshtml");
    }

    /// <summary>
    /// 「关联页面」页签：列出引用指定模板的全部 DynWebPage（外部配置），
    /// 每行可运行（iframe 钻取到该 Template 关联的 WebPage）或进入参数配置。
    /// </summary>
    [HttpGet("/Platform/TplTabs/Pages")]
    public IActionResult Pages(long id)
    {
        var tpl = id > 0 ? _templates.GetById((int)id) : null;
        var rows = id > 0
            ? _webPages.Query(p => p.TemplateId == (int)id).ToList()
            : new List<DynWebPage>();
        ViewBag.TplId = id;
        ViewBag.TplName = tpl?.Name;
        return View("~/Views/DynTemplates/TplTabs/TplPages.cshtml", rows);
    }
}
