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

    // 获取快捷方式（支持分页和筛选）
    [HttpGet("/api/desktop/shortcuts")]
    public async Task<IActionResult> GetShortcuts([FromQuery] int page = 1, [FromQuery] int limit = 10,
        [FromQuery] string? name = null, [FromQuery] string? openType = null, [FromQuery] int? solutionId = null)
    {
        using var db = _dbContext.Create();
        var query = db.Queryable<DesktopShortcut>().Where(s => s.IsEnabled);
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(s => s.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(openType))
            query = query.Where(s => s.OpenType == openType);
        if (solutionId.HasValue)
            query = query.Where(s => s.SolutionId == solutionId.Value);

        var total = await query.CountAsync();
        var list = await query.OrderBy(s => s.SortOrder)
            .Skip((page - 1) * limit).Take(limit).ToListAsync();
        return Ok(new { success = true, data = list, count = total });
    }

    // 获取解决方案（支持分页和筛选）
    [HttpGet("/api/desktop/solutions")]
    public async Task<IActionResult> GetSolutions([FromQuery] int page = 1, [FromQuery] int limit = 10,
        [FromQuery] string? name = null)
    {
        using var db = _dbContext.Create();
        var query = db.Queryable<DesktopSolution>().Where(s => s.IsEnabled);
        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(s => s.Name.Contains(name));

        var total = await query.CountAsync();
        var list = await query.OrderBy(s => s.SortOrder)
            .Skip((page - 1) * limit).Take(limit).ToListAsync();
        return Ok(new { success = true, data = list, count = total });
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

    // 更新窗口位置和大小
    [HttpPost("/api/desktop/shortcut/{id}/window")]
    public async Task<IActionResult> UpdateWindowSize(int id, [FromBody] WindowSizeModel model)
    {
        using var db = _dbContext.Create();
        await db.Updateable<DesktopShortcut>()
            .SetColumns(s => s.PosX == model.PosX)
            .SetColumns(s => s.PosY == model.PosY)
            .SetColumns(s => s.Width == model.Width)
            .SetColumns(s => s.Height == model.Height)
            .Where(s => s.Id == id)
            .ExecuteCommandAsync();
        return Ok(new { success = true });
    }

    public class WindowSizeModel
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
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
