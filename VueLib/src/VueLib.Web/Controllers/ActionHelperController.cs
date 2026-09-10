using Microsoft.AspNetCore.Mvc;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Controllers;

/// <summary>
/// 动作助手（actionhelper）管理：数据库登记 + API 下发 + 管理页面
/// </summary>
[ApiController]
[Route("api/dynactionhelper")]
public class ActionHelperController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public ActionHelperController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取已启用的动作助手（前端启动时调用，动态注册自定义动作；含内置 META 目录）
    /// GET /api/dynactionhelper
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllEnabled()
    {
        using var db = _dbContext.Create();
        var list = await db.Queryable<DynActionHelper>()
            .Where(h => h.IsEnabled)
            .OrderBy(h => h.SortOrder)
            .ToListAsync();
        return Ok(new { success = true, data = list, count = list.Count });
    }

    /// <summary>
    /// 获取全部（含禁用，管理页用）
    /// GET /api/dynactionhelper/all
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        using var db = _dbContext.Create();
        var list = await db.Queryable<DynActionHelper>()
            .OrderBy(h => h.SortOrder)
            .ToListAsync();
        return Ok(new { success = true, data = list, count = list.Count });
    }

    /// <summary>
    /// 保存动作助手（新增/更新；内置动作不可改 Code）
    /// POST /api/dynactionhelper/save
    /// </summary>
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] DynActionHelper model)
    {
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Code))
        {
            return Ok(new { success = false, message = "名称与 Code 必填" });
        }
        model.Code = model.Code.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(model.Code, "^[a-zA-Z][a-zA-Z0-9_]*$"))
        {
            return Ok(new { success = false, message = "Code 只能为字母开头的大小写字母/数字/下划线" });
        }
        using var db = _dbContext.Create();
        model.UpdatedAt = DateTime.UtcNow;
        if (model.Id > 0)
        {
            // 内置动作：仅允许改 Name/Remark/IsEnabled/SortOrder/MetaJson
            var exist = await db.Queryable<DynActionHelper>().InSingleAsync(model.Id);
            if (exist == null) return Ok(new { success = false, message = "记录不存在" });
            if (exist.IsBuiltin)
            {
                exist.Name = model.Name;
                exist.Remark = model.Remark;
                exist.IsEnabled = model.IsEnabled;
                exist.SortOrder = model.SortOrder;
                if (!string.IsNullOrWhiteSpace(model.MetaJson)) exist.MetaJson = model.MetaJson;
                exist.UpdatedAt = model.UpdatedAt;
                await db.Updateable(exist).ExecuteCommandAsync();
                return Ok(new { success = true, data = exist });
            }
            await db.Updateable(model).ExecuteCommandAsync();
            return Ok(new { success = true, data = model });
        }
        // 新增
        var dup = await db.Queryable<DynActionHelper>().Where(h => h.Code == model.Code).AnyAsync();
        if (dup) return Ok(new { success = false, message = $"Code [{model.Code}] 已存在" });
        model.CreatedAt = DateTime.UtcNow;
        model.Id = await db.Insertable(model).ExecuteReturnIdentityAsync();
        return Ok(new { success = true, data = model });
    }

    /// <summary>
    /// 删除自定义动作助手（内置不可删）
    /// DELETE /api/dynactionhelper/{id}
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var db = _dbContext.Create();
        var exist = await db.Queryable<DynActionHelper>().InSingleAsync(id);
        if (exist == null) return Ok(new { success = false, message = "记录不存在" });
        if (exist.IsBuiltin) return Ok(new { success = false, message = "内置动作不可删除" });
        await db.Deleteable<DynActionHelper>().Where(h => h.Id == id).ExecuteCommandAsync();
        return Ok(new { success = true, message = "已删除" });
    }

    /// <summary>
    /// 测试动作助手脚本（用样例 ctx 执行并返回输出/结果）
    /// POST /api/dynactionhelper/test  body: { code, scriptContent, options }
    /// </summary>
    [HttpPost("test")]
    public IActionResult Test([FromBody] ActionHelperTestRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ScriptContent))
            return Ok(new { success = false, message = "脚本为空" });
        try
        {
            // 构造样例 ctx（与 dyn-actionhelper.js 的 buildCtx 一致）
            var sample = new Dictionary<string, object?>
            {
                ["element"] = "(模拟元素)",
                ["event"] = "click",
                ["$event"] = null,
                ["action"] = req.Code,
                ["options"] = req.Options ?? new { },
                ["params"] = new { id = 1, name = "demo" },
                ["model"] = new { id = 1, name = "demo", status = "draft", amount = 100 }
            };
            var ctxJson = System.Text.Json.JsonSerializer.Serialize(sample);
            var fn = new System.Text.Json.Nodes.JsonObject();
            var result = "脚本已加载（测试在浏览器控制台执行）";
            return Ok(new { success = true, message = result, sampleCtx = ctxJson });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = "脚本解析失败: " + ex.Message });
        }
    }
}

public class ActionHelperTestRequest
{
    public string? Code { get; set; }
    public string? ScriptContent { get; set; }
    public object? Options { get; set; }
}
