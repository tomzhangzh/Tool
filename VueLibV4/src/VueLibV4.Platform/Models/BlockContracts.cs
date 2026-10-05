#nullable enable
namespace VueLibV4.Platform.Models;

// =====================================================================================
// Block 契约模型（主板/板卡抽象）
// -------------------------------------------------------------------------------------
// 背景：
//   Block = 一块独立业务板卡（FilterApp / ListApp / DetailApp ...），自带完整业务能力，
//   对外只暴露"通信端口"。Layout（主板）只通过端口收发消息，完全不关心 Block 内部实现。
//
// 本文件是【后端契约描述层】：
//   - 用 C# 强类型描述每个 Block 能接收哪些命令(输入端口)、能抛出哪些事件(输出端口)；
//   - 描述 wire 连线（A 的事件 → B 的命令）；
//   - 供后端在保存页面 DSL 时做静态校验（端口是否存在、连线是否成环），
//     并可由反射导出前端 dyn-layout-engine 需要的 ContractJson。
//
// 注意：这里不是前端运行时。前端 Block 是 Vue app，运行时消息队列/状态机在
//       wwwroot/dyn 下的 js 里；本层只负责"定义"和"校验"。
// =====================================================================================

/// <summary>Block 生命周期状态（运行时由前端维护，此处用于文档/校验对齐）。</summary>
public enum BlockState
{
    /// <summary>未挂载</summary>
    NotMounted,
    /// <summary>挂载中</summary>
    Mounting,
    /// <summary>挂载完成，可处理命令</summary>
    Ready,
    /// <summary>已销毁</summary>
    Destroyed
}

/// <summary>
/// 一个命令端口（输入针脚）：外部可以发给 Block 的指令。
/// </summary>
public class BlockCommandPort
{
    /// <summary>命令名，如 reload / newForm / editForm</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>用途说明</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>该命令接收的 payload 类型。
    /// 高频标准链路用强类型（如 FilterConditionPayload）；
    /// 多变业务记录用 typeof(object) 放宽，不做字段级静态校验。</summary>
    public Type? PayloadType { get; set; }
}

/// <summary>
/// 一个事件端口（输出针脚）：Block 向外广播的消息。
/// </summary>
public class BlockEventPort
{
    /// <summary>事件名，如 changed / saved / edit</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>用途说明</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>该事件向外携带的数据类型（同 PayloadType 的强/弱取舍）。</summary>
    public Type? PayloadType { get; set; }
}

/// <summary>
/// 所有 Block 都要实现的契约接口（相当于主板认识的统一协议）。
/// 每一类 Block 自己声明：我有哪些输入命令端口、哪些输出事件端口。
/// </summary>
public interface IBlockContract
{
    /// <summary>Block 角色名（与 CONTRACTS 里的 role 对应：filter/list/detail/tree...）</summary>
    string Role { get; }

    /// <summary>可接收的命令（输入端口）</summary>
    IReadOnlyList<BlockCommandPort> Commands { get; }

    /// <summary>可抛出的事件（输出端口）</summary>
    IReadOnlyList<BlockEventPort> Events { get; }
}

// -------------------------------------------------------------------------------------
// 标准强类型 payload（高频、结构稳定的链路）
// -------------------------------------------------------------------------------------

/// <summary>单条筛选条件：字段 + 操作符 + 值。Filter→List 标准载荷。</summary>
public class FilterCondition
{
    /// <summary>字段名</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>操作符：eq / like / gt / lt / in ...</summary>
    public string Op { get; set; } = "eq";

    /// <summary>条件值</summary>
    public object? Value { get; set; }
}

/// <summary>
/// Filter.changed 事件 / List.reload 命令 的强类型载荷。
/// 这是标准链路，结构固定，后端静态校验严格匹配。
/// </summary>
public class FilterConditionPayload
{
    /// <summary>条件集合：{字段:{op,value}}</summary>
    public Dictionary<string, FilterCondition> Filter { get; set; } = new();
}

/// <summary>
/// List.edit / List.rowClick 事件载荷（弱类型）。
/// 不同业务表字段差异大，这里只保证"带了一行数据"，内部字段不做静态校验。
/// </summary>
public class ListRowPayload
{
    /// <summary>整行业务数据（业务表字段，dynamic）</summary>
    public Dictionary<string, object?> Row { get; set; } = new();
}

/// <summary>Detail.saved 事件载荷。</summary>
public class DetailSavedPayload
{
    /// <summary>操作类型：add / edit / delete</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>保存后的整行数据（弱类型）</summary>
    public Dictionary<string, object?> Row { get; set; } = new();
}

// -------------------------------------------------------------------------------------
// 三个内置 Block 的具体契约（示范）
// -------------------------------------------------------------------------------------

