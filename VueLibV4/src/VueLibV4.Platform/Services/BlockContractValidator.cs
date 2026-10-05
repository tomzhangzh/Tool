#nullable enable
using VueLibV4.Platform.Models;

namespace VueLibV4.Platform.Services;

/// <summary>校验结果：Errors 阻断保存，Warnings 仅提示。</summary>
public class BlockValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public bool IsValid => Errors.Count == 0;

    public override string ToString()
        => $"valid={IsValid}, errors={Errors.Count}, warnings={Warnings.Count}";
}

/// <summary>
/// Block 契约校验器（主板层静态检查）。
/// 保存页面 DSL 时调用，不合法的连线在前端渲染前就拦住。
/// 校验内容：
///   1. 槽位引用的 Block 角色已登记契约；
///   2. wire 的源事件端口、目标命令端口真实存在；
///   3. 事件 payload 类型与命令 payload 类型是否兼容（弱类型 object 一律放行）；
///   4. 节点连线图是否存在潜在环（A→B→A，死循环风险）。
/// </summary>
public class BlockContractValidator
{
    private readonly IReadOnlyDictionary<string, IBlockContract> _contracts;

    public BlockContractValidator(IEnumerable<IBlockContract> contracts)
    {
        _contracts = contracts.ToDictionary(c => c.Role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>校验整个页面规格 + 一批 wire。</summary>
    /// <param name="spec">页面布局规格（含槽位）</param>
    /// <param name="wires">全部生效的 wire（壳默认 + 页面追加，已合并）</param>
    public BlockValidationResult Validate(PageLayoutSpec spec, IEnumerable<WireSpec> wires)
    {
        var result = new BlockValidationResult();
        ValidateSlots(spec, result);
        ValidateWires(spec, wires, result);
        DetectCycle(wires, result);
        return result;
    }

    // ---------------- 1. 槽位 ----------------

    private void ValidateSlots(PageLayoutSpec spec, BlockValidationResult r)
    {
        foreach (var (slotName, slot) in spec.Slots)
        {
            if (string.IsNullOrWhiteSpace(slot.Block))
            {
                r.Errors.Add($"槽位 \"{slotName}\" 未指定 block 角色");
                continue;
            }
            if (!_contracts.ContainsKey(slot.Block))
                r.Errors.Add($"槽位 \"{slotName}\" 引用了未登记的 Block 角色: \"{slot.Block}\""
                    + $"（已登记=[{string.Join(",", _contracts.Keys)}]）");
        }
    }

    // ---------------- 2. wire 端口 ----------------

    private void ValidateWires(PageLayoutSpec spec, IEnumerable<WireSpec> wires, BlockValidationResult r)
    {
        foreach (var w in wires)
        {
            var (srcNode, srcEvent) = SplitRef(w.From);
            if (string.IsNullOrEmpty(srcNode))
            {
                r.Errors.Add($"wire 的 from 必须是 \"节点.事件\": \"{w.From}\"");
                continue;
            }

            var srcContract = ResolveContract(spec, srcNode);
            if (srcContract == null)
            {
                r.Errors.Add($"wire 源节点 \"{srcNode}\" 未在 slots 声明或无契约（{w.From}）");
                continue;
            }

            var evPort = srcContract.Events.FirstOrDefault(e => e.Name == srcEvent);
            if (evPort == null)
                r.Errors.Add($"wire 源端口未声明: {w.From}"
                    + $"（block={srcContract.Role}，事件=[{string.Join(",", srcContract.Events.Select(e => e.Name))}]）");

            foreach (var t in w.To ?? new List<string>())
            {
                var (tgtNode, tgtCmd) = SplitRef(t);
                if (string.IsNullOrEmpty(tgtNode))
                {
                    r.Errors.Add($"wire 的 to 必须是 \"节点.命令\": \"{t}\"");
                    continue;
                }
                // shell 端口不参与 Block 契约校验
                if (tgtNode.Equals("shell", StringComparison.OrdinalIgnoreCase))
                    continue;

                var tgtContract = ResolveContract(spec, tgtNode);
                if (tgtContract == null)
                {
                    r.Errors.Add($"wire 目标节点 \"{tgtNode}\" 未在 slots 声明或无契约（来自 {w.From}）");
                    continue;
                }
                var cmdPort = tgtContract.Commands.FirstOrDefault(c => c.Name == tgtCmd);
                if (cmdPort == null)
                    r.Errors.Add($"wire 目标端口未声明: {t}"
                        + $"（block={tgtContract.Role}，命令=[{string.Join(",", tgtContract.Commands.Select(c => c.Name))}]）");

                // payload 类型兼容（弱类型 object 放行）
                if (evPort != null && cmdPort != null)
                {
                    var warn = CheckPayloadCompat(evPort.PayloadType, cmdPort.PayloadType);
                    if (warn != null) r.Warnings.Add($"wire {w.From} → {t}: {warn}");
                }
            }
        }
    }

    // ---------------- 3. 工具 ----------------

    /// <summary>按 slot 名找到 Block 契约；"shell" 返回 null（壳端口不校验）。</summary>
    private IBlockContract? ResolveContract(PageLayoutSpec spec, string node)
    {
        if (node.Equals("shell", StringComparison.OrdinalIgnoreCase)) return null;
        if (!spec.Slots.TryGetValue(node, out var slot)) return null;
        return _contracts.TryGetValue(slot.Block, out var c) ? c : null;
    }

    private static (string node, string port) SplitRef(string? r)
    {
        var s = r ?? string.Empty;
        var i = s.IndexOf('.');
        return i <= 0 ? (string.Empty, string.Empty) : (s[..i], s[(i + 1)..]);
    }

    /// <summary>
    /// 事件 payload 类型 → 命令 payload 类型 的兼容性。
    /// 规则：任一为 null（未声明）不校验；命令端是 object 一律放行；
    /// 可赋值（含相等）放行；否则给警告。
    /// </summary>
    private static string? CheckPayloadCompat(Type? eventType, Type? cmdType)
    {
        if (eventType == null || cmdType == null) return null;
        if (cmdType == typeof(object)) return null;
        if (eventType == cmdType) return null;
        if (cmdType.IsAssignableFrom(eventType)) return null;
        return $"payload 类型可能不匹配：事件发出 {eventType.Name}，命令接收 {cmdType.Name}";
    }

    // ---------------- 4. 环检测（节点级有向图 DFS 三色） ----------------

    private static void DetectCycle(IEnumerable<WireSpec> wires, BlockValidationResult r)
    {
        var adj = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        void AddEdge(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return;
            if (!adj.TryGetValue(a, out var set)) adj[a] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add(b);
        }

        foreach (var w in wires)
        {
            var (fn, _) = SplitRef(w.From);
            if (string.IsNullOrEmpty(fn)) continue;
            foreach (var t in w.To ?? new List<string>())
            {
                var (tn, _) = SplitRef(t);
                AddEdge(fn, tn);
            }
        }

        const int WHITE = 0, GRAY = 1, BLACK = 2;
        var color = adj.Keys.ToDictionary(k => k, _ => WHITE, StringComparer.OrdinalIgnoreCase);
        string? cycleEdge = null;

        void Dfs(string n)
        {
            color[n] = GRAY;
            if (adj.TryGetValue(n, out var outs))
            {
                foreach (var nx in outs)
                {
                    if (!color.ContainsKey(nx)) color[nx] = WHITE;
                    if (color[nx] == GRAY) { cycleEdge = $"{n} → {nx}"; return; }
                    if (color[nx] == WHITE) { Dfs(nx); if (cycleEdge != null) return; }
                }
            }
            color[n] = BLACK;
        }

        foreach (var n in adj.Keys.ToList())
        {
            if (color[n] == WHITE)
            {
                Dfs(n);
                if (cycleEdge != null)
                {
                    r.Warnings.Add($"检测到潜在循环连线（可能导致无限刷新）: {cycleEdge}");
                    break;
                }
            }
        }
    }
}
