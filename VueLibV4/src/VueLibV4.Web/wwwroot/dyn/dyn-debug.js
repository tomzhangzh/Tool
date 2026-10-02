/* dyn-debug.js — Dyn 体系调试护栏（?dyndebug=1）
 * ------------------------------------------------------------
 * 启用方式（任一）：
 *   1) URL 带 ?dyndebug=1
 *   2) localStorage.DYN_DEBUG = '1'
 *   3) window.DYN_DEBUG = true（需在 dyn-lib 之前设置）
 *
 * 能力：
 *   - 页面右下角浮层，列出当前全部 Block（role / 已注册命令 / 已订阅事件）
 *   - 每个 Block 的关键参数最终值 + 获胜层（table/loadUrl/filterInit/rowId/project...）
 *     —— 串表/预置丢失类问题一眼定位，不用再手写 inspect
 *   - 参数链拓扑：每个 ctx 的 id 与父链（L1 声明遮蔽 bug 直接可见）
 *   - Notes：收集运行期告警（未知 op、未注册命令等），带来源与时间
 *   - window.DynDebug.note(msg, meta) 供各模块统一上报
 *
 * Vue 查看器（🎯 拾取）：
 *   - 进入拾取后，页面上点任意元素（不会触发业务点击），沿 DOM 向上标注
 *     VueApp(__dynApp) / Block(__dynBlock) / 壳槽 / DynParams ctx，并定位"所属 VueApp"
 *   - 展示该 app 根代理的 data 快照 + computed/methods 清单；
 *     同时挂到 window.__sel（元素）/ window.__selApp（app 代理），可在 Console 深查
 *   - 控制台也可直接 DynDebug.pick()（再点页面）或 DynDebug.inspect($0)
 *
 * 零依赖、原生 DOM；样式自带注入，不污染业务 css。
 */
