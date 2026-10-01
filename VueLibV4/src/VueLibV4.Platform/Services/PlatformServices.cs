using SqlSugar;
using VueLibV4.Platform.Models;
using VueLibV4.Services.Data.Sugar;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Platform.Services;

/*
 * 平台强类型服务：每个实体一对 接口 + 实现。
 * 业务接口同时继承 ISugarService<T>（获得全套强类型 CRUD/分页）与 IScopeDependency
 * （启动扫描自动注册，生命周期 Scoped），无需手工 AddScoped。
 * 实现类构造注入平台库 ISqlSugarClient，由 AddVueLibPlatform 统一装配。
 *
 * 控制器用法：
 *   public class DesktopShortcutController : Controller
 *   {
 *       private readonly IDesktopShortcutService _svc;
 *       public DesktopShortcutController(IDesktopShortcutService svc) => _svc = svc;
 *   }
 */

// ============ 桌面 ============

public interface IDesktopSolutionService : ISugarService<DesktopSolution>, IScopeDependency { }
public class DesktopSolutionService : SugarService<DesktopSolution>, IDesktopSolutionService
{
    public DesktopSolutionService(ISqlSugarClient db) : base(db) { }
}

public interface IDesktopShortcutService : ISugarService<DesktopShortcut>, IScopeDependency
{
    /// <summary>按解决方案列出有效快捷方式（排序）</summary>
    List<DesktopShortcut> ListBySolution(int? solutionId);
}

public class DesktopShortcutService : SugarService<DesktopShortcut>, IDesktopShortcutService
{
    public DesktopShortcutService(ISqlSugarClient db) : base(db) { }

    public List<DesktopShortcut> ListBySolution(int? solutionId)
        => List(s => s.IsActive && s.SolutionId == solutionId, "SortNo ASC, Id ASC");
}

// ============ 项目 / 字典 ============

public interface IDynProjectService : ISugarService<DynProject>, IScopeDependency { }
public class DynProjectService : SugarService<DynProject>, IDynProjectService
{
    public DynProjectService(ISqlSugarClient db) : base(db) { }
}

// ============ 系统菜单（树形，桌面快捷数据源） ============

public interface ISysMenuService : ISugarService<SysMenu>, IScopeDependency { }
public class SysMenuService : SugarService<SysMenu>, ISysMenuService
{
    public SysMenuService(ISqlSugarClient db) : base(db) { }
}

public interface IDynDictService : ISugarService<DynDict>, IScopeDependency
{
    /// <summary>按字典类型取启用项（排序）</summary>
    List<DynDict> ListByType(string dictType);
}

public class DynDictService : SugarService<DynDict>, IDynDictService
{
    public DynDictService(ISqlSugarClient db) : base(db) { }

    public List<DynDict> ListByType(string dictType)
        => List(d => d.IsActive && d.DictType == dictType, "SortNo ASC, Id ASC");
}

// ============ 设计器资产 ============

public interface IComponentMetaService : ISugarService<ComponentMeta>, IScopeDependency
{
    /// <summary>按 UI 平台取启用组件（Common 公用 + 指定平台）</summary>
    List<ComponentMeta> ListByUiPlatform(string uiPlatform);
}

public class ComponentMetaService : SugarService<ComponentMeta>, IComponentMetaService
{
    public ComponentMetaService(ISqlSugarClient db) : base(db) { }

    public List<ComponentMeta> ListByUiPlatform(string uiPlatform)
        => List(c => c.IsActive && (c.UiPlatform == "Common" || c.UiPlatform == uiPlatform),
            "Category ASC, SortNo ASC, Id ASC");
}

public interface IDynActionHelperService : ISugarService<DynActionHelper>, IScopeDependency
{
    /// <summary>按动作 Code 取启用项</summary>
    DynActionHelper GetByCode(string code);
}

