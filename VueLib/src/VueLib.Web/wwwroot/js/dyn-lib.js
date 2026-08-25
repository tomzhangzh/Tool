/* ============================================================================
 * dyn-lib.js  —— 属性驱动的动态 UI jsLib（Vue3 UMD + jQuery + lodash + Element Plus）
 * ----------------------------------------------------------------------------
 * 一句话：把"服务器渲染的 HTML + 属性标记"变成"可交互的 Vue 应用"，并让局部
 * 更新（postback 返回分部视图 → 渲染回容器）像全页刷新一样简单，同时防止内存泄漏。
 *
 * 核心属性：
 *   dyn-init='{...}'                         把该容器初始化为一个 Vue 应用/组件，
 *                                            Model 取属性里的 JSON（或容器内
 *                                           <script type="application/json" tag="dynmodel"> 的值）。
 *                                            app 对象写入 $(el).data("app")，也可用 dyn.getApp(el) 取得。
 *                                            支持嵌套：应用内再放 dyn-init 不会冲突。
 *   data-dyn-url="/xxx"                      该容器的“数据源”：首次挂载(空)或刷新时，
 *                                            POST 当前 Model 到该地址，把返回的分部视图
 *                                            注入容器并初始化。这是快速创建 UI 的入口。
 *   dyn-click-postback='{url,resetPage,confirm,close,reload,message}'
 *                                            点击时把“当前元素最近的 dyn-init 祖先”的 Model
 *                                            序列化 POST 给后端；后端执行后返回分部视图 HTML
 *                                            → 渲染回该祖先的 dyn-init；若返回 JSON → 合并进 Model。
 *                                            元素上的 data-* 属性会合并进请求参数（如 :data-id）。
 *   dyn-click-open='{url,params,title,width}' 打开模态框并加载分部视图（内部可再含 dyn-init）。
 *   dyn-click-close                          关闭最近的模态框。
 *   dyn-click-reload[="选择器"]               重新加载 data-dyn-url 容器。
 *
 * 组件配置脚本：容器内的 <script tag="dynconfig"> 不会被浏览器执行，
 * 由本库读取并注入 Vue setup（可返回方法/计算属性等）。
 * ============================================================================ */
