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

    // ===== 祖先查找统一入口 =====
    // 沿祖先链向上找第一个匹配选择器的元素（含自身）。原生 closest 优先，jQuery 兜底。
    function findAncestor(el, selector) {
        el = resolve(el);
        if (!el) return null;
        if (el.closest) return el.closest(selector) || null;
        if ($) { var r = $(el).closest(selector); return r.length ? r.get(0) : null; }
        return null;
    }
    // 找最近的 [dyn-init] 祖先（含自身）
    function closestDynInit(el) {
        return findAncestor(el, '[dyn-init]');
    }
    // 找最近的 [data-url],[data-dyn-url] 祖先（含自身）
    function closestDataUrl(el) {
        return findAncestor(el, '[data-url],[data-dyn-url]');
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

    function fetchPartial(url, params, type, dataType) {
        type = type || 'POST';
        params = params || {};
        dataType = dataType || 'html';
        return new Promise(function (resolvePromise, reject) {
            $.ajax({
                url: url,
                type: type,
                data: type === 'GET' ? $.param(params) : JSON.stringify(params),
                contentType: type === 'GET' ? undefined : 'application/json',
                dataType: dataType
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
        var anc = closestDynInit(el);
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
            var anc = el.parentElement ? closestDynInit(el.parentElement) : null;
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
        // 3) 初始化动作（dyn-{action}-init）
        initActions(root);
    }

    function initAll() { init(document.body); }

    /* ---------------- reload ---------------- */

    // 序列化容器内表单输入为参数对象（不限于 <form>，容器内任意 input/select/textarea）
    function serializeForm(root) {
        if (!root || !root.querySelectorAll) return null;
        var $inputs = $(':input', root).filter(function () {
            var n = this.name || '';
            return !!n && !/^dyn-|^data-|^_|^v-|^inspector-|^doubao-/.test(n);
        });
        if (!$inputs.length) return null;
        var o = {};
        $inputs.each(function () {
            var $e = $(this);
            var n = this.name;
            if (this.type === 'radio') { if (this.checked) o[n] = $e.val(); return; }
            if (this.type === 'checkbox') { if (this.checked) o[n] = $e.val(); return; }
            o[n] = $e.val();
        });
        return o;
    }

    // 汇总 reload 请求参数：固定参数(__dynCfg.params) → Vue model → form 序列化 → 显式 extra
    function collectParams(targetEl, extra) {
        var params = {};
        var cfg = targetEl.__dynCfg || {};
        if (cfg.params) params = Object.assign({}, cfg.params);
        var inner = targetEl.hasAttribute('dyn-init') ? targetEl : (targetEl.querySelector('[dyn-init]') || null);
        var app = inner ? getApp(inner) : null;
        if (app && app._instance) params = Object.assign(params, deepClone(app._instance.proxy.model));
        else if (inner) params = Object.assign(params, parseModel(inner) || {});
        else params = Object.assign(params, parseModel(targetEl) || {});
        var fp = serializeForm(inner || targetEl);
        if (fp) params = Object.assign(params, fp);
        if (extra) params = Object.assign(params, extra);
        return params;
    }

    function reload(target, opts) {
        opts = opts || {};
        // 支持命名函数 / 函数引用 / 全局回调（模板路由页 RouteList 等场景）
        if (typeof target === 'function') { try { return Promise.resolve(target()); } catch (e) { return Promise.resolve(null); } }
        if (typeof target === 'string' && typeof window[target] === 'function') {
            try { return Promise.resolve(window[target]()); } catch (e) { return Promise.resolve(null); }
        }
        var el = resolve(target);
        if (!el) {
            // #dynHost 等 Shell 专属选择器在模板路由页不存在 → 兜底走页面注册的刷新回调
            if (typeof window.__dynRouteReload === 'function') {
                try { return Promise.resolve(window.__dynRouteReload()); } catch (e) { return Promise.resolve(null); }
            }
            return Promise.resolve(null);
        }

        var targetEl = null;
        if (el.hasAttribute('dyn-init') || el.hasAttribute('data-dyn-url') || el.hasAttribute('data-url')) targetEl = el;
        else targetEl = closestDynInit(el);
        if (!targetEl) return Promise.resolve(null);

        // url 读取优先级：显式 opts.url → __dynCfg.url → data-dyn-url → data-url
        var cfg = targetEl.__dynCfg || {};
        var url = opts.url || cfg.url || targetEl.getAttribute('data-dyn-url') || targetEl.getAttribute('data-url');
        if (!url) { console.warn('[dyn-lib] reload 目标缺少 url（data-url / data-dyn-url / __dynCfg.url）', targetEl); return Promise.resolve(null); }

        // 参数三层合并：固定(__dynCfg.params) → Vue model → form；显式 opts.params 最后覆盖
        var params = collectParams(targetEl, opts.params);

        // P0: 检测是否是 dyn-init app 本身（Vue 管理下）
        var isDynInitApp = targetEl.hasAttribute('dyn-init');
        if (isDynInitApp) {
            // dyn-init app：用 JSON 方式更新（model 数据驱动，Vue 自动重新渲染）
            return fetchPartial(url, params, opts.method || cfg.method || 'POST', 'json').then(function (data) {
                if (data && typeof data === 'object') {
                    var app = getApp(targetEl);
                    if (app && app._instance) {
                        Object.assign(app._instance.proxy.model, data);
                    }
                }
                return init(targetEl);
            }).catch(function (err) {
                showMessage('刷新失败：' + ((err && err.message) || err), 'error');
                return null;
            });
        }

        // 纯 HTML 容器：直接 innerHTML
        return fetchPartial(url, params, opts.method || cfg.method || 'POST').then(function (html) {
            unmount(targetEl);
            targetEl.innerHTML = html;
            return init(targetEl);
        }).catch(function (err) {
            showMessage('刷新失败：' + ((err && err.message) || err), 'error');
            return null;
        });
    }

    // updateEl(selector, url, param)：从 selector 元素开始，沿祖先链向上找最近的含
    // data-url / data-dyn-url 的容器并刷新（closest 天然覆盖"继续向上直到 body"，无匹配则跳过）
    function updateEl(selector, url, params) {
        var el = resolve(selector);
        if (!el) return Promise.resolve(null);
        var target = closestDataUrl(el);
        if (!target) return Promise.resolve(null);
        return reload(target, { url: url, params: params });
    }

    // setDynCfg(el, cfg)：更新元素的 __dynCfg 配置（url + params + method），同步更新 data-url
    function setDynCfg(el, cfg) {
        el = resolve(el);
        if (!el) return;
        el.__dynCfg = Object.assign({}, el.__dynCfg, cfg);
        if (cfg.url) el.setAttribute('data-url', cfg.url);
    }

    /* ---------------- postback：找到祖先 Model → POST → 处理响应 ---------------- */

    function postback(el, opts) {
        opts = opts || {};
        el = resolve(el) || (opts.target ? resolve(opts.target) : null);
        if (!el) return $.Deferred().reject().promise();

        var ancEl = closestDynInit(el) || el;
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
        var host = findAncestor(el, '.dyn-modal-host');
        if (!host) return;
        var app = host.__dynApp;
        if (app && app._instance) app._instance.proxy.visible = false;
        else host.remove();
    }

    /* ============================================================================
     * 动作注册表 + 通用事件委托（属性驱动：dyn-{event}-{action}='{JSON options}'）
     * ----------------------------------------------------------------------------
     *   dyn-click-postdata='{"url":"/x","confirm":true,"message":"保存成功"}'
     *   dyn-click-reload='{"selector":"#list"}'
     *   dyn-change-reload（change 事件）
     *   dyn-{action}-init                          初始化动作（initActions 扫描执行）
     *
     * 动作统一挂在 actionHelper 上（挂方法即动作），自动进入委托选择器，
     * 扩展新动作无需改委托代码。动作统一收一个 ctx 上下文对象（比 common.js 的函数
     * 签名反射更安全、更显式）：
     *   ctx = { element, event, $event, targetInfo, action, options, params,
     *           model(最近 dyn-init 祖先), url(祖先 data-dyn-url) }
     * 兼容旧属性 dyn-click-postback/open/close/reload（作为内置动作注册）。
     * ============================================================================ */

    var ACTION_EVENTS = ['change', 'click', 'dblclick', 'error', 'focus', 'select', 'mouseover'];
    var _actions = {};
    var _initActions = {};
    var _selCache = {};

    // ===== 约定式动作（actionHelper）：唯一的动作注册方式，无需 registerAction / registerInitAction =====
    // 用法（参考 common.js 的「挂方法即动作」）：
    //   dyn.actionHelper.post = function (ctx) { ... }      → 自动支持 dyn-click-post / dyn-change-post / ...
    //   fn._events = ['click']                              → 限定事件（缺省全部 ACTION_EVENTS；[] 表示不绑事件，仅作 init 用）
    //   fn._init   = true                                   → 同时注册为初始化动作（dyn-init-{name}）
    //   fn._skip   = true                                   → 跳过（辅助方法用下划线前缀或此标记隔离）
    // 运行时新增动作后调用 dyn.rebind()（= autoBindActions()）重新生成委托选择器。
    var actionHelper = {};
    function autoBindActions() {
        Object.keys(actionHelper).forEach(function (name) {
            var fn = actionHelper[name];
            if (typeof fn !== 'function' || fn._skip) return;
            var evs = fn._events ? fn._events.slice() : ACTION_EVENTS.slice();
            if (evs.length) {
                _actions[name] = fn;
                evs.forEach(function (ev) { if (ACTION_EVENTS.indexOf(ev) < 0) ACTION_EVENTS.push(ev); });
            }
            if (fn._init) _initActions[name] = fn;
        });
        _selCache = {};
        return dyn;
    }
    // 内置/自定义动作的便捷定义：挂到 actionHelper 并标注事件与 init 标记
    function defineAction(name, fn, events, init) {
        fn._events = events || [];
        if (init) fn._init = true;
        actionHelper[name] = fn;
        return fn;
    }

    // 构造统一上下文（动作方法的唯一入参）
    function buildCtx(el, eventName, $event, options, actionName) {
        options = options || {};
        var params = {};
        if (el && el.attributes) {
            [].forEach.call(el.attributes, function (a) {
                if (a.name.indexOf('data-') === 0 && a.name.indexOf('data-dyn') !== 0 && a.name.indexOf('data-v-') !== 0) {
                    params[a.name.substring(5)] = a.value;
                }
            });
        }
        options.params = Object.assign({}, params, options.params || {});
        var ancEl = closestDynInit(el) || el;
        var app = getApp(ancEl);
        return {
            element: el, el: el,
            event: eventName, $event: $event, targetInfo: $event,
            action: actionName,
            options: options,
            params: options.params,
            model: getModel(ancEl) || parseModel(ancEl) || {},
            vm: app && app._instance ? app._instance.proxy : null,  // Vue 组件实例
            url: options.url || (ancEl && ancEl.getAttribute ? ancEl.getAttribute('data-dyn-url') : '') || ''
        };
    }

    // 为某事件生成委托选择器（由注册表动态生成，注册动作时失效缓存）
    function selectorFor(eventName) {
        if (_selCache[eventName]) return _selCache[eventName];
        var sels = [];
        Object.keys(_actions).forEach(function (name) {
            // DOM 属性名一律被 HTML 解析器小写化，这里用小写生成选择器保证匹配
            sels.push('[dyn-' + eventName + '-' + name.toLowerCase() + ']');
        });
        _selCache[eventName] = sels.join(',');
        return _selCache[eventName];
    }

    // 解析元素属性：找 dyn-{event}-{action}，返回 { action, raw }
    function resolveActionAttr(el, eventName) {
        var prefix = 'dyn-' + eventName + '-';
        if (!el || !el.attributes) return null;
        var hit = null;
        [].forEach.call(el.attributes, function (a) {
            if (a.name.indexOf(prefix) === 0) hit = { action: a.name.substring(prefix.length), raw: a.value };
        });
        return hit;
    }

    // 解析 options：JSON 优先；裸字符串兼容旧 dyn-click-reload 的选择器写法
    function parseActionOptions(raw) {
        if (!raw || !raw.trim()) return {};
        var t = raw.trim();
        if (t.charAt(0) === '{') { try { return JSON.parse(t); } catch (e) { return { selector: t }; } }
        return { selector: t };
    }

    // 大小写不敏感查找动作：HTML 属性名会被浏览器转成小写（如 dyn-click-setVueModel → setvuemodel），
    // 而动作可能以驼峰注册（setVueModel），这里做兼容匹配。
    function resolveAction(name) {
        if (_actions[name]) return _actions[name];
        var lower = name.toLowerCase();
        var keys = Object.keys(_actions);
        for (var i = 0; i < keys.length; i++) {
            if (keys[i].toLowerCase() === lower) return _actions[keys[i]];
        }
        return null;
    }

    // 通用事件委托：每个事件一个 document capture 监听器，覆盖动态渲染出的所有元素
    ACTION_EVENTS.forEach(function (ev) {
        document.addEventListener(ev, function (e) {
            var sel = selectorFor(ev);
            if (!sel) return;
            var el = e.target && e.target.closest ? e.target.closest(sel) : null;
            if (!el) return;
            var hit = resolveActionAttr(el, ev);
            if (!hit) return;
            var fn = resolveAction(hit.action);
            if (!fn) return;
            var ctx = buildCtx(el, ev, e, parseActionOptions(hit.raw), hit.action);
            // P5: 提供 prevent 选项（默认阻止）
            var prevent = ctx.options.prevent !== false;
            if (prevent) {
                e.preventDefault();
                e.stopPropagation();
            }
            try { fn(ctx); }
            catch (err) {
                console.error('[dyn-lib] 动作执行失败: ' + hit.action, err);
                showMessage('操作失败：' + ((err && err.message) || err), 'error');
            }
        }, true);
    });

    // ===== 内置动作（全部挂 actionHelper，约定式自动绑定；兼容旧 dyn-click-postback/open/close/reload） =====
    defineAction('postback', function (ctx) {
        var o = ctx.options || {};
        if (o.confirm) {
            var msg = o.confirm === true ? '确定执行该操作吗？' : o.confirm;
            return confirmAsync(msg).then(function (ok) { if (ok) return postback(ctx.element, o); });
        }
        return postback(ctx.element, o);
    }, ['click', 'change']);
    defineAction('postdata', function (ctx) {
        return _actions.postback(ctx);
    }, ['click', 'change']);
    defineAction('confirm-postdata', function (ctx) {
        var o = Object.assign({}, ctx.options || {});
        if (!o.confirm) o.confirm = true;
        return _actions.postback(Object.assign({}, ctx, { options: o }));
    }, ['click', 'change']);
    defineAction('reload', function (ctx) {
        var o = ctx.options || {};
        return reload(o.selector || ctx.element);
    }, ['click', 'change']);
    defineAction('open', function (ctx) {
        return open(ctx.options || {}, ctx.element);
    }, ['click']);
    defineAction('close', function (ctx) {
        return close(ctx.element);
    }, ['click']);
    defineAction('updateel', function (ctx) {
        var o = ctx.options || {};
        return updateEl(o.selector || ctx.element, o.url, o.params);
    }, ['click', 'change']);
    // evaljs：事件动作 + 初始化动作共用同一实现
    defineAction('evaljs', function (ctx) {
        var code = ctx.options;
        if (typeof code === 'object') code = code.code || code.selector || code;
        if (!code) return;
        try {
            // 使用 new Function 执行代码
            // eslint-disable-next-line no-new-func
            var result = new Function("ctx", `return ${code}`)(ctx);
            return result;
        } catch (err) {
            console.error('[dyn-lib] evalJS 执行失败', err);
            showMessage('执行失败：' + ((err && err.message) || err), 'error');
        }
    }, ['click', 'change'], true);
    // ===== 内置初始化动作（dyn-init-{action}：页面/Vue 初始化完毕立即执行） =====
    // dyn-init-load='{"url":"/x"}'：请求后端，由后端 HTML 填充本 div，随后 init(div)，
    // 并将 url 写入 div 的 data-url（后续可被 reload 动作读取，作为数据源）。
    defineAction('load', function (ctx) {
        var o = ctx.options || {};
        var url = o.url || ctx.url;
        if (!url) { console.warn('[dyn-lib] dyn-init-load 缺少 url', ctx.element); return; }
        var el = ctx.element;
        // 完整请求配置存 element 上（url + 固定参数 + method），data-url 存 url 供声明式读取/reload 兜底
        el.__dynCfg = { url: url, params: o.params || {}, method: o.method || 'POST' };
        if (o.writeUrl !== false) el.setAttribute('data-url', url);

        // P0: 检测是否在 Vue 管理下（dyn-init app 内部）
        var isVueManaged = closestDynInit(el);
        if (isVueManaged) {
            console.warn('[dyn-lib] dyn-init-load 元素在 Vue 管理下，建议用 Vue 方式更新（通过 model 数据驱动）', el);
            // 仍然尝试 innerHTML，但可能破坏 Vue
        }

        return fetchPartial(url, o.params || {}, o.method || 'POST').then(function (html) {
            el.innerHTML = html;
            return init(el);
        }).catch(function (err) {
            showMessage('加载失败：' + ((err && err.message) || err), 'error');
            return null;
        });
    }, [], true);

    // dyn-init-evaljs='alert("Hello World")'：页面/Vue 初始化完毕立即执行 JavaScript 代码
    // （evaljs 的事件 + init 双注册已在上方 defineAction('evaljs', ..., true) 完成，这里仅保留说明）

    // ===== setVueModel：设置 Vue model 值（支持 dyn-init/click/change-setVueModel） =====
    // 属性约定：
    //   dyn-click-setVueModel='{"modelName":"user.name","model":"张三","settimeout":100}'
    //   dyn-change-setVueModel='{"modelName":"user.age","model":30}'
    //   dyn-init-setVueModel='{"modelName":"page.title","model":"首页"}'
    //   dyn-click-setVueModel='{"TargetEl":"#other-container","modelName":"items[0].name","model":"x"}'
    // 参数：
    //   modelName  —— model 路径，支持点路径与数组下标（如 "user.name"、"items[0].name"）
    //   model      —— 要设置的值（JSON 字符串自动解析为对象/数组）；change 事件缺省时取元素 value
    //   TargetEl   —— 可选，指定目标 Vue 容器（选择器字符串或 DOM 元素）；缺省用当前元素向上找最近的 VueApp
    //   settimeout —— 延迟毫秒（可选）
    function setVueModel(ctx, modelName, value, delay) {
        var o = ctx.options || {};
        if (modelName == null) modelName = o.modelName || o.name || o.path;
        // change/input 事件且未显式给 model 时，从元素取值（input/select/textarea 等）
        if (value === undefined) {
            value = ('model' in o) ? o.model : o.value;
            if (value === undefined && ctx.element && 'value' in ctx.element) {
                value = ctx.element.value;
            }
        }
        if (delay == null) delay = o.settimeout || o.delay || 0;
        if (!modelName) { console.warn('[dyn-lib] setVueModel 缺少 modelName', ctx.element); return; }
        // 定位目标 Vue model：TargetEl 指定容器；缺省用当前元素向上查找
        var target = getVueModel(ctx.element, o.TargetEl || o.targetEl || o.target);
        if (!target) { console.warn('[dyn-lib] setVueModel 未找到 Vue model（需在 dyn-init 容器内或指定 TargetEl）', ctx.element); return; }
        // value 若是 JSON 字符串则尝试解析为对象/数组（失败保持原字符串）
        if (typeof value === 'string') {
            var t = value.trim();
            if (t.charAt(0) === '{' || t.charAt(0) === '[') {
                try { value = JSON.parse(t); } catch (e) { /* 保持字符串 */ }
            }
        }
        var doSet = function () { setPathVal(target, modelName, value); };
        if (delay > 0) { setTimeout(doSet, delay); return; }
        doSet();
        return target;
    }
    // setVueModel：事件动作（click/change）+ 初始化动作（dyn-init-setVueModel）共用同一实现
    defineAction('setVueModel', function (ctx) {
        return setVueModel(ctx);
    }, ['click', 'change'], true);

    // ===== 初始化动作扫描 =====
    // 属性约定：dyn-init-{action}（页面/Vue 初始化完毕立即执行，推荐）
    //           兼容旧命名 dyn-{action}-init
    // 注意：DOM 属性名一律被 HTML 解析器小写化，且部分环境 qsa/hasAttribute 大小写敏感，
    //       因此用 name.toLowerCase() 生成属性名保证匹配；动作名大小写由 _initActions 直接命中。
    function initActions(root) {
        root = resolve(root) || document.body;
        if (!root) return;
        Object.keys(_initActions).forEach(function (name) {
            var lower = name.toLowerCase();
            ['dyn-init-' + lower, 'dyn-' + lower + '-init'].forEach(function (attrName) {
                var targets = [];
                if (root.nodeType === 1 && root.hasAttribute && root.hasAttribute(attrName)) targets.push(root);
                if (root.querySelectorAll) targets = targets.concat([].slice.call(root.querySelectorAll('[' + attrName + ']')));
                targets.forEach(function (el) {
                    if (el.__dynInitDone) return;
                    el.__dynInitDone = true;
                    var raw = el.getAttribute(attrName);
                    var ctx = buildCtx(el, 'init', null, parseActionOptions(raw), name);
                    try { _initActions[name](ctx); }
                    catch (err) { console.error('[dyn-lib] 初始化动作失败: ' + name, err); }
                });
            });
        });
    }

    /* ---------------- 事件总线（跨组件解耦通信：设计器等场景） ---------------- */

    var _busListeners = {};

    function busOn(type, handler) {
        if (!type || typeof handler !== 'function') return function () { };
        (_busListeners[type] = _busListeners[type] || []).push(handler);
        return function () { busOff(type, handler); };
    }

    function busOff(type, handler) {
        var arr = _busListeners[type];
        if (!arr) return;
        var i = handler ? arr.indexOf(handler) : -1;
        if (i >= 0) arr.splice(i, 1);
        else delete _busListeners[type];
    }

    function busEmit(type, payload) {
        var arr = _busListeners[type];
        if (!arr) return;
        arr.slice().forEach(function (h) {
            try { h(payload); } catch (e) { console.error('[dyn.eventBus] 事件处理异常: ' + type, e); }
        });
    }

    function busClear(type) {
        if (type) delete _busListeners[type];
        else _busListeners = {};
    }

    var eventBus = { on: busOn, off: busOff, emit: busEmit, clear: busClear };

    /* ---------------- 通用路径 / 组件工具（设计器拆分复用） ---------------- */

    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce(function (o, k) { return (o == null) ? undefined : o[k]; }, obj);
    }

    // 路径设置统一走 lodash 的 _.set（原生支持点路径与数组下标：a.b.c、a[0].b）
    function setPathVal(obj, path, value) {
        if (global._ && typeof global._.set === 'function') {
            global._.set(obj, path, value);
            return;
        }
        console.warn('[dyn-lib] setPathVal 需要 lodash（_.set）支持路径设置', path);
    }

    // 查找目标 VueApp 的 model：
    //   - targetEl 传入时（选择器字符串或 DOM 元素），从该容器向上找 dyn-init 祖先取 model；
    //   - 缺省用 el 本身向上查找。内部经 getModel → getProxy：Vue 3.5+ 走 __dynProxy，旧版走 app._instance.proxy。
    function getVueModel(el, targetEl) {
        var src = null;
        if (targetEl) {
            src = (typeof targetEl === 'string') ? document.querySelector(targetEl) : targetEl;
        }
        if (!src) src = el;
        if (!src) return null;
        var host = closestDynInit(src) || src;
        return getModel(host);
    }

    var DESIGNER_CONTAINERS = ['DynNForm', 'DynNCellGroup', 'DynNDivContainer', 'DynNGrid', 'DynElDivContainer', 'DynElCard', 'DynElRow', 'DynElCol', 'DynElTabs'];

    function isContainerComp(name) {
        return DESIGNER_CONTAINERS.indexOf(name) >= 0;
    }

    // 简单自增 id
    var _uidSeq = 0;
    function nextId(prefix) {
        return (prefix || 'dyn') + (++_uidSeq) + '_' + Date.now().toString(36);
    }

    /* ---------------- 对外 API ---------------- */

    var dyn = {
        VERSION: '1.1.0',
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
        confirmAsync: confirmAsync,
        /* --- 拆分复用扩展 --- */
        eventBus: eventBus,
        getByPath: getByPath,
        setPathVal: setPathVal,
        isContainerComp: isContainerComp,
        nextId: nextId,
        /* --- 祖先查找统一入口 --- */
        findAncestor: findAncestor,
        closestDynInit: closestDynInit,
        closestDataUrl: closestDataUrl,
        /* --- 动作注册表 + 通用委托（约定式：挂 actionHelper 即注册） --- */
        initActions: initActions,
        actions: _actions,
        initActionList: _initActions,
        buildCtx: buildCtx,
        resolveAction: resolveAction,
        actionEvents: ACTION_EVENTS.slice(),
        /* --- 约定式动作（actionHelper）：挂方法即动作 --- */
        actionHelper: actionHelper,
        autoBindActions: autoBindActions,
        rebind: autoBindActions,
        updateEl: updateEl,
        serializeForm: serializeForm,
        setDynCfg: setDynCfg,
        setVueModel: setVueModel
    };

    global.dyn = dyn;

    // 内置动作注册完成后，把 actionHelper 上的约定式动作批量绑定
    autoBindActions();

})(window);