public class DynActionHelperService : SugarService<DynActionHelper>, IDynActionHelperService
{
    public DynActionHelperService(ISqlSugarClient db) : base(db) { }

    public DynActionHelper GetByCode(string code)
        => Query(a => a.IsActive && a.Code == code).First();
}

public interface IDynTemplateService : ISugarService<DynTemplate>, IScopeDependency { }
public class DynTemplateService : SugarService<DynTemplate>, IDynTemplateService
{
    public DynTemplateService(ISqlSugarClient db) : base(db) { }
}

/// <summary>动态积木（Block）元数据服务：Code 稳定引用 + 按角色列出可用实现</summary>
public interface IDynBlockService : ISugarService<DynBlock>, IScopeDependency
{
    /// <summary>按 Code 取启用中的 Block（无则 null）</summary>
    DynBlock? GetByCode(string code);

    /// <summary>按槽位角色列启用 Block（filter/list/detail/tree），SortNo 排序</summary>
    List<DynBlock> ListByRole(string role);
}

public class DynBlockService : SugarService<DynBlock>, IDynBlockService
{
    public DynBlockService(ISqlSugarClient db) : base(db) { }

    public DynBlock? GetByCode(string code)
        => Query(b => b.IsActive && b.Code == code).First();

    public List<DynBlock> ListByRole(string role)
        => List(b => b.IsActive && b.ImplementsRole == role, "SortNo ASC, Id ASC");
}

/// <summary>模板-积木槽位关系服务：参数 schema 组装与装配时按模板取槽位</summary>
public interface IDynTemplateBlockService : ISugarService<DynTemplateBlock>, IScopeDependency
{
    /// <summary>按模板取全部槽位关系（SortNo 排序）</summary>
    List<DynTemplateBlock> ListByTemplate(int templateId);
}

public class DynTemplateBlockService : SugarService<DynTemplateBlock>, IDynTemplateBlockService
{
    public DynTemplateBlockService(ISqlSugarClient db) : base(db) { }

    public List<DynTemplateBlock> ListByTemplate(int templateId)
        => List(r => r.TemplateId == templateId, "SortNo ASC, Id ASC");
}

public interface IDynWebPageService : ISugarService<DynWebPage>, IScopeDependency
{
    /// <summary>按项目列页面</summary>
    List<DynWebPage> ListByProject(int? projectId);
}

public class DynWebPageService : SugarService<DynWebPage>, IDynWebPageService
{
    public DynWebPageService(ISqlSugarClient db) : base(db) { }

    public List<DynWebPage> ListByProject(int? projectId)
        => List(p => p.IsActive && p.ProjectId == projectId, "Id ASC");
}

public interface IPageSettingService : ISugarService<PageSetting>, IScopeDependency
{
    /// <summary>按区域类型（Filter/List/Detail）取设置</summary>
    List<PageSetting> ListByType(string settingType);
}

public class PageSettingService : SugarService<PageSetting>, IPageSettingService
{
    public PageSettingService(ISqlSugarClient db) : base(db) { }

    public List<PageSetting> ListByType(string settingType)
        => List(s => s.IsActive && s.SettingType == settingType, "SortNo ASC, Id ASC");
}

public interface IDynComService : ISugarService<DynCom>, IScopeDependency { }
public class DynComService : SugarService<DynCom>, IDynComService
{
    public DynComService(ISqlSugarClient db) : base(db) { }
}

// ============ 表结构显示名字典（PageGen 中文名来源） ============

public interface IDynSchemaLabelService : ISugarService<DynSchemaLabel>, IScopeDependency
{
    /// <summary>
    /// 批量解析列显示名：表字段层（ProjectId+TableName+ColumnName）优先，
    /// 未命中回退通用列名层（ProjectId=0,TableName=''）；都没有则不进返回字典，由调用方英文拆词兜底。
    /// 返回 key = 列名（忽略大小写），value = 显示名。
    /// </summary>
    Dictionary<string, string> ResolveLabels(int projectId, string table, IEnumerable<string> columns);

