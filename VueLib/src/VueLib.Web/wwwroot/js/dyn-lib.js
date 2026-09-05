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
        if (!url) { if (opts.url === undefined && !cfg.url) return Promise.resolve(null); console.warn('[dyn-lib] reload 目标缺少 url（data-url / data-dyn-url / __dynCfg.url）', targetEl); return Promise.resolve(null); }

        // 参数三层合并：固定(__dynCfg.params) → Vue model → form；显式 opts.params 最后覆盖
        var params = collectParams(targetEl, opts.params);

        // P0: 检测是否是 dyn-init app 本身（Vue 管理下）
        // 自适应刷新：响应能解析为 JSON 对象 → 合并进 model（Vue 自动重渲染）；
        // 否则视为 HTML 片段 → 卸载旧 app 后整段替换并重新 init
        return fetchPartial(url, params, opts.method || cfg.method || 'POST', 'text').then(function (text) {
            var data = null;
            try { data = JSON.parse(text); } catch (e) { }
            if (data && typeof data === 'object') {
                var app = getApp(targetEl);
                if (app && app._instance) {
                    Object.assign(app._instance.proxy.model, data);
                }
                return init(targetEl);
            }
            unmount(targetEl);
            targetEl.innerHTML = text;
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
            // Shapeless 模式：JSON 顶层 actions 指令 → 逐个执行（showmessage/setwindow/chain/reload 等）
            runJsonActions(res, ancEl);
            return;
        }

        if (opts.message) showMessage(opts.message, 'success');
    }

    /* ---------------- JSON 动作指令（Shapeless 模式） ----------------
     * 后端返回 JSON 时可在顶层携带 actions 数组，dyn-lib 拿到后自动执行：
     *   { action: 'setwindow', options: {...} }        → 调用已注册动作
     *   { action: 'chain', options: { steps: [...] } } → 动作链
     *   { script: 'window.location=...' }               → 直接执行 JS
     */
    function runJsonActions(res, rootEl) {
        if (!res || typeof res !== 'object' || !Array.isArray(res.actions) || !res.actions.length) return;
        var root = rootEl || document.body;
        res.actions.forEach(function (item) {
            if (!item || typeof item !== 'object') return;
            try {
                if (item.script) {
                    // eslint-disable-next-line no-new-func
                    new Function('ctx', 'return (' + item.script + ')')({});
                    return;
                }
                var name = item.action;
                var fn = resolveAction(name);
                if (!fn) { console.warn('[dyn-lib] JSON 动作未注册: ' + name, item); return; }
                var fakeEl = document.createElement('div');
                fakeEl.style.display = 'none';
                if (root && root.nodeType === 1) root.appendChild(fakeEl);
                var ctx = buildCtx(fakeEl, 'init', null, item.options || {}, name);
                Promise.resolve(fn(ctx)).catch(function (err) {
                    console.error('[dyn-lib] JSON 动作执行失败: ' + name, err);
                });
            } catch (err) {
                console.error('[dyn-lib] JSON 动作执行异常', err);
            }
        });
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
                    fetchPartial(url, opts.params || {}, opts.method || 'GET').then(function (html) {
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
        return holder;   // 返回模态宿主（供 chain 步骤拿到窗口宿主）
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
    var _selCache = {};
    var _actionMeta = {};

    // ===== 约定式动作（actionHelper）：唯一的动作注册方式，无需 registerAction / registerInitAction =====
    // 用法（参考 common.js 的「挂方法即动作」）：
    //   dyn.actionHelper.post = function (ctx) { ... }      → 挂上即可：
    //       1) 被任意 dyn-{event}-post 属性触发（事件由属性名决定）
    //       2) 被 dyn-init-post 属性在初始化（页面/Vue init 或 innerHTML 更新）时触发
    //   fn._events = ['click']                              → 可选白名单：事件委托只认这些事件（不设 = 全部事件属性均可）
    //   fn._skip   = true                                   → 跳过（辅助方法用下划线前缀或此标记隔离）
    // 触发规则：事件是否触发由元素上的 dyn-{event}-{action} 属性名决定（与 common.js 的 t-{event}-{fn} 一致）；
    //           初始化是否触发由 dyn-init-{action} / dyn-{action}-init 属性决定；没有对应动作函数时静默忽略。
    // 运行时新增动作后调用 dyn.rebind()（= autoBindActions()）重新生成委托选择器。
    var actionHelper = {};
    function autoBindActions() {
        Object.keys(actionHelper).forEach(function (name) {
            var fn = actionHelper[name];
            if (typeof fn !== 'function' || fn._skip) return;
            // 挂上即注册：同一函数既可用于事件委托（dyn-{event}-{name}），也可用于初始化扫描（dyn-init-{name}）
            _actions[name] = fn;
            // 登记自描述元数据（供 dyn.actionList() 枚举 / 工具面板 / 自动生成文档）
            _actionMeta[name] = {
                name: name,
                label: fn._label || name,
                doc: fn._doc || '',
                events: fn._events ? fn._events.slice() : ACTION_EVENTS.slice()
            };
        });
        _selCache = {};
        return dyn;
    }
    // 内置/自定义动作的便捷定义：挂到 actionHelper。事件与初始化均由属性名驱动，无需任何标记。
    function defineAction(name, fn) {
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
            var fn = _actions[name];
            // 动作声明了 _events 白名单时只认这些事件；未声明 = 全部事件属性均可触发
            if (fn && fn._events && fn._events.indexOf(eventName) < 0) return;
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
            // 异步执行动作（支持 async / 返回 Promise）；reject 统一提示，不阻断后续
            Promise.resolve(fn(ctx)).catch(function (err) {
                console.error('[dyn-lib] 动作执行失败: ' + hit.action, err);
                showMessage('操作失败：' + ((err && err.message) || err), 'error');
            });
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
    });
    defineAction('postdata', function (ctx) {
        return _actions.postback(ctx);
    });
    defineAction('confirm-postdata', function (ctx) {
        var o = Object.assign({}, ctx.options || {});
        if (!o.confirm) o.confirm = true;
        return _actions.postback(Object.assign({}, ctx, { options: o }));
    });
    defineAction('reload', function (ctx) {
        var o = ctx.options || {};
        return reload(o.selector || ctx.element);
    });
    defineAction('open', function (ctx) {
        return open(ctx.options || {}, ctx.element);
    });
    defineAction('close', function (ctx) {
        close(ctx.element);
        return true;
    });
    defineAction('updateel', function (ctx) {
        var o = ctx.options || {};
        return updateEl(o.selector || ctx.element, o.url, o.params);
    });
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
    });
    // ===== 内置动作（含初始化动作，均挂 actionHelper，事件/初始化均由属性名驱动） =====
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
    });

    // dyn-init-evaljs='alert("Hello World")'：页面/Vue 初始化完毕立即执行 JavaScript 代码
    // （evaljs 已在上方 defineAction 挂载，init 扫描自动命中 dyn-init-evaljs）

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
    // setVueModel：挂上即支持 dyn-{event}-setVueModel（事件）+ dyn-init-setVueModel（初始化）
    defineAction('setVueModel', function (ctx) {
        return setVueModel(ctx);
    });

    // ===== 初始化动作扫描 =====
    // 属性约定：dyn-init-{action}（页面/Vue 初始化完毕立即执行，推荐）
    //           兼容旧命名 dyn-{action}-init
    // 说明：初始化动作与事件动作共用 actionHelper（挂上即注册），扫描所有已挂载动作，
    //       命中 dyn-init-{name} / dyn-{name}-init 属性即执行，无需 _init 标记。
    // 注意：DOM 属性名一律被 HTML 解析器小写化，且部分环境 qsa/hasAttribute 大小写敏感，
    //       因此用 name.toLowerCase() 生成属性名保证匹配。
    function initActions(root) {
        root = resolve(root) || document.body;
        if (!root) return;
        Object.keys(_actions).forEach(function (name) {
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
                    Promise.resolve(_actions[name](ctx)).catch(function (err) {
                        console.error('[dyn-lib] 初始化动作失败: ' + name, err);
                    });
                });
            });
        });
    }

    /* ============================================================================
     * 扩展动作集：openwindow / setwindow / setdyncom / toast / copy / download / setattr
     * 全部挂 actionHelper（挂方法即动作、属性驱动），零注册自动进入委托与初始化扫描。
     * 触发：dyn-click-openwindow='{...}'（事件）、dyn-init-openwindow='{...}'（初始化）
     * 每个动作带 _doc 自描述（dyn.actionList() 可枚举，便于工具面板/自动生成文档）。
     * ============================================================================ */

    // ---- 轻量独立窗口（无桌面系统时 openwindow type=window 使用；setwindow 可控制） ----
    function createDynWindow(o) {
        var win = document.createElement('div');
        win.className = 'dyn-window';
        var width = o.width || 800, height = o.height || 600;
        var x = Math.max(20, Math.round((window.innerWidth - width) / 2));
        var y = Math.max(20, Math.round((window.innerHeight - height) / 2));
        win.style.cssText = 'position:fixed;z-index:3000;left:' + x + 'px;top:' + y + 'px;width:' + width + 'px;height:' + height + 'px;'
            + 'background:#fff;border:1px solid #dcdfe6;border-radius:8px;box-shadow:0 8px 30px rgba(0,0,0,.18);'
            + 'display:flex;flex-direction:column;overflow:hidden;';
        win.innerHTML =
            '<div class="dyn-window-bar" style="display:flex;align-items:center;height:36px;background:#f5f7fa;border-bottom:1px solid #e4e7ed;cursor:move;flex:0 0 auto;user-select:none;">'
            + '<span class="dyn-window-title" style="flex:1;padding:0 12px;font-size:13px;color:#303133;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">' + (o.title || '窗口') + '</span>'
            + '<span class="dyn-window-btn" data-act="fullscreen" title="最大化" style="padding:0 8px;cursor:pointer;color:#909399;">⛶</span>'
            + '<span class="dyn-window-btn" data-act="min" title="最小化" style="padding:0 8px;cursor:pointer;color:#909399;">—</span>'
            + '<span class="dyn-window-btn" data-act="close" title="关闭" style="padding:0 10px;cursor:pointer;color:#909399;">✕</span>'
            + '</div>'
            + '<div class="dyn-window-body" style="flex:1;position:relative;overflow:hidden;background:#fff;">'
            + (o.url ? '<iframe src="' + o.url + '" style="width:100%;height:100%;border:none;"></iframe>' : '')
            + '</div>';
        document.body.appendChild(win);
        var bar = win.querySelector('.dyn-window-bar');
        var dragging = false, dx = 0, dy = 0;
        bar.addEventListener('mousedown', function (e) {
            if (e.target.closest && e.target.closest('.dyn-window-btn')) return;
            dragging = true; dx = e.clientX - win.offsetLeft; dy = e.clientY - win.offsetTop;
            var onMove = function (ev) { if (!dragging) return; win.style.left = (ev.clientX - dx) + 'px'; win.style.top = (ev.clientY - dy) + 'px'; };
            var onUp = function () { dragging = false; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
            document.addEventListener('mousemove', onMove); document.addEventListener('mouseup', onUp);
        });
        win.querySelectorAll('.dyn-window-btn').forEach(function (b) {
            b.addEventListener('click', function () {
                var act = b.getAttribute('data-act');
                if (act === 'close') win.remove();
                else if (act === 'fullscreen') {
                    var full = win.classList.toggle('dyn-window-full');
                    if (full) { win.style.width = '100vw'; win.style.height = '100vh'; win.style.left = '0'; win.style.top = '0'; }
                    else { win.style.width = width + 'px'; win.style.height = height + 'px'; }
                } else if (act === 'min') { win.style.display = (win.style.display === 'none') ? 'flex' : 'none'; }
            });
        });
        win.__dynWin = { width: width, height: height };
        return win;
    }

    // 查找窗体宿主：轻量窗口 → ElementUI 模态 → LayUI 弹层 → 桌面窗口
    // 优先从元素向上找（元素在窗口内部时）；否则回退取页面最上层（最后创建的）窗口宿主，
    // 便于窗口外的按钮也能控制"当前活动窗口"。
    function findWindowHost(el) {
        if (el) {
            var up = findAncestor(el, '.dyn-window') || findAncestor(el, '.dyn-modal-host')
                || findAncestor(el, '.layui-layer') || findAncestor(el, '.window');
            if (up) return up;
        }
        if (typeof document === 'undefined') return null;
        var wins = document.querySelectorAll('.dyn-window');
        if (wins.length) return wins[wins.length - 1];
        var dlgs = document.querySelectorAll('.dyn-modal-host');
        if (dlgs.length) return dlgs[dlgs.length - 1];
        var layers = document.querySelectorAll('.layui-layer');
        if (layers.length) return layers[layers.length - 1];
        var desk = document.querySelectorAll('.window');
        if (desk.length) return desk[desk.length - 1];
        return null;
    }

    // ---- openwindow：打开窗体（自动探测可用 UI 库） ----
    // dyn-click-openwindow='{"url":"/x","title":"标题","type":"auto|modal|layer|newtab|window","width":800,"height":600,"params":{}}'
    function openwindow(ctx) {
        var o = ctx.options || {};
        var type = (o.type || 'auto').toLowerCase();
        if (type === 'auto') type = (window.layui && layui.layer) ? 'layer' : (window.ElementPlus ? 'modal' : 'window');
        if (type === 'newtab') { window.open(o.url || o.href || 'about:blank', '_blank'); return; }
        if (type === 'layer') {
            if (window.layui && layui.layer) {
                // 返回 Promise<layerIndex>，供 chain 步骤拿到弹层索引
                return new Promise(function (resolve) {
                    layui.use(['layer'], function () {
                        var idx = layui.layer.open({
                            type: 2, title: o.title || '窗口',
                            area: [(o.width || 800) + 'px', (o.height || 600) + 'px'],
                            content: o.url || 'about:blank'
                        });
                        resolve(idx);
                    });
                });
            }
            showMessage('LayUI layer 不可用，已回退模态', 'warning');
        }
        if (type === 'window') { return createDynWindow(o); }
        // modal（默认）：复用 el-dialog 模态
        return open({ url: o.url, title: o.title, width: (o.width || 800) + 'px', params: o.params, method: o.method }, ctx.element);
    }
    openwindow._events = ['click'];
    openwindow._label = '打开窗体';
    openwindow._doc = '打开窗体：type=auto(自动)/modal(ElementPlus 模态)/layer(LayUI 弹层)/newtab(新标签)/window(轻量窗口)，支持 url/title/width/height/params';

    // ---- setwindow：设置所在窗体的标题/尺寸/全屏/最小化/关闭 ----
    // dyn-click-setwindow='{"title":"新标题","width":1000,"height":700,"fullscreen":true,"minimize":false,"close":false}'
    function setwindow(ctx) {
        var o = ctx.options || {};
        var host = findWindowHost(ctx.element);
        if (!host) { showMessage('未找到所在窗口', 'warning'); return; }
        if (host.classList.contains('dyn-window')) {
            if (o.title) { var t = host.querySelector('.dyn-window-title'); if (t) t.textContent = o.title; }
            if (o.width || o.height) { if (o.width) host.style.width = o.width + 'px'; if (o.height) host.style.height = o.height + 'px'; }
            if (o.fullscreen) { host.style.width = '100vw'; host.style.height = '100vh'; host.style.left = '0'; host.style.top = '0'; }
            if (o.close) host.remove();
            return;
        }
        if (host.classList.contains('dyn-modal-host') && host.__dynApp && host.__dynApp._instance) {
            var p = host.__dynApp._instance.proxy;
            if (o.title) p.title = o.title;
            if (o.width) p.width = (typeof o.width === 'number' ? o.width + 'px' : o.width);
            if (o.close) p.visible = false;
            return;
        }
        if (host.classList.contains('layui-layer') && window.layui && layui.layer) {
            if (o.close) { layui.layer.close(layui.layer.index || 0); return; }
            if (o.title) { var tt = host.querySelector('.layui-layer-title'); if (tt) tt.textContent = o.title; }
            if (o.width) host.style.width = o.width + 'px';
            return;
        }
        // 桌面窗口（DOM 兜底）
        if (o.title) { var t2 = host.querySelector('.window-title, .title'); if (t2) t2.textContent = o.title; }
        if (o.width || o.height) { if (o.width) host.style.width = o.width + 'px'; if (o.height) host.style.height = o.height + 'px'; }
        if (o.close) host.remove();
    }
    setwindow._events = ['click'];
    setwindow._label = '设置窗口';
    setwindow._doc = '设置所在窗口：title/width/height/fullscreen/minimize/close（支持轻量窗口/ElementPlus 模态/LayUI 弹层/桌面窗口）';

    // ---- setdyncom：设置目标 DynCom 组件配置（configjson/modeljson） ----
    // dyn-click-setdyncom='{"configjson":{...},"modeljson":{...},"selector":"#com","mode":"merge|replace"}'
    function setdyncom(ctx) {
        var o = ctx.options || {};
        var target = o.selector ? (typeof o.selector === 'string' ? document.querySelector(o.selector) : o.selector) : ctx.element;
        if (!target) { showMessage('setdyncom 未找到目标组件', 'warning'); return; }
        var parse = function (v) {
            if (v === undefined || v === null) return null;
            if (typeof v === 'string') { var t = v.trim(); if (t.charAt(0) === '{' || t.charAt(0) === '[') { try { return JSON.parse(t); } catch (e) { return v; } } return v; }
            return v;
        };
        var cfg = parse(o.configjson);
        var mdl = parse(o.modeljson);
        var mode = o.mode || 'merge';
        // 1) 更新 data-* 属性（声明式，供宿主读取）
        if (cfg !== null) target.setAttribute('data-config', (typeof cfg === 'object' ? JSON.stringify(cfg) : String(cfg)));
        if (mdl !== null) target.setAttribute('data-model', (typeof mdl === 'object' ? JSON.stringify(mdl) : String(mdl)));
        // 2) 更新元素上的组件节点存储（__dyncom）
        if (target.__dyncom && typeof target.__dyncom === 'object') {
            if (cfg !== null) target.__dyncom.config = mode === 'replace' ? cfg : Object.assign(target.__dyncom.config || {}, cfg);
            if (mdl !== null) target.__dyncom.model = mode === 'replace' ? mdl : Object.assign(target.__dyncom.model || {}, mdl);
        }
        // 3) 尝试更新 Vue 组件实例（__vueParentComponent.props / setupState）
        var inst = target.__vueParentComponent;
        if (inst) {
            try {
                if (cfg !== null && inst.props) {
                    if (mode === 'replace') Object.keys(inst.props).forEach(function (k) { delete inst.props[k]; });
                    Object.assign(inst.props, cfg);
                }
                if (mdl !== null && inst.setupState) Object.assign(inst.setupState, mdl);
                if (inst.update) inst.update();
            } catch (e) { console.warn('[dyn-lib] setdyncom 更新 Vue 实例失败', e); }
        }
        // 4) 派发自定义事件，宿主可监听重渲染
        target.dispatchEvent(new CustomEvent('dyn:comchange', { detail: { config: cfg, model: mdl, mode: mode }, bubbles: true }));
        return { target: target, config: cfg, model: mdl };
    }
    setdyncom._events = ['click'];
    setdyncom._label = '设置组件配置';
    setdyncom._doc = '设置目标 DynCom 组件配置：configjson/modeljson（对象或 JSON 字符串）+ selector 定位，更新 data-config/data-model/组件实例并派发 dyn:comchange';

    // ---- toast：统一提示（ElementPlus.ElMessage / NutUI.toast / layui layer.msg 自动探测） ----
    function toast(ctx) {
        var o = ctx.options || {};
        var text = o.text || o.message || o.msg || '';
        var type = o.type || 'success';
        if (!text) return;
        if (window.layui && layui.layer && layui.use && (!window.ElementPlus || o.layui)) {
            try { layui.use(['layer'], function () { layui.layer.msg(text); }); return; } catch (e) { }
        }
        showMessage(text, type);
    }
    toast._events = ['click'];
    toast._label = '提示消息';
    toast._doc = '统一提示：text/message + type(success/error/warning)，ElementPlus/NutUI/layui 自动探测';

    // ---- copy：复制文本到剪贴板 ----
    // dyn-click-copy='{"text":"要复制的文本"}'；不带 text 时取元素 value/textContent
    function copy(ctx) {
        var o = ctx.options || {};
        var text = o.text;
        if (text === undefined) text = (ctx.element && 'value' in ctx.element) ? ctx.element.value : (ctx.element ? ctx.element.textContent : '');
        if (text === undefined || text === null) return;
        var s = String(text);
        function done() { showMessage('已复制：' + s.slice(0, 20) + (s.length > 20 ? '…' : ''), 'success'); }
        function fallbackCopy() {
            var ta = document.createElement('textarea');
            ta.value = s; ta.style.position = 'fixed'; ta.style.opacity = '0';
            document.body.appendChild(ta); ta.select();
            try { document.execCommand('copy'); } catch (e) { }
            document.body.removeChild(ta);
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(s).then(done).catch(function () { fallbackCopy(); done(); });
        } else { fallbackCopy(); done(); }
    }
    copy._events = ['click'];
    copy._label = '复制文本';
    copy._doc = '复制文本到剪贴板：text 选项，或取元素 value/textContent';

    // ---- download：下载文件 ----
    // dyn-click-download='{"url":"/files/x.pdf","filename":"x.pdf"}'
    function download(ctx) {
        var o = ctx.options || {};
        var url = o.url || (ctx.element && (ctx.element.href || ctx.element.getAttribute('data-url')));
        if (!url) { showMessage('download 缺少 url', 'warning'); return; }
        var a = document.createElement('a');
        a.href = url; a.download = o.filename || o.name || '';
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
    }
    download._events = ['click'];
    download._label = '下载文件';
    download._doc = '下载文件：url + filename/name';

    // ---- setattr：设置元素属性/样式/文本/HTML ----
    // dyn-click-setattr='{"selector":"#x","attr":{"title":"新标题"},"style":{"color":"red"},"text":"新文本","html":"<b>x</b>"}'
    function setattr(ctx) {
        var o = ctx.options || {};
        var el = o.selector ? (typeof o.selector === 'string' ? document.querySelector(o.selector) : o.selector) : ctx.element;
        if (!el) { showMessage('setattr 未找到元素', 'warning'); return; }
        if (o.attr) Object.keys(o.attr).forEach(function (k) { el.setAttribute(k, o.attr[k]); });
        if (o.style) Object.keys(o.style).forEach(function (k) { el.style[k] = o.style[k]; });
        if (o.text !== undefined) el.textContent = o.text;
        if (o.html !== undefined) el.innerHTML = o.html;
    }
    setattr._events = ['click'];
    setattr._label = '设置元素';
    setattr._doc = '设置元素：attr(属性)/style(样式)/text/html(内容)，selector 定位';

    // ---- confirm：确认框（供 chain 链做分支；返回 Promise<true|false>） ----
    // dyn-click-confirm='{"message":"确定执行？"}'   （单独用仅弹确认框，无副作用）
    function confirmAction(ctx) {
        var o = ctx.options || {};
        var msg = o.message || o.msg || o.text || '确定执行该操作吗？';
        return confirmAsync(msg);
    }
    confirmAction._events = ['click'];
    confirmAction._label = '确认框';
    confirmAction._doc = '确认框：message/msg/text，返回 true(确定)/false(取消)——供 chain 链做中止分支';

    // ---- chain：动作链（按序 await 执行 steps；步骤返回 false 中止；上一步返回值注入 ctx.$result） ----
    // dyn-click-chain='{"steps":[{"action":"confirm","options":{...}},{"action":"openwindow","options":{...}},...]}'
    // 每个步骤可写 {action, options}；options 缺省时步骤对象本身即 options；字符串步骤 = {action: 字符串}
    function chain(ctx) {
        var steps = ctx.options && ctx.options.steps;
        if (Array.isArray(ctx.options)) steps = ctx.options;
        if (!Array.isArray(steps)) { showMessage('chain 需要 steps 数组', 'warning'); return; }
        var last;
        var run = function (i) {
            if (i >= steps.length) return Promise.resolve(last);
            var s = steps[i];
            if (!s) return run(i + 1);
            var actionName = (typeof s === 'string') ? s : (s.action || '');
            var opt = (typeof s === 'object' && s.options) ? s.options : (typeof s === 'string' ? {} : s);
            if (!actionName) { console.warn('[dyn-lib] chain 步骤缺少 action: ', i, s); return run(i + 1); }
            var fn = resolveAction(actionName);
            if (!fn) { showMessage('chain 步骤动作不存在: ' + actionName, 'error'); return run(i + 1); }
            // 步骤 ctx：以链起点元素为上下文，注入链信息 $step/$result/$chain
            var stepCtx = buildCtx(ctx.element, 'chain', ctx.$event || null, opt, actionName);
            stepCtx.$step = i;
            stepCtx.$result = last;
            stepCtx.$chain = steps;
            stepCtx.$chainAction = actionName;
            if (ctx.$event) stepCtx.$event = ctx.$event;
            return Promise.resolve(fn(stepCtx)).then(function (r) {
                if (r === false) { console.log('[dyn-lib] chain 中止于步骤 ' + i + ' (' + actionName + ')'); return last; }
                last = r;
                return run(i + 1);
            });
        };
        return run(0);
    }
    chain._events = ['click'];
    chain._label = '动作链';
    chain._doc = '动作链：steps 数组按序 await 执行；步骤返回 false 中止；上一步返回值注入下一步 ctx.$result，返回最后一步结果';


    // 注册扩展动作到 actionHelper（挂方法即动作，autoBindActions 自动纳入委托与元数据登记）
    actionHelper.confirm = confirmAction;
    actionHelper.chain = chain;
    actionHelper.openwindow = openwindow;
    actionHelper.setwindow = setwindow;
    actionHelper.setdyncom = setdyncom;
    actionHelper.toast = toast;
    actionHelper.copy = copy;
    actionHelper.download = download;
    actionHelper.setattr = setattr;
    // ---- showmessage：显示消息提示（复用 dyn 内部 showMessage，自动探测 ElementPlus/NutUI/LayUI） ----
    // dyn-click-showmessage='{"message":"保存成功","type":"success","title":"提示"}'
    function showmessage(ctx) {
        var o = ctx.options || {};
        showMessage(o.message || '操作成功', o.type || 'success');
    }
    showmessage._events = ['click'];
    showmessage._label = '消息提示';
    showmessage._doc = '显示消息提示：message / type(success|error|warning|info)';

    // ---- redirect：页面跳转 ----
    // dyn-click-redirect='{"url":"/Home/Index"}'
    function redirect(ctx) {
        var o = ctx.options || {};
        var url = o.url || o.href;
        if (url) window.location.href = url;
    }
    redirect._events = ['click'];
    redirect._label = '页面跳转';
    redirect._doc = '跳转到指定 URL：url';

    actionHelper.showmessage = showmessage;
    actionHelper.redirect = redirect;

    // ---- grid：3屏管理组件动作（Filter + Grid + Detail） ----
    // 用法：dyn-click-grid='{"action":"search|clear|add|edit|delete|load","confirm":"..","title":"..","width":"..","id":1}'
    // 容器约定：组件根元素 class="dyn-grid"，并带 data-filter-url / data-list-url / data-add-url / data-edit-url / data-delete-url
    // 行为：search=序列化 filter 区域 → 设置到 list 的 model.Filter → list 重新 load；clear=清空 filter 后同上；
    //       add=open 模态加载 addUrl（带 id=0）；edit=open 模态加载 editUrl（带行 id）；
    //       delete=confirm 后 POST deleteUrl 并刷新 list；load=仅刷新 list
    function grid(ctx) {
        var o = ctx.options || {};
        var el = ctx.element;
        var host = el && el.closest ? el.closest('.dyn-grid') : null;
        if (!host) { showMessage('未找到 3 屏管理组件容器（.dyn-grid）', 'error'); return; }
        var act = String(o.action || 'search').toLowerCase();
        function attr(n) { return host.getAttribute('data-' + n + '-url') || ''; }
        var urls = { filter: attr('filter'), list: attr('list'), add: attr('add'), edit: attr('edit'), del: attr('delete') };
        var filterEl = host.querySelector('.dyn-grid-filter');
        var listEl = host.querySelector('.dyn-grid-list');
        var rowId = o.id != null ? o.id
            : (ctx.params && ctx.params.id != null ? ctx.params.id
                : (ctx.params && ctx.params['data-id'] != null ? ctx.params['data-id'] : null));

        // 序列化 filter 区域输入（name 驱动）→ 过滤空值 → 设置到 list 的 model.Filter
        function applyFilter() {
            var f = {};
            if (filterEl) {
                var ser = serializeForm(filterEl) || {};
                Object.keys(ser).forEach(function (k) {
                    var v = ser[k];
                    if (v !== '' && v != null && !(Array.isArray(v) && v.length === 0)) f[k] = v;
                });
            }
            if (listEl) setVueModel({ element: listEl, options: {} }, 'Filter', f);
            return f;
        }
        function loadList(f) {
            if (!urls.list) { showMessage('未配置 listUrl（data-list-url）', 'error'); return; }
            // 筛选值包成 { Filter: f } 匹配后端 DynSummaryPost.Filter；无筛选时传空对象触发默认查询
            return reload(listEl, { url: urls.list, params: f && Object.keys(f).length ? { Filter: f } : {} });
        }
        function clearFilterInputs(root) {
            if (!root) return;
            var inputs = [].slice.call(root.querySelectorAll('input, select, textarea'));
            inputs.forEach(function (inp) {
                var t = (inp.type || '').toLowerCase();
                if (t === 'checkbox' || t === 'radio') { inp.checked = false; }
                else if (inp.tagName === 'SELECT') { inp.selectedIndex = 0; }
                else { inp.value = ''; }
            });
            inputs.forEach(function (inp) {
                inp.dispatchEvent(new Event('input', { bubbles: true }));
                inp.dispatchEvent(new Event('change', { bubbles: true }));
            });
        }

        if (act === 'search') return loadList(applyFilter());
        if (act === 'clear') { clearFilterInputs(filterEl); return loadList(applyFilter()); }
        if (act === 'load') return loadList();
        if (act === 'add') {
            if (!urls.add) { showMessage('未配置 addUrl（data-add-url）', 'error'); return; }
            return open({ url: urls.add, params: { id: 0 }, title: o.title || '新增', width: o.width || '720px' }, el);
        }
        if (act === 'edit') {
            if (!urls.edit) { showMessage('未配置 editUrl（data-edit-url）', 'error'); return; }
            if (rowId == null) { showMessage('缺少记录 ID', 'warning'); return; }
            return open({ url: urls.edit, params: { id: rowId }, title: o.title || '编辑', width: o.width || '720px' }, el);
        }
        if (act === 'delete') {
            if (!urls.del) { showMessage('未配置 deleteUrl（data-delete-url）', 'error'); return; }
            if (rowId == null) { showMessage('缺少记录 ID', 'warning'); return; }
            var doDel = function () {
                // id 走 URL query（后端 int id 参数从 query 绑定），body 仅保留筛选参数
                var delUrl = urls.del + (urls.del.indexOf('?') >= 0 ? '&' : '?') + 'id=' + encodeURIComponent(rowId);
                return postback(el, { url: delUrl, reload: listEl, message: o.message || '已删除' });
            };
            if (o.confirm) return confirmAsync(o.confirm).then(function (ok) { if (ok) return doDel(); });
            return doDel();
        }
        showMessage('未知 grid 动作: ' + act, 'error');
    }
    grid._events = ['click'];
    grid._label = '3屏管理';
    grid._doc = '3屏管理组件（Filter+Grid+Detail）：action=search|clear|add|edit|delete|load；容器需 .dyn-grid + data-filter/list/add/edit/delete-url；行 id 取 data-id';
    actionHelper.grid = grid;
    // 动作清单函数：返回所有已注册动作的 {name,label,doc,events}
    function actionList() {
        return Object.keys(_actionMeta).map(function (k) { return Object.assign({}, _actionMeta[k]); });
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
        // 优先命中自身或后代最近的 dyn-init（面板类容器 app 常挂在内部 div 上），再向上找祖先
        var host = null;
        if (src.hasAttribute && src.hasAttribute('dyn-init')) host = src;
        else if (src.querySelector) host = src.querySelector('[dyn-init]');
        if (!host) host = closestDynInit(src) || src;
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
        initActionList: _actions, // 初始化动作与事件动作共用 _actions（属性驱动）
        buildCtx: buildCtx,
        resolveAction: resolveAction,
        actionEvents: ACTION_EVENTS.slice(),
        /* --- 约定式动作（actionHelper）：挂方法即动作 --- */
        actionHelper: actionHelper,
        actionList: actionList, // 动作清单：{name,label,doc,events}
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

