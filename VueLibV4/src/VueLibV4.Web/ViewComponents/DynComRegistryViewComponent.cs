using Microsoft.AspNetCore.Mvc;
using VueLibV4.Web.Services;

namespace VueLibV4.Web.ViewComponents;

/// <summary>
/// 组件注册表 ViewComponent —— 把数据库中已启用的组件清单渲染进页面
/// （"dyncom 在 view 中定义"：统一注册表由服务端 _Layout 调用本组件输出，前端消费）
/// </summary>
public class DynComRegistryViewComponent : ViewComponent
{
    private readonly ComponentService _componentService;

    public DynComRegistryViewComponent(ComponentService componentService)
    {
        _componentService = componentService;
    }

    public IViewComponentResult Invoke()
    {
        List<object> list;
        try
        {
            var rows = _componentService.GetEnabledListAsync();
            list = rows.Select(c => new
            {
                name = c.ComponentName,
                label = c.Label,
                category = c.Category,
                type = c.ComponentType,
                icon = c.Icon,
                loadUrl = c.LoadUrl,
                uiPlatform = c.UiPlatform,
                isComposite = c.IsComposite
            }).Cast<object>().ToList();
        }
        catch
        {
            list = new List<object>();
        }
        return View("Default", list);
    }
}