(function (global) {
    'use strict';
var __plugin = {
    name: 'debug', stage: 'debug', requires: ['dyn'],
    setup: function (ctx) {
    'use strict';

    function enabled() {
        try {
            if (global.DYN_DEBUG === true) return true;
            if (global.localStorage && localStorage.getItem('DYN_DEBUG') === '1') return true;
            var s = global.location && global.location.search || '';
            return /[?&]dyndebug=1\b/.test(s);
        } catch (e) { return false; }
    }

    var notes = [];
    var panelEl = null;
    var bodyEl = null;
    var collapsed = false;
    var timer = null;

    // ---- Vue 查看器状态 ----
    var selectedEl = null;   // 最近拾取的元素
    var picking = false;     // 是否正在拾取
    var hoverEl = null;      // 拾取中悬停元素
    var overlayEl = null;    // 悬停高亮框

    var DynDebug = {
        enabled: enabled(),
        /** 上报一条调试记录：msg 文本，meta={source,table,...} */
        note: function (msg, meta) {
            if (!this.enabled) return;
            notes.push({ t: new Date(), msg: String(msg == null ? '' : msg), meta: meta || {} });
            if (notes.length > 200) notes.shift();
            this.scheduleRender();
        },
        clear: function () { notes.length = 0; this.render(); },
        /** 立即扫描渲染 */
        render: function () { if (panelEl) draw(); },
        scheduleRender: function () {
            if (!panelEl) return;
            if (renderQueued) return;
            renderQueued = true;
            // 告警只影响 Notes 区，不碰 inspector（避免打断 JSON 文本选择/避免重复深快照）
            setTimeout(function () { renderQueued = false; drawBlocksNotes(); }, 200);
        },
        /** 进入拾取模式（下一次点击的元素成为查看目标；Esc 取消） */
        pick: function () { startPicking(); },
        /** 取消拾取 */
        cancelPick: function () { stopPicking(); },
        /** 直接查看指定元素：返回 {el, ownerApp, chain, vueChain} 描述对象（控制台友好） */
        inspect: function (el) {
            var info = describe(el);
            if (info) {
                selectedEl = el || null;
                global.__sel = selectedEl;
                global.__selApp = info.ownerProxy || info.ownerApp; // 根代理：可直接读写 data/调方法
                global.__selAppRaw = info.ownerApp;                 // Vue 应用实例（unmount/config 等）
                if (panelEl) drawInspector(true);
            }
            return info;
        }
    };
    var renderQueued = false;
    global.DynDebug = DynDebug;

    if (!DynDebug.enabled) return;

    var WATCH_KEYS = ['table', 'tableName', 'keyField', 'project', 'loadUrl', 'deleteUrl',
        'addUrl', 'editUrl', 'filterInit', 'rowId', 'preFill', 'modalPageId'];

    function esc(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

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
            card.innerHTML = '<div class="dyndb-card-h" data-b-h></div>'
                + '<div data-b-static></div>'
                + '<div class="dyndb-params" data-b-params></div>'
                + '<div class="dyndb-line dyndb-chain" data-b-chain></div>';
            host.appendChild(card);
            rec = { card: card, rows: new Map(), sig: null, chainTxt: null };
            blockCards.set(el, rec);
        }
        // 静态部分（role/id/标记/cmds/events）：签名变了才重建
        if (rec.sig !== sig) {
            rec.sig = sig;
            rec.card.querySelector('[data-b-h]').innerHTML =
                '<b>' + esc(role) + '</b>'
                + (el.id ? ' <span class="dyndb-id">#' + esc(el.id) + '</span>' : '')
                + (intro && intro.destroyed ? ' <span class="dyndb-bad">destroyed</span>' : '')
                + (!h ? ' <span class="dyndb-bad">未挂载 __dynBlock</span>' : '');
            var st = '';
            if (intro) {
                st += '<div class="dyndb-line"><span class="dyndb-k">cmds</span> '
                    + (intro.commands.length ? intro.commands.map(esc).join(', ') : '<span class="dyndb-na">（无）</span>') + '</div>';
                if (intro.events.length)
                    st += '<div class="dyndb-line"><span class="dyndb-k">events</span> ' + intro.events.map(esc).join(', ') + '</div>';
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
                        row.innerHTML = '<span class="dyndb-k"></span> <span class="dyndb-val dyndb-bpv"></span> '
                            + '<span class="dyndb-layer dyndb-bpl"></span>';
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
        rec.rows.forEach(function (row, k) {
            if (!seen[k]) { row.remove(); rec.rows.delete(k); }
        });
    }

    // ============================================================
    //  Vue 查看器：元素拾取 + 向上找所属 VueApp
    // ============================================================

    function elDesc(el) {
        if (!el || el.nodeType !== 1) return '(非元素)';
        var s = el.tagName.toLowerCase();
        if (el.id) s += '#' + el.id;
        var cls = (el.getAttribute && el.getAttribute('class') || '').trim().split(/\s+/).filter(Boolean);
        if (cls.length) s += '.' + cls.slice(0, 3).join('.');
        return s;
    }

    /** 深度受限的快照（避免循环引用/巨型响应式对象卡死）；maxDepth 默认 3 */
    function snapshot(v, depth, seen, maxDepth) {
        depth = depth || 0; seen = seen || new Set(); maxDepth = maxDepth || 3;
        if (v === null || v === undefined) return v;
        var t = typeof v;
        if (t === 'function') return '[Function]';
        if (t !== 'object') return v;
        if (depth >= maxDepth) return Array.isArray(v) ? '[Array×' + v.length + ']' : '{…}';
        if (seen.has(v)) return '[Circular]';
        seen.add(v);
        // 跳过 Vue 内部 __vnode/$ 开头实例与 DOM
        if (v.nodeType) return '[DOM ' + elDesc(v) + ']';
        if (v.__v_isRef) return snapshot(v.value, depth, seen, maxDepth);
        var out;
        if (Array.isArray(v)) {
            out = v.slice(0, 20).map(function (x) { return snapshot(x, depth + 1, seen, maxDepth); });
            if (v.length > 20) out.push('[…+' + (v.length - 20) + ']');
        } else {
            out = {};
            var keys = Object.keys(v).filter(function (k) { return k.charAt(0) !== '_' && k.charAt(0) !== '$'; }).slice(0, 40);
            keys.forEach(function (k) {
                try { out[k] = snapshot(v[k], depth + 1, seen, maxDepth); } catch (e) { out[k] = '[err]'; }
            });
        }
        seen.delete(v);
        return out;
    }

    function safeJson(v, indent) {
        try { return JSON.stringify(v, null, indent || 0); } catch (e) { return '[JSON 序列化失败: ' + e.message + ']'; }
    }
    function clip(s, max) {
        s = String(s == null ? '' : s);
        return s.length > max ? s.slice(0, max) + '\n…（已截断，完整值见 Console: __selComp.jsonconfig）' : s;
    }

    /** Vue 内部组件链（dev 构建走 __vueParentComponent；prod 构建该属性被裁剪，返回 [] 由 vnode 遍历兜底） */
    function vueComponentChain(el) {
        var chain = [];
        try {
            var inst = el.__vueParentComponent;
            var guard = 0;
            while (inst && guard++ < 30) {
                var type = inst.type || {};
                var name = type.name || type.__name || (type.__file ? type.__file.split('/').pop().replace(/\.\w+$/, '') : null);
                chain.push(name || ('<' + (typeof type === 'string' ? type : 'Anonymous') + '>'));
                inst = inst.parent;
            }
        } catch (e) { }
        return chain;
    }

    /**
     * 生产构建没有 __vueParentComponent：从根内部实例的 subTree 遍历 vnode 树，
     * 沿"组件 vnode"入栈，找到 DOM 元素所在的组件链（根 → 最深）。
     * 节点依据：vnode.component（已挂载组件）/ vnode.el（元素/文本节点）/ children 数组 / Suspense.activeBranch。
     */
    function findComponentChain(rootProxy, targetEl) {
        var rootInst = null;
        try { rootInst = rootProxy.$ || (rootProxy.$options && rootProxy.$options._instance) || null; } catch (e) { }
        if (!rootInst || !rootInst.subTree) return [];
        var found = null, steps = 0;

        function visitVnode(vn, stack) {
            if (found || !vn) return;
            if (++steps > 30000) return;
            // 组件 vnode：进入它的渲染子树（带着更深的组件栈）
            if (vn.component) { visitTree(vn.component.subTree, stack.concat(vn.component)); return; }
            if (vn.el && vn.el === targetEl) { found = stack.slice(); return; }
            if (vn.suspense && vn.suspense.activeBranch) visitTree(vn.suspense.activeBranch, stack);
            var ch = vn.children;
            if (Array.isArray(ch)) for (var i = 0; i < ch.length; i++) visitTree(ch[i], stack);
        }
        function visitTree(node, stack) { visitVnode(node, stack); }

        try { visitTree(rootInst.subTree, [rootInst]); } catch (e) { }
        return found || [];
    }

    /** 组件名：显式 name（cshtml 组件加载时 opt.name=组件名，prod 不被裁剪）→ appContext 注册表反查 → __name */
    function componentName(inst) {
        var ty = (inst && inst.type) || {};
        if (ty.name) return ty.name;
        try {
            var regs = (inst.appContext && inst.appContext.components) || {};
            for (var k in regs) if (regs[k] === ty) return k;
        } catch (e) { }
        return ty.__name || '<anon>';
    }

    /**
     * 取根组件代理。dyn-core 挂载时：
     *   el.__dynApp   = createApp() 的应用实例（unmount/config 在这）
     *   el.__dynProxy = app.mount(el) 的根组件代理（$data/$options/方法在这；也镜像到 app.__dynProxy）
     * 兜底再试 Vue 内部 app._instance.proxy。
     */
    function rootProxyOf(app, rootEl) {
        try { if (rootEl && rootEl.__dynProxy) return rootEl.__dynProxy; } catch (e) { }
        if (!app) return null;
        try { if (app.__dynProxy) return app.__dynProxy; } catch (e) { }
        if (app.$data !== undefined || (app.$options !== undefined && app.$ !== undefined)) return app; // 已是组件代理
        try {
            var inst = app._instance;
            if (inst && inst.proxy) return inst.proxy;
        } catch (e) { }
        // prod 构建（vue.global.prod.js）_instance 可能缺失：渲染器在容器上留有根 vnode
        try {
            var vn = app._container && app._container._vnode;
            if (vn && vn.component && vn.component.proxy) return vn.component.proxy;
        } catch (e) { }
        return null;
    }

    /** 页面全部 Dyn app 根节点（含弹窗/远程片段/嵌套 app）。 */
    var rootsCacheAt = 0, rootsCache = [];
    function allAppRoots() {
        var now = Date.now();
        if (rootsCache.length && now - rootsCacheAt < 3000) return rootsCache;
        var nodes = [];
        try {
            nodes = [].slice.call(document.querySelectorAll(
                '[data-dyn-mode="createApp"],[dyn-init]'
            )).filter(function (n) {
                if (n.getAttribute('data-dyn-mode') === 'createApp') return true;
                return (n.getAttribute('dyn-init') || '').split('|').some(function (tok) {
                    return /^\s*(ActionHelper\.)?createapp\s*(\(|$)/i.test(tok.trim());
                });
            });
        } catch (e) { }
        // 兜底：非 dyn 标记挂载的裸 Vue app（如 PageGen 的 Vue.createApp）靠 Vue 标准标记
        // __vue_app__（prod 也会写）；inspect 是手动操作，3s 缓存内全量扫一次 * 可接受
        try {
            [].slice.call(document.querySelectorAll('*')).forEach(function (n) {
                if ((n.__dynApp || n.__dynProxy || n.__vue_app__) && nodes.indexOf(n) < 0) nodes.push(n);
            });
        } catch (e) { }
        rootsCache = nodes; rootsCacheAt = now;
        return nodes;
    }

    /**
     * 沿 DOM 向上描述：返回各层标记 + 最近的所属 VueApp。
     * 所属规则：自身/祖先上第一个挂 __dynApp 的节点（dyn-core 每个 createApp 根都有登记）。
     * 兜底：向上找不到时（元素被 Teleport 到 body，如 el-select 下拉/popover/日期面板），
     * 枚举全部 app 根，在各自 vnode 树里反查谁的子树渲染了该元素。
     */
    function describe(el) {
        if (!el || el.nodeType !== 1) return null;
        var chain = [];
        var ownerApp = null;
        var ownerRoot = null;
        var node = el, guard = 0;
        while (node && node.nodeType === 1 && guard++ < 80) {
            var badges = [];
            var nodeApp = node.__dynApp || node.__vue_app__;   // __vue_app__：裸 Vue.createApp 的标准标记
            if (nodeApp) { badges.push('VueApp'); if (!ownerApp) { ownerApp = nodeApp; ownerRoot = node; } }
            if (node.hasAttribute && node.hasAttribute('data-dyn-mode')) badges.push('dyn:' + node.getAttribute('data-dyn-mode'));
            if (node.__dynBlock) badges.push('Block:' + (node.getAttribute('data-blk-role') || '?'));
            else if (node.hasAttribute && node.hasAttribute('data-blk-role')) badges.push('Block?' + node.getAttribute('data-blk-role'));
            if (node.hasAttribute && node.hasAttribute('data-layout-slot')) badges.push('slot:' + node.getAttribute('data-layout-slot'));
            if (node.hasAttribute && node.hasAttribute('data-dyn-layout')) badges.push('shell:' + node.getAttribute('data-dyn-layout'));
            if (node.__dynLayout) badges.push('shellRoot');
            var ctxId = null;
            try { if (global.DynParams) { var pc = DynParams.fromEl(node); if (pc && pc.id) ctxId = pc.id; } } catch (e) { }
            if (ctxId) badges.push('ctx:' + ctxId);
            chain.push({ desc: elDesc(node), self: node === el, badges: badges });
            if (node.tagName === 'BODY') break;
            node = node.parentElement;
        }
        // Teleport 兜底：DOM 祖先没有 app 根时，在所有 app 的 vnode 子树里反查
        var teleported = false;
        if (!ownerApp) {
            var roots = allAppRoots();
            for (var i = 0; i < roots.length; i++) {
                var rApp = roots[i].__dynApp || roots[i].__vue_app__;
                var rp = rootProxyOf(rApp, roots[i]);
                if (!rp) continue;
                if (findComponentChain(rp, el).length) {
                    ownerApp = rApp; ownerRoot = roots[i]; teleported = true; break;
                }
            }
        }
        return {
            el: el,
            ownerApp: ownerApp,
            ownerProxy: rootProxyOf(ownerApp, ownerRoot),
            ownerRoot: ownerRoot,
            teleported: teleported,
            chain: chain,
            vueChain: vueComponentChain(el)
        };
    }

    function appDataInfo(proxy, ownerRoot) {
        var info = { name: '?', dataKeys: [], dataSnap: null, computed: [], methods: [], err: null };
        try {
            var optName = proxy.$options && (proxy.$options.name || proxy.$options.__name);
            // 我们的 BlockApp 大多没显式 name：退到根节点的 data-blk-role / data-dyn-mode
            var domName = ownerRoot && (ownerRoot.getAttribute('data-blk-role')
                || ownerRoot.getAttribute('data-dyn-mode') || '');
            info.name = optName || (domName ? 'dyn:' + domName : 'AnonymousApp');
        } catch (e) { }
        try {
            var data = proxy.$data || {};
            info.dataKeys = Object.keys(data);
            info.dataSnap = snapshot(data, 0);
        } catch (e) { info.err = e.message; }
        try {
            if (proxy.$options) {
                info.computed = Object.keys(proxy.$options.computed || {});
                info.methods = Object.keys(proxy.$options.methods || {});
            }
        } catch (e) { }
        return info;
    }

    // ============================================================
    // DynParams 逐层透视：每个参数按 L0→L4 优先级列出【所有】提供层，
    // 第一行=获胜值（绿底✓），其余=被谁覆盖（暗灰+来源 ctx/元素）
    // ============================================================
    function shortParamVal(v) {
        if (v === undefined) return 'undefined';
        if (v === null) return 'null';
        var s;
        try { s = typeof v === 'string' ? v : JSON.stringify(v); } catch (e) { s = String(v); }
        if (s == null) s = String(v);
        return s.length > 80 ? s.slice(0, 80) + '…' : s;
    }

    function paramsLayersSection(P) {
        if (!global.DynParams || !P) return '';
        var chain = [];
        var cc = P, guard = 0;
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
            + '<span class="dyndb-layer dyndb-l0">L0</span>调用点/运行时'
            + '<span class="dyndb-layer dyndb-l1">L1</span>容器静态'
            + '<span class="dyndb-layer dyndb-shared">sh</span>共享实体'
            + '<span class="dyndb-layer dyndb-l2">L2</span>业务Model'
            + '<span class="dyndb-layer dyndb-l34">L3</span>父链继承'
            + '<span class="dyndb-layer dyndb-l34">L4</span>URL兜底'
            + '</div>';

        shown.forEach(function (k) {
            var cands = [];
            var sharedSeen = {};
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
            if (Object.prototype.hasOwnProperty.call(urlMap, k))
                cands.push({ tag: 'L4', cls: 'dyndb-l34', value: urlMap[k], who: 'URL query' });
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
                    + (isWin ? '' : '<span class="dyndb-pcovered">被覆盖</span>')
                    + '</div>';
            });
            html += '</div>';
        });
        if (keys.length > shown.length)
            html += '<div class="dyndb-line dyndb-na">…另有 ' + (keys.length - shown.length)
                + ' 个参数；Console 执行 DynParams.fromEl(__sel).inspect() 看全部</div>';
        html += '</div>';
        return html;
    }

    function inspectorSection(state) {
        if (picking) {
            return '<div class="dyndb-card dyndb-pick">'
                + '<div class="dyndb-card-h"><b>🎯 拾取中</b></div>'
                + '<div class="dyndb-line">点击页面任意元素查看其所属 VueApp；按 <b>Esc</b> 取消。'
                + '（业务点击已被拦截）</div></div>';
        }
        if (!selectedEl || !document.contains(selectedEl) || !state) return '';
        var info = state.info;
        if (info && info.err) {
            return '<div class="dyndb-card"><div class="dyndb-bad">describe 异常: ' + esc(info.err) + '</div></div>';
        }
        var comps = state.comps;
        var dynComp = state.dynComp;

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
                html += '<div class="dyndb-chain-row' + (c.self ? ' is-self' : '') + '">'
                    + '<span class="dyndb-chain-el">' + esc(c.desc) + '</span>'
                    + c.badges.map(function (b) {
                        var cls = b.indexOf('VueApp') === 0 ? 'dyndb-tag-app'
                            : b.indexOf('Block') === 0 ? 'dyndb-tag-blk'
                            : b.indexOf('shell') === 0 || b.indexOf('slot') === 0 ? 'dyndb-tag-shell'
                            : 'dyndb-tag-ctx';
                        return ' <span class="dyndb-tag ' + cls + '">' + esc(b) + '</span>';
                    }).join('')
                    + '</div>';
            });
            html += '</div>';
        }

        if (info.vueChain.length) {
            html += '<div class="dyndb-line"><span class="dyndb-k">Vue组件</span> '
                + esc(info.vueChain.slice(0, 10).join(' ← ')) + '</div>';
        }

        // 组件链/最近 DynDynamicCom 已在 buildInspState 中算好（state.comps/state.dynComp）

        if (comps.length) {
            var names = comps.map(componentName);
            html += '<div class="dyndb-line"><span class="dyndb-k">组件链</span></div><div class="dyndb-comp-chain">';
            names.forEach(function (nm, i2) {
                var cls = nm === 'DynDynamicCom' ? 'dyndb-tag-app'
                    : /^El/.test(nm) ? 'dyndb-tag-el' : 'dyndb-tag-comp';
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
                var jcStr = safeJson(jc, 1);
                html += '<pre class="dyndb-json" title="点击折叠/展开">' + esc(clip(jcStr, 3000)) + '</pre></div>';
            }
            if (pmi !== null && typeof pmi !== 'undefined') {
                html += '<div class="dyndb-dyncom" data-flash="pmi"><div class="dyndb-dyncom-h">parentmodelinfo</div>';
                var pmiStr = safeJson(pmi, 1);
                html += '<pre class="dyndb-json" title="点击折叠/展开">' + esc(clip(pmiStr, 1800)) + '</pre></div>';
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
            if (ai.computed.length)
                html += '<div class="dyndb-line"><span class="dyndb-k">computed</span> ' + esc(ai.computed.join(', ')) + '</div>';
            if (ai.methods.length)
                html += '<div class="dyndb-line"><span class="dyndb-k">methods</span> <span class="dyndb-na">'
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

    function startPicking() {
        if (picking || !document.body) return;
        if (pinned) setPinned(false);   // 重新拾取意味着要看新快照
        picking = true;
        hoverEl = null;
        if (panelEl) panelEl.classList.add('dyndb-picking');
        if (activeTab !== 'view') switchTab('view');   // 拾取提示卡在查看 Tab
        document.addEventListener('mousemove', onPickMove, true);
        document.addEventListener('click', onPickClick, true);
        document.addEventListener('keydown', onPickKey, true);
        drawInspector(true);
    }

    function stopPicking() {
        if (!picking) return;
        picking = false; hoverEl = null;
        if (overlayEl) overlayEl.style.display = 'none';
        if (panelEl) panelEl.classList.remove('dyndb-picking');
        document.removeEventListener('mousemove', onPickMove, true);
        document.removeEventListener('click', onPickClick, true);
        document.removeEventListener('keydown', onPickKey, true);
        drawInspector(true);
    }

    function onPickMove(e) {
        var t = e.target;
        if (t === overlayEl) return;
        if (panelEl && panelEl.contains(t)) { hoverEl = null; moveOverlay(null); return; }
        if (t && t.nodeType === 1) { hoverEl = t; moveOverlay(t); }
    }

    function onPickClick(e) {
        var t = e.target;
        if (panelEl && panelEl.contains(t)) return; // 面板内按钮走正常逻辑
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
            drawInspector(true);
        }
    }

    function onPickKey(e) {
        if (e.key === 'Escape') { e.preventDefault(); stopPicking(); }
    }

    // ============================================================
    //  分区渲染 + 缓存
    //  inspector 很贵（vnode 全树遍历 + jsonconfig 深快照），且 2s 轮询整体重绘会
    //  打断用户在 JSON 块里的文本选择。拆成三个容器：inspector 只在拾取/换选/手动
    //  刷新时重算，轮询只更新 Blocks/Notes；inspector 按"选中元素+拾取态+是否仍在文档"缓存。
    // ============================================================
    var inspEl = null, blocksEl = null, notesEl = null, paramsEl = null, liveBarEl = null,
        traceEl = null, actionsEl = null;
    var activeTab = 'view';
    var activePsub = 'layers';
    // inspState：结构性缓存（describe DOM 链 + vnode 组件树遍历，很贵），只在换选/强制刷新时重算；
    // 数据实时性不靠轮询——buildInspState 时对 dynComp 的 parentmodelinfo/jsonconfig 及 app $data
    // 注册 $watch，值一变立即重绘（editForm 换 form 引用、表单逐字段修改都能捕获）。
    var inspState = null;
    var liveUnwatch = [];
    var liveStale = false;     // 数据已变但用户正在面板里选文本，暂缓重绘
    var pendingFlash = null;   // {data:[],jc:[],pmi:[]} 下次渲染后高亮的变化键
    var pinned = false;        // 钉住快照：暂停一切自动重绘
    var notesSig = '';
    var traceBuf = [];
    var traceUnsub = null;
    var traceQueued = false;
    var actFilter = '';
    var dragState = null;
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
            if (pendingFlash[part].length < 12 && pendingFlash[part].indexOf(k) < 0)
                pendingFlash[part].push(k);
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
        if (inspState && inspState.el === selectedEl && !force) return inspState;
        teardownLive();
        inspState = null;
        if (!selectedEl) return null;
        var info;
        try { info = describe(selectedEl); } catch (e) { info = { err: e.message, chain: [], vueChain: [] }; }
        if (!info) return null;
        var comps = info.ownerProxy ? findComponentChain(info.ownerProxy, selectedEl) : [];
        var dynComps = comps.filter(function (i) { return componentName(i) === 'DynDynamicCom'; });
        var dynComp = dynComps.length ? dynComps[dynComps.length - 1] : null;
        // 供 Console 使用：最近 DynDynamicCom 代理 + 组件链代理
        global.__selComp = dynComp && dynComp.proxy ? dynComp.proxy : null;
        global.__selComps = comps.map(function (i) { return i.proxy; }).filter(Boolean);
        inspState = { el: selectedEl, info: info, comps: comps, dynComp: dynComp };
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
                if (inspState.info && inspState.info.ownerProxy)
                    inspState._base.data = shallowSig(inspState.info.ownerProxy.$data);
            }
        } catch (e) { }
    }

    function renderInspHtml() {
        if (!inspEl || pinned) return;
        setLiveStale(false);
        inspEl.innerHTML = inspectorSection(inspState);
        applyFlashAndRebase();
        drawParams();   // 参数层 Tab 与查看卡共享同一份选中态，顺带刷新
        drawActions();  // 动作 Tab 同理
    }

    /** DynParams Tab：渲染选中元素所属参数上下文的逐层透视（无选中时给引导） */
    function drawParams() {
        if (!paramsEl) return;
        if (picking) {
            paramsEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">🎯 拾取中…点击页面元素后，这里显示它的 L0–L4 参数层</div></div>';
            return;
        }
        if (!selectedEl || !document.contains(selectedEl)) {
            paramsEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">先用 🎯拾取 选择一个元素，再看它的 DynParams 参数层</div></div>';
            return;
        }
        try {
            var P = global.DynParams ? DynParams.fromEl(selectedEl) : null;
            paramsEl.innerHTML = P ? paramsLayersSection(P)
                : '<div class="dyndb-card"><div class="dyndb-line dyndb-na">DynParams 未加载</div></div>';
        } catch (e) {
            paramsEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-bad">参数层渲染异常: ' + esc(e.message) + '</div></div>';
        }
    }

    // ---------------- 参数变更轨迹（DynParams.onTrace） ----------------
    function pad2(n) { return ('0' + n).slice(-2); }
    function fmtTraceTime(t) { return pad2(t.getHours()) + ':' + pad2(t.getMinutes()) + ':' + pad2(t.getSeconds()); }

    function drawTrace() {
        if (!traceEl) return;
        if (!traceBuf.length) {
            traceEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">'
                + '暂无轨迹：ctx.set（本层覆盖）/ ctx.commit（共享实体）/ fork 传参 都会记录（最近 100 条，新→旧）'
                + '</div></div>';
            return;
        }
        var html = '<div class="dyndb-card"><div class="dyndb-card-h"><b>参数变更轨迹</b>'
            + '<span class="dyndb-id">' + traceBuf.length + ' 条</span></div>';
        traceBuf.slice(0, 60).forEach(function (ev) {
            html += '<div class="dyndb-tr dyndb-tr-' + esc(ev.kind) + '">'
                + '<span class="dyndb-time">' + fmtTraceTime(ev.t) + '</span>'
                + '<span class="dyndb-tag dyndb-act-' + (ev.kind === 'commit' ? 'db' : ev.kind === 'set' ? 'bi' : 'comp') + '">'
                + esc(ev.kind) + '</span>'
                + '<span class="dyndb-tr-ctx">' + esc(ev.ctxId || '') + '</span>'
                + (ev.el ? '<span class="dyndb-id">' + esc(ev.el) + '</span>' : '')
                + '<b class="dyndb-tr-key">' + esc(ev.key || '') + '</b>'
                + '<span class="dyndb-tr-vals"><span class="dyndb-tr-old">' + esc(shortParamVal(ev.old)) + '</span>'
                + ' → <span class="dyndb-val">' + esc(shortParamVal(ev.value)) + '</span></span>'
                + (ev.by ? '<span class="dyndb-pby">by:' + esc(ev.by) + '</span>' : '')
                + (ev.fromCtx ? '<span class="dyndb-id">fork↑' + esc(ev.fromCtx) + '</span>' : '')
                + '</div>';
        });
        html += '</div>';
        traceEl.innerHTML = html;
    }

    function switchTab(tab) {
        if (['view', 'params', 'actions'].indexOf(tab) < 0) return;
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
        if (name !== 'layers' && name !== 'trace') return;
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
        if (picking || !selectedEl || !document.contains(selectedEl)) {
            teardownLive();
            inspState = null;
            global.__selComp = null; global.__selComps = [];
            setLiveStale(false);
            inspEl.innerHTML = inspectorSection(null);
            drawParams();
            drawActions();
            return;
        }
        buildInspState(force);
        renderInspHtml();
    }

    /** 2s 轮询对 inspector 只做存在性兜底（元素被移除/弹窗关闭），数据变化由 $watch 负责 */
    function tickInspector() {
        if (!inspEl || picking || pinned) return;
        if (!selectedEl || !document.contains(selectedEl)) drawInspector(false);
    }

    function drawBlocksNotes() {
        if (!blocksEl || !notesEl || pinned) return;
        var blocks = [];
        try { blocks = [].slice.call(document.querySelectorAll('[data-blk-role]')); } catch (e) { }
        if (!blocksEl.querySelector('.dyndb-blocks-host')) {
            blocksEl.innerHTML = '<div class="dyndb-sec-h" data-b-count></div><div class="dyndb-blocks-host"></div>';
        }
        blocksEl.querySelector('[data-b-count]').textContent = 'Blocks（' + blocks.length + '）';
        var host = blocksEl.querySelector('.dyndb-blocks-host');
        blockCards.forEach(function (rec, el) {
            if (!document.contains(el) || blocks.indexOf(el) < 0) {
                rec.card.remove();
                blockCards.delete(el);
            }
        });
        if (blocks.length) {
            var empty = host.querySelector('.dyndb-empty');
            if (empty) empty.remove();
            blocks.forEach(function (el, i) {
                syncBlockCard(el, i, host);
                var rec = blockCards.get(el);
                var anchor = host.children[i] || null;
                if (rec.card !== anchor) host.insertBefore(rec.card, anchor);
            });
        } else {
            blockCards.forEach(function (rec) { rec.card.remove(); });
            blockCards.clear();
            if (!host.querySelector('.dyndb-empty')) {
                var d = document.createElement('div');
                d.className = 'dyndb-empty';
                d.textContent = '当前页面没有已渲染的 Block';
                host.appendChild(d);
            }
        }
        // Notes 内容签名未变就不重绘（同样保护文本选择）
        var last = notes[notes.length - 1];
        var ns = notes.length + '|' + (last ? last.t.getTime() + '|' + last.msg + '|' + (last.meta.source || '') : '');
        if (ns !== notesSig) {
            notesSig = ns;
            notesEl.innerHTML =
                '<div class="dyndb-sec-h">Notes（' + notes.length + '）</div>'
                + (notes.length ? notes.slice().reverse().map(function (n) {
                    return '<div class="dyndb-note"><span class="dyndb-time">' + fmtTraceTime(n.t) + '</span> '
                        + (n.meta.source ? '<span class="dyndb-src">' + esc(n.meta.source) + '</span> ' : '')
                        + esc(n.msg) + '</div>';
                }).join('') : '<div class="dyndb-empty">暂无告警</div>');
        }
    }

    /** 手动刷新：inspector 强制重算（app 后挂载/状态变化后）+ Blocks/Notes */
    function draw() {
        if (pinned) return;
        drawInspector(true);
        drawBlocksNotes();
    }

    // ============================================================
    //  动作 Tab：元素（含祖先链）→ 动作绑定 → actionhelper + 参数 + 注册表
    // ============================================================
    var ACT_EVENTS = ['click', 'dblclick', 'change', 'select'];
    var ACT_PIPE_ATTR = { click: 'dyn-click', dblclick: 'dyn-dblclick', change: 'dyn-change', select: 'dyn-select' };

    function actionMetaOf(name) {
        try { if (global.dyn && typeof dyn.getMeta === 'function') return dyn.getMeta(name); } catch (e) { }
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
            binds.push({ ev: 'cfg', kind: 'ref', attr: 'data-dyn-action-ref', raw: refId,
                cfg: valid ? cfg : null, parsed: parsed });
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
                binds.forEach(function (b) {
                    if (b.ev !== 'cfg') { b.effective = !effective[b.ev]; effective[b.ev] = 1; }
                });
                groups.push({ el: node, self: node === el, binds: binds });
            }
            if (node.tagName === 'BODY') break;
            node = node.parentElement;
        }
        return groups;
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

    /** 全页扫描：所有自身带动作绑定的元素（调试面板自身除外；隐藏配置块 cfg 不算） */
    function scanPageActionEls() {
        var out = [];
        try {
            var all = document.querySelectorAll('*');
            for (var i = 0; i < all.length; i++) {
                var n = all[i];
                if (panelEl && panelEl.contains(n)) continue;
                if (n.hasAttribute && n.hasAttribute('data-dyn-action-cfg')) continue;
                var binds = nodeOwnBinds(n);
                if (binds.length) out.push({ el: n, visible: elVisible(n), binds: binds });
            }
        } catch (e) { }
        return out;
    }

    function actionStepHtml(st) {
        var meta = actionMetaOf(st.action);
        var html = '<div class="dyndb-astep"><span class="dyndb-pcheck">▸</span>'
            + '<b class="dyndb-aname">' + esc(st.action) + '</b>';
        if (!meta) {
            html += ' <span class="dyndb-tag dyndb-bad-tag" title="dyn.resolveAction 找不到该动作">未注册</span>';
        } else if (meta.Id) {
            html += ' <span class="dyndb-tag dyndb-act-db">DB·' + esc(String(meta.ActionType || 'script')) + '</span>'
                + '<span class="dyndb-id" title="' + esc(meta.Name || st.action) + '">' + esc(meta.Name || st.action) + '</span>';
        } else {
            html += ' <span class="dyndb-tag dyndb-act-bi">内置</span>'
                + (meta.doc ? '<span class="dyndb-id" title="' + esc(meta.doc) + '">ℹ</span>' : '');
        }
        ['$before', '$onSuccess', '$onFail', '$after'].forEach(function (k) {
            if (st.options && st.options[k] !== undefined)
                html += ' <span class="dyndb-tag dyndb-act-comp" title="组合动作钩子">' + k + '</span>';
        });
        if (st.options && Object.keys(st.options).length) {
            html += '<pre class="dyndb-json dyndb-ajson" title="点击折叠/展开">'
                + esc(clip(safeJson(st.options, 1), 900)) + '</pre>';
        }
        html += '</div>';
        return html;
    }

    var pageActEls = [];          // scanPageActionEls 最近一次结果（索引稳定，供 locate/展开引用）
    var pageActExpand = {};       // 索引 -> 是否展开详情
    var pageActFilter = '';

    /** 一个绑定的动作名链 chips（未注册动作红名） */
    function bindChainHtml(b) {
        if (b.kind === 'ref') {
            if (!b.cfg) return '<span class="dyndb-bad">ref #' + esc(b.raw) + ' 缺失</span>';
            return '<span class="dyndb-tag dyndb-act-comp">ref#' + esc(b.raw) + '</span>'
                + '<span class="dyndb-id">隐藏配置</span>';
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
                h += '<div class="dyndb-line dyndb-bad">找不到配置块 #' + esc(b.raw)
                    + '（需带 data-dyn-action-cfg 标记）</div>';
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

    function registryListHtml(q, hostSel) {
        // hostSel 仅用于注释占位；列表内容统一在此生成（过滤输入时复用）
        var rows = [];
        try { rows = (global.dyn && typeof dyn.actionList === 'function') ? dyn.actionList() : []; } catch (e) { }
        var list = rows.filter(function (m) {
            return !q || (m.name || '').toLowerCase().indexOf(q) >= 0
                || (m.label || '').toLowerCase().indexOf(q) >= 0;
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
        pageActEls.forEach(function (item, idx) { if (item.visible) visibleCount++; });
        pageActEls.forEach(function (item, idx) {
            if (q) {
                var hit = elDesc(item.el).toLowerCase().indexOf(q) >= 0
                    || item.binds.some(function (b) {
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

    function drawActions() {
        if (!actionsEl || activeTab !== 'actions') return;
        var regRows = [];
        try { regRows = (global.dyn && typeof dyn.actionList === 'function') ? dyn.actionList() : []; } catch (e) { }

        if (picking) {
            actionsEl.innerHTML = '<div class="dyndb-card"><div class="dyndb-line dyndb-na">🎯 拾取中…清单已暂停刷新</div></div>';
            return;
        }

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
        if (selectedEl && document.contains(selectedEl)) {
            var groups = collectActionBindings(selectedEl);
            html += '<div class="dyndb-card"><div class="dyndb-card-h"><b>拾取元素动作链</b>'
                + '<span class="dyndb-id">' + esc(elDesc(selectedEl)) + '</span>'
                + '<button class="dyndb-btn dyndb-float" data-act="pick">换一个</button></div>';
            if (!groups.length) {
                html += '<div class="dyndb-line dyndb-na">该元素及其祖先上没有动作绑定</div>';
            }
            groups.forEach(function (g) {
                html += '<div class="dyndb-agroup' + (g.self ? ' is-self' : '') + '"><div class="dyndb-agroup-h">'
                    + (g.self ? '<b>本元素</b> ' : '<span class="dyndb-id">祖先 ↑ </span>')
                    + esc(elDesc(g.el)) + '</div>';
                g.binds.forEach(function (b) {
                    html += '<div class="dyndb-abind"><span class="dyndb-tag dyndb-act-ev">' + esc(b.ev) + '</span>'
                        + '<span class="dyndb-id">' + esc(b.kind === 'pipe' ? '管道' : '隐藏配置') + '</span>'
                        + bindChainHtml(b);
                    if (b.ev !== 'cfg')
                        html += b.effective
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
            + '<input class="dyndb-ainput" data-act="act-filter" placeholder="过滤动作名 / 说明…" value="' + esc(actFilter) + '">'
            + '<div data-actlist>' + registryListHtml(actFilter.toLowerCase()) + '</div></div>';
        actionsEl.innerHTML = html;
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
        var payload = { time: new Date().toISOString(), url: location.href };
        if (!selectedEl) { payload.note = '未拾取元素'; return payload; }
        payload.element = elDesc(selectedEl);
        if (inspState) {
            var info = inspState.info;
            payload.teleported = !!info.teleported;
            payload.domChain = info.chain.filter(function (c) { return c.self || c.badges.length; })
                .slice(0, 15).map(function (c) { return { el: c.desc, self: c.self, badges: c.badges }; });
            payload.componentChain = inspState.comps.map(componentName);
            var dc = inspState.dynComp;
            if (dc) {
                var props = dc.props || {};
                try { payload.jsonconfig = snapshot(props.jsonconfig, 0, null, 8); } catch (e) { }
                try { payload.parentmodelinfo = snapshot(props.parentmodelinfo, 0, null, 6); } catch (e) { }
            }
            if (info.ownerProxy) {
                payload.app = {
                    name: appDataInfo(info.ownerProxy, info.ownerRoot).name,
                    data: snapshot(info.ownerProxy.$data || {}, 0, null, 5)
                };
            }
        }
        try {
            var P = global.DynParams && DynParams.fromEl(selectedEl);
            if (P) payload.params = P.inspect();
        } catch (e) { }
        try {
            payload.actions = collectActionBindings(selectedEl).map(function (g) {
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
        var text = safeJson(buildContextPayload(), 2);
        copyText(text).then(function (ok) { flashHeadBtn('copy', ok ? '已复制✓' : '复制失败'); });
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
            panelEl.style.left = ui.left + 'px';
            panelEl.style.top = ui.top + 'px';
            panelEl.style.right = 'auto';
            panelEl.style.bottom = 'auto';
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
            if (!e.altKey || picking) return;
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
            + '.dyndb-panel{position:fixed;right:10px;bottom:10px;width:420px;max-height:60vh;z-index:2147483600;'
            + 'font:12px/1.5 Consolas,Menlo,monospace;background:#1e1e1e;color:#d4d4d4;'
            + 'border:1px solid #454545;border-radius:8px;box-shadow:0 6px 24px rgba(0,0,0,.45);display:flex;flex-direction:column;overflow:hidden}'
            + '.dyndb-head{display:flex;align-items:center;gap:8px;padding:6px 10px;background:#2d2d30;cursor:pointer;user-select:none}'
            + '.dyndb-head b{color:#4ec9b0;letter-spacing:.5px}.dyndb-head .dyndb-spacer{flex:1}'
            + '.dyndb-btn{background:#3c3c3c;color:#ddd;border:1px solid #555;border-radius:4px;padding:1px 8px;cursor:pointer;font:12px Consolas}'
            + '.dyndb-btn:hover{background:#4a4a4a}'
            + '.dyndb-body{overflow:auto;padding:8px 10px}.dyndb-panel.dyndb-collapsed .dyndb-body{display:none}'
            + '.dyndb-tabbar{display:flex;gap:2px;padding:4px 6px 0;background:#252526;border-bottom:1px solid #3a3a3a}'
            + '.dyndb-panel.dyndb-collapsed .dyndb-tabbar{display:none}'
            + '.dyndb-tab{background:transparent;color:#9a9a9a;border:1px solid transparent;border-bottom:none;'
            + 'border-radius:5px 5px 0 0;padding:3px 12px;cursor:pointer;font:12px Consolas}'
            + '.dyndb-tab:hover{color:#ddd;background:#2f2f33}'
            + '.dyndb-tab.is-active{color:#4ec9b0;background:#1e1e1e;border-color:#3a3a3a;font-weight:bold}'
            + '.dyndb-sec-h{color:#569cd6;font-weight:bold;margin:8px 0 4px;border-bottom:1px solid #3a3a3a;padding-bottom:2px}'
            + '.dyndb-live-dot{color:#4ec9b0;font-size:10px;margin-left:6px;font-weight:normal;opacity:.85}'
            + '.dyndb-livebar{margin:0 0 6px;padding:4px 8px;background:#3a2f10;border:1px solid #b58900;'
            + 'border-radius:4px;color:#e6c65a;font-size:11px;cursor:pointer;text-align:center}'
            + '.dyndb-livebar:hover{background:#4a3c14}'
            + '.dyndb-card{border:1px solid #3a3a3a;border-radius:5px;padding:6px 8px;margin-bottom:6px;background:#252526}'
            + '.dyndb-card-h b{color:#dcdcaa}.dyndb-id{color:#808080}'
            + '.dyndb-line{margin:2px 0;word-break:break-all}.dyndb-k{display:inline-block;min-width:78px;color:#9cdcfe}'
            + '.dyndb-val{color:#ce9178}.dyndb-na{color:#6a6a6a}'
            + '.dyndb-layer{padding:0 5px;border-radius:3px;font-size:11px;margin-left:4px}'
            + '.dyndb-l0{background:#4e2a1e;color:#f48771}.dyndb-l1{background:#143d2e;color:#4ec9b0}'
            + '.dyndb-l2{background:#2d2350;color:#c586c0}.dyndb-l34{background:#4a3a10;color:#d7ba7d}'
            + '.dyndb-shared{background:#0e3a4a;color:#569cd6}'
            + '.dyndb-pllegend{margin:4px 0 6px;padding:4px 6px;background:#222;border:1px solid #333;border-radius:4px;'
            + 'font-size:10px;color:#aaa;line-height:2}'
            + '.dyndb-pllegend .dyndb-layer{margin:0 3px 0 8px}'
            + '.dyndb-pkey{margin:5px 0 3px;padding-top:4px;border-top:1px dashed #333}'
            + '.dyndb-pkey-name{color:#dcdcaa;font-weight:bold}'
            + '.dyndb-prow{display:flex;align-items:center;gap:5px;margin:1px 0 1px 8px;font-size:11px;line-height:1.5;border-radius:3px;padding:0 4px}'
            + '.dyndb-prow.is-win{background:#1f3328}'
            + '.dyndb-prow.is-lost{color:#7a7a7a}'
            + '.dyndb-pcheck{width:12px;flex:none;color:#4ec9b0;font-weight:bold}'
            + '.is-lost .dyndb-pcheck{color:#666}'
            + '.dyndb-prow .dyndb-layer{margin-left:0;flex:none;padding:0 4px}'
            + '.dyndb-pval{color:#ce9178;word-break:break-all}'
            + '.is-lost .dyndb-pval{color:#8f8478}'
            + '.dyndb-pwho{margin-left:auto;color:#6a9955;font-size:10px;flex:none;max-width:170px;overflow:hidden;'
            + 'text-overflow:ellipsis;white-space:nowrap}'
            + '.dyndb-pby{color:#c586c0;font-size:10px;flex:none}'
            + '.dyndb-pcovered{color:#999;font-size:10px;border:1px solid #555;border-radius:3px;padding:0 4px;flex:none}'
            + '.dyndb-chain{color:#808080}.dyndb-bad{color:#f48771;font-weight:bold}'
            + '.dyndb-note{border-left:3px solid #f48771;padding:2px 6px;margin:3px 0;background:#2a2020}'
            + '.dyndb-time{color:#808080}.dyndb-src{color:#c586c0}'
            + '.dyndb-empty{color:#6a6a6a;padding:4px 0}'
            /* —— Vue 查看器 —— */
            + '.dyndb-insp{border-color:#2d6e4e;box-shadow:0 0 0 1px #143d2e inset}'
            + '.dyndb-pick{border-color:#b58900;background:#2e2a18}'
            + '.dyndb-float{float:right}'
            + '.dyndb-chain-list{margin:4px 0;border:1px solid #3a3a3a;border-radius:4px;overflow:hidden}'
            + '.dyndb-chain-row{padding:2px 6px;border-bottom:1px solid #2e2e2e}'
            + '.dyndb-chain-row:last-child{border-bottom:none}'
            + '.dyndb-chain-row.is-self{background:#2f3a4d}'
            + '.dyndb-chain-el{color:#9cdcfe;word-break:break-all}'
            + '.dyndb-tag{display:inline-block;padding:0 5px;margin-left:4px;border-radius:3px;font-size:11px;white-space:nowrap}'
            + '.dyndb-tag-app{background:#143d2e;color:#4ec9b0;border:1px solid #2d6e4e}'
            + '.dyndb-tag-blk{background:#3d2e14;color:#d7ba7d;border:1px solid #6e5a2d}'
            + '.dyndb-tag-shell{background:#2d2350;color:#c586c0;border:1px solid #5a3d7a}'
            + '.dyndb-tag-ctx{background:#0e3a4a;color:#569cd6;border:1px solid #1f5a70}'
            + '.dyndb-appname{color:#4ec9b0}'
            + '.dyndb-console-hint{color:#808080;font-size:11px;margin-top:4px}'
            + '.dyndb-comp-chain{margin:3px 0 6px;line-height:2}'
            + '.dyndb-tag-comp{background:#333344;color:#c8c8e0;border:1px solid #555566}'
            + '.dyndb-tag-el{background:#2e2e22;color:#c8c87a;border:1px solid #5a5a33}'
            + '.dyndb-chain-arrow{color:#6a6a6a;margin:0 3px}'
            + '.dyndb-dyncom{border:1px solid #2d6e4e;border-radius:4px;margin:6px 0;padding:5px 7px;background:#1f2b25}'
            + '.dyndb-dyncom-h{color:#4ec9b0;font-weight:bold;margin-bottom:3px}'
            + '.dyndb-dyncom-name{color:#dcdcaa;font-weight:normal}'
            + '.dyndb-json{margin:4px 0 0;padding:6px;max-height:220px;overflow:auto;'
            + 'background:#1a1a1a;border:1px solid #333;border-radius:3px;color:#c0c0c0;'
            + 'font:11px/1.45 Consolas,Menlo,monospace;white-space:pre;word-break:normal;'
            + 'scrollbar-width:thin;scrollbar-color:#4a4a4a #1a1a1a}'
            + '.dyndb-json::-webkit-scrollbar{width:8px;height:8px}'
            + '.dyndb-json::-webkit-scrollbar-track{background:#1a1a1a;border-radius:4px}'
            + '.dyndb-json::-webkit-scrollbar-thumb{background:#444;border-radius:4px;border:1px solid #1a1a1a}'
            + '.dyndb-json::-webkit-scrollbar-thumb:hover{background:#5a5a5a}'
            + '.dyndb-json::-webkit-scrollbar-corner{background:#1a1a1a}'
            + '.dyndb-body{scrollbar-width:thin;scrollbar-color:#4a4a4a #1e1e1e}'
            + '.dyndb-body::-webkit-scrollbar{width:9px}'
            + '.dyndb-body::-webkit-scrollbar-thumb{background:#3f3f3f;border-radius:5px}'
            + '.dyndb-body::-webkit-scrollbar-thumb:hover{background:#555}'
            + '.dyndb-body::-webkit-scrollbar-track{background:#1e1e1e}'
            + '.dyndb-pick-overlay{position:absolute;z-index:2147483599;pointer-events:none;'
            + 'border:2px solid #4ec9b0;background:rgba(78,201,176,.12);border-radius:2px;'
            + 'box-shadow:0 0 0 99999px rgba(0,0,0,.15)}'
            + '.dyndb-panel.dyndb-picking{box-shadow:0 0 0 2px #b58900,0 6px 24px rgba(0,0,0,.45)}'
            + '.dyndb-panel.dyndb-picking *{cursor:crosshair !important}'
            /* —— 面板增强：resize / 钉住 / 折叠 JSON —— */
            + '.dyndb-panel{resize:both;min-width:340px;min-height:180px}'
            + '.dyndb-head{cursor:move}'
            + '.dyndb-head .dyndb-btn{cursor:pointer}'
            + '.dyndb-btn.is-on{background:#143d2e;color:#4ec9b0;border-color:#2d6e4e}'
            + '.dyndb-panel.dyndb-pinned{border-color:#b58900;box-shadow:0 0 0 1px #b58900,0 6px 24px rgba(0,0,0,.45)}'
            + '.dyndb-json.is-collapsed{max-height:34px;overflow:hidden;cursor:pointer;position:relative}'
            + '.dyndb-json{cursor:pointer}'
            + '@keyframes dyndb-flash{0%{background-color:#6b5a10}70%{background-color:#4a3f10}100%{background-color:transparent}}'
            + '.dyndb-flash{animation:dyndb-flash 1.5s ease-out;border-radius:3px}'
            + 'div.dyndb-flash{padding:2px 4px;margin:-2px -4px}'
            + '.dyndb-flash-chip{color:#e6c65a;font-size:10px;margin:2px 0;font-weight:bold}'
            /* —— DynParams 子 Tab（参数层/轨迹） —— */
            + '.dyndb-psubbar{display:flex;align-items:center;gap:4px;margin:2px 0 6px}'
            + '.dyndb-psub{background:#2a2a2e;color:#9a9a9a;border:1px solid #444;border-radius:4px;'
            + 'padding:2px 10px;cursor:pointer;font:11px Consolas}'
            + '.dyndb-psub:hover{color:#ddd;background:#333337}'
            + '.dyndb-psub.is-active{color:#4ec9b0;border-color:#2d6e4e;background:#143d2e}'
            + '.dyndb-psub-clear{margin-left:auto}'
            + '.dyndb-tr{display:flex;align-items:center;gap:6px;padding:2px 4px;margin:2px 0;'
            + 'border-left:3px solid #555;font-size:11px;flex-wrap:wrap}'
            + '.dyndb-tr-set{border-left-color:#f48771}'
            + '.dyndb-tr-commit{border-left-color:#569cd6}'
            + '.dyndb-tr-fork{border-left-color:#c586c0}'
            + '.dyndb-tr-ctx{color:#4ec9b0;flex:none}'
            + '.dyndb-tr-key{color:#dcdcaa;flex:none}'
            + '.dyndb-tr-vals{min-width:0;word-break:break-all}'
            + '.dyndb-tr-old{color:#7a7a7a;text-decoration:line-through}'
            /* —— 动作 Tab —— */
            + '.dyndb-agroup{border:1px solid #3a3a3a;border-radius:4px;margin:6px 0;overflow:hidden}'
            + '.dyndb-agroup.is-self{border-color:#2d6e4e}'
            + '.dyndb-agroup-h{padding:3px 7px;background:#2b2b2f;color:#9cdcfe;font-size:11px}'
            + '.dyndb-abind{padding:3px 7px 1px;background:#26262a;display:flex;align-items:center;gap:6px;flex-wrap:wrap}'
            + '.dyndb-astep{padding:2px 7px 2px 22px;display:flex;align-items:center;gap:6px;flex-wrap:wrap}'
            + '.dyndb-aname{color:#dcdcaa}'
            + '.dyndb-act-ev{background:#0e3a4a;color:#569cd6;border:1px solid #1f5a70;margin-left:0}'
            + '.dyndb-act-bi{background:#143d2e;color:#4ec9b0;border:1px solid #2d6e4e}'
            + '.dyndb-act-db{background:#2d2350;color:#c586c0;border:1px solid #5a3d7a}'
            + '.dyndb-act-comp{background:#4a3a10;color:#d7ba7d;border:1px solid #6e5a2d}'
            + '.dyndb-act-eff{background:#1f3328;color:#4ec9b0;border:1px solid #2d6e4e}'
            + '.dyndb-act-lost{background:#333;color:#888;border:1px solid #555;text-decoration:line-through}'
            + '.dyndb-bad-tag{background:#4e2a1e;color:#f48771;border:1px solid #7a3a2a}'
            + '.dyndb-ajson{margin:2px 0 4px 22px;max-height:150px}'
            /* —— 页面动作元素清单 —— */
            + '.dyndb-prow2{display:flex;align-items:center;gap:6px;padding:3px 6px;margin:3px 0 1px;'
            + 'background:#222936;border:1px solid #33405a;border-radius:4px;cursor:pointer;flex-wrap:wrap}'
            + '.dyndb-prow2:hover{background:#2a3548;border-color:#4ec9b0}'
            + '.dyndb-prow2.is-hidden-el{background:#252525;border-color:#3a3a3a;cursor:default;opacity:.75}'
            + '.dyndb-loc-ico{flex:none;font-size:12px}'
            + '.dyndb-pel{color:#9cdcfe;word-break:break-all;min-width:0}'
            + '.dyndb-aexp{margin-left:auto;flex:none;padding:0 7px;line-height:18px}'
            + '.dyndb-abind.is-dim{opacity:.65}'
            + '.dyndb-achain-name{color:#dcdcaa;margin:0 2px}'
            + '.dyndb-achain-name.is-unreg{color:#f48771;border-bottom:1px dashed #f48771;font-weight:bold}'
            + '.dyndb-pick-overlay.dyndb-locate{border-color:#dcdcaa;background:rgba(220,220,170,.18);'
            + 'box-shadow:0 0 0 99999px rgba(0,0,0,.25),0 0 0 4px rgba(220,220,170,.5);'
            + 'animation:dyndb-locate-pulse .55s ease-in-out 0s 3 alternate;pointer-events:none}'
            + '@keyframes dyndb-locate-pulse{from{opacity:.55}to{opacity:1}}'
            + '.dyndb-ainput{width:calc(100% - 16px);margin:4px 8px;padding:3px 6px;background:#1a1a1a;'
            + 'color:#ddd;border:1px solid #444;border-radius:4px;font:11px Consolas;box-sizing:border-box}'
            + '.dyndb-ainput:focus{outline:none;border-color:#2d6e4e}'
            + '.dyndb-arow{padding:2px 8px;font-size:11px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}'
            + '.dyndb-arow:hover{background:#2a2d33}'
            + '.dyndb-arow b{color:#ce9178;font-weight:normal}';
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
            '<div class="dyndb-head" data-act="toggle"><b>dyn-debug</b>'
            + '<span class="dyndb-spacer"></span>'
            + '<button class="dyndb-btn" data-act="pick">🎯拾取</button>'
            + '<button class="dyndb-btn" data-act="pin" title="钉住当前快照（暂停自动刷新）">📌</button>'
            + '<button class="dyndb-btn" data-act="copy" title="复制完整上下文 JSON（元素链/配置/数据/参数/动作）">📋</button>'
            + '<button class="dyndb-btn" data-act="refresh">刷新</button>'
            + '<button class="dyndb-btn" data-act="clear">清空</button>'
            + '<button class="dyndb-btn" data-act="hide">－</button></div>'
            + '<div class="dyndb-tabbar">'
            + '<button class="dyndb-tab is-active" data-tab="view">App / 组件</button>'
            + '<button class="dyndb-tab" data-tab="params">DynParams</button>'
            + '<button class="dyndb-tab" data-tab="actions">动作</button>'
            + '</div>'
            + '<div class="dyndb-body">'
            + '<div class="dyndb-pane" data-pane="view">'
            + '<div class="dyndb-livebar" data-act="live-refresh" style="display:none">● 数据有更新（正在选择文本，点击刷新）</div>'
            + '<div class="dyndb-insp-slot"></div>'
            + '<div class="dyndb-blocks-slot"></div>'
            + '<div class="dyndb-notes-slot"></div>'
            + '</div>'
            + '<div class="dyndb-pane" data-pane="params" style="display:none">'
            + '<div class="dyndb-psubbar">'
            + '<button class="dyndb-psub is-active" data-psub="layers">参数层</button>'
            + '<button class="dyndb-psub" data-psub="trace">轨迹</button>'
            + '<button class="dyndb-btn dyndb-psub-clear" data-act="clear-trace">清空轨迹</button>'
            + '</div>'
            + '<div class="dyndb-psub-pane" data-psub-pane="layers"><div class="dyndb-params-slot"></div></div>'
            + '<div class="dyndb-psub-pane" data-psub-pane="trace" style="display:none"><div class="dyndb-trace-slot"></div></div>'
            + '</div>'
            + '<div class="dyndb-pane" data-pane="actions" style="display:none">'
            + '<div class="dyndb-actions-slot"></div>'
            + '</div>'
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
            if (act === 'pick') { startPicking(); return; }
            if (act === 'live-refresh') { drawInspector(true); return; }
            if (act === 'locate') {
                var li = +actEl.getAttribute('data-idx');
                locateEl(pageActEls[li] && pageActEls[li].el);
                return;
            }
            if (act === 'aexpand') {
                var ei = +actEl.getAttribute('data-idx');
                pageActExpand[ei] = !pageActExpand[ei];
                drawActions();
                return;
            }
            if (act === 'rescan') { pageActExpand = {}; drawActions(); return; }
            if (act === 'pin') { setPinned(!pinned); return; }
            if (act === 'copy') { copyContext(); return; }
            if (act === 'clear-trace') {
                try { if (global.DynParams && DynParams._clearTrace) DynParams._clearTrace(); } catch (e2) { }
                traceBuf = []; drawTrace();
                return;
            }
            if (act === 'refresh') { if (pinned) setPinned(false); else draw(); return; }
            if (act === 'clear') { DynDebug.clear(); return; }
            if (act === 'hide') { panelEl.style.display = 'none'; remountBtn(); return; }
            if (act === 'toggle' && actEl.classList.contains('dyndb-head')) {
                collapsed = !collapsed;
                panelEl.classList.toggle('dyndb-collapsed', collapsed);
            }
        });
        // 动作页过滤：只刷清单行，输入框不重建（不失焦、不重扫）
        panelEl.addEventListener('input', function (e) {
            if (e.target && e.target.getAttribute && e.target.getAttribute('data-act') === 'actpage-filter') {
                pageActFilter = e.target.value || '';
                var ph = actionsEl && actionsEl.querySelector('[data-pagelist]');
                if (ph) ph.innerHTML = pageListView().html;
                return;
            }
        });
        // 动作注册表过滤：只刷列表，输入框不重建（不失焦）
        panelEl.addEventListener('input', function (e) {
            if (e.target && e.target.getAttribute && e.target.getAttribute('data-act') === 'act-filter') {
                actFilter = e.target.value || '';
                var host = actionsEl && actionsEl.querySelector('[data-actlist]');
                if (!host) return;
                var q = actFilter.toLowerCase();
                var rows = [];
                try { rows = (global.dyn && typeof dyn.actionList === 'function') ? dyn.actionList() : []; } catch (e2) { }
                var list = rows.filter(function (m) {
                    return !q || (m.name || '').toLowerCase().indexOf(q) >= 0
                        || (m.label || '').toLowerCase().indexOf(q) >= 0;
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
                host.innerHTML = lh;
            }
        });

        initPanelUi();

        // 参数轨迹：先拉环形缓冲，再订阅实时事件（打开轨迹子页时 150ms 防抖刷新）
        try { traceBuf = (global.DynParams && DynParams.trace) ? DynParams.trace() : []; } catch (e) { traceBuf = []; }
        if (global.DynParams && DynParams.onTrace) {
            traceUnsub = DynParams.onTrace(function (ev) {
                traceBuf.unshift(ev);
                if (traceBuf.length > 100) traceBuf.pop();
                if (activeTab === 'params' && activePsub === 'trace' && !pinned && !traceQueued) {
                    traceQueued = true;
                    setTimeout(function () { traceQueued = false; drawTrace(); }, 150);
                }
            });
        }

        // 2s 轮询只刷 Blocks/Notes + 元素存活兜底；inspector 的数据变化走 $watch 实时推送
        timer = setInterval(function () {
            if (pinned) return;
            drawBlocksNotes(); tickInspector();
        }, 2000);
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
    boot();
    }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);
