using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 工具页面：组件检查、ER 图、页面图谱、Markdown 查看器
/// </summary>
[Area("Platform")]
public class ToolController : Controller
{
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;
    private readonly IDynTemplateBlockService _tplBlocks;
    private readonly IDynBlockService _blocks;

    public ToolController(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings,
        IDynTemplateBlockService tplBlocks,
        IDynBlockService blocks)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
        _tplBlocks = tplBlocks;
        _blocks = blocks;
    }

    /// <summary>组件检查：在线测试组件</summary>
    [HttpGet("/Platform/Page/ComponentCheck")]
    public IActionResult ComponentCheck()
    {
        ViewData["Title"] = "组件检查 - VueLibV4";
        return View(string.Format(Page, "ComponentCheck"));
    }

    [HttpGet("/Platform/Page/PageGraphDemo")]
    public IActionResult PageGraphDemo(int id = 34)
    {
        ViewData["Title"] = "页面组成图 - VueLibV4";
        var g = BuildPageGraph(id);
        if (g.WpMissing) return Content($"WebPage {id} 不存在");
        var json = JsonSerializer.Serialize(
            new { isV2 = g.IsV2, error = g.Error, wpName = g.WpName, nodes = g.Nodes, edges = g.Edges },
            new JsonSerializerOptions { WriteIndented = false });
        ViewData["GraphJson"] = json;
        ViewData["WpName"] = g.WpName;
        ViewData["IsV2"] = g.IsV2;
        if (!g.IsV2) ViewData["ErrorMsg"] = g.Error;
        return View(string.Format(Page, "PageGraphDemo"));
    }

    /// <summary>图谱 JSON 端点：供页面实例编辑弹窗的"页面组成图"内嵌小图拉取。</summary>
    [HttpGet("/Platform/Page/PageGraphData")]
    public IActionResult PageGraphData(int id)
    {
        var g = BuildPageGraph(id);
        return new JsonResult(ApiResult.Ok(new
        {
            isV2 = g.IsV2,
            error = g.Error,
            wpName = g.WpName,
            nodes = g.Nodes,
            edges = g.Edges
        }));
    }

    // ============================ 页面组成图（递归） ============================
    // 布局：每个 WebPage 是一个虚线"组框"，组内三列：Template → 槽位/Tab → PageSetting；
    // 子页面（tabDialogPageId / childUrl 指向的 DynWebPage）递归成右侧的新组框，深度无限（遇重复引用折叠为虚节点）。
    private const double GRP_W = 760, GRP_TOP = 46, GRP_PAD = 20;
    private const double TPL_X = 16,  TPL_W = 200;
    private const double SRC_X = 236, SRC_W = 230;
    private const double LEAF_X = 486, LEAF_W = 250, LEAF_H = 70, LEAF_GAP = 12;
    private const double ROW_GAP = 18, CHILD_DX = 800;

    private static readonly string C_WP_STROKE = "#1e88e5";
    private static readonly string C_T_FILL = "#e8f5e9",  C_T_STROKE = "#43a047",  C_T_TEXT = "#1b5e20";
    private static readonly string C_B_FILL = "#fff3e0",  C_B_STROKE = "#fb8c00",  C_B_TEXT = "#e65100";
    private static readonly string C_PS_FILL = "#f4f0fa", C_PS_STROKE = "#9b59b6", C_PS_TEXT = "#5b2c6f";
    private static readonly string C_FIX_STROKE = "#c0392b", C_FIX_TEXT = "#922b21";
    private static readonly string C_TAB_FILL = "#eef2ff", C_TAB_STROKE = "#6366f1", C_TAB_TEXT = "#3730a3";
    private static readonly string C_DLG_FILL = "#f3eefb", C_DLG_STROKE = "#7e57c2", C_DLG_TEXT = "#4a2b7a";
    private static readonly string C_CUS_FILL = "#e0f7fa", C_CUS_STROKE = "#00838f", C_CUS_TEXT = "#006064";
    private static readonly string C_GREY_FILL = "#f5f5f5", C_GREY_STROKE = "#909399", C_GREY_TEXT = "#616161";

    private sealed class GraphRow
    {
        public string SrcKind = "tab";      // blk | tab | dialog
        public string Title = "", Sub = "", Slot = "", EdgeLabel = "";
        public DynBlock? Blk;
        public bool Required = true;
        public string? NodeId;
        public List<(string kind, int? psId, int? wpId, string? url, string? title, string? edgeLabel)> Leaves = new();
    }

    private sealed class GraphBuild
    {
        public bool WpMissing, IsV2 = true;
        public string Error = "", WpName = "";
        public List<object> Nodes = new(), Edges = new();
        public Dictionary<int, PageSetting> PsCache = new();
        public Dictionary<int, DynBlock> BlkById = new();
        public Dictionary<string, (string id, string project)> ModalMap = new();
        public HashSet<int> Visited = new();
        public int Seq;
        public readonly double[] NextY = new double[32];
        /// <summary>当前正在构建的组框 id，组内节点 parent 挂它；递归子组时保存/恢复。</summary>
        public string? PendingContainer;
        /// <summary>最近一次 BuildWp 产出的组框（或折叠/异常节点）id，供父级连边。</summary>
        public string? LastContainerId;
        /// <summary>每个 WP 组内的积木事件节点（事件流边只在同组内连接）。</summary>
        public List<List<(string nodeId, List<string> emits, List<string> listens)>> GroupEventNodes = new();
        public string Nid(string p) => p + (++Seq);
        public (string id, string project) Modal(string code) =>
            ModalMap.TryGetValue(code, out var v) ? v : ("", "");
    }

    private GraphBuild BuildPageGraph(int rootId)
    {
        var ctx = new GraphBuild();
        var root = _webPages.GetById(rootId);
        if (root == null) { ctx.WpMissing = true; return ctx; }
        ctx.WpName = root.Name;

        foreach (var b in _blocks.Query(x => true).ToList()) ctx.BlkById[b.Id] = b;
        var modalCodes = new[] { "dynwebpage-modal", "dyntemplate-modal", "dynblock-modal", "pagesetting-modal" };
        foreach (var m in _webPages.Query(x => modalCodes.Contains(x.Code)).ToList())
            ctx.ModalMap[m.Code] = (m.Id.ToString(), m.ProjectId?.ToString() ?? "");

        BuildWp(root, 20, 20, 0, ctx);
        if (ctx.IsV2 && string.IsNullOrEmpty(ctx.Error))
            AddEventEdges(ctx);
        return ctx;
    }

    /// <summary>递归构建一个 WebPage 组框，返回组框高度（含内边距）。</summary>
    private double BuildWp(DynWebPage wp, double gx, double gy, int depth, GraphBuild ctx)
    {
        var tpl = wp.TemplateId > 0 ? _templates.GetById(wp.TemplateId.Value) : null;
        bool isV2 = tpl != null && (tpl.ViewPath?.Contains("DynTemplates") ?? false);
        if (!isV2)
        {
            // V1 / 无壳：灰节点，不展开（图谱只支持 V2）
            var vId = ctx.Nid("V");
            ctx.Nodes.Add(new
            {
                id = vId, x = gx, y = gy, width = 320, height = 64, shape = "rect",
                attrs = new
                {
                    body = new { fill = C_GREY_FILL, stroke = C_GREY_STROKE, strokeWidth = 1.5, rx = 6, ry = 6, cursor = "pointer" },
                    label = new { text = $"📄 {wp.Name}\n#{wp.Id} · V1 壳（{tpl?.Code ?? "无模板"}），不展开", fill = C_GREY_TEXT, fontSize = 12 }
                },
                data = WpData(wp, ctx)
            });
            ctx.LastContainerId = vId;
            ctx.IsV2 = false;
            ctx.Error = $"WebPage(id={wp.Id}) 使用的是 V1 壳模板（{tpl?.Code ?? "无"}），页面组成图仅支持 V2 架构页面。";
            return 90;
        }
        if (!ctx.Visited.Add(wp.Id))
        {
            // 已展开过（循环引用/被多处复用）：折叠为引用节点，不再递归
            var lId = ctx.Nid("L");
            ctx.Nodes.Add(new
            {
                id = lId, x = gx, y = gy + 12, width = LEAF_W, height = LEAF_H, shape = "rect",
                attrs = new
                {
                    body = new { fill = "#e3f2fd", stroke = C_WP_STROKE, strokeWidth = 1.5, strokeDasharray = "5 4", rx = 6, ry = 6, cursor = "pointer" },
                    label = new { text = $"📄↩ {wp.Name}\n#{wp.Id} · 复用/循环引用", fill = "#0d47a1", fontSize = 12 }
                },
                data = WpData(wp, ctx)
            });
            ctx.LastContainerId = lId;
            return LEAF_H + 24;
        }

        // 预生成组框 id：组内节点 parent 都挂它；递归子组时保存/恢复 PendingContainer
        var cId = ctx.Nid("G");
        // X6 fromJSON 按数组顺序 addCell，父节点必须先于子节点：组框最后插入到本组起始位置
        int groupStart = ctx.Nodes.Count;
        var savedContainer = ctx.PendingContainer;
        ctx.PendingContainer = cId;

        var param = string.IsNullOrEmpty(wp.ParamsJson) ? new JObject() : JObject.Parse(wp.ParamsJson);
        var blocksNode = param["blocks"] as JObject ?? new JObject();
        int SlotSetting(string slot) =>
            blocksNode[slot] is JObject sv && int.TryParse(sv["settingId"]?.ToString(), out var sid2) ? sid2 : 0;

        var rows = new List<GraphRow>();
        var blkNodes = new List<(string nodeId, List<string> emits, List<string> listens)>();

        // —— 1) 模板槽位行：DynTemplateBlock 关系表为准，ParamsJson.blocks 并集兜底 ——
        var seenSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (wp.TemplateId != null)
        {
            foreach (var rowTb in _tplBlocks.ListByTemplate(wp.TemplateId.Value))
            {
                seenSlots.Add(rowTb.Slot);
                ctx.BlkById.TryGetValue(rowTb.BlockId, out var blk0);
                var r = new GraphRow
                {
                    SrcKind = "blk", Blk = blk0, Slot = rowTb.Slot, Required = rowTb.Required,
                    Title = blk0 != null ? blk0.Name : $"Block#{rowTb.Slot}",
                    Sub = $"slot={rowTb.Slot}" + (blk0 != null ? $" · {blk0.Code}" : "（未注册积木）"),
                    EdgeLabel = "配置"
                };
                var sid = SlotSetting(rowTb.Slot);
                if (sid > 0) r.Leaves.Add(("ps", sid, null, null, null, "配置"));
                rows.Add(r);
            }
        }
        foreach (var prop in blocksNode.Properties())
        {
            if (seenSlots.Contains(prop.Name)) continue;
            var extra = ctx.BlkById.Values.FirstOrDefault(b => string.Equals(b.ImplementsRole, prop.Name, StringComparison.OrdinalIgnoreCase))
                        ?? ctx.BlkById.Values.FirstOrDefault(b => string.Equals(b.Code, prop.Name, StringComparison.OrdinalIgnoreCase));
            var r = new GraphRow
            {
                SrcKind = "blk", Blk = extra, Slot = prop.Name, Required = false,
                Title = extra != null ? extra.Name : $"Block#{prop.Name}",
                Sub = $"slot={prop.Name}" + (extra != null ? $" · {extra.Code}" : "（未注册积木）"),
                EdgeLabel = "配置"
            };
            var sid = SlotSetting(prop.Name);
            if (sid > 0) r.Leaves.Add(("ps", sid, null, null, null, "配置"));
            rows.Add(r);
        }

        int detailSid = SlotSetting("detail");
        int? IntParam(string k) => int.TryParse(param[k]?.ToString(), out var v) ? v : null;

        // —— 2) 主信息页签 masterDetailSettingId（与 detail 槽同值时不重复画）——
        var masterSid = IntParam("masterDetailSettingId");
        if (masterSid is > 0 && masterSid != detailSid)
            rows.Add(new GraphRow
            {
                SrcKind = "tab", Title = "📑 主信息页签", Sub = "masterDetailSettingId", EdgeLabel = "主信息",
                Leaves = { ("ps", masterSid, null, null, null, "主信息") }
            });

        // —— 3) 原生子表 child.{table,listSettingId,detailSettingId} ——
        if (param["child"] is JObject ch)
        {
            var cr = new GraphRow
            {
                SrcKind = "tab",
                Title = "📑 子表页签",
                Sub = $"child · {ch["table"]} ←{ch["foreignKey"]}",
            };
            if (IntOf(ch, "listSettingId") is int ls) cr.Leaves.Add(("ps", ls, null, null, null, "子表-列表"));
            if (IntOf(ch, "detailSettingId") is int ds) cr.Leaves.Add(("ps", ds, null, null, null, "子表-详情"));
            if (cr.Leaves.Count > 0) rows.Add(cr);
        }

        // —— 4) childUrl：DynWebPage?id=N → 递归子页面；其他 action → 自定义片段 ——
        var childUrl = param["childUrl"]?.ToString();
        if (!string.IsNullOrWhiteSpace(childUrl))
        {
            var childTitle = param["childTabTitle"]?.ToString() ?? "子页面页签";
            var m = Regex.Match(childUrl, @"DynWebPage\?(?:[^""']*?[?&])?id=(\d+)", RegexOptions.IgnoreCase);
            var r = new GraphRow { SrcKind = "tab", Title = "📑 " + childTitle, Sub = "childUrl" };
            if (m.Success)
                r.Leaves.Add(("wp", null, int.Parse(m.Groups[1].Value), null, childTitle, childTitle));
            else
                r.Leaves.Add(("custom", null, null, childUrl, childTitle, childTitle));
            rows.Add(r);
        }

        // —— 5) 编辑弹窗 tabDialogPageId → 递归子页面 ——
        if (IntParam("tabDialogPageId") is int dlgId)
            rows.Add(new GraphRow
            {
                SrcKind = "dialog", Title = "🪟 编辑弹窗页签", Sub = $"tabDialogPageId={dlgId}",
                Leaves = { ("wp", null, dlgId, null, "编辑弹窗", "编辑弹窗") }
            });

        // ===== 布局 =====
        double innerTop = gy + GRP_TOP;
        double y = innerTop;
        double totalInner = 0;
        foreach (var r in rows)
        {
            int leafCount = Math.Max(1, r.Leaves.Count);
            double rowH = leafCount * LEAF_H + (leafCount - 1) * LEAF_GAP;
            r.NodeId = BuildSrcNode(r, gx, y, rowH, ctx, blkNodes);
            double ly = y;
            foreach (var leaf in r.Leaves)
            {
                switch (leaf.kind)
                {
                    case "ps":
                        var psId = leaf.psId!.Value;
                        var psNodeId = BuildPsNode(psId, gx + LEAF_X, ly, ctx);
                        AddEdge(ctx, r.NodeId, psNodeId, C_PS_STROKE, "5 5", 1.5, leaf.edgeLabel);
                        ly += LEAF_H + LEAF_GAP;
                        break;
                    case "custom":
                        var cusId = BuildCustomNode(leaf.url!, gx + LEAF_X, y, ctx);
                        AddEdge(ctx, r.NodeId, cusId, C_CUS_STROKE, "5 5", 1.5, leaf.edgeLabel);
                        break;
                    case "wp":
                        var childWp = _webPages.GetById(leaf.wpId!.Value);
                        if (childWp == null)
                        {
                            var missId = BuildMissingNode(leaf.wpId!.Value, gx + LEAF_X, y, ctx);
                            AddEdge(ctx, r.NodeId, missId, C_GREY_STROKE, "5 5", 1.5, leaf.edgeLabel);
                        }
                        else
                        {
                            double cgy = Math.Max(y, ctx.NextY[Math.Min(depth + 1, 31)]);
                            double childH = BuildWp(childWp, gx + CHILD_DX, cgy, depth + 1, ctx);
                            ctx.NextY[Math.Min(depth + 1, 31)] = cgy + childH + 40;
                            AddEdge(ctx, r.NodeId, ctx.LastContainerId!, C_WP_STROKE, null, 2, leaf.edgeLabel);
                        }
                        // 递归会改写 PendingContainer，恢复到本组
                        ctx.PendingContainer = cId;
                        break;
                }
            }
            y += rowH + ROW_GAP;
            totalInner += rowH + ROW_GAP;
        }
        totalInner = Math.Max(totalInner - ROW_GAP, LEAF_H);
        double groupH = GRP_TOP + totalInner + GRP_PAD;

        // Template 节点（一个，纵贯所有行）
        var tId = ctx.Nid("T");
        ctx.Nodes.Add(new
        {
            id = tId, x = gx + TPL_X, y = innerTop, width = TPL_W, height = totalInner, shape = "rect",
            parent = cId,
            attrs = new
            {
                body = new { fill = C_T_FILL, stroke = C_T_STROKE, strokeWidth = 2, rx = 6, ry = 6, cursor = "pointer" },
                label = new { text = $"🧩 {tpl.Name}\n#{tpl.Id} · {tpl.Code}", fill = C_T_TEXT, fontSize = 12 }
            },
            data = new
            {
                kind = "tpl", name = tpl.Name, code = tpl.Code, rowId = tpl.Id,
                modalId = ctx.Modal("dyntemplate-modal").id, project = ctx.Modal("dyntemplate-modal").project
            }
        });
        // 组框（前插到本组所有节点之前；X6 fromJSON 按顺序 addCell，父必须先于子）
        ctx.Nodes.Insert(groupStart, new
        {
            id = cId, x = gx, y = gy, width = GRP_W, height = groupH, zIndex = 0, shape = "rect",
            attrs = new
            {
                body = new { fill = "#fcfdff", stroke = C_WP_STROKE, strokeWidth = 1.5, strokeDasharray = "8 5", rx = 10, ry = 10, cursor = "pointer" },
                label = new
                {
                    text = $"📄 {wp.Name}   #{wp.Id} · {wp.Code}", fill = "#0d47a1", fontSize = 14, fontWeight = "bold",
                    refX = 14, refY = 12, textAnchor = "start", textVerticalAnchor = "top"
                }
            },
            data = WpData(wp, ctx)
        });

        ctx.GroupEventNodes.Add(blkNodes);
        ctx.PendingContainer = savedContainer;
        ctx.LastContainerId = cId;
        return groupH;
    }

    // ---------- 节点 / 边构造辅助 ----------

    private string BuildSrcNode(GraphRow r, double x, double y, double h, GraphBuild ctx,
        List<(string nodeId, List<string> emits, List<string> listens)> blkNodes)
    {
        var id = ctx.Nid(r.SrcKind == "blk" ? "B" : "S");
        var (fill, stroke, text) = r.SrcKind switch
        {
            "blk" => (C_B_FILL, C_B_STROKE, C_B_TEXT),
            "dialog" => (C_DLG_FILL, C_DLG_STROKE, C_DLG_TEXT),
            _ => (C_TAB_FILL, C_TAB_STROKE, C_TAB_TEXT),
        };

        var emits = new List<string>();
        var listens = new List<string>();
        if (r.Blk != null)
        {
            if (!string.IsNullOrEmpty(r.Blk.Events))
                try { emits = JsonSerializer.Deserialize<List<string>>(r.Blk.Events) ?? new(); } catch { }
            if (!string.IsNullOrEmpty(r.Blk.Commands))
                try { listens = JsonSerializer.Deserialize<List<string>>(r.Blk.Commands) ?? new(); } catch { }
            blkNodes.Add((id, emits, listens));
        }

        var label = r.SrcKind == "blk"
            ? BlockIcon(r.Slot) + " " + r.Title + "\n" + r.Sub
            : r.Title + "\n" + r.Sub;

        ctx.Nodes.Add(new
        {
            id, x = x + SRC_X, y, width = SRC_W, height = h, shape = "rect",
            parent = ctx.PendingContainer,
            attrs = new
            {
                body = new
                {
                    fill, stroke, strokeWidth = 2,
                    strokeDasharray = (r.SrcKind == "blk" && !r.Required) ? "6 4" : (string?)null,
                    rx = 6, ry = 6,
                    cursor = r.SrcKind == "blk" ? "pointer" : "default"
                },
                label = new { text = label, fill = text, fontSize = 12 }
            },
            data = new
            {
                kind = r.SrcKind == "blk" ? "blk" : r.SrcKind,
                name = r.Title, code = r.Blk?.Code ?? "", slot = r.Slot,
                rowId = r.Blk?.Id ?? 0,
                modalId = r.Blk != null ? ctx.Modal("dynblock-modal").id : "",
                project = r.Blk != null ? ctx.Modal("dynblock-modal").project : "",
                emits = string.Join(",", emits), listens = string.Join(",", listens)
            }
        });
        return id;
    }

    private string BuildPsNode(int psId, double x, double y, GraphBuild ctx)
    {
        if (!ctx.PsCache.TryGetValue(psId, out var ps))
        {
            ps = _settings.GetById(psId);
            if (ps != null) ctx.PsCache[psId] = ps;
        }
        var id = ctx.Nid("PS");
        if (ps == null)
        {
            ctx.Nodes.Add(new
            {
                id, x, y, width = LEAF_W, height = LEAF_H, shape = "rect", parent = ctx.PendingContainer,
                attrs = new
                {
                    body = new { fill = C_GREY_FILL, stroke = C_GREY_STROKE, strokeWidth = 1.5, rx = 6, ry = 6, cursor = "default" },
                    label = new { text = $"⚠️ PageSetting #{psId}\n配置不存在或已删除", fill = C_GREY_TEXT, fontSize = 12 }
                },
                data = new { kind = "ps", name = "缺失配置", rowId = psId, modalId = "", project = "" }
            });
            return id;
        }

        // 固定视图：手写 Razor 视图（生成视图或 Back 局部视图），非动态控件渲染
        bool fixedView = !string.IsNullOrWhiteSpace(ps.GeneratedViewPath)
                         || (string.Equals(ps.RenderMode, "Back", StringComparison.OrdinalIgnoreCase)
                             && !string.IsNullOrWhiteSpace(ps.PartialPath));
        var viewPath = !string.IsNullOrWhiteSpace(ps.GeneratedViewPath) ? ps.GeneratedViewPath : ps.PartialPath;
        var label = (fixedView ? "🔒 " : "⚙️ ") + ps.Name + "\n#" + ps.Id + " · " + ps.Code
                    + (fixedView ? "\n" + System.IO.Path.GetFileName(viewPath!) : "");

        ctx.Nodes.Add(new
        {
            id, x, y, width = LEAF_W, height = LEAF_H, shape = "rect", parent = ctx.PendingContainer,
            attrs = new
            {
                body = new { fill = C_PS_FILL, stroke = fixedView ? C_FIX_STROKE : C_PS_STROKE, strokeWidth = fixedView ? 2.5 : 1.5, rx = 6, ry = 6, cursor = "pointer" },
                label = new { text = label, fill = fixedView ? C_FIX_TEXT : C_PS_TEXT, fontSize = 12 }
            },
            data = new
            {
                kind = "ps", name = ps.Name, code = ps.Code, rowId = ps.Id,
                modalId = ctx.Modal("pagesetting-modal").id, project = ctx.Modal("pagesetting-modal").project,
                fixedView, fixedViewPath = viewPath ?? ""
            }
        });
        return id;
    }

    private string BuildCustomNode(string url, double x, double y, GraphBuild ctx)
    {
        var id = ctx.Nid("U");
        ctx.Nodes.Add(new
        {
            id, x, y, width = LEAF_W, height = LEAF_H, shape = "rect", parent = ctx.PendingContainer,
            attrs = new
            {
                body = new { fill = C_CUS_FILL, stroke = C_CUS_STROKE, strokeWidth = 1.5, rx = 6, ry = 6, cursor = "default" },
                label = new { text = "🔗 自定义片段\n" + Trunc(url, 40), fill = C_CUS_TEXT, fontSize = 11 }
            },
            data = new { kind = "custom", name = "自定义片段", url, modalId = "", rowId = 0, project = "" }
        });
        return id;
    }

    private string BuildMissingNode(int wpId, double x, double y, GraphBuild ctx)
    {
        var id = ctx.Nid("M");
        ctx.Nodes.Add(new
        {
            id, x, y, width = LEAF_W, height = LEAF_H, shape = "rect", parent = ctx.PendingContainer,
            attrs = new
            {
                body = new { fill = C_GREY_FILL, stroke = C_GREY_STROKE, strokeWidth = 1.5, strokeDasharray = "5 4", rx = 6, ry = 6, cursor = "default" },
                label = new { text = $"⚠️ WebPage #{wpId}\n页面不存在或已删除", fill = C_GREY_TEXT, fontSize = 12 }
            },
            data = new { kind = "custom", name = $"缺失页面 #{wpId}", modalId = "", rowId = 0, project = "" }
        });
        return id;
    }

    private object WpData(DynWebPage wp, GraphBuild ctx) => new
    {
        kind = "wp", name = wp.Name, code = wp.Code, rowId = wp.Id,
        modalId = ctx.Modal("dynwebpage-modal").id, project = ctx.Modal("dynwebpage-modal").project
    };

    private static void AddEdge(GraphBuild ctx, string source, string target, string color, string? dash, double width, string? label)
    {
        ctx.Edges.Add(new
        {
            source, target, router = new { name = "orth" },
            attrs = new
            {
                line = new
                {
                    stroke = color, strokeWidth = width,
                    strokeDasharray = dash,
                    targetMarker = new { name = "block" }
                }
            },
            labels = string.IsNullOrEmpty(label) ? null : new[] { new { attrs = new { text = label, fill = color, fontSize = 11 }, position = 0.5 } }
        });
    }

    private static string BlockIcon(string role) =>
        role == "filter" ? "🔍" : role == "list" ? "📋" : role == "detail" ? "📝" : role == "tree" ? "🌳" : "🧩";

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";

    /// <summary>事件总线边：仅在同一个 WebPage 组内的积木之间连接（query / create,update / saved）。</summary>
    private static void AddEventEdges(GraphBuild ctx)
    {
        foreach (var group in ctx.GroupEventNodes)
        {
            var queryEdges = new HashSet<string>();
            var signalPairs = new Dictionary<string, List<string>>();
            var savedEdges = new HashSet<string>();

            foreach (var (fromId, emits, _) in group)
            {
                foreach (var ev in emits)
                {
                    foreach (var (toId, _, toListens) in group)
                    {
                        if (toId == fromId || !toListens.Contains(ev)) continue;
                        var pair = fromId + "→" + toId;
                        switch (ev)
                        {
                            case "query": queryEdges.Add(pair); break;
                            case "create":
                            case "update":
                                if (!signalPairs.TryGetValue(pair, out var lst)) signalPairs[pair] = lst = new();
                                lst.Add(ev);
                                break;
                            case "saved": savedEdges.Add(pair); break;
                        }
                    }
                }
            }

            foreach (var pair in queryEdges)
            {
                var p = pair.Split('→');
                ctx.Edges.Add(new
                {
                    source = p[0], target = p[1], router = new { name = "orth" },
                    attrs = new { line = new { stroke = "#e6a23c", strokeWidth = 2.5, targetMarker = new { name = "classic" } } },
                    labels = new[] { new { attrs = new { text = "query", fill = "#e6a23c", fontSize = 12, fontWeight = "bold" }, position = 0.5 } }
                });
            }
            foreach (var kv in signalPairs)
            {
                var p = kv.Key.Split('→');
                ctx.Edges.Add(new
                {
                    source = p[0], target = p[1], router = new { name = "orth" },
                    attrs = new { line = new { stroke = "#409eff", strokeWidth = 2, targetMarker = new { name = "classic" } } },
                    labels = new[] { new { attrs = new { text = string.Join("/", kv.Value), fill = "#409eff", fontSize = 12, fontWeight = "bold" }, position = 0.5 } }
                });
            }
            foreach (var pair in savedEdges)
            {
                var p = pair.Split('→');
                ctx.Edges.Add(new
                {
                    source = p[0], target = p[1], router = new { name = "orth" },
                    attrs = new { line = new { stroke = "#f56c6c", strokeWidth = 3, targetMarker = new { name = "classic" } } },
                    labels = new[] { new { attrs = new { text = "saved", fill = "#f56c6c", fontSize = 12, fontWeight = "bold" }, position = 0.5 } }
                });
            }
        }
    }

    private static int? IntOf(JToken? t, string key) =>
        t is JObject o && int.TryParse(o[key]?.ToString(), out var v) ? v : null;

    [HttpGet("/Platform/Page/DbErGraph")]
    public IActionResult DbErGraph()
    {
        ViewData["Title"] = "数据库 ER 关系图 - VueLibV4";
        return View(string.Format(Page, "DbErGraph"));
    }

    /// <summary>Markdown 文档查看器（docs 目录内文档；安全：规范化路径并限制在 docs 内）</summary>
    [HttpGet("/Platform/Page/MdViewer")]
    public IActionResult MdViewer(string path)
    {
        ViewData["Title"] = "文档 - VueLibV4";
        ViewData["ApiBase"] = "/api";
        // 解决方案根 docs 目录（AppContext.BaseDirectory = src/VueLibV4.Web/bin/Debug/net8.0/）
        var docsRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs"));
        if (!Directory.Exists(docsRoot)) return Content("文档目录不存在：" + docsRoot);
        var safe = Path.GetFullPath(Path.Combine(docsRoot, path ?? ""));
        if (!safe.StartsWith(docsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && !safe.Equals(docsRoot, StringComparison.OrdinalIgnoreCase))
            return Content("无效文档路径");
        if (!System.IO.File.Exists(safe)) return Content("文档不存在：" + path);
        ViewBag.MdPath = path;
        ViewBag.MdContent = System.IO.File.ReadAllText(safe, System.Text.Encoding.UTF8);
        return View(string.Format(Page, "MdViewer"));
    }
}
