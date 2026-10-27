/* dyn-debug.js V4 调试系统内核（调试系统唯一注册 DynKernel 插件的文件）
 * ------------------------------------------------------------
 * 职责：启用判定 / 全局门面 window.DynDebug / 三个能力模块按需加载 / 面板引导，
 *       并提供各能力模块共用的元素与 Vue 组件解析工具（describe / 组件链 / 上下文快照）。
 * 启用（任一，默认关闭）：window.DYN_LIB_CONFIG.debug === true、localStorage['dyn.debug'] === '1'、
 *   URL 带 dyn_debug=1 或 ?dyn_debug；兼容旧开关 window.DYN_DEBUG / localStorage.DYN_DEBUG / ?dyndebug=1。
 * 能力模块（默认不随基础链加载，DynDebug.load() 按序拉取）：
 *   dyn-debug-probe.js  元素拾取 / 查看卡 / 参数层 / Blocks 卡片
 *   dyn-debug-trace.js  Notes 日志 / 参数变更轨迹 / 动作绑定清单 / 上下文快照
 *   dyn-debug-panel.js  浮层 UI（DOM / 按钮 / Tab / 轮询）
 *
 * window.DynDebug 门面在子模块未就绪时全部安全降级：note 为 no-op、inspect 返回 null、pick 忽略。
 * 模块间共享：本文件创建唯一命名空间 global.DynDebugKit（下称 K），子模块只允许向 K 追加属性，
 *   禁止整体重新赋值 global.DynDebugKit；K.probe / K.trace / K.panel 由对应模块挂载。
 */