(function (global) {
    'use strict';

    var Vue = global.Vue;
    if (!Vue) { console.error('[dyn-lib] 请先引入 Vue3 UMD（vue.global.js）'); return; }
    var $ = global.jQuery;

    var _uidSeq = 0;
    var _holder = null;
    var _appMap = (typeof WeakMap !== 'undefined') ? new WeakMap() : null;

    /* ---------------- 工具 ---------------- */

    function resolve(target) {
        if (!target) return null;
        if (typeof target === 'string') return document.querySelector(target);
        if (target.jquery) return target.get(0);
        if (target.nodeType === 1) return target;
        return null;
    }

    function deepClone(o) {
        try { return JSON.parse(JSON.stringify(o)); } catch (e) { return {}; }
    }

    function parseModel(el) {
        var attr = el.getAttribute('dyn-init');
        if (attr && attr.trim()) {
            attr = attr.trim();
            if (attr.charAt(0) === '#') {           // dyn-init="#selector" → 读取 script[type=application/json]
                var node = document.querySelector(attr);
                if (node) { try { return JSON.parse(node.textContent); } catch (e) { } }
                return {};
            }
            try { return JSON.parse(attr); } catch (e) {
                console.error('[dyn-lib] dyn-init 不是合法 JSON：', attr);
                return {};
            }
        }
        // 空属性：容器内若有 <script type="application/json" tag="dynmodel"> 则由其提供 Model
        return null;
    }

    function readModelScript(el) {
        var s = el.querySelector('script[type="application/json"][tag="dynmodel"]');
        if (s) { try { return JSON.parse(s.textContent); } catch (e) { console.error('[dyn-lib] dynmodel 解析失败', e); } }
        return null;
    }

    function fetchPartial(url, params, type) {
        type = type || 'POST';
        params = params || {};
        return new Promise(function (resolvePromise, reject) {
            $.ajax({
                url: url,
                type: type,
                data: type === 'GET' ? $.param(params) : JSON.stringify(params),
                contentType: type === 'GET' ? undefined : 'application/json',
                dataType: 'html'
            }).done(function (res) { resolvePromise(res); })
              .fail(function (xhr) { reject(new Error(extractError(xhr))); });
        });
    }

    function extractError(xhr) {
        try { if (xhr.responseJSON && xhr.responseJSON.Message) return xhr.responseJSON.Message; } catch (e) { }
        return 'HTTP ' + (xhr.status || 0);
    }

    function showMessage(msg, type) {
        if (!msg) return;
        try {
            if (global.ElementPlus && ElementPlus.ElMessage) {
                if (type === 'error') ElementPlus.ElMessage.error(msg);
                else ElementPlus.ElMessage.success(msg);
                return;
            }
        } catch (e) { }
        (type === 'error' ? console.error : console.log)('[dyn-lib] ' + msg);
    }

    function confirmAsync(msg) {
        if (global.ElementPlus && ElementPlus.ElMessageBox) {
            return ElementPlus.ElMessageBox.confirm(msg, '提示', {
                type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消'
            }).then(function () { return true; }).catch(function () { return false; });
        }
        return Promise.resolve(global.confirm(msg));
    }

    /* ---------------- app 存储：WeakMap + $(el).data("app") ---------------- */

    function storeApp(el, app) {
        if (_appMap) _appMap.set(el, app);
        if ($) { try { $(el).data('app', app); } catch (e) { } }
    }
    function getAppByEl(el) {
        if (_appMap && _appMap.has(el)) return _appMap.get(el);
        if ($) { try { var d = $(el).data('app'); if (d) return d; } catch (e) { } }
        return null;
    }
    function removeApp(el) {
        if (_appMap) _appMap.delete(el);
        if ($) { try { $(el).removeData('app'); } catch (e) { } }
    }

    /* ---------------- 嵌套遮蔽：把子 dyn-init 脱离子树，避免与外层 Vue 冲突 ---------------- */

    function holderEl() {
        if (!_holder) {
            _holder = document.createElement('div');
            _holder.id = 'dyn-holder';
            _holder.style.display = 'none';
            document.body.appendChild(_holder);
        }
        return _holder;
    }

    function maskNested(el, out) {
        if (!el.querySelectorAll) return;
        var nested = [].slice.call(el.querySelectorAll('[dyn-init]'));
        nested.forEach(function (child) {
            if (child.__dynApp || child.__dynMounting) return; // 已由上层处理
            var uid = child.getAttribute('data-dyn-uid') || ('dyn' + (++_uidSeq));
            child.setAttribute('data-dyn-uid', uid);
            var host = document.createElement('dyn-host');     // 占位：外层模板只渲染一个空的自定义元素
            host.setAttribute('data-dyn-uid', uid);
            child.parentNode.insertBefore(host, child);
            holderEl().appendChild(child);                     // 脱离外层模板编译范围
            out.push({ child: child, host: host, uid: uid });
        });
    }

    /* ---------------- 挂载 / 卸载 ---------------- */

    function mountCore(el) {
        if (el.__dynApp) return el.__dynApp;

        // 1) 读取组件配置脚本（tag="dynconfig"），随后移除所有 script
        var cfg = null;
        var cfgScript = el.querySelector('script[tag="dynconfig"]');
        if (cfgScript) {
            try {
                cfg = new Function('element', 'dyn',
                    (cfgScript.textContent || '') + '\n; return typeof dynConfig !== "undefined" ? dynConfig : null;')(el, dyn);
            } catch (e) { console.error('[dyn-lib] dynconfig 执行失败', e); }
        }

        // 2) 解析 Model：优先容器内 dynmodel 脚本，其次 dyn-init 属性
        //    注意：必须先读 dynmodel 再移除脚本，否则会把数据源一起删掉，Model 变空。
        var model = readModelScript(el) || parseModel(el) || {};
        $(el).find('script').remove();

        // 3) 遮蔽嵌套 dyn-init（先于读取模板，外层不会编译到子应用内容）
        var nested = [];
        maskNested(el, nested);

        // 4) 组装组件：模板 = 容器现有 innerHTML；Model 响应式暴露为 model
        var component = {
            template: el.innerHTML,
            data: function () { return {}; },
            setup: function () {
                var m = Vue.reactive(model);
                var exposed = { model: m, element: el, dyn: dyn };
                if (cfg && typeof cfg.setup === 'function') {
                    var extra = cfg.setup({ model: m, element: el }) || {};
                    Object.keys(extra).forEach(function (k) {
                        if (k !== 'model') exposed[k] = extra[k];
                    });
                }
                return exposed;
            }
        };
        if (cfg) {
            ['data', 'computed', 'methods', 'watch', 'created', 'beforeMount', 'mounted',
             'updated', 'beforeUnmount', 'unmounted'].forEach(function (k) {
                if (cfg[k]) component[k] = cfg[k];
            });
            Object.keys(cfg).forEach(function (k) {
                if (!(k in component) && k !== 'setup' && k !== 'template') component[k] = cfg[k];
            });
        }

        // 5) 创建并挂载
        var app = Vue.createApp(component);
        if (global.ElementPlus) app.use(ElementPlus);
        if (global.ElementPlusIconsVue) {
            Object.keys(global.ElementPlusIconsVue).forEach(function (k) { app.component(k, global.ElementPlusIconsVue[k]); });
        }
        app.config.globalProperties.$dyn = dyn;
        el.__dynApp = app;
        el.__dynModel = model;
        el.__dynLoaded = true;
        storeApp(el, app);
        // Vue 3.5 起 app._instance 不再被填充，直接取 mount 返回的 proxy 供 getModel 使用
        try { el.__dynProxy = app.mount(el) || null; } catch (e) { el.__dynProxy = null; throw e; }

        // 6) 恢复嵌套子树并递归挂载
        nested.forEach(function (item) {
            if (item.host && item.host.parentNode) item.host.appendChild(item.child);
            mount(item.child);
        });

        return app;
    }

    function mount(el) {
        return new Promise(function (resolvePromise) {
            el = resolve(el);
            if (!el) return resolvePromise(null);
            if (el.__dynApp) return resolvePromise(el.__dynApp);
            if (el.__dynMounting) return resolvePromise(null);

            el.__dynMounting = true;
            var url = el.getAttribute('data-dyn-url');
            var force = el.getAttribute('data-dyn-load') === 'true';
            var empty = el.childElementCount === 0;
            var needLoad = url && !el.__dynLoaded && (empty || force);

            if (needLoad) {
                var model = parseModel(el) || {};
                fetchPartial(url, model, 'POST').then(function (html) {
                    el.innerHTML = html;
                    el.__dynMounting = false;
                    try { resolvePromise(mountCore(el)); }
                    catch (e) { console.error('[dyn-lib] mount 失败', e); el.__dynMounting = false; resolvePromise(null); }
                }).catch(function (err) {
                    el.__dynMounting = false;
                    el.innerHTML = '<div class="dyn-loading">加载失败：' + ((err && err.message) || err) + '</div>';
                    console.error('[dyn-lib] 加载失败', err);
                    resolvePromise(null);
                });
            } else {
                el.__dynMounting = false;
                try { resolvePromise(mountCore(el)); }
                catch (e) { console.error('[dyn-lib] mount 失败', e); resolvePromise(null); }
            }
        });
    }

    function unmount(el) {
        el = resolve(el);
        if (!el) return;
        if (el.querySelectorAll) {
            [].slice.call(el.querySelectorAll('[dyn-init]')).forEach(function (n) { unmount(n); });
        }
        if (el.__dynApp) {
            try { el.__dynApp.unmount(); } catch (e) { }
            el.__dynApp = null;
            el.__dynModel = null;
            removeApp(el);
        }
    }

    function render(el, html) {
        el = resolve(el);
        if (!el) return Promise.resolve(null);
        unmount(el);
        el.innerHTML = html || '';
        return mount(el);
    }

    /* ---------------- 查询：app / model ---------------- */

    function getApp(el) {
        el = resolve(el);
        if (!el) return null;
        var a = getAppByEl(el);
        if (a) return a;
        var anc = el.closest ? el.closest('[dyn-init]') : null;
        return anc ? getAppByEl(anc) : null;
    }

    function getProxy(el) {
        el = resolve(el);
        if (!el) return null;
        if (el.__dynProxy) return el.__dynProxy;
        var app = getApp(el);
        return app && app._instance ? app._instance.proxy : null;
    }

    function getModel(el) {
        var p = getProxy(el);
        return p ? p.model : null;
    }

    /* ---------------- 初始化扫描 ---------------- */

    function topDynInit(root) {
        var all = [];
        if (root.nodeType === 1 && root.hasAttribute && root.hasAttribute('dyn-init')) all.push(root);
        if (root.querySelectorAll) all = all.concat([].slice.call(root.querySelectorAll('[dyn-init]')));
        var result = [];
        all.forEach(function (el) {
            if (el.__dynApp || el.__dynMounting) return;
            // 只处理“当前扫描范围内”的顶层：其 dyn-init 祖先若也在本集合内则跳过（由外层应用递归处理）
            var anc = el.parentElement ? el.parentElement.closest('[dyn-init]') : null;
            if (anc && all.indexOf(anc) >= 0) return;
            result.push(el);
        });
        return result;
    }

    function loadHost(h) {
        var url = h.getAttribute('data-dyn-url');
        if (!url) return;
        var force = h.getAttribute('data-dyn-load') === 'true';
        if (h.childElementCount > 0 && !force) { init(h); return; }
        fetchPartial(url, {}, 'POST').then(function (html) {
            h.innerHTML = html;
            init(h);
        }).catch(function (err) {
            h.innerHTML = '<div class="dyn-loading">加载失败：' + ((err && err.message) || err) + '</div>';
        });
    }

    function init(root) {
        root = resolve(root) || document.body;
        if (!root) return;

        // 1) 懒加载容器：data-dyn-url 且无 dyn-init（纯容器，内部由分部视图自带 dyn-init）
        var hosts = [];
        if (root.nodeType === 1 && root.hasAttribute && root.hasAttribute('data-dyn-url') && !root.hasAttribute('dyn-init')) hosts.push(root);
        if (root.querySelectorAll) hosts = hosts.concat([].slice.call(root.querySelectorAll('[data-dyn-url]:not([dyn-init])')));
        hosts.forEach(function (h) {
            if (h.__dynLoaded) return;
            h.__dynLoaded = true;
            loadHost(h);
        });

        // 2) 顶层 dyn-init 应用
        var tops = topDynInit(root);
        tops.forEach(function (el) { mount(el); });
    }

    function initAll() { init(document.body); }

    /* ---------------- reload ---------------- */

    function reload(target) {
        var el = resolve(target);
        if (!el) return Promise.resolve(null);

        var $t;
        if (el.hasAttribute('dyn-init')) $t = $(el);
        else if (el.hasAttribute('data-dyn-url')) $t = $(el);
        else $t = $(el).closest('[dyn-init]');
        var targetEl = $t && $t.length ? $t.get(0) : null;
        if (!targetEl) return Promise.resolve(null);

        var url = targetEl.getAttribute('data-dyn-url');
        if (!url) { console.warn('[dyn-lib] reload 目标缺少 data-dyn-url', targetEl); return Promise.resolve(null); }

        // 取当前 Model（host 里已挂载的 dyn-init 应用，或自身就是 dyn-init）
        var model = {};
        var inner = targetEl.hasAttribute('dyn-init') ? targetEl : (targetEl.querySelector('[dyn-init]') || null);
        var app = inner ? getApp(inner) : null;
        if (app && app._instance) model = deepClone(app._instance.proxy.model);
        else if (inner) model = parseModel(inner) || {};
        else model = parseModel(targetEl) || {};

        return fetchPartial(url, model, 'POST').then(function (html) {
            unmount(targetEl);
            targetEl.innerHTML = html;
            return init(targetEl);
        }).catch(function (err) {
            showMessage('刷新失败：' + ((err && err.message) || err), 'error');
            return null;
        });
    }

    /* ---------------- postback：找到祖先 Model → POST → 处理响应 ---------------- */

    function postback(el, opts) {
        opts = opts || {};
        el = resolve(el) || (opts.target ? resolve(opts.target) : null);
        if (!el) return $.Deferred().reject().promise();

        var $anc = $(el).closest('[dyn-init]');
        var ancEl = $anc.length ? $anc.get(0) : el;
        var model = getModel(ancEl) || parseModel(ancEl) || {};
        if (opts.resetPage && model.PageInfo) model.PageInfo.CurrentPage = 1;

        var url = opts.url || ancEl.getAttribute('data-dyn-url') || '';
        if (!url) { console.error('[dyn-lib] dyn-click-postback 缺少 url', el); return $.Deferred().reject().promise(); }
        var qs = $.param(opts.params || {});
        var fullUrl = qs ? url + (url.indexOf('?') >= 0 ? '&' : '?') + qs : url;

        return $.ajax({
            url: fullUrl,
            type: 'POST',
            data: JSON.stringify(deepClone(model)),
            contentType: 'application/json'
        }).done(function (res) { handleResponse(ancEl, res, opts); })
          .fail(function (xhr) { showMessage(extractError(xhr), 'error'); });
    }

    function handleResponse(ancEl, res, opts) {
        opts = opts || {};
        var isHtml = typeof res === 'string' && res.trim().charAt(0) === '<';
        // 后端统一走 System.Text.Json camelCase（success/message）；同时兼容 PascalCase 响应
        var failed = res && typeof res === 'object' && (res.Success === false || res.success === false);

        if (failed) { showMessage(res.Message || '操作失败', 'error'); return; }

        if (opts.close) {
            // 保存类操作：刷新目标 + 关闭模态 + 提示
            if (opts.reload) reload(opts.reload);
            close(ancEl || opts.reload);
            if (opts.message) showMessage(opts.message, 'success');
            return;
        }

        if (isHtml) {
            // 后端返回分部视图 → 渲染回当前 dyn-init 容器并重新挂载（用户核心诉求）
            render(ancEl, res);
            if (opts.message) showMessage(opts.message, 'success');
            return;
        }

        if (res && typeof res === 'object') {
            // 后端返回 JSON → 合并进 Model（响应式自动刷新视图）
            var proxy = getProxy(ancEl);
            if (proxy && proxy.model) Object.assign(proxy.model, res);
            if (opts.reload) reload(opts.reload);
            if (opts.message) showMessage(opts.message, 'success');
            return;
        }

        if (opts.message) showMessage(opts.message, 'success');
    }

    /* ---------------- 模态 ---------------- */

    function open(opts, triggerEl) {
        opts = opts || {};
        var url = opts.url;
        if (!url) { console.error('[dyn-lib] dyn-click-open 缺少 url', triggerEl); return; }

        var holder = document.createElement('div');
        holder.className = 'dyn-modal-host';
        holder.id = 'dyn-modal-' + (++_uidSeq);
        document.body.appendChild(holder);

        var app = Vue.createApp({
            data: function () {
                return { visible: true, title: opts.title || '对话框', width: opts.width || '60%', loading: true, html: '', error: '' };
            },
            template: '<el-dialog v-model="visible" :title="title" :width="width" top="6vh" :close-on-click-modal="false" :teleported="false" @closed="onClosed">'
                    + '<div v-if="loading" class="dyn-modal-loading">加载中...</div>'
                    + '<div v-else-if="error" class="dyn-modal-error">{{error}}</div>'
                    + '<div v-else v-html="html" class="dyn-modal-body"></div>'
                    + '</el-dialog>',
            methods: {
                load: function () {
                    var self = this;
                    fetchPartial(url, opts.params || {}, 'GET').then(function (html) {
                        self.html = html;
                        self.loading = false;
                        self.$nextTick(function () {
                            // el-dialog 已 teleported=false，内容就在 holder 内；
                            // 用 holder 定位比 self.$el 更稳（Vue 根元素指向可能不同）。
                            var bodyEl = holder.querySelector('.dyn-modal-body');
                            if (bodyEl) init(bodyEl); // 分部视图内部的 dyn-init 在这里挂载
                        });
                    }).catch(function (err) {
                        self.error = (err && err.message) || '加载失败';
                        self.loading = false;
                    });
                },
                onClosed: function () {
                    unmount(holder);   // 先卸载内部 dyn-init 应用，防泄漏
                    app.unmount();
                    holder.remove();
                }
            },
            mounted: function () { this.load(); }
        });
        if (global.ElementPlus) app.use(ElementPlus);
        holder.__dynApp = app;
        app.mount(holder);
    }

    function close(el) {
        el = resolve(el);
        if (!el) return;
        var host = el.closest ? el.closest('.dyn-modal-host') : null;
        if (!host) return;
        var app = host.__dynApp;
        if (app && app._instance) app._instance.proxy.visible = false;
        else host.remove();
    }

    /* ---------------- 事件委托：一次绑定，覆盖所有动态渲染出的元素 ---------------- */

    function parseOpts(el, attr) {
        var raw = el.getAttribute(attr);
        var opts = {};
        if (raw && raw.trim()) {
            try { opts = JSON.parse(raw); }
            catch (e) { console.error('[dyn-lib] ' + attr + ' 不是合法 JSON：', raw); }
        }
        var params = {};
        if (el.attributes) {
            [].forEach.call(el.attributes, function (a) {
                if (a.name.indexOf('data-') === 0 && a.name.indexOf('data-dyn') !== 0 && a.name.indexOf('data-v-') !== 0) {
                    params[a.name.substring(5)] = a.value;
                }
            });
        }
        opts.params = Object.assign({}, params, opts.params || {});
        return opts;
    }

    document.addEventListener('click', function (e) {
        var el = e.target && e.target.closest
            ? e.target.closest('[dyn-click-postback],[dyn-click-open],[dyn-click-close],[dyn-click-reload]')
            : null;
        if (!el) return;
        e.preventDefault();
        e.stopPropagation();

        if (el.hasAttribute('dyn-click-postback')) {
            var opts = parseOpts(el, 'dyn-click-postback');
            if (opts.confirm) {
                var msg = opts.confirm === true ? '确定执行该操作吗？' : opts.confirm;
                confirmAsync(msg).then(function (ok) { if (ok) postback(el, opts); });
            } else {
                postback(el, opts);
            }
        } else if (el.hasAttribute('dyn-click-open')) {
            open(parseOpts(el, 'dyn-click-open'), el);
        } else if (el.hasAttribute('dyn-click-close')) {
            close(el);
        } else if (el.hasAttribute('dyn-click-reload')) {
            var sel = el.getAttribute('dyn-click-reload');
            reload(sel || el);
        }
    }, true);

    /* ---------------- 卸载兜底：DOM 被外部移除时自动卸载 app，防止内存溢出 ---------------- */

    if (typeof MutationObserver !== 'undefined' && document.body) {
        new MutationObserver(function (muts) {
            muts.forEach(function (m) {
                if (!m.removedNodes) return;
                [].forEach.call(m.removedNodes, function (n) {
                    if (n.nodeType !== 1) return;
                    if (n.__dynApp) unmount(n);
                    else if (n.querySelectorAll) {
                        [].slice.call(n.querySelectorAll('[dyn-init]')).forEach(function (c) { if (c.__dynApp) unmount(c); });
                    }
                });
            });
        }).observe(document.body, { childList: true, subtree: true });
    }

    /* ---------------- 对外 API ---------------- */

    var dyn = {
        VERSION: '1.0.0',
        init: init,
        initAll: initAll,
        mount: mount,
        unmount: unmount,
        render: render,
        reload: reload,
        getApp: getApp,
        getProxy: getProxy,
        getModel: getModel,
        postback: postback,
        open: open,
        close: close,
        fetchPartial: fetchPartial,
        parseModel: parseModel,
        deepClone: deepClone,
        json: deepClone,
        showMessage: showMessage,
        confirmAsync: confirmAsync
    };

    global.dyn = dyn;

})(window);
