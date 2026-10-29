#nullable enable
using VueLibV4.Web.Core;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Dtos;
using VueLibV4.Platform.Models;

namespace VueLibV4.Web.Services;

/// <summary>
/// 组件服务 - 元数据驱动（不再运行时轮询 view 目录）：
///   ① 组件清单直接读 ComponentMeta（含 UiPlatform / LoadUrl / ViewPath）
///   ② 组件定义按 ComponentMeta.ViewPath 渲染对应 Razor View；ViewPath 为空则回退 DB 内 Template/Script/Style
/// 前端通过 LoadUrl 异步加载组件定义；LoadUrl 为空表示全局组件（如 ElFormItem）无需动态加载。
/// 数据访问统一走强类型 IComponentMetaService；Razor 视图渲染为 Web 层职责，保留在本服务。
/// </summary>
public class ComponentService
{
    private readonly IComponentMetaService _components;
    private readonly RazorComponentRenderer _razorRenderer;
    private readonly ILogger<ComponentService> _logger;

    public ComponentService(
        IComponentMetaService components,
        RazorComponentRenderer razorRenderer,
        ILogger<ComponentService> logger)
    {
        _components = components;
        _razorRenderer = razorRenderer;
        _logger = logger;
    }

    /// <summary>所有已启用组件清单（全部来自 ComponentMeta）</summary>
    public List<ComponentListItemDto> GetEnabledListAsync()
    {
        var list = new List<ComponentListItemDto>();
        try
        {
            // SqlSugar 的 ISugarQueryable 不支持 ThenBy，先取回内存再排序，保证"分类 + Id"两级顺序
            var rows = _components.Query(c => c.IsActive).ToList()
                .OrderBy(c => c.Category)
                .ThenBy(c => c.Id)
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
                    IsComposite = !string.IsNullOrEmpty(r.DefaultConfigJson),
                    IsContainer = r.IsContainer,
                    AcceptAll = r.AcceptAll,
                    AllowDrop = r.AllowDrop
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

        var row = _components.Query(c => c.ComponentName == componentName && c.IsActive).First();

        // 1. ViewPath 指定的 Razor View 是唯一来源：渲染/解析失败直接报错，不静默回退 DB。
        //    历史上这里 catch 后回退 DB，而 DB 中这类组件 TemplateContent/ScriptContent 为空，
        //    结果把编译错误掩盖成"success=true + 空定义"，前端只看到组件加载不出内容。
        if (row != null && !string.IsNullOrWhiteSpace(row.ViewPath))
        {
            var define = _razorRenderer.RenderByPath(row.ViewPath, componentName).GetAwaiter().GetResult();
            if (define != null) return define;
            throw new InvalidOperationException(
                $"组件 [{componentName}] 的视图 {row.ViewPath} 未解析出 template/script，请检查该 .cshtml 的 Razor 标记");
        }

        // 2. 回退数据库内组件内容（仅 ViewPath 为空的纯 DB 组件）
        if (row != null)
        {
            return new ComponentDefineDto
            {
                ComponentName = componentName,
                ComponentType = (int)ComponentType.Common,
                // Category 是设计器面板的分组名（如"隐藏组件"），不是路由；此处不再复用
                RoutePath = null,
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
            // 批量拉取用于设计器初始化：单个组件渲染失败不应拖垮整批，记录日志后跳过
            try
            {
                var define = GetDefineByName(name);
                if (define != null) result.Add(define);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量获取组件 [{Name}] 定义失败，已跳过", name);
            }
        }
        return result;
    }
}