(function (global) {
'use strict';

/** 子模块加载顺序：探针 → 轨迹 → 面板（面板运行期依赖前两者的成员） */
var SUB_MODULES = ['dyn-debug-probe.js', 'dyn-debug-trace.js', 'dyn-debug-panel.js'];
var FALLBACK_VERSION = '4.4.4';

/** 全局共享命名空间（函数声明提升，字面量可直接引用） */
var K = global.DynDebugKit = {
    enabled: false,     // 调试启用态：插件 setup 时判定一次
    ready: false,       // 三个能力模块是否已全部就绪
    loading: null,      // 加载 Promise（幂等）
    probe: null,
    trace: null,
    panel: null,
    esc: esc,
    clip: clip,
    safeJson: safeJson,
    snapshot: snapshot,
    elDesc: elDesc,
    shortParamVal: shortParamVal,
    describe: describe,
    appDataInfo: appDataInfo,
    componentName: componentName,
    findComponentChain: findComponentChain,
    vueComponentChain: vueComponentChain,
    rootProxyOf: rootProxyOf,
    allAppRoots: allAppRoots,
    load: load
};

/** HTML 转义（各能力模块拼字符串共用） */
function esc(s) {
    return String(s == null ? '' : s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

/** 截断过长文本，超出部分提示去 Console 取完整值 */
function clip(s, max) {
    s = String(s == null ? '' : s);
    return s.length > max ? s.slice(0, max) + '\n…（已截断，完整值见 Console: __selComp.jsonconfig）' : s;
}

function safeJson(v, indent) {
    try { return JSON.stringify(v, null, indent || 0); } catch (e) { return '[JSON 序列化失败: ' + e.message + ']'; }
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

/** 元素的简洁描述：tag#id.class1.class2 */
function elDesc(el) {
    if (!el || el.nodeType !== 1) return '(非元素)';
    var s = el.tagName.toLowerCase();
    if (el.id) s += '#' + el.id;
    var cls = (el.getAttribute && el.getAttribute('class') || '').trim().split(/\s+/).filter(Boolean);
    if (cls.length) s += '.' + cls.slice(0, 3).join('.');
    return s;
}

/** 参数值的短文本（参数层/轨迹共用，超长截断到 80） */
function shortParamVal(v) {
    if (v === undefined) return 'undefined';
    if (v === null) return 'null';
    var s;
    try { s = typeof v === 'string' ? v : JSON.stringify(v); } catch (e) { s = String(v); }
    if (s == null) s = String(v);
    return s.length > 80 ? s.slice(0, 80) + '…' : s;
}

// ============================================================
//  元素 / Vue 组件解析（探针、轨迹、面板共用）
// ============================================================

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
        nodes = [].slice.call(document.querySelectorAll('[data-dyn-mode="createApp"],[dyn-init]')).filter(function (n) {
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
    var chain = [], ownerApp = null, ownerRoot = null, node = el, guard = 0;
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
            if (findComponentChain(rp, el).length) { ownerApp = rApp; ownerRoot = roots[i]; teleported = true; break; }
        }
    }
    return {
        el: el, ownerApp: ownerApp, ownerProxy: rootProxyOf(ownerApp, ownerRoot), ownerRoot: ownerRoot,
        teleported: teleported, chain: chain, vueChain: vueComponentChain(el)
    };
}

function appDataInfo(proxy, ownerRoot) {
    var info = { name: '?', dataKeys: [], dataSnap: null, computed: [], methods: [], err: null };
    try {
        var optName = proxy.$options && (proxy.$options.name || proxy.$options.__name);
        // 我们的 BlockApp 大多没显式 name：退到根节点的 data-blk-role / data-dyn-mode
        var domName = ownerRoot && (ownerRoot.getAttribute('data-blk-role') || ownerRoot.getAttribute('data-dyn-mode') || '');
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

/**
 * @description 调试启用判定（生产默认关闭）：配置开关 / localStorage / URL 参数
 * @returns {boolean}
 */
function enabled() {
    try {
        if (global.DYN_LIB_CONFIG && global.DYN_LIB_CONFIG.debug === true) return true;
        if (global.localStorage && global.localStorage.getItem('dyn.debug') === '1') return true;
        if (global.DYN_DEBUG === true) return true;     // 兼容旧开关
        if (global.localStorage && global.localStorage.getItem('DYN_DEBUG') === '1') return true;
        var s = global.location && global.location.search || '';
        return /[?&]dyn_debug\b/.test(s) || /[?&]dyndebug=1\b/.test(s);
    } catch (e) { return false; }
}

/**
 * @description 子模块 URL：优先复用 DynLib.script（内容哈希 + 版本号回退），独立页兜底拼 /dyn/
 * @param {string} rel
 * @returns {string}
 */
function scriptUrl(rel) {
    try {
        if (global.DynLib && typeof global.DynLib.script === 'function') return global.DynLib.script(rel);
    } catch (e) { }
    var v = (global.DYN_ASSET_HASHES || {})[rel] || FALLBACK_VERSION;
    var base = '/dyn/';
    try {
        var el = document.currentScript;
        if (el && el.src) base = new URL('.', el.src).href;
    } catch (e) { }
    return base + rel + '?v=' + v;
}

/**
 * @description 动态插入一个脚本并等待其执行完成
 * @param {string} rel 相对 /dyn/ 的模块路径
 * @returns {Promise<boolean>}
 */
function loadScript(rel) {
    return new Promise(function (resolve, reject) {
        var el = document.createElement('script');
        el.src = scriptUrl(rel);
        el.async = false;
        el.onload = function () { resolve(true); };
        el.onerror = function () { reject(new Error('[DynDebug]脚本加载失败:' + rel)); };
        document.head.appendChild(el);
    });
}

/**
 * @description 按需顺序加载三个能力模块（幂等；未启用时直接返回 false，不产生任何请求）
 * @returns {Promise<boolean>}
 */
function load() {
    if (K.loading) return K.loading;
    if (!enabled()) return Promise.resolve(false);
    K.enabled = true;
    DynDebug.enabled = true;
    K.loading = SUB_MODULES.reduce(function (p, rel) {
        return p.then(function () { return loadScript(rel); });
    }, Promise.resolve()).then(function () {
        K.ready = true;
        DynDebug.ready = true;
        return true;
    }, function (err) {
        console.warn('[DynDebug] 调试模块加载失败', err);
        return false;
    });
    return K.loading;
}

/** 对外门面：未加载能力模块时全部为安全空实现，供业务模块无条件调用 */
var DynDebug = {
    enabled: false,
    ready: false,
    /** 上报一条调试记录：msg 文本，meta={source,table,...} */
    note: function (msg, meta) { if (K.trace) K.trace.note(msg, meta); },
    clear: function () { if (K.trace) K.trace.clear(); },
    /** 立即扫描渲染 */
    render: function () { if (K.panel) K.panel.draw(); },
    scheduleRender: function () { if (K.panel) K.panel.scheduleRender(); },
    /** 进入拾取模式（下一次点击的元素成为查看目标；Esc 取消） */
    pick: function () { if (K.probe) K.probe.startPicking(); },
    cancelPick: function () { if (K.probe) K.probe.stopPicking(); },
    /** 直接查看指定元素：返回 {el, ownerApp, chain, vueChain} 描述对象（控制台友好） */
    inspect: function (el) { return K.probe ? K.probe.inspect(el) : null; },
    /** 加载三个能力模块（生产默认不调用） */
    load: load
};
global.DynDebug = DynDebug;

// ---------------------- 插件壳（全调试系统唯一注册 DynKernel 的地方） ----------------------
var __plugin = {
    name: 'debug', stage: 'debug', requires: ['dyn'],
    setup: function (ctx) {
        K.enabled = enabled();
        DynDebug.enabled = K.enabled;
        global.DynDebug = DynDebug;
        if (!K.enabled) return;
        load();
    }
};
if (global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__ = global.__DYN_KERNEL_PENDING__ || []).push(__plugin);
})(window);
