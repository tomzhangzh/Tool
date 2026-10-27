/* dyn-debug-probe.js V4 调试探针（元素拾取 / Vue 查看卡 / DynParams 参数层 / Blocks 卡片）
 * 职责：inspect($0) 与 🎯 拾取交互、定位高亮、Vue 查看卡 / 参数层卡 / Blocks 参数卡片的 HTML 生成。
 * 依赖：dyn-debug.js（K.describe / K.appDataInfo / K.componentName / K.esc / K.clip /
 *       K.safeJson / K.snapshot / K.elDesc / K.shortParamVal），K.panel（Tab 切换，运行期调用）。
 * 约定：只允许向 K 追加 K.probe，不得整体重新赋值 global.DynDebugKit；本文件不注册 DynKernel 插件。
 */
(function (global) {
'use strict';
var K = global.DynDebugKit;
if (!K) return;

var esc = K.esc, clip = K.clip, safeJson = K.safeJson, snapshot = K.snapshot,
    elDesc = K.elDesc, shortParamVal = K.shortParamVal, describe = K.describe,
    appDataInfo = K.appDataInfo, componentName = K.componentName, findComponentChain = K.findComponentChain;
var WATCH_KEYS = ['table', 'tableName', 'keyField', 'project', 'loadUrl', 'deleteUrl',
    'addUrl', 'editUrl', 'filterInit', 'rowId', 'preFill', 'modalPageId'];

// ---- 拾取状态 ----
var selectedEl = null;   // 最近拾取的元素
var picking = false;     // 是否正在拾取
var hoverEl = null;      // 拾取中悬停元素
var overlayEl = null;    // 悬停高亮框

function fmtVal(v) {
    if (v === undefined) return '<span class="dyndb-na">undefined</span>';
    if (v === null) return '<span class="dyndb-na">null</span>';
    var s;
    try { s = typeof v === 'string' ? v : JSON.stringify(v); } catch (e) { s = String(v); }
    if (s.length > 120) s = s.slice(0, 120) + '…';
    return '<span class="dyndb-val">' + esc(s) + '</span>';
}

function layerClass(layer) {
    if (!layer) return 'dyndb-l0';
    if (layer.indexOf('L0') === 0) return 'dyndb-l0';
    if (layer.indexOf('L1') === 0) return 'dyndb-l1';
    if (layer.indexOf('L2') === 0) return 'dyndb-l2';
    if (layer.indexOf('L3') === 0 || layer.indexOf('L4') === 0) return 'dyndb-l34';
    return 'dyndb-shared';
}

// ============================================================
//  Blocks 增量渲染：卡片只创建一次，轮询只更新变化的参数行/标记，
//  不再整块 innerHTML 重写（避免闪烁、避免打断文本选择）
// ============================================================
var blockCards = new Map();   // blockEl -> {card, rows:Map(key->rowEl), sig, chainTxt}

function blockChainText(P) {
    var chain = [], c = P, guard = 0;
    while (c && guard++ < 20) {
        var tag = c.id;
        var nLocal = Object.keys(c.local || {}).length;
        var nShared = Object.keys(c.shared || {}).length;
        if (nLocal) tag += ' [L1×' + nLocal + ']';
        if (nShared) tag += ' [sh×' + nShared + ']';
        chain.push(tag);
        c = c.parent;
    }
    return chain.join(' → ');
}

function syncBlockCard(el, idx, host) {
    var rec = blockCards.get(el);
    var h = el.__dynBlock;
    var intro = h && h.introspect ? h.introspect() : null;
    var role = el.getAttribute('data-blk-role') || ('block#' + idx);
    var sig = [role, el.id || '', !!(intro && intro.destroyed), !h,
        (intro ? intro.commands : []).join(','), (intro ? intro.events : []).join(',')].join('|');
    if (!rec) {
        var card = document.createElement('div');
        card.className = 'dyndb-card';
        card.innerHTML = '<div class="dyndb-card-h" data-b-h></div><div data-b-static></div>'
            + '<div class="dyndb-params" data-b-params></div><div class="dyndb-line dyndb-chain" data-b-chain></div>';
        host.appendChild(card);
        rec = { card: card, rows: new Map(), sig: null, chainTxt: null };
        blockCards.set(el, rec);
    }
    // 静态部分（role/id/标记/cmds/events）：签名变了才重建
    if (rec.sig !== sig) {
        rec.sig = sig;
        rec.card.querySelector('[data-b-h]').innerHTML = '<b>' + esc(role) + '</b>'
            + (el.id ? ' <span class="dyndb-id">#' + esc(el.id) + '</span>' : '')
            + (intro && intro.destroyed ? ' <span class="dyndb-bad">destroyed</span>' : '')
            + (!h ? ' <span class="dyndb-bad">未挂载 __dynBlock</span>' : '');
        var st = '';
        if (intro) {
            st += '<div class="dyndb-line"><span class="dyndb-k">cmds</span> '
                + (intro.commands.length ? intro.commands.map(esc).join(', ') : '<span class="dyndb-na">（无）</span>') + '</div>';
            if (intro.events.length) st += '<div class="dyndb-line"><span class="dyndb-k">events</span> ' + intro.events.map(esc).join(', ') + '</div>';
        }
        rec.card.querySelector('[data-b-static]').innerHTML = st;
    }
    var paramsHost = rec.card.querySelector('[data-b-params]');
    var chainEl = rec.card.querySelector('[data-b-chain]');
    var seen = {};
    try {
        var P = global.DynParams && DynParams.fromEl(el);
        if (P) {
            var insp = P.inspect(WATCH_KEYS);
            WATCH_KEYS.forEach(function (k) {
                var r = insp[k];
                if (!r || r.value === undefined) return;
                seen[k] = 1;
                var vs;
                try { vs = typeof r.value === 'string' ? r.value : JSON.stringify(r.value); } catch (e) { vs = String(r.value); }
                if (vs == null) vs = 'null';
                if (vs.length > 120) vs = vs.slice(0, 120) + '…';
                var row = rec.rows.get(k);
                if (!row) {
                    row = document.createElement('div');
                    row.className = 'dyndb-line';
                    row.innerHTML = '<span class="dyndb-k"></span> <span class="dyndb-val dyndb-bpv"></span> <span class="dyndb-layer dyndb-bpl"></span>';
                    row.querySelector('.dyndb-k').textContent = k;
                    paramsHost.appendChild(row);
                    rec.rows.set(k, row);
                }
                var vEl = row.querySelector('.dyndb-bpv');
                if (vEl.textContent !== vs) vEl.textContent = vs;
                var lEl = row.querySelector('.dyndb-bpl');
                var lt = r.layer || '?';
                if (lEl.textContent !== lt) lEl.textContent = lt;
                lEl.className = 'dyndb-layer dyndb-bpl ' + layerClass(r.layer);
            });
            var ct = blockChainText(P);
            if (rec.chainTxt !== ct) {
                rec.chainTxt = ct;
                chainEl.innerHTML = '<span class="dyndb-k">ctx链</span> ' + esc(ct);
            }
        }
    } catch (e) { /* 参数读取偶发异常不影响静态部分 */ }
    rec.rows.forEach(function (row, k) { if (!seen[k]) { row.remove(); rec.rows.delete(k); } });
}

/** 渲染 Blocks 卡片区（面板 2s 轮询只更新变化行） */
function renderBlocks(host) {
    var blocks = [];
    try { blocks = [].slice.call(document.querySelectorAll('[data-blk-role]')); } catch (e) { }
    if (!host.querySelector('.dyndb-blocks-host')) host.innerHTML = '<div class="dyndb-sec-h" data-b-count></div><div class="dyndb-blocks-host"></div>';
    host.querySelector('[data-b-count]').textContent = 'Blocks（' + blocks.length + '）';
    var listHost = host.querySelector('.dyndb-blocks-host');
    blockCards.forEach(function (rec, el) {
        if (!document.contains(el) || blocks.indexOf(el) < 0) { rec.card.remove(); blockCards.delete(el); }
    });
    if (blocks.length) {
        var empty = listHost.querySelector('.dyndb-empty');
        if (empty) empty.remove();
        blocks.forEach(function (el, i) {
            syncBlockCard(el, i, listHost);
            var rec = blockCards.get(el);
            var anchor = listHost.children[i] || null;
            if (rec.card !== anchor) listHost.insertBefore(rec.card, anchor);
        });
    } else {
        blockCards.forEach(function (rec) { rec.card.remove(); });
        blockCards.clear();
        if (!listHost.querySelector('.dyndb-empty')) {
            var d = document.createElement('div');
            d.className = 'dyndb-empty';
            d.textContent = '当前页面没有已渲染的 Block';
            listHost.appendChild(d);
        }
    }
}

// ============================================================
//  DynParams 逐层透视：每个参数按 L0→L4 优先级列出【所有】提供层，
//  第一行=获胜值（绿底✓），其余=被谁覆盖（暗灰+来源 ctx/元素）
// ============================================================
function paramsLayersSection(P) {
    if (!global.DynParams || !P) return '';
    var chain = [], cc = P, guard = 0;
    while (cc && guard++ < 30) { chain.push(cc); cc = cc.parent; }
    if (!chain.length) return '';
    // 参数白名单：runtime/local/shared/URL 出现的键才透视（L2 model 的业务字段不刷屏，
    // 但白名单键在 model 层也有值时照常作为候选展示覆盖关系）
    var keySet = {};
    function addKeys(o) { try { Object.keys(o || {}).forEach(function (k) { keySet[k] = 1; }); } catch (e) { } }
    var urlMap = {};
    try { new URLSearchParams(location.search).forEach(function (v, k) { urlMap[k] = v; }); } catch (e) { }
    addKeys(urlMap);
    chain.forEach(function (node) { addKeys(node.runtime); addKeys(node.local); addKeys(node.shared); });
    var keys = Object.keys(keySet);
    if (!keys.length) return '';
    keys.sort(function (a, b) {
        var ia = WATCH_KEYS.indexOf(a), ib = WATCH_KEYS.indexOf(b);
        if (ia >= 0 && ib >= 0) return ia - ib;
        if (ia >= 0) return -1;
        if (ib >= 0) return 1;
        return a < b ? -1 : a > b ? 1 : 0;
    });
    var shown = keys.slice(0, 40);
    function committedBy(k) {
        for (var i = 0; i < chain.length; i++) {
            try { var m = chain[i]._provenance && chain[i]._provenance[k]; if (m && m.by) return m.by; } catch (e) { }
        }
        return null;
    }
    var html = '<div class="dyndb-card dyndb-params2"><div class="dyndb-card-h"><b>DynParams 参数层</b>'
        + '<span class="dyndb-id">ctx ' + esc(P.id) + ' · 链深 ' + chain.length + '</span></div>';
    html += '<div class="dyndb-pllegend" title="数字越小优先级越高；L3 不是独立存储，而是父链 ctx 的各层（候选行标 ↑继承）">'
        + '<span class="dyndb-layer dyndb-l0">L0</span>调用点/运行时<span class="dyndb-layer dyndb-l1">L1</span>容器静态'
        + '<span class="dyndb-layer dyndb-shared">sh</span>共享实体<span class="dyndb-layer dyndb-l2">L2</span>业务Model'
        + '<span class="dyndb-layer dyndb-l34">L3</span>父链继承<span class="dyndb-layer dyndb-l34">L4</span>URL兜底</div>';
    shown.forEach(function (k) {
        var cands = [], sharedSeen = {};
        chain.forEach(function (node) {
            var v, inherited = node !== chain[0];
            try { v = node.runtime[k]; } catch (e) { v = undefined; }
            if (v !== undefined) cands.push({ tag: 'L0', cls: 'dyndb-l0', value: v, who: node.id, whoEl: node.el, inherited: inherited });
            try { v = node.local[k]; } catch (e) { v = undefined; }
            if (v !== undefined) cands.push({ tag: 'L1', cls: 'dyndb-l1', value: v, who: node.id, whoEl: node.el, inherited: inherited });
            // shared 全链同一引用，每键只展示一次（就近节点）
            try { v = node.shared ? node.shared[k] : undefined; } catch (e) { v = undefined; }
            if (v !== undefined && !sharedSeen[k]) {
                sharedSeen[k] = 1;
                cands.push({ tag: 'sh', cls: 'dyndb-shared', value: v, who: node.id, whoEl: node.el, inherited: inherited, by: committedBy(k) });
            }
            var m = node.el && node.el.__dynModel;
            if (m && typeof m === 'object' && !Array.isArray(m)) {
                try { v = m[k]; } catch (e) { v = undefined; }
                if (v !== undefined) cands.push({ tag: 'L2', cls: 'dyndb-l2', value: v, who: node.id, whoEl: node.el, inherited: inherited });
            }
        });
        if (Object.prototype.hasOwnProperty.call(urlMap, k)) cands.push({ tag: 'L4', cls: 'dyndb-l34', value: urlMap[k], who: 'URL query' });
        if (!cands.length) return;
        html += '<div class="dyndb-pkey"><span class="dyndb-pkey-name">' + esc(k) + '</span>';
        cands.forEach(function (cd, i) {
            var isWin = i === 0;   // 候选收集顺序严格复刻 resolveOne
            var whoTxt = cd.who === 'URL query' ? 'URL query'
                : cd.who + (cd.whoEl && cd.whoEl.nodeType === 1 ? ' · ' + elDesc(cd.whoEl) : '');
            html += '<div class="dyndb-prow ' + (isWin ? 'is-win' : 'is-lost') + '">'
                + '<span class="dyndb-pcheck">' + (isWin ? '✓' : '·') + '</span>'
                + (cd.inherited ? '<span class="dyndb-layer dyndb-l34" title="来自父链 ctx（L3）">↑L3</span>' : '')
                + '<span class="dyndb-layer ' + cd.cls + '">' + cd.tag + '</span>'
                + '<span class="dyndb-pval">' + esc(shortParamVal(cd.value)) + '</span>'
                + '<span class="dyndb-pwho">' + esc(whoTxt) + '</span>'
                + (cd.by ? '<span class="dyndb-pby">commit:' + esc(cd.by) + '</span>' : '')
                + (isWin ? '' : '<span class="dyndb-pcovered">被覆盖</span>') + '</div>';
        });
        html += '</div>';
    });
    if (keys.length > shown.length) html += '<div class="dyndb-line dyndb-na">…另有 ' + (keys.length - shown.length)
        + ' 个参数；Console 执行 DynParams.fromEl(__sel).inspect() 看全部</div>';
    html += '</div>';
    return html;
}

/** 选中元素所属参数上下文的逐层透视 HTML（无上下文时给出说明） */
function paramsHtml(el) {
    if (!el) return '<div class="dyndb-card"><div class="dyndb-line dyndb-na">先用 🎯拾取 选择一个元素，再看它的 DynParams 参数层</div></div>';
    try {
        var P = global.DynParams ? DynParams.fromEl(el) : null;
        return P ? paramsLayersSection(P) : '<div class="dyndb-card"><div class="dyndb-line dyndb-na">DynParams 未加载</div></div>';
    } catch (e) {
        return '<div class="dyndb-card"><div class="dyndb-bad">参数层渲染异常: ' + esc(e.message) + '</div></div>';
    }
}

/** Vue 查看卡（App / 组件链 / jsonconfig / 数据快照） */
function inspectorSection(state) {
    if (picking) {
        return '<div class="dyndb-card dyndb-pick"><div class="dyndb-card-h"><b>🎯 拾取中</b></div>'
            + '<div class="dyndb-line">点击页面任意元素查看其所属 VueApp；按 <b>Esc</b> 取消。（业务点击已被拦截）</div></div>';
    }
    if (!selectedEl || !document.contains(selectedEl) || !state) return '';
    var info = state.info;
    if (info && info.err) return '<div class="dyndb-card"><div class="dyndb-bad">describe 异常: ' + esc(info.err) + '</div></div>';
    var comps = state.comps, dynComp = state.dynComp;
    var html = '<div class="dyndb-card dyndb-insp">';
    html += '<div class="dyndb-card-h"><b>Vue 查看</b>'
        + '<span class="dyndb-live-dot" title="实时订阅中：值变化自动刷新">●实时</span>'
        + '<button class="dyndb-btn dyndb-float" data-act="pick">换一个</button></div>';
    html += '<div class="dyndb-line"><span class="dyndb-k">元素</span> ' + esc(elDesc(selectedEl))
        + (info.teleported ? ' <span class="dyndb-tag dyndb-tag-shell">Teleport↗</span>' : '') + '</div>';
    // 向上链（只列带标记的层 + 自身），最近的在最前
    var marked = info.chain.filter(function (c) { return c.self || c.badges.length; });
    if (marked.length) {
        html += '<div class="dyndb-chain-list">';
        marked.slice(0, 15).forEach(function (c) {
            html += '<div class="dyndb-chain-row' + (c.self ? ' is-self' : '') + '"><span class="dyndb-chain-el">' + esc(c.desc) + '</span>'
                + c.badges.map(function (b) {
                    var cls = b.indexOf('VueApp') === 0 ? 'dyndb-tag-app'
                        : b.indexOf('Block') === 0 ? 'dyndb-tag-blk'
                        : b.indexOf('shell') === 0 || b.indexOf('slot') === 0 ? 'dyndb-tag-shell'
                        : 'dyndb-tag-ctx';
                    return ' <span class="dyndb-tag ' + cls + '">' + esc(b) + '</span>';
                }).join('') + '</div>';
        });
        html += '</div>';
    }
    if (info.vueChain.length) html += '<div class="dyndb-line"><span class="dyndb-k">Vue组件</span> '
        + esc(info.vueChain.slice(0, 10).join(' ← ')) + '</div>';
    // 组件链/最近 DynDynamicCom 已在面板 buildInspState 中算好（state.comps/state.dynComp）
    if (comps.length) {
        var names = comps.map(componentName);
        html += '<div class="dyndb-line"><span class="dyndb-k">组件链</span></div><div class="dyndb-comp-chain">';
        names.forEach(function (nm, i2) {
            var cls = nm === 'DynDynamicCom' ? 'dyndb-tag-app' : /^El/.test(nm) ? 'dyndb-tag-el' : 'dyndb-tag-comp';
            html += (i2 ? '<span class="dyndb-chain-arrow">→</span>' : '')
                + '<span class="dyndb-tag ' + cls + '" title="' + esc(nm) + '">' + esc(nm.length > 26 ? nm.slice(0, 25) + '…' : nm) + '</span>';
        });
        html += '</div>';
    }
    // DynParams 逐层透视已移至「DynParams」Tab（本卡聚焦 App/组件/数据）
    // —— DynDynamicCom 的两个关键 props：jsonconfig（组件配置树）/ parentmodelinfo（父模型） ——
    if (dynComp) {
        var props = dynComp.props || {};
        var jc = null, pmi = null;
        try { jc = snapshot(props.jsonconfig, 0, null, 6); } catch (e) { jc = { err: e.message }; }
        try { pmi = snapshot(props.parentmodelinfo, 0, null, 4); } catch (e) { pmi = { err: e.message }; }
        if (jc) {
            var jcComp = jc.component || '(空)', jcModel = jc.modelname || '';
            var jcKeys = jc && typeof jc === 'object' ? Object.keys(jc).slice(0, 12) : [];
            html += '<div class="dyndb-dyncom" data-flash="jc"><div class="dyndb-dyncom-h">jsonconfig'
                + ' <span class="dyndb-dyncom-name">' + esc(String(jcComp)) + '</span>'
                + (jcModel ? ' <span class="dyndb-id">model=' + esc(jcModel) + '</span>' : '') + '</div>';
            html += '<div class="dyndb-line dyndb-na">键: ' + esc(jcKeys.join(', ')) + '</div>';
            html += '<pre class="dyndb-json" title="点击折叠/展开">' + esc(clip(safeJson(jc, 1), 3000)) + '</pre></div>';
        }
        if (pmi !== null && typeof pmi !== 'undefined') {
            html += '<div class="dyndb-dyncom" data-flash="pmi"><div class="dyndb-dyncom-h">parentmodelinfo</div>'
                + '<pre class="dyndb-json" title="点击折叠/展开">' + esc(clip(safeJson(pmi, 1), 1800)) + '</pre></div>';
        }
    }
    if (info.ownerApp) {
        if (!info.ownerProxy) {
            html += '<div class="dyndb-line dyndb-bad">找到 __dynApp 但取不到根代理（Vue 版本差异？app._instance 缺失）</div></div>';
            return html;
        }
        var ai = appDataInfo(info.ownerProxy, info.ownerRoot);
        html += '<div class="dyndb-line"><span class="dyndb-k">所属App</span> <b class="dyndb-appname">' + esc(ai.name) + '</b>'
            + ' <span class="dyndb-id">' + esc(elDesc(info.ownerRoot)) + '</span></div>';
        if (ai.computed.length) html += '<div class="dyndb-line"><span class="dyndb-k">computed</span> ' + esc(ai.computed.join(', ')) + '</div>';
        if (ai.methods.length) html += '<div class="dyndb-line"><span class="dyndb-k">methods</span> <span class="dyndb-na">'
            + esc(ai.methods.slice(0, 24).join(', ') + (ai.methods.length > 24 ? '…' : '')) + '</span></div>';
        if (ai.err) {
            html += '<div class="dyndb-line dyndb-bad">data 读取失败: ' + esc(ai.err) + '</div>';
        } else if (ai.dataKeys.length) {
            html += '<div class="dyndb-params">';
            ai.dataKeys.slice(0, 30).forEach(function (k) {
                var v; try { v = ai.dataSnap[k]; } catch (e) { v = '[err]'; }
                var s;
                try { s = typeof v === 'string' ? v : JSON.stringify(v); } catch (e) { s = String(v); }
                if (s && s.length > 160) s = s.slice(0, 160) + '…';
                html += '<div class="dyndb-line" data-dk="' + esc(k) + '"><span class="dyndb-k">' + esc(k) + '</span> '
                    + '<span class="dyndb-val">' + esc(s == null ? 'null' : s) + '</span></div>';
            });
            html += '</div>';
        }
        html += '<div class="dyndb-line dyndb-console-hint">Console：<b>__selApp</b>=app代理 <b>__selComp</b>=最近DynDynamicCom（__selComp.jsonconfig 取完整配置） <b>__sel</b>=元素</div>';
    } else {
        html += '<div class="dyndb-line dyndb-bad">该元素向上没有找到 __dynApp（不属于任何 Dyn VueApp，或在 app 挂载完成前拾取）</div>';
    }
    html += '</div>';
    return html;
}

// ---- 拾取交互 ----

function ensureOverlay() {
    if (overlayEl) return overlayEl;
    overlayEl = document.createElement('div');
    overlayEl.className = 'dyndb-pick-overlay';
    overlayEl.style.display = 'none';
    document.body.appendChild(overlayEl);
    return overlayEl;
}

function moveOverlay(toEl) {
    var ov = ensureOverlay();
    if (!toEl || !toEl.getBoundingClientRect) { ov.style.display = 'none'; return; }
    var r = toEl.getBoundingClientRect();
    ov.style.display = 'block';
    ov.style.left = (r.left + global.scrollX) + 'px';
    ov.style.top = (r.top + global.scrollY) + 'px';
    ov.style.width = r.width + 'px';
    ov.style.height = r.height + 'px';
}

/** 元素当前是否可见（有尺寸且未被 display:none/visibility:hidden；不含视口判断） */
function elVisible(el) {
    try {
        if (!el || el.nodeType !== 1) return false;
        var r = el.getBoundingClientRect();
        if (r.width === 0 && r.height === 0) return false;
        var st = global.getComputedStyle ? getComputedStyle(el) : null;
        if (st && (st.display === 'none' || st.visibility === 'hidden')) return false;
        return true;
    } catch (e) { return false; }
}

var locateTimer = null;
/** 在页面上高亮定位元素：滚动到视口中央 + 黄色遮罩脉冲约 1.6s */
function locateEl(el) {
    if (!el || !elVisible(el)) return false;
    try { el.scrollIntoView({ block: 'center', behavior: 'auto' }); } catch (e) { }
    setTimeout(function () {
        var ov = ensureOverlay();
        ov.classList.add('dyndb-locate');
        moveOverlay(el);
        clearTimeout(locateTimer);
        locateTimer = setTimeout(function () {
            ov.style.display = 'none';
            ov.classList.remove('dyndb-locate');
        }, 1700);
    }, 50);
    return true;
}

function startPicking() {
    if (picking || !document.body) return;
    if (K.panel && K.panel.isPinned()) K.panel.setPinned(false);   // 重新拾取意味着要看新快照
    picking = true;
    hoverEl = null;
    if (K.panel) {
        K.panel.markPicking(true);
        K.panel.switchTab('view');   // 拾取提示卡在查看 Tab
    }
    document.addEventListener('mousemove', onPickMove, true);
    document.addEventListener('click', onPickClick, true);
    document.addEventListener('keydown', onPickKey, true);
    if (K.panel) K.panel.drawInspector(true);
}

function stopPicking() {
    if (!picking) return;
    picking = false; hoverEl = null;
    if (overlayEl) overlayEl.style.display = 'none';
    if (K.panel) K.panel.markPicking(false);
    document.removeEventListener('mousemove', onPickMove, true);
    document.removeEventListener('click', onPickClick, true);
    document.removeEventListener('keydown', onPickKey, true);
    if (K.panel) K.panel.drawInspector(true);
}

function onPickMove(e) {
    var t = e.target;
    if (t === overlayEl) return;
    if (K.panel && K.panel.contains(t)) { hoverEl = null; moveOverlay(null); return; }
    if (t && t.nodeType === 1) { hoverEl = t; moveOverlay(t); }
}

function onPickClick(e) {
    var t = e.target;
    if (K.panel && K.panel.contains(t)) return; // 面板内按钮走正常逻辑
    e.preventDefault();
    e.stopPropagation();
    if (e.stopImmediatePropagation) e.stopImmediatePropagation();
    var picked = hoverEl || (t && t.nodeType === 1 ? t : null);
    stopPicking();
    if (picked) {
        selectedEl = picked;
        global.__sel = picked;
        var info = describe(picked);
        global.__selApp = info ? (info.ownerProxy || info.ownerApp) : null;
        global.__selAppRaw = info ? info.ownerApp : null;
        if (K.panel) K.panel.drawInspector(true);
    }
}

function onPickKey(e) {
    if (e.key === 'Escape') { e.preventDefault(); stopPicking(); }
}

/** 直接查看指定元素：返回 {el, ownerApp, chain, vueChain} 描述对象（控制台友好） */
function inspect(el) {
    var info = describe(el);
    if (info) {
        selectedEl = el || null;
        global.__sel = selectedEl;
        global.__selApp = info.ownerProxy || info.ownerApp; // 根代理：可直接读写 data/调方法
        global.__selAppRaw = info.ownerApp;                 // Vue 应用实例（unmount/config 等）
        if (K.panel) K.panel.drawInspector(true);
    }
    return info;
}

K.probe = {
    describe: describe, elDesc: elDesc, snapshot: snapshot, appDataInfo: appDataInfo,
    componentName: componentName, findComponentChain: findComponentChain,
    inspectorHtml: inspectorSection, paramsHtml: paramsHtml, renderBlocks: renderBlocks,
    elVisible: elVisible, locateEl: locateEl, inspect: inspect,
    startPicking: startPicking, stopPicking: stopPicking,
    get selectedEl() { return selectedEl; },
    get picking() { return picking; }
};
})(window);
