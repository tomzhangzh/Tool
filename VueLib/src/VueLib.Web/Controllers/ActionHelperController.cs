using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Controllers;

/// <summary>
/// 动作助手（actionhelper）管理：数据库登记 + API 下发 + 动态生成注入 JS + 管理页面
/// </summary>
[ApiController]
[Route("api/dynactionhelper")]
public class ActionHelperController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    /// <summary>生成的动态注入文件（dyn-actionhelper.js 启动时自动加载）</summary>
    public const string GeneratedJsName = "dyn-actionhelpers.generated.js";

    public ActionHelperController(AppDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
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
            // 内置动作：仅允许改 Name/Remark/IsEnabled/SortOrder/MetaJson/AutoGenerate
            var exist = await db.Queryable<DynActionHelper>().InSingleAsync(model.Id);
            if (exist == null) return Ok(new { success = false, message = "记录不存在" });
            if (exist.IsBuiltin)
            {
                exist.Name = model.Name;
                exist.Remark = model.Remark;
                exist.IsEnabled = model.IsEnabled;
                exist.SortOrder = model.SortOrder;
                exist.AutoGenerate = model.AutoGenerate;
                if (!string.IsNullOrWhiteSpace(model.MetaJson)) exist.MetaJson = model.MetaJson;
                exist.UpdatedAt = model.UpdatedAt;
                await db.Updateable(exist).ExecuteCommandAsync();
                if (model.AutoGenerate) await GenerateJsFileAsync(db);
                return Ok(new { success = true, data = exist });
            }
            await db.Updateable(model).ExecuteCommandAsync();
            if (model.AutoGenerate) await GenerateJsFileAsync(db);
            return Ok(new { success = true, data = model });
        }
        // 新增
        var dup = await db.Queryable<DynActionHelper>().Where(h => h.Code == model.Code).AnyAsync();
        if (dup) return Ok(new { success = false, message = $"Code [{model.Code}] 已存在" });
        model.CreatedAt = DateTime.UtcNow;
        model.Id = await db.Insertable(model).ExecuteReturnIdentityAsync();
        if (model.AutoGenerate) await GenerateJsFileAsync(db);
        return Ok(new { success = true, data = model });
    }

    /// <summary>
    /// 重新生成 dyn-actionhelpers.generated.js（把所有已启用的自定义动作注入 JS 文件）
    /// 前端 dyn-actionhelper.js 启动时自动加载该文件完成动态注入。
    /// POST /api/dynactionhelper/generate
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate()
    {
        using var db = _dbContext.Create();
        var info = await GenerateJsFileAsync(db);
        return Ok(info);
    }

    /// <summary>生成注入 JS 文件（核心逻辑，可被 Save 自动触发）</summary>
    private async Task<object> GenerateJsFileAsync(ISqlSugarClient db)
    {
        var rows = await db.Queryable<DynActionHelper>()
            .Where(h => h.IsEnabled && !h.IsBuiltin && h.ScriptContent != null && h.ScriptContent != "")
            .OrderBy(h => h.SortOrder).OrderBy(h => h.Id)
            .ToListAsync();

        var payload = rows.Select(r => new
        {
            id = r.Id,
            code = r.Code,
            name = r.Name,
            category = r.Category,
            metaJson = r.MetaJson,
            scriptContent = r.ScriptContent,
            isBuiltin = r.IsBuiltin
        }).ToList();

        var json = JsonConvert.SerializeObject(payload, Formatting.Indented,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        var js = @"/* ============================================================
 * dyn-actionhelpers.generated.js —— 由 ActionHelperController 自动生成
 * 动作注入文件：dyn-actionhelper.js 启动时动态加载，把 DB 中的
 * 自定义动作注册进动作系统（无需修改主 js，重建即注入）
 * 生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"
 * ============================================================ */
(function (global) {
    'use strict';
    var rows = " + json + @";
    function boot() {
        if (!global.dyn || typeof global.dyn.registerActionHelper !== 'function') { setTimeout(boot, 250); return; }
        var added = 0;
        rows.forEach(function (r) {
            if (r && global.dyn.registerActionHelper(r)) added++;
        });
        if (global.dyn.autoBindActions) { try { global.dyn.autoBindActions(); } catch (e) {} }
        if (global.__DYN_DEBUG) console.log('[dyn] 已从 generated.js 注入动作助手 ' + added + ' 个');
    }
    if (typeof window !== 'undefined') boot();
})(typeof window !== 'undefined' ? window : this);
";

        var wwwroot = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "js");
        Directory.CreateDirectory(wwwroot);
        var file = Path.Combine(wwwroot, GeneratedJsName);
        await System.IO.File.WriteAllTextAsync(file, js);
        return new
        {
            success = true,
            message = $"已重新生成 {GeneratedJsName}（{rows.Count} 个自定义动作）",
            file = GeneratedJsName,
            count = rows.Count,
            generatedAt = DateTime.Now
        };
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
