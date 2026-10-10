using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using VueLibV4.Platform.Services;

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
        ViewData["Title"] = "页面组成图 Demo - VueLibV4";
        var wp = _webPages.GetById(id);
        if (wp == null) return Content($"WebPage {id} 不存在");

        var tpl = wp.TemplateId > 0 ? _templates.GetById(wp.TemplateId) : null;
        var param = string.IsNullOrEmpty(wp.ParamsJson) ? new JObject() : JObject.Parse(wp.ParamsJson);
        var blocksNode = param["blocks"] as JObject ?? new JObject();

        // ===== 仅 V2 架构：模板 ViewPath 必须包含 DynTemplates =====
        bool isV2 = tpl != null && (tpl.ViewPath?.Contains("DynTemplates") ?? false);
        if (!isV2)
        {
            ViewData["ErrorMsg"] = $"WebPage(id={id}) 使用的是 V1 壳模板（{tpl?.Code ?? "无"}），PageGraphDemo 仅支持 V2 架构页面。";
            ViewData["GraphJson"] = "{}";
            ViewData["WpName"] = wp.Name;
            ViewData["IsV2"] = false;
            return View(string.Format(Page, "PageGraphDemo"));
        }

        // ===== 积木装配（规范来源：DynTemplateBlock 模板-槽位关系表，SortNo 排序，不写死）=====
        var allBlocks = _blocks.Query(x => true).ToList();
        var blkById = allBlocks.ToDictionary(b => b.Id);

        // 实例级槽位配置：ParamsJson.blocks[slot].settingId（树等无 PageSetting 的槽位可能没有）
        int SlotSetting(string slot) =>
            blocksNode[slot] is JObject sv && int.TryParse(sv["settingId"]?.ToString(), out var sid2) ? sid2 : 0;

        var blockInfos = new List<(string slot, VueLibV4.Platform.Models.DynBlock blk, int settingId, bool required)>();
        var seenSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (wp.TemplateId != null)
        {
            foreach (var row in _tplBlocks.ListByTemplate(wp.TemplateId.Value))
            {
                seenSlots.Add(row.Slot);
                blkById.TryGetValue(row.BlockId, out var blk0);
                blockInfos.Add((row.Slot, blk0, SlotSetting(row.Slot), row.Required));
            }
        }
        // 并集兜底：实例 ParamsJson.blocks 声明、但模板关系表缺失的槽位（按 role/code 匹配积木）
        foreach (var prop in blocksNode.Properties())
        {
            var slot = prop.Name;
            if (seenSlots.Contains(slot)) continue;
            var extra = allBlocks.FirstOrDefault(b => string.Equals(b.ImplementsRole, slot, StringComparison.OrdinalIgnoreCase))
                        ?? allBlocks.FirstOrDefault(b => string.Equals(b.Code, slot, StringComparison.OrdinalIgnoreCase));
            blockInfos.Add((slot, extra, SlotSetting(slot), false));
        }

        // ===== 各类资源的 modal 编辑页（固定 code → modal id）=====
        // WP modal（code=dynwebpage-modal, id=35）、T modal（code=dyntemplate-modal, id=32）
        // Block modal（code=dynblock-modal, id=33）、PageSetting modal（code=pagesetting-modal, id=27）
        var modalCodes = new[] { "dynwebpage-modal", "dyntemplate-modal", "dynblock-modal", "pagesetting-modal" };
        var modalWps = _webPages.Query(x => modalCodes.Contains(x.Code)).ToList();
        // code → (modal 页面 Id, modal 所属 ProjectId)：点击节点时用 DetailModalV2 片段 + rowId 回填该实体
        var modalMap = modalWps.ToDictionary(
            m => m.Code,
            m => (id: m.Id.ToString(), project: m.ProjectId?.ToString() ?? ""));
        (string id, string project) Modal(string code) =>
            modalMap.TryGetValue(code, out var v) ? v : ("", "");

        var wpM = Modal("dynwebpage-modal");
        var tplM = Modal("dyntemplate-modal");
        var blkM = Modal("dynblock-modal");
        var psM = Modal("pagesetting-modal");

        // ===== 组装 X6 Graph JSON =====
        var nodes = new List<object>();
        var edges = new List<object>();

        // ===== 四类节点配色（视觉区分）：WP 蓝 / Template 绿 / Block 橙 / PageSetting 紫 =====
        string C_WP_FILL = "#e3f2fd", C_WP_STROKE = "#1e88e5", C_WP_TEXT = "#0d47a1";
        string C_T_FILL = "#e8f5e9",  C_T_STROKE = "#43a047",  C_T_TEXT = "#1b5e20";
        string C_B_FILL = "#fff3e0",  C_B_STROKE = "#fb8c00",  C_B_TEXT = "#e65100";
        string C_PS_FILL = "#f4f0fa", C_PS_STROKE = "#9b59b6", C_PS_TEXT = "#5b2c6f";

        // WP 节点
        nodes.Add(new {
            id = "WP", x = 60, y = 200, width = 230, height = 70, shape = "rect",
            attrs = new {
                body = new { fill = C_WP_FILL, stroke = C_WP_STROKE, strokeWidth = 2, rx = 6, ry = 6, cursor = "pointer" },
                label = new { text = $"📄 {wp.Name}\nid={wp.Id} · {wp.Code}", fill = C_WP_TEXT, fontSize = 13 }
            },
            data = new {
                kind = "wp", name = wp.Name, code = wp.Code, rowId = wp.Id,
                modalId = wpM.id, project = wpM.project
            }
        });

        // Template 节点
        if (tpl != null)
        {
            nodes.Add(new {
                id = "T", x = 330, y = 200, width = 230, height = 70, shape = "rect",
                attrs = new {
                    body = new { fill = C_T_FILL, stroke = C_T_STROKE, strokeWidth = 2, rx = 6, ry = 6, cursor = "pointer" },
                    label = new { text = $"🧩 {tpl.Name}\nid={tpl.Id} · {tpl.Code}", fill = C_T_TEXT, fontSize = 13 }
                },
                data = new {
                    kind = "tpl", name = tpl.Name, code = tpl.Code, rowId = tpl.Id,
                    modalId = tplM.id, project = tplM.project
                }
            });
            edges.Add(new {
                source = "WP", target = "T", router = new { name = "orth" },
                attrs = new { line = new { stroke = "#909399", strokeWidth = 1.5, targetMarker = new { name = "block" } } }
            });
        }

        // Block 列 + PageSetting 列
        const int BLOCK_X = 610;
        const int SETTING_X = 880;
        const int Y_GAP = 140;
        const int Y_BASE = 120;

        var blockDict = new Dictionary<string, (List<string> emits, List<string> listens)>();
        int idx = 0;

        foreach (var (slot, blk, settingId, required) in blockInfos)
        {
            var bId = $"B{idx}";
            var y = Y_BASE + idx * Y_GAP;
            idx++;

            string role = blk?.ImplementsRole ?? slot;
            string blkName = blk != null ? blk.Name : $"Block#{slot}";

            var emits = new List<string>();
            var listens = new List<string>();
            if (blk != null && !string.IsNullOrEmpty(blk.Events))
                try { emits = JsonSerializer.Deserialize<List<string>>(blk.Events) ?? new List<string>(); }
                catch { }
            if (blk != null && !string.IsNullOrEmpty(blk.Commands))
                try { listens = JsonSerializer.Deserialize<List<string>>(blk.Commands) ?? new List<string>(); }
                catch { }
            blockDict[bId] = (emits, listens);

            var icon = role == "filter" ? "🔍" : role == "list" ? "📋"
                     : role == "detail" ? "📝" : role == "tree" ? "🌳" : "🧩";
            string blkLabel = blk != null
                ? $"{icon} {blkName}\nid={blk.Id} · {blk.Code} · slot={slot}"
                : $"{icon} {blkName}\nslot={slot}（未注册积木）";

            nodes.Add(new {
                id = bId, x = BLOCK_X, y = y, width = 250, height = 70, shape = "rect",
                attrs = new {
                    // 非必选槽位虚线边框（Required 来自 DynTemplateBlock，数据驱动）
                    body = new { fill = C_B_FILL, stroke = C_B_STROKE, strokeWidth = 2, strokeDasharray = required ? null : "6 4", rx = 6, ry = 6, cursor = "pointer" },
                    label = new { text = blkLabel, fill = C_B_TEXT, fontSize = 13 }
                },
                data = new {
                    kind = "blk", name = blkName, code = blk?.Code ?? "", slot,
                    rowId = blk?.Id ?? 0, modalId = blkM.id, project = blkM.project,
                    emits = string.Join(",", emits), listens = string.Join(",", listens)
                }
            });

            if (tpl != null)
                edges.Add(new {
                    source = "T", target = bId, router = new { name = "orth" },
                    attrs = new { line = new { stroke = "#909399", strokeWidth = 1, targetMarker = new { name = "block" } } }
                });

            if (settingId > 0)
            {
                var ps = _settings.GetById(settingId);
                if (ps != null)
                {
                    var psId = $"PS{idx}";
                    nodes.Add(new {
                        id = psId, x = SETTING_X, y = y, width = 250, height = 70, shape = "rect",
                        attrs = new {
                            body = new { fill = C_PS_FILL, stroke = C_PS_STROKE, strokeWidth = 1.5, rx = 6, ry = 6, cursor = "pointer" },
                            label = new { text = $"⚙️ {ps.Name}\nid={ps.Id} · {ps.Code}", fill = C_PS_TEXT, fontSize = 13 }
                        },
                        data = new {
                            kind = "ps", name = ps.Name, code = ps.Code,
                            rowId = ps.Id, modalId = psM.id, project = psM.project
                        }
                    });
                    edges.Add(new {
                        source = bId, target = psId, router = new { name = "orth" },
                        attrs = new { line = new { stroke = "#9b59b6", strokeWidth = 1.5, strokeDasharray = "5 5", targetMarker = new { name = "block" } } },
                        labels = new[] { new { attrs = new { text = "配置", fill = "#9b59b6", fontSize = 11 }, position = 0.5 } }
                    });
                }
            }
        }

        // ===== 事件总线通信边 =====
        var queryEdges = new HashSet<string>();
        var signalPairs = new Dictionary<string, List<string>>(); // create / update 合并
        var savedEdges = new HashSet<string>();

        foreach (var kv in blockDict)
        {
            var fromId = kv.Key;
            var (emits, listens) = kv.Value;
            foreach (var emitEv in emits)
            {
                foreach (var toKv in blockDict)
                {
                    var toId = toKv.Key;
                    if (toId == fromId) continue;
                    var (_, toListens) = toKv.Value;
                    if (!toListens.Contains(emitEv)) continue;

                    string pair = $"{fromId}→{toId}";
                    switch (emitEv)
                    {
                        case "query": queryEdges.Add(pair); break;
                        case "create":
                        case "update":
                            if (!signalPairs.ContainsKey(pair)) signalPairs[pair] = new List<string>();
                            signalPairs[pair].Add(emitEv);
                            break;
                        case "saved": savedEdges.Add(pair); break;
                    }
                }
            }
        }

        foreach (var pair in queryEdges)
        {
            var p = pair.Split('→');
            edges.Add(new {
                source = p[0], target = p[1], router = new { name = "orth" },
                attrs = new { line = new { stroke = "#e6a23c", strokeWidth = 2.5, targetMarker = new { name = "classic" } } },
                labels = new[] { new { attrs = new { text = "query", fill = "#e6a23c", fontSize = 12, fontWeight = "bold" }, position = 0.5 } }
            });
        }
        foreach (var kv in signalPairs)
        {
            var p = kv.Key.Split('→');
            edges.Add(new {
                source = p[0], target = p[1], router = new { name = "orth" },
                attrs = new { line = new { stroke = "#409eff", strokeWidth = 2, targetMarker = new { name = "classic" } } },
                labels = new[] { new { attrs = new { text = string.Join("/", kv.Value), fill = "#409eff", fontSize = 12, fontWeight = "bold" }, position = 0.5 } }
            });
        }
        foreach (var pair in savedEdges)
        {
            var p = pair.Split('→');
            edges.Add(new {
                source = p[0], target = p[1], router = new { name = "orth" },
                attrs = new { line = new { stroke = "#f56c6c", strokeWidth = 3, targetMarker = new { name = "classic" } } },
                labels = new[] { new { attrs = new { text = "saved", fill = "#f56c6c", fontSize = 12, fontWeight = "bold" }, position = 0.5 } }
            });
        }

        var json = JsonSerializer.Serialize(new { nodes, edges }, new JsonSerializerOptions { WriteIndented = false });

        ViewData["GraphJson"] = json;
        ViewData["WpName"] = wp.Name;
        ViewData["IsV2"] = true;
        return View(string.Format(Page, "PageGraphDemo"));
    }

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
