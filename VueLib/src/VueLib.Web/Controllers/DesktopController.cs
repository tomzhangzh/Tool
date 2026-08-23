using Microsoft.AspNetCore.Mvc;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Controllers;

public class DesktopController : Controller
{
    private readonly AppDbContext _dbContext;

    public DesktopController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 桌面主页
    public IActionResult Index() => View();

    // 快捷方式管理
    public IActionResult ShortcutManage() => View();

    // 解决方案管理
    public IActionResult SolutionManage() => View();

    // 页面管理
    public IActionResult PageManage() => View();

    // ===== API =====

    // 获取所有快捷方式
    [HttpGet("/api/desktop/shortcuts")]
    public async Task<IActionResult> GetShortcuts()
    {
        using var db = _dbContext.Create();
        var list = await db.Queryable<DesktopShortcut>()
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        return Ok(new { success = true, data = list });
    }

    // 获取所有解决方案
    [HttpGet("/api/desktop/solutions")]
    public async Task<IActionResult> GetSolutions()
    {
        using var db = _dbContext.Create();
        var list = await db.Queryable<DesktopSolution>()
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        return Ok(new { success = true, data = list });
    }

    // 保存快捷方式（新增/更新）
    [HttpPost("/api/desktop/shortcut")]
    public async Task<IActionResult> SaveShortcut([FromBody] DesktopShortcut model)
    {
        using var db = _dbContext.Create();
        if (model.Id > 0)
        {
            await db.Updateable(model).ExecuteCommandAsync();
        }
        else
        {
            model.Id = await db.Insertable(model).ExecuteReturnIdentityAsync();
        }
        return Ok(new { success = true, data = model });
    }

    // 删除快捷方式
    [HttpDelete("/api/desktop/shortcut/{id}")]
    public async Task<IActionResult> DeleteShortcut(int id)
    {
        using var db = _dbContext.Create();
        await db.Deleteable<DesktopShortcut>(id).ExecuteCommandAsync();
        return Ok(new { success = true });
    }

    // 保存解决方案
    [HttpPost("/api/desktop/solution")]
    public async Task<IActionResult> SaveSolution([FromBody] DesktopSolution model)
    {
        using var db = _dbContext.Create();
        if (model.Id > 0)
        {
            await db.Updateable(model).ExecuteCommandAsync();
        }
        else
        {
            model.Id = await db.Insertable(model).ExecuteReturnIdentityAsync();
        }
        return Ok(new { success = true, data = model });
    }

    // 删除解决方案
    [HttpDelete("/api/desktop/solution/{id}")]
    public async Task<IActionResult> DeleteSolution(int id)
    {
        using var db = _dbContext.Create();
        // 解除关联快捷方式
        await db.Updateable<DesktopShortcut>()
            .SetColumns(s => s.SolutionId == null)
            .Where(s => s.SolutionId == id)
            .ExecuteCommandAsync();
        await db.Deleteable<DesktopSolution>(id).ExecuteCommandAsync();
        return Ok(new { success = true });
    }
}
