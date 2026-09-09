using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>组件注册与解析：Razor 渲染快照入库 + 版本管理</summary>
public class ComponentRegistryService
{
    private readonly ISqlSugarClient _sugar;
    private readonly ViewRenderService _renderer;
    private readonly IConfiguration _config;

    public ComponentRegistryService(ISqlSugarClient sugar, ViewRenderService renderer, IConfiguration config)
    {
        _sugar = sugar;
        _renderer = renderer;
        _config = config;
    }

    private ISqlSugarClient Platform => _sugar.AsTenant().GetConnectionScope("platform");

    public record RazorComponentEntry(string Name, string ViewPath, string Category, string DisplayName, string Icon);

    /// <summary>读取 appsettings 中的 Razor 组件清单</summary>
    public List<RazorComponentEntry> GetRazorComponentEntries()
    {
        var list = new List<RazorComponentEntry>();
        var section = _config.GetSection("VueLib:RazorComponents");
        foreach (var item in section.GetChildren())
        {
            list.Add(new RazorComponentEntry(
                item["name"] ?? "",
                item["viewPath"] ?? "",
                item["category"] ?? "Common",
                item["displayName"] ?? item["name"] ?? "",
                item["icon"] ?? "Box"));
        }
        return list.Where(x => !string.IsNullOrEmpty(x.Name)).ToList();
    }

    /// <summary>注册/刷新磁盘 Razor 组件：渲染快照入库，内容变化则版本+1</summary>
    public async Task<int> RefreshRazorComponentsAsync()
    {
        var entries = GetRazorComponentEntries();
        var changed = 0;
        foreach (var e in entries)
        {
            string text;
            try
            {
                text = await _renderer.RenderToStringAsync(e.ViewPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComponentRegistry] 渲染失败 {e.Name} ({e.ViewPath}): {ex.Message}");
                continue;
            }

            var current = await Platform.Queryable<LcComponent>()
                .Where(c => c.Name == e.Name && c.IsCurrent)
                .OrderBy(c => c.Version, OrderByType.Desc)
                .FirstAsync();

            var normalized = text.Trim();
            if (current != null && (current.SourceType != "razor" || (current.ComponentText ?? "").Trim() == normalized))
            {
                // 无变化或已被手工接管（demo 组件），跳过
                if (current.SourceType != "razor") continue;
                continue;
            }

            var newVersion = (current?.Version ?? 0) + 1;
            if (current != null)
            {
                current.IsCurrent = false;
                current.UpdatedAt = DateTime.Now;
                await Platform.Updateable(current).ExecuteCommandAsync();
            }
            await Platform.Insertable(new LcComponent
            {
                Name = e.Name,
                DisplayName = e.DisplayName,
                Category = e.Category,
                Icon = e.Icon,
                Version = newVersion,
                IsCurrent = true,
                SourceType = "razor",
                RazorPath = e.ViewPath,
                ComponentText = normalized
            }).ExecuteCommandAsync();
            changed++;
        }
        return changed;
    }

    /// <summary>解析组件：name + version（version 为空 → 当前版本）</summary>
    public async Task<LcComponent?> ResolveAsync(string name, int? version)
    {
        if (version.HasValue && version.Value > 0)
        {
            return await Platform.Queryable<LcComponent>()
                .Where(c => c.Name == name && c.Version == version.Value)
                .OrderBy(c => c.Version, OrderByType.Desc)
                .FirstAsync();
        }
        return await Platform.Queryable<LcComponent>()
            .Where(c => c.Name == name && c.IsCurrent)
            .OrderBy(c => c.Version, OrderByType.Desc)
            .FirstAsync();
    }

    /// <summary>获取组件元数据列表（含全部版本，供设计器版本锁定 UI）</summary>
    public async Task<object> GetMetaListAsync()
    {
        var components = await Platform.Queryable<LcComponent>().ToListAsync();
        return components
            .GroupBy(c => c.Name)
            .Select(g =>
            {
                var current = g.FirstOrDefault(x => x.IsCurrent) ?? g.OrderByDescending(x => x.Version).First();
                return new
                {
                    current.Name,
                    current.DisplayName,
                    current.Category,
                    current.Icon,
                    current.SourceType,
                    Version = current.Version,
                    // 组合组件：列表即带配置（前端同步注册时直接构建）
                    CompositeConfigJson = current.SourceType == "composite" ? current.CompositeConfigJson : null,
                    ConfigSchemaJson = current.SourceType == "composite" ? current.ConfigSchemaJson : null,
                    Versions = g.OrderByDescending(x => x.Version).Select(x => new { x.Version, x.IsCurrent, x.SourceType, x.UpdatedAt }).ToList()
                };
            })
            .OrderBy(x => x.Category).ThenBy(x => x.DisplayName)
            .ToList();
    }
}
