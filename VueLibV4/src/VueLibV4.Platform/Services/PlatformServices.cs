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
