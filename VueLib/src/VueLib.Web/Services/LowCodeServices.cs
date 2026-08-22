using VueLib.Web.Data;
using VueLib.Web.Dtos;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 低代码页面服务 - 页面配置 CRUD
/// </summary>
public class PageSettingService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<PageSettingService> _logger;

    public PageSettingService(AppDbContext dbContext, ILogger<PageSettingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<PageSetting>> GetListAsync(string? category = null)
    {
        using var db = _dbContext.Create();
        var query = db.Queryable<PageSetting>().Where(p => p.IsEnabled);
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);
        return await query.OrderBy(p => p.SortOrder).OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<PageSetting?> GetByCodeAsync(string pageCode)
    {
        if (string.IsNullOrWhiteSpace(pageCode)) return null;
        using var db = _dbContext.Create();
        return await db.Queryable<PageSetting>()
            .Where(p => p.PageCode == pageCode && p.IsEnabled)
            .FirstAsync();
    }

    public async Task<PageSetting?> GetByIdAsync(int id)
    {
        using var db = _dbContext.Create();
        return await db.Queryable<PageSetting>().InSingleAsync(id);
    }

    public async Task<int> SaveAsync(PageSetting page)
    {
        using var db = _dbContext.Create();
        page.UpdatedAt = DateTime.UtcNow;
        if (page.Id > 0)
        {
            await db.Updateable(page).ExecuteCommandAsync();
            return page.Id;
        }
        page.CreatedAt = DateTime.UtcNow;
        return await db.Insertable(page).ExecuteReturnIdentityAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var db = _dbContext.Create();
        return await db.Deleteable<PageSetting>().In(id).ExecuteCommandAsync() > 0;
    }
}

/// <summary>
/// 组件元数据服务 - 查询可拖拽组件列表
/// </summary>
public class ComponentMetaService
{
    private readonly AppDbContext _dbContext;

    public ComponentMetaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ComponentMeta>> GetEnabledListAsync(int? componentType = null)
    {
        using var db = _dbContext.Create();
        var query = db.Queryable<ComponentMeta>().Where(c => c.IsEnabled);
        if (componentType.HasValue)
            query = query.Where(c => c.ComponentType == componentType.Value);
        return await query.OrderBy(c => c.ComponentType).OrderBy(c => c.SortOrder).ToListAsync();
    }

    public async Task<ComponentMeta?> GetByNameAsync(string componentName)
    {
        using var db = _dbContext.Create();
        return await db.Queryable<ComponentMeta>()
            .Where(c => c.ComponentName == componentName && c.IsEnabled)
            .FirstAsync();
    }
}