    /// <summary>表整体显示名（ProjectId+TableName+ColumnName=''），无则 null。</summary>
    string? ResolveTableLabel(int projectId, string table);

    /// <summary>
    /// 幂等回写【表字段层】显示名（PageGen 自动回写的唯一入口）。
    /// 纪律：projectId&lt;=0 或表名/列名为空直接跳过——永远不写通用层，避免一次生成污染跨项目通用词。
    /// 键已存在则更新 Label。
    /// </summary>
    void UpsertTableLabels(int projectId, string table, IReadOnlyDictionary<string, string> labels);
}

public class DynSchemaLabelService : SugarService<DynSchemaLabel>, IDynSchemaLabelService
{
    public DynSchemaLabelService(ISqlSugarClient db) : base(db) { }

    public Dictionary<string, string> ResolveLabels(int projectId, string table, IEnumerable<string> columns)
    {
        var wanted = columns.Where(c => !string.IsNullOrWhiteSpace(c))
                            .Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (wanted.Count == 0) return result;

        // 表字段层
        if (projectId > 0 && !string.IsNullOrWhiteSpace(table))
        {
            var rows = Query(x => x.ProjectId == projectId && x.TableName == table && x.ColumnName != "").ToList();
            foreach (var r in rows)
                if (wanted.Contains(r.ColumnName) && !string.IsNullOrWhiteSpace(r.Label))
                    result[r.ColumnName] = r.Label;
        }
        // 通用列名层（仅补表字段层未命中的）
        var missing = wanted.Where(w => !result.ContainsKey(w)).ToList();
        if (missing.Count > 0)
        {
            var common = Query(x => x.ProjectId == 0 && x.TableName == "").ToList();
            foreach (var r in common)
                if (missing.Contains(r.ColumnName, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(r.Label))
                    result[r.ColumnName] = r.Label;
        }
        return result;
    }

    public string? ResolveTableLabel(int projectId, string table)
    {
        if (projectId <= 0 || string.IsNullOrWhiteSpace(table)) return null;
        return Query(x => x.ProjectId == projectId && x.TableName == table && x.ColumnName == "")
            .First()?.Label;
    }

    public void UpsertTableLabels(int projectId, string table, IReadOnlyDictionary<string, string> labels)
    {
        // 硬性护栏：只写表字段层
        if (projectId <= 0 || string.IsNullOrWhiteSpace(table) || labels == null || labels.Count == 0) return;

        var existing = Query(x => x.ProjectId == projectId && x.TableName == table && x.ColumnName != "").ToList();
        var map = new Dictionary<string, DynSchemaLabel>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in existing) map[e.ColumnName] = e;

        // 通用列名层：表层只存"与通用层不同的差异"。值与通用层一致时不新增；
        // 历史已写入的冗余行顺手清掉（同值表层记录无信息量）。
        var common = Query(x => x.ProjectId == 0 && x.TableName == "").ToList()
            .ToDictionary(x => x.ColumnName, x => x.Label, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.Now;
        foreach (var kv in labels)
        {
            var col = kv.Key?.Trim() ?? "";
            var label = kv.Value?.Trim() ?? "";
            if (col.Length == 0 || label.Length == 0) continue;
            var sameAsCommon = common.TryGetValue(col, out var cl) && cl == label;

            if (map.TryGetValue(col, out var row))
            {
                if (sameAsCommon) { Delete(row); continue; }   // 与通用层同值 → 去冗余
                if (row.Label == label) continue;
                row.Label = label; row.UpdateTime = now;
                Update(row);
            }
            else
            {
                if (sameAsCommon) continue;                    // 与通用层同值 → 不复制
                Insert(new DynSchemaLabel
                {
                    ProjectId = projectId, TableName = table.Trim(), ColumnName = col,
                    Label = label, CreateTime = now, UpdateTime = now
                });
            }
        }
    }
}
