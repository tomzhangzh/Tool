using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Text.Json;
using System.Text.Json.Serialization;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>低代码平台接口：组件元数据/渲染(版本)、页面 CRUD、组合组件、Options 解析</summary>
[ApiController]
[Route("api/lowcode")]
public class LowCodeController : ControllerBase
{
    private readonly ISqlSugarClient _sugar;
    private readonly ComponentRegistryService _registry;
    private readonly OptionsService _options;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LowCodeController(ISqlSugarClient sugar, ComponentRegistryService registry, OptionsService options)
    {
        _sugar = sugar;
        _registry = registry;
        _options = options;
    }

    private ISqlSugarClient Platform => _sugar.AsTenant().GetConnectionScope("platform");

    // ---------- 组件元数据（设计器用） ----------
    [HttpGet("components")]
    public async Task<IActionResult> Components()
        => Ok(new { success = true, data = await _registry.GetMetaListAsync() });

    // ---------- 组件渲染快照（运行时懒加载用；version=0 取当前版本） ----------
    [HttpGet("component/render")]
    public async Task<IActionResult> Render([FromQuery] string name, [FromQuery] int version = 0)
    {
        if (string.IsNullOrEmpty(name)) return Ok(new { success = false, message = "name 不能为空" });
        var comp = await _registry.ResolveAsync(name, version <= 0 ? null : version);
        if (comp == null) return Ok(new { success = false, message = $"组件不存在: {name}@{version}" });
        return Ok(new
        {
            success = true,
            data = new
            {
                name = comp.Name,
                version = comp.Version,
                displayName = comp.DisplayName,
                sourceType = comp.SourceType,
                componentText = comp.ComponentText,
                compositeConfigJson = comp.CompositeConfigJson,
                configSchemaJson = comp.ConfigSchemaJson
            }
        });
    }

    // ---------- 刷新磁盘 Razor 组件注册（开发工具） ----------
    [HttpPost("component/refresh")]
    public async Task<IActionResult> Refresh()
        => Ok(new { success = true, changed = await _registry.RefreshRazorComponentsAsync() });

    // ---------- 删除组件（组件管理：删除全部版本） ----------
    [HttpDelete("component/{name}")]
    public async Task<IActionResult> DeleteComponent(string name)
    {
        var count = await Platform.Deleteable<LcComponent>().Where(c => c.Name == name).ExecuteCommandAsync();
        return Ok(new { success = true, deleted = count });
    }

    // ---------- 组件快照文本 ----------
    [HttpGet("component/snapshot")]
    public async Task<IActionResult> Snapshot([FromQuery] string name, [FromQuery] int? version = null)
    {
        var q = Platform.Queryable<LcComponent>().Where(c => c.Name == name);
        var comp = version.HasValue
            ? await q.Where(c => c.Version == version.Value).FirstAsync()
            : await q.OrderBy(c => c.Version, OrderByType.Desc).FirstAsync();
        if (comp == null) return Ok(new { success = false, message = "组件不存在" });
        return Ok(new
        {
            success = true,
            data = new
            {
                name = comp.Name,
                displayName = comp.DisplayName,
                category = comp.Category,
                version = comp.Version,
                isCurrent = comp.IsCurrent,
                sourceType = comp.SourceType,
                updatedAt = comp.UpdatedAt,
                compositeConfigJson = comp.CompositeConfigJson,
                componentText = comp.ComponentText
            }
        });
    }

