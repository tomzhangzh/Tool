#nullable enable
using SqlSugar;

namespace VueLibV4.Platform.Models;

/// <summary>
/// 动态项目：属于某个 DesktopSolution。一个解决方案可有多个项目，项目本质是"不同的数据库"。
/// SolutionId 外键使用 int Id（DesktopSolution.Id）；ConnectionString 支持 "(startup)" 表示用平台库同源连接。
/// </summary>
[SugarTable("DynProject")]
public class DynProject
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    /// <summary>所属解决方案 Id（DesktopSolution.Id，可空 = 未归类）</summary>
    public int? SolutionId { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? DbType { get; set; } = "SqlServer";

    [SugarColumn(Length = 1000, IsNullable = true)]
    public string? ConnectionString { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ExtJson { get; set; }
}

/// <summary>
/// 平台数据字典：仅存 Platform 平台库的全局公共字典（PageCategory / TemplateCategory / ActionType / TargetType 等）。
/// 业务私有字典放入各业务库的 BusinessDict（BusinessDb），与平台字典双向隔离。
/// </summary>
[SugarTable("DynDict")]
public class DynDict
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string DictType { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string DictCode { get; set; } = string.Empty;

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? DictName { get; set; }

    /// <summary>父级字典 Id（支持树形词典）</summary>
    public int? ParentId { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Remark { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ExtJson { get; set; }
}

/// <summary>
/// 组件元数据：驱动设计器与运行时。
/// 组件定义双源：若 Views/Shared/Components/{ComponentName}.cshtml 有定义则代码优先，否则用 DB 中的 Template/Script/Style。
/// AcceptAll=接受任意子组件；AllowDrop/CanDropInto=拖拽准入白名单；SlotsDefine=插槽定义；PropsMeta=属性面板 schema。
/// </summary>
[SugarTable("ComponentMeta")]
public class ComponentMeta
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string ComponentName { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? Label { get; set; }

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    /// <summary>所属 UI 平台：Common（公用）/ ElementUI / NutUI</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string UiPlatform { get; set; } = "Common";

    /// <summary>前端动态加载组件定义的接口地址（为空=全局组件无需动态加载）</summary>
    [SugarColumn(Length = 300, IsNullable = true)]
    public string? LoadUrl { get; set; }

    /// <summary>后端 Razor View 路径（仅后端渲染用），不再运行时轮询目录</summary>
    [SugarColumn(Length = 300, IsNullable = true)]
    public string? ViewPath { get; set; }

    public bool AcceptAll { get; set; } = false;

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? AllowDrop { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? CanDropInto { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? SlotsDefine { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? PropsMeta { get; set; }

    /// <summary>组件 template（DB 双定义源之一；Razor View 定义优先）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? TemplateContent { get; set; }

    /// <summary>组件 script（export default {...}）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ScriptContent { get; set; }

    /// <summary>组件 style</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? StyleContent { get; set; }

    /// <summary>属性 schema（旧字段，PropsMeta 的别名）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? PropertyConfigJson { get; set; }

    /// <summary>默认配置（拖入画布时的初始配置）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? DefaultConfigJson { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>是否容器组件（可拖入子组件，设计器自动创建 Sortable）</summary>
    public bool IsContainer { get; set; } = false;

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ExtJson { get; set; }

    /// <summary>设计器通用元信息（JSON）：childrenSchemaPath 子项数组路径 / childKey / childTitleField / supportDragSort，框架自动生成子项新增/删除/排序</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? DesignerMeta { get; set; }

    /// <summary>设计器扩展操作按钮定义（JSON 数组）：command=内置命令 / view=自定义视图；仅设计器读取，运行态不下发</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? DesignerOperates { get; set; }
}

/// <summary>
/// 动作助手注册表：所有动作（含脚本）必须入库。
/// ActionType: script / url / api / chain；Script 存脚本或 URL 模板或动作链 JSON。
/// 前端运行时从 /all 动态加载注册；Code 为动作名（dyn-click-{code} / dyn-init-{code}）。
/// </summary>
[SugarTable("DynActionHelper")]
public class DynActionHelper
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? ActionType { get; set; } = "script";

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Script { get; set; }

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Language { get; set; } = "javascript";

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ParamsJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 页面模板：预定义模板（学生管理 CRUD / 左树右列表 / 主页 / 空白页 等）。
/// DefaultJson = 模板默认配置/默认值；ConfigJson = 模板参数 schema（DynamicControl 渲染参数面板）。
/// </summary>
[SugarTable("DynTemplate")]
public class DynTemplate
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? Icon { get; set; }

    /// <summary>模板默认配置/默认值 JSON：新建页面实例时复制作为初始参数；等价于旧字段 TemplateJson（页面配置树）。</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? DefaultJson { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ConfigJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    /// <summary>
    /// 外壳 Razor 视图路径（例：~/Views/DynTemplates/CrudBasic.cshtml）。
    /// DynWebPage 运行时根据实例 → 模板 → 组装 DynSharedModel → 渲染此外壳视图。
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    public string? ViewPath { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 动态网页：真正的动态页面。选择一个 DynTemplate，结合用户配置参数（ParamsJson）实例化 PageJson 即可运行。
/// PageJson 为空时回退模板 DefaultJson（模板即页面）。
/// 外键一律使用 int Id（ProjectId → DynProject.Id；TemplateId → DynTemplate.Id）。
/// 实例参数统一存于 ParamsJson：模板自身参数顶层扁平 + blocks 槽位分组（各 Block 的 settingId/model/options）。
/// </summary>
[SugarTable("DynWebPage")]
public class DynWebPage
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    /// <summary>所属项目 Id（DynProject.Id）</summary>
    public int? ProjectId { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>使用的模板 Id（DynTemplate.Id）</summary>
    public int? TemplateId { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? PageJson { get; set; }

    /// <summary>
    /// 实例参数 JSON（原 ConfigJson 改名）：用户在参数面板填写的配置。
    /// 新结构 = 模板自身参数（顶层扁平，与旧版完全兼容）+ blocks 槽位分组：
    /// { TableName:'...', ...模板参数, blocks:{ filter:{settingId,model,options}, list:{...}, detail:{...} } }
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ParamsJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Url { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ExtJson { get; set; }
}

/// <summary>
/// 三屏页面设置（M4）：一条记录对应“筛选区 / 列表区 / 详情区”之一的组件配置树（ConfigJson）。
/// DynWebPage 通过实例参数 ConfigJson 中的 Filter/List/DetailPageSettingId 绑定三屏，由模板 View 渲染。
/// </summary>
[SugarTable("PageSetting")]
public class PageSetting
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Filter=筛选区 / List=列表区 / Detail=详情区</summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    public string SettingType { get; set; } = "List";

    public int? ProjectId { get; set; }

    [SugarColumn(Length = 100, IsNullable = true)]
    public string? TableName { get; set; }

    /// <summary>组件配置树（与设计器 PageJson 同构）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ConfigJson { get; set; }

    /// <summary>渲染方式：Front=前端DynCom动态控件 / Back=后端Razor局部视图</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string RenderMode { get; set; } = "Front";

    [SugarColumn(Length = 200, IsNullable = true)]
    public string? PartialPath { get; set; }

    public string? DefaultJson { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 动态组件（DynCom）库：设计时与运行时共用一套代码；组件定义存库（ConfigJson）。
/// 若代码（dyn-com.js）中已定义同名组件，则优先使用代码中的实现（"如果存在定义不用在数据库中定义"）。
/// </summary>
[SugarTable("DynCom")]
public class DynCom
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>对应 dyn-com.js 中的实现名</summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    public string? ComponentName { get; set; }

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ConfigJson { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;

    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ExtJson { get; set; }
}

/// <summary>
/// 动态积木（Block）：可复用的独立功能单元（FilterApp / ListApp / DetailApp …）。
/// 终态支持 DB 动态注册：ViewPath（静态 cshtml，开发期便利）与 HtmlCode/ScriptCode（在线编辑）双源；
/// 当前阶段内置 Block 以 ViewPath 为准。
/// 接线参数遵循 UI 包语义：ParamConfigJson=参数表单 UI 树（dyn-dynamic-com 可渲染），
/// ParamDefaultJson=参数 model 骨架/默认值；Commands/Events 为命令/事件契约清单（JSON 数组）。
/// </summary>
[SugarTable("DynBlock")]
public class DynBlock
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>积木编码（filter/list/detail，全局唯一，关系/装配按 Code 稳定引用）</summary>
    [SugarColumn(Length = 64, IsNullable = false)]
    public string Code { get; set; } = string.Empty;

    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    [SugarColumn(Length = 50, IsNullable = true)]
    public string? Category { get; set; }

    /// <summary>实现的槽位角色：filter/list/detail/tree（同一角色允许有多个 Block 实现）</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string ImplementsRole { get; set; } = string.Empty;

    /// <summary>静态 Razor 视图路径（内置 Block；在线 HtmlCode 为空时使用）</summary>
    [SugarColumn(Length = 300, IsNullable = true)]
    public string? ViewPath { get; set; }

    /// <summary>在线 HTML（终态：DB 动态布局，本阶段预留）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? HtmlCode { get; set; }

    /// <summary>在线脚本（终态：DB 动态脚本，本阶段预留）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ScriptCode { get; set; }

    /// <summary>接线参数 UI 包之 ConfigJson：参数表单 UI 树（与 PageSetting.ConfigJson 同构）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ParamConfigJson { get; set; }

    /// <summary>接线参数 UI 包之 DefaultJson：参数 model 骨架/默认值</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? ParamDefaultJson { get; set; }

    /// <summary>接受的命令清单（JSON 数组，如 ["loadData","reload"]；契约文档/在线接线用）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Commands { get; set; }

    /// <summary>发出的事件清单（JSON 数组，如 ["add","edit"]；契约文档/在线接线用）</summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    public string? Events { get; set; }

    [SugarColumn(Length = 500, IsNullable = true)]
    public string? Description { get; set; }

    public int SortNo { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 模板-积木槽位关系：一个 Template 的每个槽位（filter/list/detail/tree）放哪个 Block。
/// (TemplateId, Slot) 唯一；WebPage 参数页按本表折叠分组渲染各 Block 的接线参数。
/// </summary>
[SugarTable("DynTemplateBlock")]
public class DynTemplateBlock
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>模板 Id（DynTemplate.Id）</summary>
    public int TemplateId { get; set; }

    /// <summary>槽位角色：filter/list/detail/tree</summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Slot { get; set; } = string.Empty;

    /// <summary>该槽位使用的 Block Id（DynBlock.Id）</summary>
    public int BlockId { get; set; }

    /// <summary>是否必选槽位（参数页提示/校验用）</summary>
    public bool Required { get; set; } = false;

    public int SortNo { get; set; } = 0;

    public DateTime CreateTime { get; set; } = DateTime.Now;
}