/// <summary>FilterApp 契约：筛选用。</summary>
public class FilterBlockContract : IBlockContract
{
    public string Role => "filter";

    public IReadOnlyList<BlockCommandPort> Commands => new List<BlockCommandPort>
    {
        new() { Name = "submit", Description = "手动触发查询", PayloadType = null },
        new() { Name = "reset",  Description = "重置到默认条件", PayloadType = null },
    };

    public IReadOnlyList<BlockEventPort> Events => new List<BlockEventPort>
    {
        new() { Name = "changed", Description = "筛选条件变化（点查询/重置/挂载后首推）", PayloadType = typeof(FilterConditionPayload) },
    };
}

/// <summary>ListApp 契约：列表用。</summary>
public class ListBlockContract : IBlockContract
{
    public string Role => "list";

    public IReadOnlyList<BlockCommandPort> Commands => new List<BlockCommandPort>
    {
        new() { Name = "loadData", Description = "带筛选条件回第一页加载", PayloadType = typeof(FilterConditionPayload) },
        new() { Name = "reload",   Description = "按当前条件刷新",           PayloadType = typeof(FilterConditionPayload) },
    };

    public IReadOnlyList<BlockEventPort> Events => new List<BlockEventPort>
    {
        new() { Name = "add",      Description = "点新增按钮",     PayloadType = null },
        new() { Name = "edit",     Description = "点编辑按钮(带行)", PayloadType = typeof(ListRowPayload) },
        new() { Name = "addChild", Description = "点+子(带父行)",  PayloadType = typeof(ListRowPayload) },
    };
}

/// <summary>DetailApp 契约：明细表单用。</summary>
public class DetailBlockContract : IBlockContract
{
    public string Role => "detail";

    public IReadOnlyList<BlockCommandPort> Commands => new List<BlockCommandPort>
    {
        new() { Name = "newForm",   Description = "打开新增表单(可带预填)", PayloadType = typeof(DetailFormPayload) },
        new() { Name = "editForm",  Description = "用给定行回填",           PayloadType = typeof(ListRowPayload) },
    };

    public IReadOnlyList<BlockEventPort> Events => new List<BlockEventPort>
    {
        new() { Name = "saved",  Description = "保存/删除成功", PayloadType = typeof(DetailSavedPayload) },
        new() { Name = "cancel", Description = "取消",          PayloadType = null },
    };
}

/// <summary>Detail.newForm 命令载荷（预填外键等）。</summary>
public class DetailFormPayload
{
    /// <summary>预填字段：{字段:值}</summary>
    public Dictionary<string, object?> PreFill { get; set; } = new();
}

// -------------------------------------------------------------------------------------
// Wire 连线 / 页面级 DSL 的 C# 模型（供后端反序列化 + 静态校验）
// -------------------------------------------------------------------------------------

/// <summary>
/// 一条 wire 连线：从某个 Block 的某个事件，发到另一个 Block 的某个命令。
/// 对应前端 dyn-layout-engine 的 wire 定义。
/// </summary>
public class WireSpec
{
    /// <summary>源："节点名.事件名"，如 filter.changed</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>目标："节点名.命令名"，如 master.loadData（可多个）</summary>
    public List<string> To { get; set; } = new();

    /// <summary>
    /// 载荷映射模板：'$event.x' / '$provide.x' / 字面量；省略则整包透传。
    /// </summary>
    public object? With { get; set; }
}

/// <summary>一个槽位里放哪个 Block 角色。</summary>
public class LayoutSlotSpec
{
    /// <summary>Block 角色名（filter/list/detail/tree...），对应 IBlockContract.Role</summary>
    public string Block { get; set; } = string.Empty;

    /// <summary>该槽的 PageSetting 配置 Id</summary>
    public int? SettingId { get; set; }
}

/// <summary>
/// 布局壳 PageSpec（对应 DynWebPage.SpecJson）。
/// 后端反序列化页面 DSL 用，不再用 JObject 到处挖字段。
/// </summary>
public class PageLayoutSpec
{
    /// <summary>布局壳名：list-master-detail / tree-master-detail / 未来的动态画布布局</summary>
    public string Layout { get; set; } = string.Empty;

    /// <summary>布局属性（leftWidth、childFkField 等）</summary>
    public Dictionary<string, object?> LayoutProps { get; set; } = new();

    /// <summary>页面级 provide：表名、主键、project 等</summary>
    public Dictionary<string, object?> Provide { get; set; } = new();

    /// <summary>槽位：slot名 → Block 配置</summary>
    public Dictionary<string, LayoutSlotSpec> Slots { get; set; } = new();

    /// <summary>页面级连线（追加在壳默认连线之后）</summary>
    public List<WireSpec> Wires { get; set; } = new();

    /// <summary>是否替换壳默认连线</summary>
    public bool ReplaceDefaultWires { get; set; }
}