    // ---------- 保存组合组件 ----------
    [HttpPost("component")]
    public async Task<IActionResult> SaveComposite([FromBody] SaveCompositeReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return Ok(new { success = false, message = "组件名不能为空" });
        if (string.IsNullOrWhiteSpace(req.CompositeConfigJson)) return Ok(new { success = false, message = "组合配置不能为空" });

        var current = await Platform.Queryable<LcComponent>()
            .Where(c => c.Name == req.Name)
            .OrderBy(c => c.Version, OrderByType.Desc)
            .FirstAsync();
        var version = (current?.Version ?? 0) + 1;
        if (current != null)
        {
            current.IsCurrent = false;
            current.UpdatedAt = DateTime.Now;
            await Platform.Updateable(current).ExecuteCommandAsync();
        }
        await Platform.Insertable(new LcComponent
        {
            Name = req.Name,
            DisplayName = req.DisplayName ?? req.Name,
            Category = "Composite",
            Icon = req.Icon ?? "Box",
            Version = version,
            IsCurrent = true,
            SourceType = "composite",
            CompositeConfigJson = req.CompositeConfigJson,
            ConfigSchemaJson = req.ConfigSchemaJson
        }).ExecuteCommandAsync();
        return Ok(new { success = true, data = new { name = req.Name, version } });
    }

    // ---------- 页面列表 ----------
    [HttpGet("pages")]
    public async Task<IActionResult> Pages()
    {
        var list = await Platform.Queryable<LcPage>().OrderBy(p => p.Code).ToListAsync();
        return Ok(new
        {
            success = true,
            data = list.Select(p => new { p.Code, p.Name, p.UpdatedAt }).ToList()
        });
    }

    // ---------- 页面详情（config + model + 版本锁） ----------
    [HttpGet("page/{code}")]
    public async Task<IActionResult> Page(string code)
    {
        var page = await Platform.Queryable<LcPage>().FirstAsync(p => p.Code == code);
        if (page == null) return Ok(new { success = false, message = $"页面不存在: {code}" });
        return Ok(new
        {
            success = true,
            data = new
            {
                code = page.Code,
                name = page.Name,
                config = JsonDocument.Parse(page.ConfigJson).RootElement,
                model = JsonDocument.Parse(page.DefaultModelJson).RootElement,
                componentVersions = JsonDocument.Parse(page.ComponentVersionsJson).RootElement
            }
        });
    }

    // ---------- 保存页面（设计器） ----------
    [HttpPost("page")]
    public async Task<IActionResult> SavePage([FromBody] SavePageReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Code)) return Ok(new { success = false, message = "页面编码不能为空" });
        var page = await Platform.Queryable<LcPage>().FirstAsync(p => p.Code == req.Code);
        if (page == null)
        {
            page = new LcPage { Code = req.Code, CreatedAt = DateTime.Now };
            await Platform.Insertable(page).ExecuteCommandAsync();
        }
        page.Name = req.Name ?? req.Code;
        page.ConfigJson = JsonOrEmpty(req.Config);
        page.DefaultModelJson = JsonOrEmpty(req.Model);
        page.ComponentVersionsJson = req.ComponentVersions.HasValue && req.ComponentVersions.Value.ValueKind != JsonValueKind.Undefined
            ? req.ComponentVersions.Value.GetRawText() : "{}";
        page.UpdatedAt = DateTime.Now;
        await Platform.Updateable(page).ExecuteCommandAsync();
        return Ok(new { success = true });
    }

    private static string JsonOrEmpty(JsonElement? el)
        => el.HasValue && el.Value.ValueKind != JsonValueKind.Undefined ? el.Value.GetRawText() : "{}";

    // ---------- Options 解析（下拉 static/dict/sql/ajax 服务端部分） ----------
    [HttpPost("options/resolve")]
    public async Task<IActionResult> ResolveOptions([FromBody] OptionResolveRequest req)
    {
        try
        {
            var items = await _options.ResolveAsync(req);
            return Ok(new { success = true, data = items });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }
}

public class SaveCompositeReq
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Icon { get; set; }
    public string? CompositeConfigJson { get; set; }
    public string? ConfigSchemaJson { get; set; }
}

public class SavePageReq
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public JsonElement? Config { get; set; }
    public JsonElement? Model { get; set; }
    public JsonElement? ComponentVersions { get; set; }
}
