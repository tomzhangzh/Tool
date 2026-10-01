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

    function blockSection(el, idx) {
        var role = el.getAttribute('data-blk-role') || ('block#' + idx);
        var h = el.__dynBlock;
        var intro = h && h.introspect ? h.introspect() : null;
        var html = '<div class="dyndb-card">';
        html += '<div class="dyndb-card-h"><b>' + esc(role) + '</b>'
            + (el.id ? ' <span class="dyndb-id">#' + esc(el.id) + '</span>' : '')
            + (intro && intro.destroyed ? ' <span class="dyndb-bad">destroyed</span>' : '')
            + (!h ? ' <span class="dyndb-bad">未挂载 __dynBlock</span>' : '')
            + '</div>';

        if (intro) {
            html += '<div class="dyndb-line"><span class="dyndb-k">cmds</span> '
                + (intro.commands.length ? intro.commands.map(esc).join(', ') : '<span class="dyndb-na">（无）</span>') + '</div>';
            if (intro.events.length)
                html += '<div class="dyndb-line"><span class="dyndb-k">events</span> ' + intro.events.map(esc).join(', ') + '</div>';
        }

        // 关键参数：值 + 获胜层
        try {
            var P = global.DynParams && DynParams.fromEl(el);
            if (P) {
                var insp = P.inspect(WATCH_KEYS);
                var rows = WATCH_KEYS.filter(function (k) { return insp[k] && insp[k].value !== undefined; });
                if (rows.length) {
                    html += '<div class="dyndb-params">';
                    rows.forEach(function (k) {
                        var r = insp[k];
                        html += '<div class="dyndb-line"><span class="dyndb-k">' + esc(k) + '</span> '
                            + fmtVal(r.value)
                            + ' <span class="dyndb-layer ' + layerClass(r.layer) + '">' + esc(r.layer || '?') + '</span></div>';
                    });
                    html += '</div>';
                }
                // 链拓扑：从本 ctx 向上
                var chain = [];
                var c = P;
                var guard = 0;
                while (c && guard++ < 20) {
                    var nLocal = Object.keys(c.local || {}).length;
                    var nShared = Object.keys(c.shared || {}).length;
                    var tag = c.id;
                    if (nLocal) tag += ' [L1×' + nLocal + ']';
                    if (nShared) tag += ' [sh×' + nShared + ']';
                    chain.push(tag);
                    c = c.parent;
                }
                html += '<div class="dyndb-line dyndb-chain"><span class="dyndb-k">ctx链</span> '
                    + esc(chain.join(' → ')) + '</div>';
            }
        } catch (e) {
            html += '<div class="dyndb-line dyndb-bad">inspect 异常: ' + esc(e.message) + '</div>';
        }
        html += '</div>';
        return html;
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
                '[data-dyn-mode="createApp"],[data-dyn-init-createapp]'
            ));
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

        // —— DynDynamicCom 的两个关键 props：jsonconfig（组件配置树）/ parentmodelinfo（父模型） ——
        if (dynComp) {
            var props = dynComp.props || {};
            var jc = null, pmi = null;
            try { jc = snapshot(props.jsonconfig, 0, null, 6); } catch (e) { jc = { err: e.message }; }
            try { pmi = snapshot(props.parentmodelinfo, 0, null, 4); } catch (e) { pmi = { err: e.message }; }

            if (jc) {
                var jcComp = jc.component || '(空)', jcModel = jc.modelname || '';
                var jcKeys = jc && typeof jc === 'object' ? Object.keys(jc).slice(0, 12) : [];
                html += '<div class="dyndb-dyncom"><div class="dyndb-dyncom-h">jsonconfig'
                    + ' <span class="dyndb-dyncom-name">' + esc(String(jcComp)) + '</span>'
                    + (jcModel ? ' <span class="dyndb-id">model=' + esc(jcModel) + '</span>' : '') + '</div>';
                html += '<div class="dyndb-line dyndb-na">键: ' + esc(jcKeys.join(', ')) + '</div>';
                var jcStr = safeJson(jc, 1);
                html += '<pre class="dyndb-json">' + esc(clip(jcStr, 3000)) + '</pre></div>';
            }
            if (pmi !== null && typeof pmi !== 'undefined') {
                html += '<div class="dyndb-dyncom"><div class="dyndb-dyncom-h">parentmodelinfo</div>';
                var pmiStr = safeJson(pmi, 1);
                html += '<pre class="dyndb-json">' + esc(clip(pmiStr, 1800)) + '</pre></div>';
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
                    html += '<div class="dyndb-line"><span class="dyndb-k">' + esc(k) + '</span> '
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
        picking = true;
        hoverEl = null;
        if (panelEl) panelEl.classList.add('dyndb-picking');
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
    var inspEl = null, blocksEl = null, notesEl = null, liveBarEl = null;
    // inspState：结构性缓存（describe DOM 链 + vnode 组件树遍历，很贵），只在换选/强制刷新时重算；
    // 数据实时性不靠轮询——buildInspState 时对 dynComp 的 parentmodelinfo/jsonconfig 及 app $data
    // 注册 $watch，值一变立即重绘（editForm 换 form 引用、表单逐字段修改都能捕获）。
    var inspState = null;
    var liveUnwatch = [];
    var liveStale = false;   // 数据已变但用户正在卡片里选文本，暂缓重绘

    function teardownLive() {
        liveUnwatch.forEach(function (off) { try { off(); } catch (e) { } });
        liveUnwatch = [];
    }

    function selectionInInsp() {
        var s = document.getSelection();
        return !!(s && !s.isCollapsed && s.anchorNode && inspEl && inspEl.contains(s.anchorNode));
    }

    function setLiveStale(v) {
        liveStale = v;
        if (liveBarEl) liveBarEl.style.display = v ? 'block' : 'none';
    }

    /** 数据被 watch 回调触发：没在选文本就立即重绘；正在选则挂"有更新"提示，选择结束自动刷 */
    function onLiveDataChange() {
        if (selectionInInsp()) { setLiveStale(true); return; }
        renderInspHtml();
    }

    /** 订阅选中组件/所属 App 的响应式变化（只在状态构建时注册一次，随换选/卸载注销） */
    function subscribeLive(state) {
        teardownLive();
        try {
            var comp = state.dynComp && state.dynComp.proxy;
            if (comp && typeof comp.$watch === 'function') {
                liveUnwatch.push(comp.$watch(function () { return comp.props.parentmodelinfo; }, onLiveDataChange, { deep: true }));
                liveUnwatch.push(comp.$watch(function () { return comp.props.jsonconfig; }, onLiveDataChange, { deep: true }));
            }
            var root = state.info.ownerProxy;
            if (root && typeof root.$watch === 'function') {
                var keys = [];
                try { keys = Object.keys(root.$data || {}).slice(0, 30); } catch (e) { }
                keys.forEach(function (k) {
                    liveUnwatch.push(root.$watch(function () { return root.$data[k]; }, onLiveDataChange));
                });
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

    function renderInspHtml() {
        if (!inspEl) return;
        setLiveStale(false);
        inspEl.innerHTML = inspectorSection(inspState);
    }

    function drawInspector(force) {
        if (!inspEl) return;
        if (picking || !selectedEl || !document.contains(selectedEl)) {
            teardownLive();
            inspState = null;
            global.__selComp = null; global.__selComps = [];
            setLiveStale(false);
            inspEl.innerHTML = inspectorSection(null);
            return;
        }
        buildInspState(force);
        renderInspHtml();
    }

    /** 2s 轮询对 inspector 只做存在性兜底（元素被移除/弹窗关闭），数据变化由 $watch 负责 */
    function tickInspector() {
        if (!inspEl || picking) return;
        if (!selectedEl || !document.contains(selectedEl)) drawInspector(false);
    }

    function drawBlocksNotes() {
        if (!blocksEl || !notesEl) return;
        var blocks = [];
        try { blocks = [].slice.call(document.querySelectorAll('[data-blk-role]')); } catch (e) { }
        blocksEl.innerHTML =
            '<div class="dyndb-sec-h">Blocks（' + blocks.length + '）</div>'
            + (blocks.length ? blocks.map(blockSection).join('')
                : '<div class="dyndb-empty">当前页面没有已渲染的 Block</div>');
        notesEl.innerHTML =
            '<div class="dyndb-sec-h">Notes（' + notes.length + '）</div>'
            + (notes.length ? notes.slice().reverse().map(function (n) {
                var hh = ('0' + n.t.getHours()).slice(-2), mm = ('0' + n.t.getMinutes()).slice(-2), ss = ('0' + n.t.getSeconds()).slice(-2);
                return '<div class="dyndb-note"><span class="dyndb-time">' + hh + ':' + mm + ':' + ss + '</span> '
                    + (n.meta.source ? '<span class="dyndb-src">' + esc(n.meta.source) + '</span> ' : '')
                    + esc(n.msg) + '</div>';
            }).join('') : '<div class="dyndb-empty">暂无告警</div>');
    }

    /** 手动刷新：inspector 强制重算（app 后挂载/状态变化后）+ Blocks/Notes */
    function draw() {
        drawInspector(true);
        drawBlocksNotes();
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
            + '.dyndb-panel.dyndb-picking *{cursor:crosshair !important}';
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
            + '<button class="dyndb-btn" data-act="refresh">刷新</button>'
            + '<button class="dyndb-btn" data-act="clear">清空</button>'
            + '<button class="dyndb-btn" data-act="hide">－</button></div>'
            + '<div class="dyndb-body">'
            + '<div class="dyndb-livebar" data-act="live-refresh" style="display:none">● 数据有更新（正在选择文本，点击刷新）</div>'
            + '<div class="dyndb-insp-slot"></div>'
            + '<div class="dyndb-blocks-slot"></div>'
            + '<div class="dyndb-notes-slot"></div>'
            + '</div>';
        document.body.appendChild(panelEl);
        bodyEl = panelEl.querySelector('.dyndb-body');
        liveBarEl = panelEl.querySelector('.dyndb-livebar');
        inspEl = panelEl.querySelector('.dyndb-insp-slot');
        blocksEl = panelEl.querySelector('.dyndb-blocks-slot');
        notesEl = panelEl.querySelector('.dyndb-notes-slot');

        panelEl.addEventListener('click', function (e) {
            var act = e.target && e.target.getAttribute && e.target.getAttribute('data-act');
            if (act === 'pick') { startPicking(); return; }
            if (act === 'live-refresh') { drawInspector(true); return; }
            if (act === 'refresh') { draw(); return; }
            if (act === 'clear') { DynDebug.clear(); return; }
            if (act === 'hide') { panelEl.style.display = 'none'; remountBtn(); return; }
            if (act === 'toggle' && e.target.classList.contains('dyndb-head')) {
                collapsed = !collapsed;
                panelEl.classList.toggle('dyndb-collapsed', collapsed);
            }
        });

        // 2s 轮询只刷 Blocks/Notes + 元素存活兜底；inspector 的数据变化走 $watch 实时推送
        timer = setInterval(function () { drawBlocksNotes(); tickInspector(); }, 2000);
        // 选中文本期间数据有更新会挂起刷新；选择一结束（复制完）自动补刷到最新
        document.addEventListener('selectionchange', function () {
            if (liveStale && !selectionInInsp()) renderInspHtml();
        });
        draw();
        console.info('%c[dyn-debug] 已启用：?dyndebug=1（Block 参数获胜层/告警浮层）', 'color:#4ec9b0');
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
})(window);
