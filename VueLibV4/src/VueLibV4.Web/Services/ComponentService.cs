#nullable enable
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Dtos;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Services;

/// <summary>
/// 组件服务 - 双定义源：
///   ① Razor View（Views/Shared/Components/{name}.cshtml）—— 代码中有定义时以代码为准
///   ② 数据库 ComponentMeta（TemplateContent/ScriptContent/StyleContent）—— 代码缺失时回退
/// 加载优先级: Razor View > DB
/// 列表合并: DB 行 + 扫描 Views/Shared/Components/*.cshtml 补全 DB 中不存在的组件
/// 平台库一律使用强类型 Model 查询（禁止免 Model 操作平台表）。
/// </summary>
public class ComponentService
{
    private readonly DbFactory _dbFactory;
    private readonly DynamicCrudService _crud;
    private readonly RazorComponentRenderer _razorRenderer;
    private readonly ILogger<ComponentService> _logger;

    public ComponentService(
        DbFactory dbFactory,
        DynamicCrudService crud,
        RazorComponentRenderer razorRenderer,
        ILogger<ComponentService> logger)
    {
        _dbFactory = dbFactory;
        _crud = crud;
        _razorRenderer = razorRenderer;
        _logger = logger;
    }

    /// <summary>所有已启用组件清单（DB 行 + 扫描 Razor 组件补全）</summary>
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
                    // ElementPlus 组件已在 setupApp 时全局注册（app.use(ElementPlus)），
                    // 不返回 LoadUrl → registerComponents 跳过异步注册，避免空定义覆盖全局组件；
                    // 非 El* 组件（Dyn*/Grid3/Card 等 View 或 DB 定义）才走动态加载。
                    // 注：IsElementPlus 仅作元数据标记，实际以组件名前缀 El 判断（避免 DynRadioGroup
                    // 等误标导致不注册）。
                    LoadUrl = r.ComponentName.StartsWith("El") ? null : $"/api/component/define/{r.ComponentName}",
                    Version = r.Version ?? "1.0.0",
                    IsComposite = !string.IsNullOrEmpty(r.DefaultConfigJson)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取 ComponentMeta 失败，仅返回 Razor 组件");
        }

        // 扫描 Razor 组件目录，合并数据库中不存在的组件
        var dbNames = new HashSet<string>(list.Select(c => c.ComponentName).Where(n => !string.IsNullOrEmpty(n)), StringComparer.OrdinalIgnoreCase);
        foreach (var rc in ScanRazorComponents())
        {
            if (!dbNames.Contains(rc.ComponentName))
            {
                list.Add(rc);
                _logger.LogInformation("从 Razor View 发现组件: {ComponentName}", rc.ComponentName);
            }
        }
        return list;
    }

    /// <summary>根据组件名称获取完整定义（Razor View 代码优先，DB 回退）</summary>
    public ComponentDefineDto? GetDefineByName(string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName)) return null;

        // 1. 代码优先：Razor View 中有定义则以代码为准
        try
        {
            var razorDefine = _razorRenderer.RenderAsync(componentName).GetAwaiter().GetResult();
            if (razorDefine != null)
            {
                // 合并 DB 元数据（若存在）
                TryFillMetaFromDb(razorDefine);
                return razorDefine;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Razor 渲染组件 {Name} 失败，尝试 DB", componentName);
        }

        // 2. 回退数据库
        try
        {
            using var db = _dbFactory.PlatformDb();
            var row = _crud.First(db, "ComponentMeta", "[ComponentName]=@n AND [IsActive]=@a", new { n = componentName, a = "1" });
            if (row != null)
            {
                return new ComponentDefineDto
                {
                    ComponentName = componentName,
                    ComponentType = (int)ComponentType.Common,
                    RoutePath = null,
                    TemplateContent = row["TemplateContent"]?.ToString() ?? "",
                    ScriptContent = row["ScriptContent"]?.ToString() ?? "",
                    StyleContent = row["StyleContent"]?.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取 DB 组件 {Name} 失败", componentName);
        }

        _logger.LogWarning("组件 [{Name}] 在 Razor View 与 DB 中均无定义", componentName);
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

    private void TryFillMetaFromDb(ComponentDefineDto define)
    {
        try
        {
            using var db = _dbFactory.PlatformDb();
            var row = _crud.First(db, "ComponentMeta", "[ComponentName]=@n", new { n = define.ComponentName });
            if (row != null)
            {
                if (define.RoutePath == null) define.RoutePath = row["Category"]?.ToString();
            }
        }
        catch { /* 元数据合并失败不阻塞 */ }
    }

    /// <summary>
    /// 扫描 Razor 组件目录：
    ///   ① Views/Shared/Components/*.cshtml（V4 标记格式）
    ///   ② Areas/ElementComponent/Views/{Container|Common|FormItem}/*.cshtml（V1 Area 组件 View）
    /// 读取文件头元数据注释，补全 DB 中不存在的组件。
    /// </summary>
    private List<ComponentListItemDto> ScanRazorComponents()
    {
        var result = new List<ComponentListItemDto>();
        var roots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };

        // ① V4 标记格式组件
        foreach (var root in roots)
        {
            var dirPath = Path.Combine(root, "Views", "Shared", "Components");
            if (!Directory.Exists(dirPath)) continue;
            foreach (var file in Directory.GetFiles(dirPath, "*.cshtml", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("_")) continue;
                var rel = Path.GetRelativePath(dirPath, file);
                if (rel.Contains(Path.DirectorySeparatorChar)) continue;

                var (compType, routePath, desc) = ReadRazorMetadata(file);
                result.Add(new ComponentListItemDto
                {
                    ComponentName = fileName,
                    ComponentType = (int)compType,
                    RoutePath = routePath,
                    Description = desc,
                    SortOrder = 999,
                    LoadUrl = $"/api/component/define/{fileName}",
                    Version = "1.0.0"
                });
            }
            break;
        }

        // ② V1 Area 组件 View（分类映射：Container→容器, Common→基础, FormItem→表单）
        var seen = new HashSet<string>(result.Select(c => c.ComponentName), StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            var areaRoot = Path.Combine(root, "Areas", "ElementComponent", "Views");
            if (!Directory.Exists(areaRoot)) continue;

            foreach (var dir in new[] { "Container", "Common", "FormItem" })
            {
                var dirPath = Path.Combine(areaRoot, dir);
                if (!Directory.Exists(dirPath)) continue;

                var category = dir switch
                {
                    "Container" => "容器",
                    "Common" => "基础",
                    _ => "表单"
                };

                foreach (var file in Directory.GetFiles(dirPath, "*.cshtml", SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.StartsWith("_")) continue;
                    if (!seen.Contains(fileName))
                    {
                        var (compType, routePath, desc) = ReadRazorMetadata(file);
                        result.Add(new ComponentListItemDto
                        {
                            ComponentName = fileName,
                            Label = fileName,
                            Category = category,
                            Icon = "🧩",
                            ComponentType = (int)compType,
                            RoutePath = routePath,
                            Description = desc,
                            SortOrder = 999,
                            LoadUrl = $"/api/component/define/{fileName}",
                            Version = "1.0.0"
                        });
                        seen.Add(fileName);
                    }
                }
            }
            break;
        }
        return result;
    }

    private static (ComponentType compType, string? routePath, string? desc) ReadRazorMetadata(string filePath)
    {
        var compType = ComponentType.Common;
        string? routePath = null;
        string? desc = null;
        try
        {
            foreach (var line in File.ReadLines(filePath).Take(50))
            {
                var typeMatch = System.Text.RegularExpressions.Regex.Match(line, @"@\*\s*ComponentType\s*:\s*(Common|Page)\s*@", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (typeMatch.Success)
                    compType = typeMatch.Groups[1].Value.Equals("Page", StringComparison.OrdinalIgnoreCase) ? ComponentType.Page : ComponentType.Common;

                var routeMatch = System.Text.RegularExpressions.Regex.Match(line, @"@\*\s*RoutePath\s*:\s*([^\s@]+)\s*@");
                if (routeMatch.Success) routePath = routeMatch.Groups[1].Value.Trim();

                var descMatch = System.Text.RegularExpressions.Regex.Match(line, @"@\*\s*Description\s*:\s*(.+?)\s*@");
                if (descMatch.Success) desc = descMatch.Groups[1].Value.Trim();
            }
        }
        catch { }
        return (compType, routePath, desc);
    }
}
