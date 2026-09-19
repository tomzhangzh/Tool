#nullable enable
using VueLibV4.Web.Core;
using VueLibV4.Web.Dtos;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Services;

/// <summary>
/// 组件服务 - 元数据驱动（不再运行时轮询 view 目录）：
///   ① 组件清单直接读 ComponentMeta（含 UiPlatform / LoadUrl / ViewPath）
///   ② 组件定义按 ComponentMeta.ViewPath 渲染对应 Razor View；ViewPath 为空则回退 DB 内 Template/Script/Style
/// 前端通过 LoadUrl 异步加载组件定义；LoadUrl 为空表示全局组件（如 ElFormItem）无需动态加载。
/// </summary>
public class ComponentService
{
    private readonly DbFactory _dbFactory;
    private readonly RazorComponentRenderer _razorRenderer;
    private readonly ILogger<ComponentService> _logger;

    public ComponentService(
        DbFactory dbFactory,
        RazorComponentRenderer razorRenderer,
        ILogger<ComponentService> logger)
    {
        _dbFactory = dbFactory;
        _razorRenderer = razorRenderer;
        _logger = logger;
    }

    /// <summary>所有已启用组件清单（全部来自 ComponentMeta）</summary>
    public List<ComponentListItemDto> GetEnabledListAsync()
    {
        var list = new List<ComponentListItemDto>();
        try
        {
            using var db = _dbFactory.PlatformDb();
            var rows = db.Queryable<ComponentMeta>()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Category)
                .OrderBy(c => c.Id)
                .ToList();
            foreach (var r in rows)
            {
                list.Add(new ComponentListItemDto
                {
                    ComponentName = r.ComponentName,
                    Label = r.Label,
                    Category = r.Category,
                    Icon = r.Icon,
                    ComponentType = (int)ComponentType.Common,
                    Description = null,
                    SortOrder = r.Id,
                    LoadUrl = r.LoadUrl,
                    UiPlatform = r.UiPlatform,
                    IsComposite = !string.IsNullOrEmpty(r.DefaultConfigJson)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 ComponentMeta 失败");
        }
        return list;
    }

    /// <summary>根据组件名称获取完整定义（按 DB.ViewPath 渲染 Razor，缺失则回退 DB 内容）</summary>
    public ComponentDefineDto? GetDefineByName(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName)) return null;

        using var db = _dbFactory.PlatformDb();
        var row = db.Queryable<ComponentMeta>()
            .First(c => c.ComponentName == componentName && c.IsActive);

        // 1. ViewPath 指定的 Razor View 优先
        if (row != null && !string.IsNullOrWhiteSpace(row.ViewPath))
        {
            try
            {
                var define = _razorRenderer.RenderByPath(row.ViewPath, componentName).GetAwaiter().GetResult();
                if (define != null) return define;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "渲染 Razor 组件 {Name} 失败，尝试 DB", componentName);
            }
        }

        // 2. 回退数据库内组件内容
        if (row != null)
        {
            return new ComponentDefineDto
            {
                ComponentName = componentName,
                ComponentType = (int)ComponentType.Common,
                RoutePath = row.Category,
                TemplateContent = row.TemplateContent ?? "",
                ScriptContent = row.ScriptContent ?? "",
                StyleContent = row.StyleContent
            };
        }

        _logger.LogWarning("组件 [{Name}] 在 ComponentMeta 中无定义", componentName);
        return null;
    }

    /// <summary>批量获取多个组件定义</summary>
    public List<ComponentDefineDto> GetDefinesByNames(IEnumerable<string> componentNames)
    {
        var names = componentNames?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList() ?? new List<string>();
        var result = new List<ComponentDefineDto>();
        foreach (var name in names)
        {
            var define = GetDefineByName(name);
            if (define != null) result.Add(define);
        }
        return result;
    }
}
