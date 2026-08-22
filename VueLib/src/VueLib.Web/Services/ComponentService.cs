using VueLib.Web.Data;
using VueLib.Web.Dtos;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 组件服务 - 负责从数据库读取组件定义，数据库不存在时回退到 Razor View
/// 加载优先级: 数据库(已启用) > Razor View (.cshtml 组件定义)
/// </summary>
public class ComponentService
{
    private readonly AppDbContext _dbContext;
    private readonly RazorComponentRenderer _razorRenderer;
    private readonly ILogger<ComponentService> _logger;

    public ComponentService(
        AppDbContext dbContext,
        RazorComponentRenderer razorRenderer,
        ILogger<ComponentService> logger)
    {
        _dbContext = dbContext;
        _razorRenderer = razorRenderer;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有已启用组件的清单（数据库优先，合并 Razor 中定义的组件）
    /// </summary>
    public async Task<List<ComponentListItemDto>> GetEnabledListAsync()
    {
        using var db = _dbContext.Create();
        var dbList = await db.Queryable<ComponentDefinition>()
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.ComponentType)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ComponentListItemDto
            {
                ComponentName = c.ComponentName,
                ComponentType = c.ComponentType,
                RoutePath = c.RoutePath,
                Description = c.Description,
                SortOrder = c.SortOrder
            })
            .ToListAsync();

        // 扫描 Razor 组件目录，合并数据库中不存在的组件
        var razorComponents = ScanRazorComponents();
        var dbNames = new HashSet<string>(dbList.Select(c => c.ComponentName));
        foreach (var rc in razorComponents)
        {
            if (!dbNames.Contains(rc.ComponentName))
            {
                dbList.Add(rc);
                _logger.LogInformation("从 Razor View 发现组件: {ComponentName}", rc.ComponentName);
            }
        }

        return dbList;
    }

    /// <summary>
    /// 根据组件名称获取完整定义（数据库优先，Razor 回退）
    /// </summary>
    public async Task<ComponentDefineDto?> GetDefineByNameAsync(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return null;

        // 1. 优先从数据库读取
        using var db = _dbContext.Create();
        var entity = await db.Queryable<ComponentDefinition>()
            .Where(c => c.ComponentName == componentName && c.IsEnabled)
            .FirstAsync();

        if (entity != null)
        {
            return new ComponentDefineDto
            {
                ComponentName = entity.ComponentName,
                ComponentType = entity.ComponentType,
                RoutePath = entity.RoutePath,
                TemplateContent = entity.TemplateContent,
                ScriptContent = entity.ScriptContent,
                StyleContent = entity.StyleContent
            };
        }

        // 2. 数据库不存在，回退到 Razor View
        _logger.LogInformation("数据库中未找到组件 [{ComponentName}]，尝试从 Razor View 加载", componentName);
        var razorDefine = await _razorRenderer.RenderAsync(componentName);
        if (razorDefine != null)
        {
            return razorDefine;
        }

        _logger.LogWarning("组件 [{ComponentName}] 在数据库和 Razor View 中均不存在", componentName);
        return null;
    }

    /// <summary>
    /// 批量获取多个组件的完整定义
    /// </summary>
    public async Task<List<ComponentDefineDto>> GetDefinesByNamesAsync(IEnumerable<string> componentNames)
    {
        var names = componentNames?.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList();
        if (names == null || names.Count == 0)
            return new List<ComponentDefineDto>();

        var result = new List<ComponentDefineDto>();
        foreach (var name in names)
        {
            var define = await GetDefineByNameAsync(name);
            if (define != null)
                result.Add(define);
        }
        return result;
    }

    /// <summary>
    /// 获取所有页面组件（用于构建路由表）
    /// </summary>
    public async Task<List<ComponentListItemDto>> GetPageComponentsAsync()
    {
        var all = await GetEnabledListAsync();
        return all.Where(c => c.ComponentType == ComponentType.Page).ToList();
    }

    /// <summary>
    /// 扫描 Views/Shared/Components/ 目录下的 .cshtml 文件
    /// 从文件名推断组件名，尝试读取文件头中的元数据注释
    /// </summary>
    private List<ComponentListItemDto> ScanRazorComponents()
    {
        var result = new List<ComponentListItemDto>();
        var componentsDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Views", "Shared", "Components");

        // 开发环境下路径可能不同，尝试多种路径
        var searchPaths = new[]
        {
            componentsDir,
            Path.Combine(Directory.GetCurrentDirectory(), "Views", "Shared", "Components")
        };

        foreach (var dirPath in searchPaths)
        {
            if (!Directory.Exists(dirPath)) continue;

            var files = Directory.GetFiles(dirPath, "*.cshtml");
            foreach (var file in files)
            {
                var componentName = Path.GetFileNameWithoutExtension(file);
                if (componentName.StartsWith("_")) continue; // 跳过局部视图

                // 尝试从文件内容读取元数据
                var (compType, routePath, desc) = ReadRazorComponentMetadata(file);
                result.Add(new ComponentListItemDto
                {
                    ComponentName = componentName,
                    ComponentType = compType,
                    RoutePath = routePath,
                    Description = desc,
                    SortOrder = 999
                });
            }
            break; // 找到第一个存在的目录即停止
        }

        return result;
    }

    /// <summary>
    /// 从 Razor 组件文件头部读取元数据注释
    /// 约定格式:
    ///   @* ComponentType: Page *@
    ///   @* RoutePath: /about *@
    ///   @* Description: 关于页面 *@
    /// </summary>
    private static (ComponentType compType, string? routePath, string? desc) ReadRazorComponentMetadata(string filePath)
    {
        var compType = ComponentType.Common;
        string? routePath = null;
        string? desc = null;

        try
        {
            // 只读取前 50 行，元数据通常在文件头部
            var lines = File.ReadLines(filePath).Take(50);
            foreach (var line in lines)
            {
                var typeMatch = System.Text.RegularExpressions.Regex.Match(line, @"@\*\s*ComponentType\s*:\s*(Common|Page)\s*@", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (typeMatch.Success)
                {
                    compType = typeMatch.Groups[1].Value.Equals("Page", StringComparison.OrdinalIgnoreCase)
                        ? ComponentType.Page
                        : ComponentType.Common;
                }

                var routeMatch = System.Text.RegularExpressions.Regex.Match(line, @"@\*\s*RoutePath\s*:\s*([^\s@]+)\s*@");
                if (routeMatch.Success)
                {
                    routePath = routeMatch.Groups[1].Value.Trim();
                }

                var descMatch = System.Text.RegularExpressions.Regex.Match(line, @"@\*\s*Description\s*:\s*(.+?)\s*@");
                if (descMatch.Success)
                {
                    desc = descMatch.Groups[1].Value.Trim();
                }
            }
        }
        catch
        {
            // 读取失败时使用默认值
        }

        return (compType, routePath, desc);
    }
}
