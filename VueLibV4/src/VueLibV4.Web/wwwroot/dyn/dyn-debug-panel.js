/* dyn-debug-panel.js V4 调试浮层（面板 DOM / Tab / 按钮 / 实时订阅 / 样式）
 * 职责：右下角浮层外壳与 Tab 切换、Vue 查看数据的实时订阅与增量重绘、
 *       钉住/复制上下文、位置尺寸记忆与样式注入、2s 轮询与引导挂载。
 * 依赖：dyn-debug.js（K.esc / K.clip / K.safeJson / K.snapshot / K.elDesc）、
 *       K.probe（describe / inspectorHtml / paramsHtml / renderBlocks / componentName / appDataInfo）、
 *       K.trace（renderNotes / drawTrace / init / actionsHtml / 动作清单交互）。
 * 约定：只允许向 K 追加 K.panel，不得整体重新赋值 global.DynDebugKit；本文件不注册 DynKernel 插件。
 */
(function (global) {
'use strict';
var K = global.DynDebugKit;
if (!K) return;

var esc = K.esc, clip = K.clip, safeJson = K.safeJson, snapshot = K.snapshot, elDesc = K.elDesc;
var panelEl = null, bodyEl = null, collapsed = false, timer = null, renderQueued = false;
var inspEl = null, blocksEl = null, notesEl = null, paramsEl = null, liveBarEl = null, traceEl = null, actionsEl = null;
var activeTab = 'view', activePsub = 'layers';
// inspState：结构性缓存（describe DOM 链 + vnode 组件树遍历，很贵），只在换选/强制刷新时重算；
// 数据实时性不靠轮询——buildInspState 时对 dynComp 的 parentmodelinfo/jsonconfig 及 app $data
// 注册 $watch，值一变立即重绘（editForm 换 form 引用、表单逐字段修改都能捕获）。
var inspState = null, liveUnwatch = [], liveStale = false, pendingFlash = null, pinned = false;
var actFilter = '', dragState = null, traceQueued = false;
var UI_KEY = 'DYN_DEBUG_UI';

function teardownLive() {
    liveUnwatch.forEach(function (off) { try { off(); } catch (e) { } });
    liveUnwatch = [];
}

function selectionInPanel() {
    var s = document.getSelection();
    return !!(s && !s.isCollapsed && s.anchorNode && panelEl && panelEl.contains(s.anchorNode));
}

function setLiveStale(v) {
    liveStale = v;
    if (liveBarEl) liveBarEl.style.display = v ? 'block' : 'none';
}

/** 顶层键 → 受限深度 JSON 签名（用于识别"哪个字段变了"，深层嵌套改动也能浅层捕获） */
function shallowSig(obj) {
    var m = {};
    try {
        Object.keys(obj || {}).forEach(function (k) {
            try { m[k] = safeJson(snapshot(obj[k], 0, null, 2)); } catch (e) { m[k] = '?'; }
        });
    } catch (e) { }
    return m;
}

function sigDiff(base, now) {
    var ks = {}, out = [];
    try { Object.keys(base || {}).forEach(function (k) { ks[k] = 1; }); } catch (e) { }
    try { Object.keys(now || {}).forEach(function (k) { ks[k] = 1; }); } catch (e) { }
    Object.keys(ks).forEach(function (k) { if (base[k] !== now[k]) out.push(k); });
    return out;
}

function addFlash(part, keys) {
    if (pinned || !keys || !keys.length) return;
    if (!pendingFlash) pendingFlash = { data: [], jc: [], pmi: [] };
    keys.forEach(function (k) {
        if (pendingFlash[part].length < 12 && pendingFlash[part].indexOf(k) < 0) pendingFlash[part].push(k);
    });
}

/** 数据被 watch 回调触发：没在选文本就立即重绘；正在选则挂"有更新"提示，选择结束自动刷 */
function onLiveDataChange() {
    if (pinned) return;
    if (selectionInPanel()) { setLiveStale(true); return; }
    renderInspHtml();
}

/** 订阅选中组件/所属 App 的响应式变化（只在状态构建时注册一次，随换选/卸载注销） */
function subscribeLive(state) {
    teardownLive();
    state._base = { jc: null, pmi: null, data: null };
    try {
        var comp = state.dynComp && state.dynComp.proxy;
        if (comp && typeof comp.$watch === 'function') {
            liveUnwatch.push(comp.$watch(function () { return comp.props.jsonconfig; }, function () {
                addFlash('jc', sigDiff(state._base.jc || {}, shallowSig(comp.props.jsonconfig)));
                onLiveDataChange();
            }, { deep: true }));
            liveUnwatch.push(comp.$watch(function () { return comp.props.parentmodelinfo; }, function () {
                addFlash('pmi', sigDiff(state._base.pmi || {}, shallowSig(comp.props.parentmodelinfo)));
                onLiveDataChange();
            }, { deep: true }));
            state._base.jc = shallowSig(comp.props.jsonconfig);
            state._base.pmi = shallowSig(comp.props.parentmodelinfo);
        }
        var root = state.info.ownerProxy;
        if (root && typeof root.$watch === 'function') {
            liveUnwatch.push(root.$watch(function () { return root.$data; }, function () {
                addFlash('data', sigDiff(state._base.data || {}, shallowSig(root.$data)));
                onLiveDataChange();
            }, { deep: true }));
            state._base.data = shallowSig(root.$data);
        }
    } catch (e) { /* 个别代理 $watch 不可用：降级为手动刷新 */ }
}

function buildInspState(force) {
    if (inspState && inspState.el === K.probe.selectedEl && !force) return inspState;
    teardownLive();
    inspState = null;
    if (!K.probe.selectedEl) return null;
    var info;
    try { info = K.probe.describe(K.probe.selectedEl); } catch (e) { info = { err: e.message, chain: [], vueChain: [] }; }
    if (!info) return null;
    var comps = info.ownerProxy ? K.probe.findComponentChain(info.ownerProxy, K.probe.selectedEl) : [];
    var dynComps = comps.filter(function (i) { return K.probe.componentName(i) === 'DynDynamicCom'; });
    var dynComp = dynComps.length ? dynComps[dynComps.length - 1] : null;
    // 供 Console 使用：最近 DynDynamicCom 代理 + 组件链代理
    global.__selComp = dynComp && dynComp.proxy ? dynComp.proxy : null;
    global.__selComps = comps.map(function (i) { return i.proxy; }).filter(Boolean);
    inspState = { el: K.probe.selectedEl, info: info, comps: comps, dynComp: dynComp };
    subscribeLive(inspState);
    return inspState;
}

function flashNode(n) {
    n.classList.remove('dyndb-flash');
    void n.offsetWidth;
    n.classList.add('dyndb-flash');
    setTimeout(function () { n.classList.remove('dyndb-flash'); }, 1500);
}

/** 渲染后把 pendingFlash 应用为行闪烁/字段变更芯片，然后以本次内容重建 diff 基线 */
function applyFlashAndRebase() {
    if (pendingFlash) {
        var f = pendingFlash;
        try {
            [].slice.call(inspEl.querySelectorAll('[data-dk]')).forEach(function (row) {
                if (f.data.indexOf(row.getAttribute('data-dk')) >= 0) flashNode(row);
            });
            [['jc', 'jsonconfig'], ['pmi', 'parentmodelinfo']].forEach(function (pair) {
                var keys = f[pair[0]];
                if (!keys.length) return;
                var box = inspEl.querySelector('[data-flash="' + pair[0] + '"]');
                if (!box) return;
                flashNode(box);
                var chip = document.createElement('div');
                chip.className = 'dyndb-flash-chip';
                chip.textContent = '⚡ 变更字段: ' + keys.slice(0, 8).join(', ') + (keys.length > 8 ? ' …' : '');
                box.insertBefore(chip, box.firstChild);
            });
        } catch (e) { }
        pendingFlash = null;
    }
    try {
        if (inspState) {
            var comp = inspState.dynComp && inspState.dynComp.proxy;
            if (comp) {
                inspState._base.jc = shallowSig(comp.props.jsonconfig);
                inspState._base.pmi = shallowSig(comp.props.parentmodelinfo);
            }
            if (inspState.info && inspState.info.ownerProxy) inspState._base.data = shallowSig(inspState.info.ownerProxy.$data);
        }
    } catch (e) { }
}

function renderInspHtml() {
    if (!inspEl || pinned) return;
    setLiveStale(false);
    inspEl.innerHTML = K.probe.inspectorHtml(inspState);
    applyFlashAndRebase();
    drawParams();   // 参数层 Tab 与查看卡共享同一份选中态，顺带刷新
    drawActions();  // 动作 Tab 同理
}

/** DynParams Tab：渲染选中元素所属参数上下文的逐层透视（无选中时给引导） */
function drawParams() {
    if (!paramsEl) return;
    if (K.probe.picking) {
        paramsEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">🎯 拾取中…点击页面元素后，这里显示它的 L0–L4 参数层</div></div>';
    } else if (!K.probe.selectedEl || !document.contains(K.probe.selectedEl)) {
        paramsEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">先用 🎯拾取 选择一个元素，再看它的 DynParams 参数层</div></div>';
    } else {
        paramsEl.innerHTML = K.probe.paramsHtml(K.probe.selectedEl);
    }
}

/** 动作 Tab：内容全部由轨迹模块生成（清单 + 祖先链 + 注册表） */
function drawActions() {
    if (!actionsEl || activeTab !== 'actions') return;
    actionsEl.innerHTML = K.trace.actionsHtml(actFilter);
}

function drawTrace() { if (traceEl) K.trace.drawTrace(traceEl); }

function switchTab(tab) {
    if (!panelEl || ['view', 'params', 'actions'].indexOf(tab) < 0) return;
    activeTab = tab;
    [].slice.call(panelEl.querySelectorAll('.dyndb-tab')).forEach(function (b) {
        b.classList.toggle('is-active', b.getAttribute('data-tab') === tab);
    });
    [].slice.call(panelEl.querySelectorAll('.dyndb-pane')).forEach(function (p) {
        p.style.display = p.getAttribute('data-pane') === tab ? '' : 'none';
    });
    if (pinned) return;
    if (tab === 'params') { if (activePsub === 'trace') drawTrace(); else drawParams(); }
    if (tab === 'actions') drawActions();
}

function switchPsub(name) {
    if (!panelEl || (name !== 'layers' && name !== 'trace')) return;
    activePsub = name;
    [].slice.call(panelEl.querySelectorAll('.dyndb-psub')).forEach(function (b) {
        b.classList.toggle('is-active', b.getAttribute('data-psub') === name);
    });
    [].slice.call(panelEl.querySelectorAll('.dyndb-psub-pane')).forEach(function (p) {
        p.style.display = p.getAttribute('data-psub-pane') === name ? '' : 'none';
    });
    if (pinned) return;
    if (name === 'trace') drawTrace(); else drawParams();
}

function drawInspector(force) {
    if (!inspEl || pinned) return;
    if (K.probe.picking || !K.probe.selectedEl || !document.contains(K.probe.selectedEl)) {
        teardownLive();
        inspState = null;
        global.__selComp = null; global.__selComps = [];
        setLiveStale(false);
        inspEl.innerHTML = K.probe.inspectorHtml(null);
        drawParams();
        drawActions();
        return;
    }
    buildInspState(force);
    renderInspHtml();
}

/** 2s 轮询对 inspector 只做存在性兜底（元素被移除/弹窗关闭），数据变化由 $watch 负责 */
function tickInspector() {
    if (!inspEl || K.probe.picking || pinned) return;
    if (!K.probe.selectedEl || !document.contains(K.probe.selectedEl)) drawInspector(false);
}

function drawBlocksNotes() {
    if (!blocksEl || !notesEl || pinned) return;
    K.probe.renderBlocks(blocksEl);
    K.trace.renderNotes(notesEl);
}

/** 手动刷新：inspector 强制重算（app 后挂载/状态变化后）+ Blocks/Notes */
function draw() {
    if (pinned) return;
    drawInspector(true);
    drawBlocksNotes();
}

/** 告警只影响 Notes 区，不干扰 inspector（避免打断 JSON 文本选择/避免重复深快照） */
function scheduleRender() {
    if (!panelEl || renderQueued) return;
    renderQueued = true;
    setTimeout(function () { renderQueued = false; drawBlocksNotes(); }, 200);
}

// ---------------- 钉住快照 / 复制上下文 ----------------
function setPinned(v) {
    pinned = v;
    if (panelEl) panelEl.classList.toggle('dyndb-pinned', v);
    var b = panelEl && panelEl.querySelector('[data-act="pin"]');
    if (b) {
        b.textContent = v ? '🔓' : '📌';
        b.classList.toggle('is-on', v);
        b.title = v ? '解除钉住，恢复实时刷新' : '钉住当前快照（暂停自动刷新）';
    }
    if (v) {
        // 钉住瞬间把其他 Tab 也渲染成当前快照，之后全部冻结
        setLiveStale(false);
        drawParams(); drawActions();
        if (activePsub === 'trace') drawTrace();
    } else {
        draw();
    }
}

function fallbackCopy(t) {
    try {
        var ta = document.createElement('textarea');
        ta.value = t;
        ta.style.cssText = 'position:fixed;opacity:0;top:0;left:0';
        document.body.appendChild(ta);
        ta.select();
        var ok = document.execCommand('copy');
        ta.remove();
        return ok;
    } catch (e) { return false; }
}

function copyText(t) {
    if (navigator.clipboard && navigator.clipboard.writeText)
        return navigator.clipboard.writeText(t).then(function () { return true; }, function () { return fallbackCopy(t); });
    return Promise.resolve(fallbackCopy(t));
}

function flashHeadBtn(act, txt) {
    var b = panelEl && panelEl.querySelector('[data-act="' + act + '"]');
    if (!b) return;
    var old = b.textContent;
    b.textContent = txt;
    setTimeout(function () { b.textContent = old; }, 1200);
}

function buildContextPayload() {
    var payload = { time: new Date().toISOString(), url: global.location.href };
    var sel = K.probe.selectedEl;
    if (!sel) { payload.note = '未拾取元素'; return payload; }
    payload.element = elDesc(sel);
    if (inspState) {
        var info = inspState.info;
        payload.teleported = !!info.teleported;
        payload.domChain = info.chain.filter(function (c) { return c.self || c.badges.length; })
            .slice(0, 15).map(function (c) { return { el: c.desc, self: c.self, badges: c.badges }; });
        payload.componentChain = inspState.comps.map(K.probe.componentName);
        var dc = inspState.dynComp;
        if (dc) {
            var props = dc.props || {};
            try { payload.jsonconfig = snapshot(props.jsonconfig, 0, null, 8); } catch (e) { }
            try { payload.parentmodelinfo = snapshot(props.parentmodelinfo, 0, null, 6); } catch (e) { }
        }
        if (info.ownerProxy) {
            payload.app = {
                name: K.probe.appDataInfo(info.ownerProxy, info.ownerRoot).name,
                data: snapshot(info.ownerProxy.$data || {}, 0, null, 5)
            };
        }
    }
    try {
        var P = global.DynParams && DynParams.fromEl(sel);
        if (P) payload.params = P.inspect();
    } catch (e) { }
    try {
        payload.actions = K.trace.collectActionBindings(sel).map(function (g) {
            return {
                el: elDesc(g.el), self: g.self,
                binds: g.binds.map(function (b) {
                    return {
                        event: b.ev, kind: b.kind, attr: b.attr, effective: !!b.effective,
                        steps: (b.steps || []).map(function (s) { return { action: s.action, options: s.options }; }),
                        refTarget: b.kind === 'ref' ? b.raw : undefined,
                        refResolved: b.kind === 'ref' ? !!b.cfg : undefined
                    };
                })
            };
        });
    } catch (e) { }
    return payload;
}

function copyContext() {
    copyText(safeJson(buildContextPayload(), 2)).then(function (ok) { flashHeadBtn('copy', ok ? '已复制✓' : '复制失败'); });
}

// ---------------- 面板 UI：位置/尺寸记忆、Alt 切 Tab、JSON 折叠 ----------------
function loadUi() {
    try { return JSON.parse(localStorage.getItem(UI_KEY) || '{}') || {}; } catch (e) { return {}; }
}
function saveUi(patch) {
    try { localStorage.setItem(UI_KEY, JSON.stringify(Object.assign(loadUi(), patch))); } catch (e) { }
}

function initPanelUi() {
    var ui = loadUi();
    if (ui.width) panelEl.style.width = ui.width + 'px';
    if (ui.height) panelEl.style.height = ui.height + 'px';
    if (ui.left != null && ui.top != null) {
        panelEl.style.left = ui.left + 'px'; panelEl.style.top = ui.top + 'px';
        panelEl.style.right = 'auto'; panelEl.style.bottom = 'auto';
    }
    // 头部拖拽移动（点按钮不拖）
    var head = panelEl.querySelector('.dyndb-head');
    head.addEventListener('mousedown', function (e) {
        if (e.target.closest && e.target.closest('button')) return;
        if (e.button !== 0) return;
        e.preventDefault();
        var r = panelEl.getBoundingClientRect();
        dragState = { dx: e.clientX - r.left, dy: e.clientY - r.top };
        function mv(ev) {
            if (!dragState) return;
            var l = Math.min(Math.max(ev.clientX - dragState.dx, 0), window.innerWidth - 120);
            var t = Math.min(Math.max(ev.clientY - dragState.dy, 0), window.innerHeight - 40);
            panelEl.style.right = 'auto'; panelEl.style.bottom = 'auto';
            panelEl.style.left = l + 'px'; panelEl.style.top = t + 'px';
        }
        function up() {
            document.removeEventListener('mousemove', mv);
            document.removeEventListener('mouseup', up);
            if (dragState) {
                dragState = null;
                var r2 = panelEl.getBoundingClientRect();
                saveUi({ left: Math.round(r2.left), top: Math.round(r2.top) });
            }
        }
        document.addEventListener('mousemove', mv);
        document.addEventListener('mouseup', up);
    });
    // 尺寸变化（CSS resize 手柄）防抖持久化；一旦显式拖高就解除 max-height 限制
    if (global.ResizeObserver) {
        var rzT = null;
        new ResizeObserver(function () {
            clearTimeout(rzT);
            rzT = setTimeout(function () {
                if (panelEl.style.height) panelEl.style.maxHeight = 'none';
                var r = panelEl.getBoundingClientRect();
                saveUi({ width: Math.round(r.width), height: Math.round(r.height) });
            }, 300);
        }).observe(panelEl);
    }
    // Alt+1/2/3 切换三个 Tab
    document.addEventListener('keydown', function (e) {
        if (!e.altKey || K.probe.picking) return;
        var t = { '1': 'view', '2': 'params', '3': 'actions' }[e.key];
        if (t) { e.preventDefault(); switchTab(t); }
    });
    // JSON 块点击折叠/展开（正在拖选文本时不触发，避免干扰复制）
    panelEl.addEventListener('click', function (e) {
        var pre = e.target && e.target.closest ? e.target.closest('pre.dyndb-json') : null;
        if (!pre || !panelEl.contains(pre)) return;
        var s = document.getSelection();
        if (s && !s.isCollapsed) return;
        pre.classList.toggle('is-collapsed');
    });
}

function injectCss() {
    if (document.getElementById('dyndb-style')) return;
    var css = ''
        + '.dyndb-panel{position:fixed;right:10px;bottom:10px;width:420px;max-height:60vh;z-index:2147483600;font:12px/1.5 Consolas,Menlo,monospace;background:#1e1e1e;color:#d4d4d4;border:1px solid #454545;border-radius:8px;box-shadow:0 6px 24px rgba(0,0,0,.45);display:flex;flex-direction:column;overflow:hidden}'
        + '.dyndb-head{display:flex;align-items:center;gap:8px;padding:6px 10px;background:#2d2d30;cursor:pointer;user-select:none}'
        + '.dyndb-head b{color:#4ec9b0;letter-spacing:.5px}.dyndb-head .dyndb-spacer{flex:1}'
        + '.dyndb-btn{background:#3c3c3c;color:#ddd;border:1px solid #555;border-radius:4px;padding:1px 8px;cursor:pointer;font:12px Consolas}'
        + '.dyndb-btn:hover{background:#4a4a4a}.dyndb-body{overflow:auto;padding:8px 10px}.dyndb-panel.dyndb-collapsed .dyndb-body{display:none}'
        + '.dyndb-tabbar{display:flex;gap:2px;padding:4px 6px 0;background:#252526;border-bottom:1px solid #3a3a3a}'
        + '.dyndb-panel.dyndb-collapsed .dyndb-tabbar{display:none}'
        + '.dyndb-tab{background:transparent;color:#9a9a9a;border:1px solid transparent;border-bottom:none;border-radius:5px 5px 0 0;padding:3px 12px;cursor:pointer;font:12px Consolas}'
        + '.dyndb-tab:hover{color:#ddd;background:#2f2f33}.dyndb-tab.is-active{color:#4ec9b0;background:#1e1e1e;border-color:#3a3a3a;font-weight:bold}'
        + '.dyndb-sec-h{color:#569cd6;font-weight:bold;margin:8px 0 4px;border-bottom:1px solid #3a3a3a;padding-bottom:2px}'
        + '.dyndb-live-dot{color:#4ec9b0;font-size:10px;margin-left:6px;font-weight:normal;opacity:.85}'
        + '.dyndb-livebar{margin:0 0 6px;padding:4px 8px;background:#3a2f10;border:1px solid #b58900;border-radius:4px;color:#e6c65a;font-size:11px;cursor:pointer;text-align:center}'
        + '.dyndb-livebar:hover{background:#4a3c14}'
        + '.dyndb-card{border:1px solid #3a3a3a;border-radius:5px;padding:6px 8px;margin-bottom:6px;background:#252526}'
        + '.dyndb-card-h b{color:#dcdcaa}.dyndb-id{color:#808080}.dyndb-line{margin:2px 0;word-break:break-all}'
        + '.dyndb-k{display:inline-block;min-width:78px;color:#9cdcfe}.dyndb-val{color:#ce9178}.dyndb-na{color:#6a6a6a}'
        + '.dyndb-layer{padding:0 5px;border-radius:3px;font-size:11px;margin-left:4px}'
        + '.dyndb-l0{background:#4e2a1e;color:#f48771}.dyndb-l1{background:#143d2e;color:#4ec9b0}'
        + '.dyndb-l2{background:#2d2350;color:#c586c0}.dyndb-l34{background:#4a3a10;color:#d7ba7d}'
        + '.dyndb-shared{background:#0e3a4a;color:#569cd6}'
        + '.dyndb-pllegend{margin:4px 0 6px;padding:4px 6px;background:#222;border:1px solid #333;border-radius:4px;font-size:10px;color:#aaa;line-height:2}'
        + '.dyndb-pllegend .dyndb-layer{margin:0 3px 0 8px}'
        + '.dyndb-pkey{margin:5px 0 3px;padding-top:4px;border-top:1px dashed #333}'
        + '.dyndb-pkey-name{color:#dcdcaa;font-weight:bold}'
        + '.dyndb-prow{display:flex;align-items:center;gap:5px;margin:1px 0 1px 8px;font-size:11px;line-height:1.5;border-radius:3px;padding:0 4px}'
        + '.dyndb-prow.is-win{background:#1f3328}.dyndb-prow.is-lost{color:#7a7a7a}'
        + '.dyndb-pcheck{width:12px;flex:none;color:#4ec9b0;font-weight:bold}.is-lost .dyndb-pcheck{color:#666}'
        + '.dyndb-prow .dyndb-layer{margin-left:0;flex:none;padding:0 4px}'
        + '.dyndb-pval{color:#ce9178;word-break:break-all}.is-lost .dyndb-pval{color:#8f8478}'
        + '.dyndb-pwho{margin-left:auto;color:#6a9955;font-size:10px;flex:none;max-width:170px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}'
        + '.dyndb-pby{color:#c586c0;font-size:10px;flex:none}'
        + '.dyndb-pcovered{color:#999;font-size:10px;border:1px solid #555;border-radius:3px;padding:0 4px;flex:none}'
        + '.dyndb-chain{color:#808080}.dyndb-bad{color:#f48771;font-weight:bold}'
        + '.dyndb-note{border-left:3px solid #f48771;padding:2px 6px;margin:3px 0;background:#2a2020}'
        + '.dyndb-time{color:#808080}.dyndb-src{color:#c586c0}.dyndb-empty{color:#6a6a6a;padding:4px 0}'
        /* —— Vue 查看器 —— */
        + '.dyndb-insp{border-color:#2d6e4e;box-shadow:0 0 0 1px #143d2e inset}.dyndb-pick{border-color:#b58900;background:#2e2a18}'
        + '.dyndb-float{float:right}'
        + '.dyndb-chain-list{margin:4px 0;border:1px solid #3a3a3a;border-radius:4px;overflow:hidden}'
        + '.dyndb-chain-row{padding:2px 6px;border-bottom:1px solid #2e2e2e}.dyndb-chain-row:last-child{border-bottom:none}'
        + '.dyndb-chain-row.is-self{background:#2f3a4d}.dyndb-chain-el{color:#9cdcfe;word-break:break-all}'
        + '.dyndb-tag{display:inline-block;padding:0 5px;margin-left:4px;border-radius:3px;font-size:11px;white-space:nowrap}'
        + '.dyndb-tag-app{background:#143d2e;color:#4ec9b0;border:1px solid #2d6e4e}'
        + '.dyndb-tag-blk{background:#3d2e14;color:#d7ba7d;border:1px solid #6e5a2d}'
        + '.dyndb-tag-shell{background:#2d2350;color:#c586c0;border:1px solid #5a3d7a}'
        + '.dyndb-tag-ctx{background:#0e3a4a;color:#569cd6;border:1px solid #1f5a70}'
        + '.dyndb-appname{color:#4ec9b0}.dyndb-console-hint{color:#808080;font-size:11px;margin-top:4px}'
        + '.dyndb-comp-chain{margin:3px 0 6px;line-height:2}'
        + '.dyndb-tag-comp{background:#333344;color:#c8c8e0;border:1px solid #555566}'
        + '.dyndb-tag-el{background:#2e2e22;color:#c8c87a;border:1px solid #5a5a33}'
        + '.dyndb-chain-arrow{color:#6a6a6a;margin:0 3px}'
        + '.dyndb-dyncom{border:1px solid #2d6e4e;border-radius:4px;margin:6px 0;padding:5px 7px;background:#1f2b25}'
        + '.dyndb-dyncom-h{color:#4ec9b0;font-weight:bold;margin-bottom:3px}.dyndb-dyncom-name{color:#dcdcaa;font-weight:normal}'
        + '.dyndb-json{margin:4px 0 0;padding:6px;max-height:220px;overflow:auto;background:#1a1a1a;border:1px solid #333;border-radius:3px;color:#c0c0c0;font:11px/1.45 Consolas,Menlo,monospace;white-space:pre;word-break:normal;scrollbar-width:thin;scrollbar-color:#4a4a4a #1a1a1a}'
        + '.dyndb-json::-webkit-scrollbar{width:8px;height:8px}.dyndb-json::-webkit-scrollbar-track{background:#1a1a1a;border-radius:4px}'
        + '.dyndb-json::-webkit-scrollbar-thumb{background:#444;border-radius:4px;border:1px solid #1a1a1a}'
        + '.dyndb-json::-webkit-scrollbar-thumb:hover{background:#5a5a5a}.dyndb-json::-webkit-scrollbar-corner{background:#1a1a1a}'
        + '.dyndb-body{scrollbar-width:thin;scrollbar-color:#4a4a4a #1e1e1e}.dyndb-body::-webkit-scrollbar{width:9px}'
        + '.dyndb-body::-webkit-scrollbar-thumb{background:#3f3f3f;border-radius:5px}.dyndb-body::-webkit-scrollbar-thumb:hover{background:#555}'
        + '.dyndb-body::-webkit-scrollbar-track{background:#1e1e1e}'
        + '.dyndb-pick-overlay{position:absolute;z-index:2147483599;pointer-events:none;border:2px solid #4ec9b0;background:rgba(78,201,176,.12);border-radius:2px;box-shadow:0 0 0 99999px rgba(0,0,0,.15)}'
        + '.dyndb-panel.dyndb-picking{box-shadow:0 0 0 2px #b58900,0 6px 24px rgba(0,0,0,.45)}'
        + '.dyndb-panel.dyndb-picking *{cursor:crosshair !important}'
        /* —— 面板增强：resize / 钉住 / 折叠 JSON —— */
        + '.dyndb-panel{resize:both;min-width:340px;min-height:180px}.dyndb-head{cursor:move}.dyndb-head .dyndb-btn{cursor:pointer}'
        + '.dyndb-btn.is-on{background:#143d2e;color:#4ec9b0;border-color:#2d6e4e}'
        + '.dyndb-panel.dyndb-pinned{border-color:#b58900;box-shadow:0 0 0 1px #b58900,0 6px 24px rgba(0,0,0,.45)}'
        + '.dyndb-json.is-collapsed{max-height:34px;overflow:hidden;cursor:pointer;position:relative}.dyndb-json{cursor:pointer}'
        + '@keyframes dyndb-flash{0%{background-color:#6b5a10}70%{background-color:#4a3f10}100%{background-color:transparent}}'
        + '.dyndb-flash{animation:dyndb-flash 1.5s ease-out;border-radius:3px}div.dyndb-flash{padding:2px 4px;margin:-2px -4px}'
        + '.dyndb-flash-chip{color:#e6c65a;font-size:10px;margin:2px 0;font-weight:bold}'
        /* —— DynParams 子 Tab（参数层/轨迹） —— */
        + '.dyndb-psubbar{display:flex;align-items:center;gap:4px;margin:2px 0 6px}'
        + '.dyndb-psub{background:#2a2a2e;color:#9a9a9a;border:1px solid #444;border-radius:4px;padding:2px 10px;cursor:pointer;font:11px Consolas}'
        + '.dyndb-psub:hover{color:#ddd;background:#333337}.dyndb-psub.is-active{color:#4ec9b0;border-color:#2d6e4e;background:#143d2e}'
        + '.dyndb-psub-clear{margin-left:auto}'
        + '.dyndb-tr{display:flex;align-items:center;gap:6px;padding:2px 4px;margin:2px 0;border-left:3px solid #555;font-size:11px;flex-wrap:wrap}'
        + '.dyndb-tr-set{border-left-color:#f48771}.dyndb-tr-commit{border-left-color:#569cd6}.dyndb-tr-fork{border-left-color:#c586c0}'
        + '.dyndb-tr-ctx{color:#4ec9b0;flex:none}.dyndb-tr-key{color:#dcdcaa;flex:none}'
        + '.dyndb-tr-vals{min-width:0;word-break:break-all}.dyndb-tr-old{color:#7a7a7a;text-decoration:line-through}'
        /* —— 动作 Tab —— */
        + '.dyndb-agroup{border:1px solid #3a3a3a;border-radius:4px;margin:6px 0;overflow:hidden}'
        + '.dyndb-agroup.is-self{border-color:#2d6e4e}'
        + '.dyndb-agroup-h{padding:3px 7px;background:#2b2b2f;color:#9cdcfe;font-size:11px}'
        + '.dyndb-abind{padding:3px 7px 1px;background:#26262a;display:flex;align-items:center;gap:6px;flex-wrap:wrap}'
        + '.dyndb-astep{padding:2px 7px 2px 22px;display:flex;align-items:center;gap:6px;flex-wrap:wrap}'
        + '.dyndb-aname{color:#dcdcaa}.dyndb-act-ev{background:#0e3a4a;color:#569cd6;border:1px solid #1f5a70;margin-left:0}'
        + '.dyndb-act-bi{background:#143d2e;color:#4ec9b0;border:1px solid #2d6e4e}'
        + '.dyndb-act-db{background:#2d2350;color:#c586c0;border:1px solid #5a3d7a}'
        + '.dyndb-act-comp{background:#4a3a10;color:#d7ba7d;border:1px solid #6e5a2d}'
        + '.dyndb-act-eff{background:#1f3328;color:#4ec9b0;border:1px solid #2d6e4e}'
        + '.dyndb-act-lost{background:#333;color:#888;border:1px solid #555;text-decoration:line-through}'
        + '.dyndb-bad-tag{background:#4e2a1e;color:#f48771;border:1px solid #7a3a2a}'
        + '.dyndb-ajson{margin:2px 0 4px 22px;max-height:150px}'
        /* —— 页面动作元素清单 —— */
        + '.dyndb-prow2{display:flex;align-items:center;gap:6px;padding:3px 6px;margin:3px 0 1px;background:#222936;border:1px solid #33405a;border-radius:4px;cursor:pointer;flex-wrap:wrap}'
        + '.dyndb-prow2:hover{background:#2a3548;border-color:#4ec9b0}'
        + '.dyndb-prow2.is-hidden-el{background:#252525;border-color:#3a3a3a;cursor:default;opacity:.75}'
        + '.dyndb-loc-ico{flex:none;font-size:12px}.dyndb-pel{color:#9cdcfe;word-break:break-all;min-width:0}'
        + '.dyndb-aexp{margin-left:auto;flex:none;padding:0 7px;line-height:18px}.dyndb-abind.is-dim{opacity:.65}'
        + '.dyndb-achain-name{color:#dcdcaa;margin:0 2px}'
        + '.dyndb-achain-name.is-unreg{color:#f48771;border-bottom:1px dashed #f48771;font-weight:bold}'
        + '.dyndb-pick-overlay.dyndb-locate{border-color:#dcdcaa;background:rgba(220,220,170,.18);box-shadow:0 0 0 99999px rgba(0,0,0,.25),0 0 0 4px rgba(220,220,170,.5);animation:dyndb-locate-pulse .55s ease-in-out 0s 3 alternate;pointer-events:none}'
        + '@keyframes dyndb-locate-pulse{from{opacity:.55}to{opacity:1}}'
        + '.dyndb-ainput{width:calc(100% - 16px);margin:4px 8px;padding:3px 6px;background:#1a1a1a;color:#ddd;border:1px solid #444;border-radius:4px;font:11px Consolas;box-sizing:border-box}'
        + '.dyndb-ainput:focus{outline:none;border-color:#2d6e4e}'
        + '.dyndb-arow{padding:2px 8px;font-size:11px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}'
        + '.dyndb-arow:hover{background:#2a2d33}.dyndb-arow b{color:#ce9178;font-weight:normal}';
    var st = document.createElement('style');
    st.id = 'dyndb-style';
    st.textContent = css;
    document.head.appendChild(st);
}

function mount() {
    if (panelEl || !document.body) return;
    injectCss();
    panelEl = document.createElement('div');
    panelEl.className = 'dyndb-panel';
    panelEl.innerHTML =
        '<div class="dyndb-head" data-act="toggle"><b>dyn-debug</b><span class="dyndb-spacer"></span>'
        + '<button class="dyndb-btn" data-act="pick">🎯拾取</button>'
        + '<button class="dyndb-btn" data-act="pin" title="钉住当前快照（暂停自动刷新）">📌</button>'
        + '<button class="dyndb-btn" data-act="copy" title="复制完整上下文 JSON（元素链/配置/数据/参数/动作）">📋</button>'
        + '<button class="dyndb-btn" data-act="refresh">刷新</button>'
        + '<button class="dyndb-btn" data-act="clear">清空</button>'
        + '<button class="dyndb-btn" data-act="hide">－</button></div>'
        + '<div class="dyndb-tabbar">'
        + '<button class="dyndb-tab is-active" data-tab="view">App / 组件</button>'
        + '<button class="dyndb-tab" data-tab="params">DynParams</button>'
        + '<button class="dyndb-tab" data-tab="actions">动作</button></div>'
        + '<div class="dyndb-body">'
        + '<div class="dyndb-pane" data-pane="view">'
        + '<div class="dyndb-livebar" data-act="live-refresh" style="display:none">● 数据有更新（正在选择文本，点击刷新）</div>'
        + '<div class="dyndb-insp-slot"></div><div class="dyndb-blocks-slot"></div><div class="dyndb-notes-slot"></div></div>'
        + '<div class="dyndb-pane" data-pane="params" style="display:none">'
        + '<div class="dyndb-psubbar">'
        + '<button class="dyndb-psub is-active" data-psub="layers">参数层</button>'
        + '<button class="dyndb-psub" data-psub="trace">轨迹</button>'
        + '<button class="dyndb-btn dyndb-psub-clear" data-act="clear-trace">清空轨迹</button></div>'
        + '<div class="dyndb-psub-pane" data-psub-pane="layers"><div class="dyndb-params-slot"></div></div>'
        + '<div class="dyndb-psub-pane" data-psub-pane="trace" style="display:none"><div class="dyndb-trace-slot"></div></div></div>'
        + '<div class="dyndb-pane" data-pane="actions" style="display:none"><div class="dyndb-actions-slot"></div></div>'
        + '</div>';
    document.body.appendChild(panelEl);
    bodyEl = panelEl.querySelector('.dyndb-body');
    liveBarEl = panelEl.querySelector('.dyndb-livebar');
    inspEl = panelEl.querySelector('.dyndb-insp-slot');
    blocksEl = panelEl.querySelector('.dyndb-blocks-slot');
    notesEl = panelEl.querySelector('.dyndb-notes-slot');
    paramsEl = panelEl.querySelector('.dyndb-params-slot');
    traceEl = panelEl.querySelector('.dyndb-trace-slot');
    actionsEl = panelEl.querySelector('.dyndb-actions-slot');

    panelEl.addEventListener('click', function (e) {
        var actEl = e.target && e.target.closest ? e.target.closest('[data-act]') : null;
        var act = actEl && actEl.getAttribute('data-act');
        var tab = e.target && e.target.getAttribute && e.target.getAttribute('data-tab');
        var psub = e.target && e.target.getAttribute && e.target.getAttribute('data-psub');
        if (tab) { switchTab(tab); return; }
        if (psub) { switchPsub(psub); return; }
        if (act === 'pick') { K.probe.startPicking(); return; }
        if (act === 'live-refresh') { drawInspector(true); return; }
        if (act === 'locate') { K.trace.locateAction(+actEl.getAttribute('data-idx')); return; }
        if (act === 'aexpand') { K.trace.toggleExpand(+actEl.getAttribute('data-idx')); drawActions(); return; }
        if (act === 'rescan') { K.trace.rescan(); drawActions(); return; }
        if (act === 'pin') { setPinned(!pinned); return; }
        if (act === 'copy') { copyContext(); return; }
        if (act === 'clear-trace') { K.trace.clearTrace(); return; }
        if (act === 'refresh') { if (pinned) setPinned(false); else draw(); return; }
        if (act === 'clear') { K.trace.clear(); return; }
        if (act === 'hide') { panelEl.style.display = 'none'; remountBtn(); return; }
        if (act === 'toggle' && actEl.classList.contains('dyndb-head')) {
            collapsed = !collapsed;
            panelEl.classList.toggle('dyndb-collapsed', collapsed);
        }
    });
    // 动作页过滤：只刷清单行，输入框不重建（不失焦、不重扫）
    panelEl.addEventListener('input', function (e) {
        if (e.target && e.target.getAttribute && e.target.getAttribute('data-act') === 'actpage-filter') {
            var ph = actionsEl && actionsEl.querySelector('[data-pagelist]');
            if (ph) ph.innerHTML = K.trace.filterPageList(e.target.value || '');
            return;
        }
        // 动作注册表过滤：只刷列表，输入框不重建（不失焦）
        if (e.target && e.target.getAttribute && e.target.getAttribute('data-act') === 'act-filter') {
            actFilter = e.target.value || '';
            var host = actionsEl && actionsEl.querySelector('[data-actlist]');
            if (host) host.innerHTML = K.trace.registryListHtml(actFilter.toLowerCase());
        }
    });

    initPanelUi();
    // 参数轨迹：先拉环形缓冲，再订阅实时事件（打开轨迹子页时 150ms 防抖刷新）
    K.trace.onUpdate = function () {
        if (activeTab === 'params' && activePsub === 'trace' && !pinned && !traceQueued) {
            traceQueued = true;
            setTimeout(function () { traceQueued = false; drawTrace(); }, 150);
        }
    };
    K.trace.init();
    // 2s 轮询只刷 Blocks/Notes + 元素存活兜底；inspector 的数据变化走 $watch 实时推送
    timer = setInterval(function () { if (!pinned) { drawBlocksNotes(); tickInspector(); } }, 2000);
    // 选中文本期间数据有更新会挂起刷新；选择一结束（复制完）自动补刷到最新
    document.addEventListener('selectionchange', function () {
        if (liveStale && !selectionInPanel()) renderInspHtml();
    });
    draw();
    console.info('%c[dyn-debug] 已启用：?dyndebug=1（拾取查看/参数层/动作绑定/轨迹；Alt+1/2/3 切 Tab）', 'color:#4ec9b0');
}

function remountBtn() {
    var b = document.createElement('button');
    b.textContent = 'dyn';
    b.style.cssText = 'position:fixed;right:10px;bottom:10px;z-index:2147483600;padding:2px 10px;'
        + 'background:#2d2d30;color:#4ec9b0;border:1px solid #454545;border-radius:6px;cursor:pointer;font:12px Consolas';
    b.onclick = function () { b.remove(); panelEl.style.display = 'flex'; };
    document.body.appendChild(b);
}

function boot() {
    if (global.DynLib && typeof global.DynLib.ready === 'function') {
        global.DynLib.ready(function () { setTimeout(mount, 300); });
    } else if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', mount);
    } else {
        mount();
    }
}

K.panel = {
    mount: mount, draw: draw, drawInspector: drawInspector, drawTrace: drawTrace,
    scheduleRender: scheduleRender, switchTab: switchTab, switchPsub: switchPsub,
    setPinned: setPinned, isPinned: function () { return pinned; },
    contains: function (n) { return !!(panelEl && panelEl.contains(n)); },
    markPicking: function (on) { if (panelEl) panelEl.classList.toggle('dyndb-picking', !!on); }
};

boot();
})(window);
