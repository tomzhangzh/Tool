using Microsoft.AspNetCore.Mvc;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.ViewComponents;

/// <summary>
/// 组件注册表 ViewComponent —— 把数据库中已启用的组件清单渲染进页面
/// （"dyncom 在 view 中定义"：统一注册表由服务端 view 输出，前端 dyn-com.js 消费）
/// </summary>
public class DynComRegistryViewComponent : ViewComponent
{
    private readonly AppDbContext _dbContext;

    public DynComRegistryViewComponent(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var list = new List<object>();
        try
        {
            using var db = _dbContext.Create();
            var rows = await db.Queryable<ComponentMeta>()
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
            list = rows.Select(c => new
            {
                name = c.ComponentName,
                label = c.Label,
                category = c.Category,
                type = c.ComponentType,
                icon = c.Icon,
                loadUrl = c.LoadUrl,
                version = c.CreatedAt.Ticks.ToString()
            }).Cast<object>().ToList();
        }
        catch
        {
            // 注册表失败不阻塞页面
        }
        return View(list);
    }
}
