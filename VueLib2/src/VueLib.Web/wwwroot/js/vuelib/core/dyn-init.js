/**
 * VueLib core/dyn-init：声明式属性驱动的独立 Vue 应用管理
 *
 * 特性：
 *  - <div dyn-init='{...}' data-dyn-url="/api/model"> 自动创建独立 Vue 应用
 *  - 多 App 嵌套：maskNested 机制（父挂载前把子 [dyn-init] 挪到隐藏容器占位，挂载完再初始化子）
 *  - model 开放：el.__dynApp / el.__dynModel，可通过 VueLib.dyn.getModel(el) 取得
 *  - 递归 unmount：销毁父容器时级联销毁所有后代 dyn-init，并清理 holder 碎片
 *  - 支持 JS 手动 createElement + appendChild + dyn.mount(dom) 动态创建
 *
 * ⚠️ 边界：dyn-init 只能出现在完全由原生 DOM 管理的区域；严禁放进
 *    VueLib 运行时（NDynamicCom）虚拟 DOM 管控的模板内部，否则节点会被 Vue 回收。
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;

    var holderId = 'vl-dyn-holder';
    var holderEl = null;

    function getHolder() {
        if (!holderEl) {
            holderEl = document.getElementById(holderId);
            if (!holderEl) {
                holderEl = document.createElement('div');
                holderEl.id = holderId;
                holderEl.style.cssText = 'display:none !important;';
                document.body.appendChild(holderEl);
            }
        }
        return holderEl;
    }

    /* ================= 嵌套隔离 maskNested ================= */
    /**
     * 挂载 el 的 Vue 应用前调用：扫描 el 下所有后代 [dyn-init]，
     * 移动到隐藏 holder 并留下 <dyn-host data-dyn-uid> 占位；返回还原函数。
     */
    function maskNested(el) {
        var hosts = [];
        var all = el.querySelectorAll('[dyn-init]');
        var holder = getHolder();
        for (var i = 0; i < all.length; i++) {
            var child = all[i];
            if (child === el) continue;
            var uid = 'dyn_' + Utils.uid();
            child.setAttribute('data-dyn-uid', uid);
            var host = document.createElement('dyn-host');
            host.setAttribute('data-dyn-uid', uid);
            child.parentNode && child.parentNode.replaceChild(host, child);
            holder.appendChild(child);
            hosts.push({ host: host, child: child, uid: uid });
        }
        return function restore() {
            hosts.forEach(function (h) {
                if (h.host.parentNode) {
                    h.host.parentNode.replaceChild(h.child, h.host);
                    h.host.remove();
                } else {
                    holder.appendChild(h.child);
                }
            });
            hosts = [];
        };
    }

    /** 清理 holder 中属于 el 子树（data-dyn-uid 匹配）的废弃碎片 */
    function cleanupHolder(el) {
        var holder = holderEl;
        if (!holder) return;
        var ownerUids = {};
        if (el) {
            var nodes = el.querySelectorAll('[data-dyn-uid]');
            for (var i = 0; i < nodes.length; i++) ownerUids[nodes[i].getAttribute('data-dyn-uid')] = true;
        }
        var frags = holder.querySelectorAll('[data-dyn-uid]');
        for (var j = frags.length - 1; j >= 0; j--) {
            var frag = frags[j];
            var uid = frag.getAttribute('data-dyn-uid');
            if (el && ownerUids[uid]) { frag.remove(); continue; }
            // 无主碎片：其占位 host 已不存在时清理
            if (!document.querySelector('dyn-host[data-dyn-uid="' + uid + '"]') &&
                !document.querySelector('[data-dyn-uid="' + uid + '"]:not(#' + holderId + ')')) {
                frag.remove();
            }
        }
    }

    /* ================= mount / unmount ================= */

    /**
     * 挂载一个 dyn-init 元素为独立 Vue 应用
     * @param {HTMLElement} el 带 dyn-init 属性的元素
     * @param {object} [options] { model: 初始模型 }
     */
    function mount(el, options) {
        el = Utils.resolveEl(el);
        if (!el) return null;
        if (el.__dynApp) { unmount(el); }

        var attrCfg = Utils.parseAttrJson(el, 'dyn-init') || {};
        var url = el.getAttribute('data-dyn-url');

        var restore = maskNested(el);   // 先把后代 dyn-init 挪走

        var opts = Utils.deepMerge({}, attrCfg, options || {});

        function doMount(modelData) {
            var model = Vue.reactive(modelData || opts.model || {});
            // 模板 = 元素当前 innerHTML（Vue 模板语法 + dyn-click-* 属性）
            var template = el.innerHTML || '<div></div>';
            var app = Vue.createApp({
                template: template,
                data: function () { return { model: model }; },
                methods: {
                    /** 模板内可直接调用：@click="runChain({steps:[...]})" */
                    runChain: function (chain, extra) {
                        return window.VueLib.actions.run(chain || [], { model: model, el: el }, extra);
                    }
                }
            });
            // 继承设计器/全局注入（如有）
            if (window.VueLib && window.VueLib._appContext) {
                // 与 VueLib 主应用共享全局组件（ElementPlus 等）
                app.config.globalProperties = window.VueLib._appContext.appContext.config.globalProperties;
            } else if (window.ElementPlus) {
                app.use(window.ElementPlus);
            }
            app.mount(el);
            el.__dynApp = app;          // 开放：实例
            el.__dynModel = model;      // 开放：响应式 model
            el.__dynUrl = url || null;

            restore();                  // 还原后代 DOM
            // 递归挂载后代 dyn-init
            var descendants = el.querySelectorAll('[dyn-init]');
            for (var i = 0; i < descendants.length; i++) mount(descendants[i]);

            if (window.VueLib.debug) {
                window.VueLib.debug.log('dyn-init', 'mounted', el, 'model:', model);
            }
        }

        if (url) {
            fetch(url).then(function (r) { return r.json(); }).then(function (json) {
                var data = json && json.success === false ? json : (json && json.data ? json.data : json);
                doMount(Utils.deepMerge({}, opts.model || {}, data || {}));
            }).catch(function (e) {
                console.error('[VueLib.dyn] data-dyn-url 加载失败', url, e);
                doMount(opts.model || {});
            });
        } else {
            doMount(opts.model || {});
        }
        return el;
    }

    /**
     * 销毁 dyn-init 应用（递归销毁所有后代 dyn-init，清理 holder 碎片）
     */
    function unmount(el) {
        el = Utils.resolveEl(el);
        if (!el) return;
        // 先递归销毁后代
        var children = el.querySelectorAll('[dyn-init]');
        for (var i = children.length - 1; i >= 0; i--) unmount(children[i]);
        if (el.__dynApp) {
            try { el.__dynApp.unmount(); } catch (e) { /* 忽略 */ }
            el.__dynApp = null;
            el.__dynModel = null;
            el.__dynUrl = null;
        }
        cleanupHolder(el);
        window.VueLib.debug && window.VueLib.debug.log('dyn-init', 'unmounted', el);
    }

    /* ================= 刷新 / 局部更新 ================= */

    /**
     * 重新渲染 dyn-init 元素（重新拉取 data-dyn-url + 重新挂载）
     */
    function reload(el) {
        el = Utils.resolveEl(el);
        if (!el) return null;
        unmount(el);
        return mount(el);
    }

    /**
     * 局部更新：把 html 或 url 内容灌入目标元素。
     *  - url 模式：fetch 后取响应文本
     *  - 不创建 Vue 应用（适合窗口内容、Grid3 分部视图；内容里的 dyn-click-* 由全局委托处理）
     */
    function updateEl(el, urlOrHtml, params) {
        el = Utils.resolveEl(el);
        if (!el) return Promise.resolve(el);
        var url = urlOrHtml;
        var html = urlOrHtml;
        if (typeof urlOrHtml === 'string' && (urlOrHtml.indexOf('<') < 0 || urlOrHtml.indexOf('http') === 0 || urlOrHtml.indexOf('/') === 0)) {
            html = null;
            var q = params ? (url.indexOf('?') >= 0 ? '&' : '?') + Utils.toQuery(params) : '';
            return fetch(url + q).then(function (r) { return r.text(); }).then(function (text) {
                el.innerHTML = text;
                if (window.VueLib.debug && el.getAttribute(window.VueLibConfig.debugAttr) != null) {
                    window.VueLib.debug.inspectEl(el);
                }
                return el;
            }).catch(function (e) {
                console.error('[VueLib.dyn] updateEl 失败', url, e);
                return el;
            });
        }
        el.innerHTML = html;
        return Promise.resolve(el);
    }

    /**
     * render：销毁旧实例 → 灌入新内容 → 重新挂载
     * @param {HTMLElement|string} el
     * @param {string} html 或 url（url 以 http 或 / 开头）
     */
    function render(el, htmlOrUrl) {
        el = Utils.resolveEl(el);
        if (!el) return null;
        unmount(el);
        if (typeof htmlOrUrl === 'string' && (htmlOrUrl.indexOf('<') < 0)) {
            return fetch(htmlOrUrl).then(function (r) { return r.text(); }).then(function (t) {
                el.innerHTML = t;
                return mount(el);
            });
        }
        el.innerHTML = htmlOrUrl;
        return mount(el);
    }

    /* ================= 初始化扫描 ================= */
    function init(rootEl) {
        var root = rootEl ? Utils.resolveEl(rootEl) : document;
        if (!root) return 0;
        var nodes = root.querySelectorAll('[dyn-init]');
        var count = 0;
        for (var i = 0; i < nodes.length; i++) {
            // 跳过已被外层 mount 处理过的（data-dyn-uid 表示已进入嵌套隔离流程）
            if (nodes[i].getAttribute('data-dyn-uid')) continue;
            mount(nodes[i]);
            count++;
        }
        return count;
    }

    function initAll() { return init(document); }

    /* ================= model 开放 ================= */

    /**
     * 取得最近 dyn-init 容器的响应式 model（向上查找）
     */
    function getModel(el) {
        el = Utils.resolveEl(el);
        while (el && el !== document) {
            if (el.__dynModel) return el.__dynModel;
            if (window.VueLib.runtime && el.__vlRuntimeRoot) return window.VueLib.runtime.currentModel;
            el = el.parentNode;
        }
        if (window.VueLib.runtime) return window.VueLib.runtime.currentModel;
        return null;
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.dyn = {
        mount: mount,
        unmount: unmount,
        reload: reload,
        render: render,
        updateEl: updateEl,
        init: init,
        initAll: initAll,
        getModel: getModel,
        _getHolder: getHolder,
        _cleanupHolder: cleanupHolder
    };
})();
