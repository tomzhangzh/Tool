using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Controllers;

/// <summary>
/// 桌面工作台：Index 桌面页 + 快捷方式/解决方案 API + 窗口状态记忆
/// </summary>
public class DesktopController : Controller
{
    private readonly ISqlSugarClient _sugar;

    public DesktopController(ISqlSugarClient sugar) => _sugar = sugar;

    private ISqlSugarClient Platform => _sugar.AsTenant().GetConnectionScope("platform");

    public IActionResult Index() => View("~/Views/Desktop/Index.cshtml");

    /// <summary>桌面管理页：?type=shortcut|solution</summary>
    public IActionResult Manage() => View("~/Views/Desktop/Manage.cshtml");

    // ==================== API ====================

    /// <summary>快捷方式列表（支持筛选）</summary>
    [HttpGet("/api/desktop/shortcuts")]
    public async Task<IActionResult> GetShortcuts([FromQuery] string? name = null, [FromQuery] string? openType = null, [FromQuery] int? solutionId = null)
    {
        var q = Platform.Queryable<DesktopShortcut>().Where(s => s.IsEnabled);
        if (!string.IsNullOrWhiteSpace(name)) q = q.Where(s => s.Name.Contains(name));
        if (!string.IsNullOrWhiteSpace(openType)) q = q.Where(s => s.OpenType == openType);
        if (solutionId.HasValue) q = q.Where(s => s.SolutionId == solutionId.Value);
        var list = await q.OrderBy(s => s.SortOrder).ToListAsync();
        return Ok(new { success = true, data = list });
    }

    /// <summary>解决方案列表</summary>
    [HttpGet("/api/desktop/solutions")]
    public async Task<IActionResult> GetSolutions()
    {
        var list = await Platform.Queryable<DesktopSolution>()
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        return Ok(new { success = true, data = list });
    }

    /// <summary>保存快捷方式（新增/更新）</summary>
    [HttpPost("/api/desktop/shortcut")]
    public async Task<IActionResult> SaveShortcut([FromBody] DesktopShortcut model)
    {
        if (model.Id > 0)
        {
            await Platform.Updateable(model).ExecuteCommandAsync();
        }
        else
        {
            model.Id = await Platform.Insertable(model).ExecuteReturnIdentityAsync();
        }
        return Ok(new { success = true, data = model });
    }

    /// <summary>删除快捷方式</summary>
    [HttpDelete("/api/desktop/shortcut/{id:int}")]
    public async Task<IActionResult> DeleteShortcut(int id)
    {
        await Platform.Deleteable<DesktopShortcut>(id).ExecuteCommandAsync();
        return Ok(new { success = true });
    }

    /// <summary>保存解决方案</summary>
    [HttpPost("/api/desktop/solution")]
    public async Task<IActionResult> SaveSolution([FromBody] DesktopSolution model)
    {
        if (model.Id > 0)
        {
            await Platform.Updateable(model).ExecuteCommandAsync();
        }
        else
        {
            model.Id = await Platform.Insertable(model).ExecuteReturnIdentityAsync();
        }
        return Ok(new { success = true, data = model });
    }

    /// <summary>删除解决方案（解除快捷方式关联）</summary>
    [HttpDelete("/api/desktop/solution/{id:int}")]
    public async Task<IActionResult> DeleteSolution(int id)
    {
        await Platform.Updateable<DesktopShortcut>()
            .SetColumns(s => new DesktopShortcut { SolutionId = null })
            .Where(s => s.SolutionId == id)
            .ExecuteCommandAsync();
        await Platform.Deleteable<DesktopSolution>(id).ExecuteCommandAsync();
        return Ok(new { success = true });
    }

    /// <summary>记忆窗口位置与大小</summary>
    [HttpPost("/api/desktop/shortcut/{id:int}/window")]
    public async Task<IActionResult> UpdateWindowSize(int id, [FromBody] WindowSizeModel model)
    {
        await Platform.Updateable<DesktopShortcut>()
            .SetColumns(s => new DesktopShortcut { PosX = model.PosX, PosY = model.PosY, Width = model.Width, Height = model.Height })
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
}
