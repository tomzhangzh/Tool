/* dyn-debug-trace.js V4 调试观测（Notes 日志 / note() 存储 / 参数变更轨迹 / 动作绑定清单）
 * 职责：window.DynDebug.note() 的落地实现、Notes 环形缓冲与渲染、DynParams.onTrace 订阅与轨迹渲染、
 *       页面动作绑定清单（元素 → 祖先链 → actionhelper + 参数 + 注册表）的采集与 HTML 生成。
 * 依赖：dyn-debug.js（K.esc / K.clip / K.safeJson / K.snapshot / K.elDesc / K.shortParamVal）、
 *       K.probe（selectedEl / picking / elVisible / locateEl），K.panel（渲染调度，运行期调用）。
 * 约定：只允许向 K 追加 K.trace，不得整体重新赋值 global.DynDebugKit；本文件不注册 DynKernel 插件。
 */
(function (global) {
'use strict';
var K = global.DynDebugKit;
if (!K) return;

var esc = K.esc, clip = K.clip, safeJson = K.safeJson, snapshot = K.snapshot,
    elDesc = K.elDesc, shortParamVal = K.shortParamVal;
var ACT_EVENTS = ['click', 'dblclick', 'change', 'select'];
var ACT_PIPE_ATTR = { click: 'dyn-click', dblclick: 'dyn-dblclick', change: 'dyn-change', select: 'dyn-select' };

var NOTES_MAX = 200;      // Notes 环形缓冲上限
var TRACE_MAX = 100;      // 参数变更轨迹环形缓冲上限
var notes = [];
var notesSig = '';        // Notes 内容签名：未变则不重绘（保护文本选择）
var traceBuf = [];

/**
 * @description 上报一条调试记录
 * @param {any} msg 文本（null 归一为空串）
 * @param {object} [meta] 附加信息，如 {source,table}
 */
function note(msg, meta) {
    if (!K.enabled) return;
    notes.push({ t: new Date(), msg: String(msg == null ? '' : msg), meta: meta || {} });
    if (notes.length > NOTES_MAX) notes.shift();
    if (K.panel) K.panel.scheduleRender();
}

function clear() {
    notes.length = 0;
    if (K.panel) K.panel.draw();
}

function pad2(n) { return ('0' + n).slice(-2); }
function fmtTraceTime(t) { return pad2(t.getHours()) + ':' + pad2(t.getMinutes()) + ':' + pad2(t.getSeconds()); }

/** Notes 列表渲染（内容签名未变直接跳过） */
function renderNotes(el) {
    var last = notes[notes.length - 1];
    var ns = notes.length + '|' + (last ? last.t.getTime() + '|' + last.msg + '|' + (last.meta.source || '') : '');
    if (ns === notesSig) return;
    notesSig = ns;
    el.innerHTML = '<div class="dyndb-sec-h">Notes（' + notes.length + '）</div>'
        + (notes.length ? notes.slice().reverse().map(function (n) {
            return '<div class="dyndb-note"><span class="dyndb-time">' + fmtTraceTime(n.t) + '</span> '
                + (n.meta.source ? '<span class="dyndb-src">' + esc(n.meta.source) + '</span> ' : '') + esc(n.msg) + '</div>';
        }).join('') : '<div class="dyndb-empty">暂无告警</div>');
}

/** 参数变更轨迹渲染（新→旧，最多 60 条） */
function drawTrace(el) {
    if (!el) return;
    if (!traceBuf.length) {
        el.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">'
            + '暂无轨迹：ctx.set（本层覆盖）/ ctx.commit（共享实体）/ fork 传参 都会记录（最近 100 条，新→旧）</div></div>';
        return;
    }
    var html = '<div class="dyndb-card"><div class="dyndb-card-h"><b>参数变更轨迹</b>'
        + '<span class="dyndb-id">' + traceBuf.length + ' 条</span></div>';
    traceBuf.slice(0, 60).forEach(function (ev) {
        html += '<div class="dyndb-tr dyndb-tr-' + esc(ev.kind) + '"><span class="dyndb-time">' + fmtTraceTime(ev.t) + '</span>'
            + '<span class="dyndb-tag dyndb-act-' + (ev.kind === 'commit' ? 'db' : ev.kind === 'set' ? 'bi' : 'comp') + '">'
            + esc(ev.kind) + '</span><span class="dyndb-tr-ctx">' + esc(ev.ctxId || '') + '</span>'
            + (ev.el ? '<span class="dyndb-id">' + esc(ev.el) + '</span>' : '')
            + '<b class="dyndb-tr-key">' + esc(ev.key || '') + '</b>'
            + '<span class="dyndb-tr-vals"><span class="dyndb-tr-old">' + esc(shortParamVal(ev.old)) + '</span>'
            + ' → <span class="dyndb-val">' + esc(shortParamVal(ev.value)) + '</span></span>'
            + (ev.by ? '<span class="dyndb-pby">by:' + esc(ev.by) + '</span>' : '')
            + (ev.fromCtx ? '<span class="dyndb-id">fork↑' + esc(ev.fromCtx) + '</span>' : '') + '</div>';
    });
    el.innerHTML = html + '</div>';
}

/** 清空轨迹：DynParams 侧缓冲 + 本模块缓冲 + 重绘 */
function clearTrace() {
    try { if (global.DynParams && global.DynParams._clearTrace) global.DynParams._clearTrace(); } catch (e) { }
    traceBuf = [];
    if (K.panel) K.panel.drawTrace();
}

/**
 * @description 初始化轨迹来源：先拉历史环形缓冲，再订阅实时事件
 * 面板挂载时调用一次；onUpdate 由面板按当前 Tab/钉住态决定是否刷新。
 */
function init() {
    try { traceBuf = (global.DynParams && global.DynParams.trace) ? global.DynParams.trace() : []; } catch (e) { traceBuf = []; }
    if (global.DynParams && global.DynParams.onTrace) {
        global.DynParams.onTrace(function (ev) {
            traceBuf.unshift(ev);
            if (traceBuf.length > TRACE_MAX) traceBuf.pop();
            if (K.trace.onUpdate) K.trace.onUpdate();
        });
    }
}

// ============================================================
//  动作绑定清单：元素（含祖先链）→ 动作绑定 → actionhelper + 参数 + 注册表
// ============================================================
function actionMetaOf(name) {
    try { if (global.dyn && typeof global.dyn.getMeta === 'function') return global.dyn.getMeta(name); } catch (e) { }
    return null;
}

/** 单个元素自身的全部动作绑定（唯一协议：管道属性 dyn-click 等 / dyn-init；另有隐藏配置引用） */
function nodeOwnBinds(node) {
    var binds = [];
    ACT_EVENTS.forEach(function (ev) {
        var an = ACT_PIPE_ATTR[ev];
        if (node.hasAttribute && node.hasAttribute(an)) {
            var raw = node.getAttribute(an) || '';
            // 管道解析统一调用内核权威实现 dyn.parsePipe，禁止另抄解析器
            binds.push({ ev: ev, kind: 'pipe', attr: an, raw: raw, steps: global.dyn.parsePipe(raw) });
        }
    });
    if (node.hasAttribute && node.hasAttribute('dyn-init')) {
        var ir = node.getAttribute('dyn-init') || '';
        binds.push({ ev: 'init', kind: 'pipe', attr: 'dyn-init', raw: ir, steps: global.dyn.parsePipe(ir) });
    }
    if (node.hasAttribute && node.hasAttribute('data-dyn-action-ref')) {
        var refId = node.getAttribute('data-dyn-action-ref') || '';
        var cfg = null;
        try { cfg = document.getElementById(refId); } catch (e) { }
        var valid = !!(cfg && cfg.hasAttribute && cfg.hasAttribute('data-dyn-action-cfg'));
        var parsed = null;
        if (valid && !(cfg.__dynObj && typeof cfg.__dynObj === 'object')) {
            try { parsed = JSON.parse((cfg.textContent || '').trim() || 'null'); } catch (e) { parsed = null; }
        }
        binds.push({ ev: 'cfg', kind: 'ref', attr: 'data-dyn-action-ref', raw: refId, cfg: valid ? cfg : null, parsed: parsed });
    }
    return binds;
}

/**
 * 收集元素自身→body 祖先链上的全部动作绑定。
 * 事件委托按 closest 匹配：同一事件只有【最近】元素的绑定生效，祖先同名绑定标"被遮蔽"。
 */
function collectActionBindings(el) {
    var groups = [], effective = {}, node = el, guard = 0;
    while (node && node.nodeType === 1 && guard++ < 40) {
        var binds = nodeOwnBinds(node);
        if (binds.length) {
            binds.forEach(function (b) { if (b.ev !== 'cfg') { b.effective = !effective[b.ev]; effective[b.ev] = 1; } });
            groups.push({ el: node, self: node === el, binds: binds });
        }
        if (node.tagName === 'BODY') break;
        node = node.parentElement;
    }
    return groups;
}

/** 全页扫描：所有自身带动作绑定的元素（调试面板自身除外；隐藏配置块 cfg 不算） */
function scanPageActionEls() {
    var out = [];
    try {
        var all = document.querySelectorAll('*');
        for (var i = 0; i < all.length; i++) {
            var n = all[i];
            if (K.panel && K.panel.contains(n)) continue;
            if (n.hasAttribute && n.hasAttribute('data-dyn-action-cfg')) continue;
            var binds = nodeOwnBinds(n);
            if (binds.length) out.push({ el: n, visible: K.probe.elVisible(n), binds: binds });
        }
    } catch (e) { }
    return out;
}

function actionStepHtml(st) {
    var meta = actionMetaOf(st.action);
    var html = '<div class="dyndb-astep"><span class="dyndb-pcheck">▸</span><b class="dyndb-aname">' + esc(st.action) + '</b>';
    if (!meta) html += ' <span class="dyndb-tag dyndb-bad-tag" title="dyn.resolveAction 找不到该动作">未注册</span>';
    else if (meta.Id) html += ' <span class="dyndb-tag dyndb-act-db">DB·' + esc(String(meta.ActionType || 'script')) + '</span>'
        + '<span class="dyndb-id" title="' + esc(meta.Name || st.action) + '">' + esc(meta.Name || st.action) + '</span>';
    else html += ' <span class="dyndb-tag dyndb-act-bi">内置</span>' + (meta.doc ? '<span class="dyndb-id" title="' + esc(meta.doc) + '">ℹ</span>' : '');
    ['$before', '$onSuccess', '$onFail', '$after'].forEach(function (k) {
        if (st.options && st.options[k] !== undefined) html += ' <span class="dyndb-tag dyndb-act-comp" title="组合动作钩子">' + k + '</span>';
    });
    if (st.options && Object.keys(st.options).length) {
        html += '<pre class="dyndb-json dyndb-ajson" title="点击折叠/展开">' + esc(clip(safeJson(st.options, 1), 900)) + '</pre>';
    }
    return html + '</div>';
}

var pageActEls = [];          // scanPageActionEls 最近一次结果（索引稳定，供定位/展开引用）
var pageActExpand = {};       // 索引 -> 是否展开详情
var pageActFilter = '';

/** 一个绑定的动作名链 chips（未注册动作红名） */
function bindChainHtml(b) {
    if (b.kind === 'ref') {
        if (!b.cfg) return '<span class="dyndb-bad">ref #' + esc(b.raw) + ' 缺失</span>';
        return '<span class="dyndb-tag dyndb-act-comp">ref#' + esc(b.raw) + '</span><span class="dyndb-id">隐藏配置</span>';
    }
    var parts = [];
    (b.steps || []).forEach(function (st, i) {
        if (i) parts.push('<span class="dyndb-chain-arrow">→</span>');
        var meta = actionMetaOf(st.action);
        parts.push('<span class="dyndb-achain-name' + (meta ? '' : ' is-unreg') + '" title="'
            + (meta ? esc(meta.label || st.action) : '动作未注册') + '">' + esc(st.action) + '</span>');
    });
    return parts.join('');
}

function bindDetailHtml(b) {
    var h = '';
    if (b.steps && b.steps.length) b.steps.forEach(function (st) { h += actionStepHtml(st); });
    if (b.kind === 'ref') {
        if (!b.cfg) {
            h += '<div class="dyndb-line dyndb-bad">找不到配置块 #' + esc(b.raw) + '（需带 data-dyn-action-cfg 标记）</div>';
        } else {
            var obj = (b.cfg.__dynObj && typeof b.cfg.__dynObj === 'object') ? b.cfg.__dynObj : b.parsed;
            h += '<div class="dyndb-line dyndb-na">隐藏配置合并进各动作 options，键：'
                + (obj && typeof obj === 'object' ? esc(Object.keys(obj).join(', ')) : '（空）') + '</div>';
            if (obj) h += '<pre class="dyndb-json dyndb-ajson" title="点击折叠/展开">'
                + esc(clip(safeJson(snapshot(obj, 0, null, 4), 1), 1200)) + '</pre>';
        }
    }
    return h;
}

/** 已注册动作清单行（动作注册表过滤输入时复用） */
function registryListHtml(q) {
    var rows = [];
    try { rows = (global.dyn && typeof global.dyn.actionList === 'function') ? global.dyn.actionList() : []; } catch (e) { }
    var list = rows.filter(function (m) {
        return !q || (m.name || '').toLowerCase().indexOf(q) >= 0 || (m.label || '').toLowerCase().indexOf(q) >= 0;
    });
    var lh = '';
    if (!list.length) lh = '<div class="dyndb-empty">无匹配动作</div>';
    list.slice(0, 80).forEach(function (m) {
        lh += '<div class="dyndb-arow"><b>' + esc(m.name) + '</b> '
            + '<span class="dyndb-tag ' + (m.Id ? 'dyndb-act-db' : 'dyndb-act-bi') + '">'
            + (m.Id ? 'DB·' + esc(String(m.ActionType || 'script')) : '内置') + '</span>'
            + (m.label && m.label !== m.name ? '<span class="dyndb-id"> ' + esc(m.label) + '</span>' : '') + '</div>';
    });
    if (list.length > 80) lh += '<div class="dyndb-empty">…另有 ' + (list.length - 80) + ' 条，请继续输入过滤</div>';
    return lh;
}

/** 基于当前 pageActEls/pageActFilter/pageActExpand 生成清单行（全量重绘与输入局部刷新共用） */
function pageListView() {
    var q = pageActFilter.toLowerCase();
    var visibleCount = 0, shown = 0, capped = 0, html = '';
    pageActEls.forEach(function (item) { if (item.visible) visibleCount++; });
    pageActEls.forEach(function (item, idx) {
        if (q) {
            var hit = elDesc(item.el).toLowerCase().indexOf(q) >= 0 || item.binds.some(function (b) {
                if (b.kind === 'ref') return ('ref' + b.raw).toLowerCase().indexOf(q) >= 0;
                return (b.steps || []).some(function (s) { return s.action.toLowerCase().indexOf(q) >= 0; });
            });
            if (!hit) return;
        }
        if (shown >= 100) { capped = 1; return; }
        shown++;
        var expanded = !!pageActExpand[idx];
        html += '<div class="dyndb-prow2' + (item.visible ? '' : ' is-hidden-el') + '" data-act="locate" data-idx="' + idx + '"'
            + (item.visible ? ' title="点击高亮定位该元素"' : ' title="元素当前不可见（弹窗未打开或 display:none）"') + '>'
            + '<span class="dyndb-loc-ico">' + (item.visible ? '🎯' : '🚫') + '</span>'
            + '<span class="dyndb-pel">' + esc(elDesc(item.el)) + '</span>'
            + (item.visible ? '' : '<span class="dyndb-tag dyndb-act-lost">隐藏</span>')
            + '<button class="dyndb-btn dyndb-aexp" data-act="aexpand" data-idx="' + idx + '">'
            + (expanded ? '－' : '＋') + '</button></div>';
        item.binds.forEach(function (b) {
            html += '<div class="dyndb-abind' + (item.visible ? '' : ' is-dim') + '">'
                + '<span class="dyndb-tag dyndb-act-ev">' + esc(b.ev) + '</span>'
                + '<span class="dyndb-id">' + esc(b.kind === 'pipe' ? '管道' : '隐藏配置') + '</span>'
                + bindChainHtml(b) + '</div>';
            if (expanded) html += bindDetailHtml(b);
        });
    });
    if (!shown) html += '<div class="dyndb-empty">' + (pageActEls.length ? '无匹配元素' : '页面上没有动作绑定元素') + '</div>';
    if (capped) html += '<div class="dyndb-empty">…仅显示前 100 个，请输入过滤条件</div>';
    return { html: html, visibleCount: visibleCount };
}

/** 动作 Tab 全部 HTML（面板在动作页激活时取用；regFilter=面板注册表过滤词；拾取中暂停刷新） */
function actionsHtml(regFilter) {
    regFilter = regFilter || '';
    var regRows = [];
    try { regRows = (global.dyn && typeof global.dyn.actionList === 'function') ? global.dyn.actionList() : []; } catch (e) { }
    if (K.probe.picking) return '<div class="dyndb-card"><div class="dyndb-line dyndb-na">🎯 拾取中…清单已暂停刷新</div></div>';
    // —— 1) 全页动作元素清单（每次重扫；弹窗/注入片段出现后点"重扫"） ——
    pageActEls = scanPageActionEls();
    var view = pageListView();
    var html = '<div class="dyndb-card dyndb-insp"><div class="dyndb-card-h"><b>页面动作元素</b>'
        + '<span class="dyndb-id" data-pagecount>' + pageActEls.length + ' 个（可见 ' + view.visibleCount + '）</span>'
        + '<button class="dyndb-btn dyndb-float" data-act="rescan">重扫</button></div>'
        + '<div class="dyndb-line dyndb-na">点击行在页面上高亮定位元素；隐藏元素（弹窗未开/display:none）不可定位，可展开看参数</div>'
        + '<input class="dyndb-ainput" data-act="actpage-filter" placeholder="过滤元素 / 动作名…" value="' + esc(pageActFilter) + '">'
        + '<div data-pagelist>' + view.html + '</div></div>';
    // —— 2) 拾取元素的祖先动作链（辅助；委托遮蔽关系在这里看） ——
    var sel = K.probe.selectedEl;
    if (sel && document.contains(sel)) {
        var groups = collectActionBindings(sel);
        html += '<div class="dyndb-card"><div class="dyndb-card-h"><b>拾取元素动作链</b>'
            + '<span class="dyndb-id">' + esc(elDesc(sel)) + '</span>'
            + '<button class="dyndb-btn dyndb-float" data-act="pick">换一个</button></div>';
        if (!groups.length) html += '<div class="dyndb-line dyndb-na">该元素及其祖先上没有动作绑定</div>';
        groups.forEach(function (g) {
            html += '<div class="dyndb-agroup' + (g.self ? ' is-self' : '') + '"><div class="dyndb-agroup-h">'
                + (g.self ? '<b>本元素</b> ' : '<span class="dyndb-id">祖先 ↑ </span>') + esc(elDesc(g.el)) + '</div>';
            g.binds.forEach(function (b) {
                html += '<div class="dyndb-abind"><span class="dyndb-tag dyndb-act-ev">' + esc(b.ev) + '</span>'
                    + '<span class="dyndb-id">' + esc(b.kind === 'pipe' ? '管道' : '隐藏配置') + '</span>' + bindChainHtml(b);
                if (b.ev !== 'cfg') html += b.effective
                    ? ' <span class="dyndb-tag dyndb-act-eff" title="closest 命中的最近绑定，实际生效">生效</span>'
                    : ' <span class="dyndb-tag dyndb-act-lost" title="后代元素上有同事件的更近绑定，本绑定不会触发">被遮蔽</span>';
                html += '</div>';
                if (b.kind === 'ref') html += bindDetailHtml(b);
                else if (b.steps && b.steps.length) b.steps.forEach(function (st) { html += actionStepHtml(st); });
            });
            html += '</div>';
        });
        html += '</div>';
    }
    // —— 3) 已注册动作注册表浏览 ——
    html += '<div class="dyndb-card"><div class="dyndb-card-h"><b>已注册动作（' + regRows.length + '）</b>'
        + '<span class="dyndb-id">内置 + 数据库动作助手</span></div>'
        + '<input class="dyndb-ainput" data-act="act-filter" placeholder="过滤动作名 / 说明…" value="' + esc(regFilter) + '">'
        + '<div data-actlist>' + registryListHtml(regFilter.toLowerCase()) + '</div></div>';
    return html;
}

/** 清单行的局部刷新（过滤输入时不重建输入框） */
function filterPageList(v) {
    pageActFilter = v || '';
    return pageListView().html;
}

function locateAction(idx) {
    var item = pageActEls[idx];
    if (item) K.probe.locateEl(item.el);
}

function toggleExpand(idx) { pageActExpand[idx] = !pageActExpand[idx]; }
function rescan() { pageActExpand = {}; }

K.trace = {
    note: note,
    clear: clear,
    renderNotes: renderNotes,
    drawTrace: drawTrace,
    clearTrace: clearTrace,
    init: init,
    actionsHtml: actionsHtml,
    collectActionBindings: collectActionBindings,
    registryListHtml: registryListHtml,
    filterPageList: filterPageList,
    locateAction: locateAction,
    toggleExpand: toggleExpand,
    rescan: rescan,
    /** 由面板注入：新轨迹到达时的刷新策略（Tab / 钉住 / 防抖由面板决定） */
    onUpdate: null
};
})(window);
