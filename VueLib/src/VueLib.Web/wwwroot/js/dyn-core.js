/**
 * dyn-core.js —— dyn 框架核心（拆分自 dyn-lib.js v1.1.0）
 * ----------------------------------------------------------------------------
 * 职责：
 *   1. 工具/祖先查找/Model 解析/fetchPartial
 *   2. app 存储（WeakMap + $(el).data("app")）、嵌套遮蔽、mount/unmount/render
 *   3. init / reload / postback / handleResponse / runJsonActions（Shapeless 动作指令）
 *   4. 模态 open/close、showMessage/confirmAsync、eventBus、路径工具
 *   5. i18n（dyn.t / dyn.lang）与全局静态配置 DYN_CFG（全部集中在文件头部）
 *
 * 依赖：vue.global.js（Vue3 UMD）、jQuery、lodash（可选）、ElementPlus（可选）
 * 配套：dyn-actionhelper.js（动作系统）→ dyn-lib.js（入口装配，兼容旧页面）
 * ----------------------------------------------------------------------------
 */
(function (global) {
    'use strict';

    /* ========================================================================
     * DYN_CFG —— 全局静态配置（用户要求：js 不要 hardcode，静态变量集中在头部）
     * ======================================================================== */
    var DYN_CFG = {
        PREFIX: '[dyn]',
        HOLDER_ID: 'dyn-holder',
        MODEL_SCRIPT_TAG: 'dynmodel',
        MODEL_SCRIPT_TYPE: 'application/json',
        ATTR_INIT: 'dyn-init',
        ATTR_URL: 'data-url',
        ATTR_DYN_URL: 'data-dyn-url',
        ATTR_DYN_LOAD: 'data-dyn-load',
        LOADING_HTML: '<div class="dyn-loading">加载失败：{{msg}}</div>',
        CONFIRM_TITLE: '提示',
        CONFIRM_OK: '确定',
        CONFIRM_CANCEL: '取消',
        CONTAINER_SELECTOR: '[data-dyn-url]:not([dyn-init])',
        API_COMPONENT_BASE: '/api/component',
        DEFAULT_LANG: 'zh-CN',
        MSG_NEED_VUE: '请先引入 Vue3 UMD（vue.global.js）',
        MSG_JSON_INVALID: 'dyn-init 不是合法 JSON：',
        MSG_MODEL_SCRIPT_FAIL: 'dynmodel 解析失败',
        MSG_RELOAD_FAIL: '刷新失败：',
        MSG_POSTBACK_NO_URL: 'dyn-click-postback 缺少 url',
        MSG_JSON_ACTION_NOT_REG: 'JSON 动作未注册: ',
        MSG_JSON_ACTION_FAIL: 'JSON 动作执行失败: ',
        MSG_JSON_ACTION_ERR: 'JSON 动作执行异常',
        MSG_SETPATH_NEED_LODASH: 'setPathVal 需要 lodash（_.set）支持路径设置',
        MSG_BUS_ERR: '事件处理异常: ',
        DESIGNER_CONTAINERS: ['DynNForm', 'DynNCellGroup', 'DynNDivContainer', 'DynNGrid', 'DynElDivContainer', 'DynElCard', 'DynElRow', 'DynElCol', 'DynElTabs']
    };

    /* ========================================================================
     * i18n —— JS 端多语言（最少实现；dyn.t / dyn.setLang / dyn.addLang）
     * ======================================================================== */
    var DYN_LANGS = {
        'zh-CN': {
            confirmTitle: '提示',
            confirmOk: '确定',
            confirmCancel: '取消',
            loadingFail: '加载失败：',
            reloadFail: '刷新失败：',
            opFail: '操作失败',
            copied: '已复制：'
        },
        'en-US': {
            confirmTitle: 'Confirm',
            confirmOk: 'OK',
            confirmCancel: 'Cancel',
            loadingFail: 'Load failed: ',
            reloadFail: 'Refresh failed: ',
            opFail: 'Operation failed',
            copied: 'Copied: '
        }
    };
    var _lang = DYN_CFG.DEFAULT_LANG;
    function t(key, vars) {
        var s = (DYN_LANGS[_lang] || DYN_LANGS[DYN_CFG.DEFAULT_LANG] || {})[key];
        if (s === undefined) s = key;
        if (vars) { Object.keys(vars).forEach(function (k) { s = String(s).replace(new RegExp('\\{\\{' + k + '\\}\\}', 'g'), vars[k]); }); }
        return s;
    }
    function setLang(lang) { _lang = lang || _lang; }
    function addLang(lang, dict) { DYN_LANGS[lang] = Object.assign({}, DYN_LANGS[lang], dict); }

    var Vue = global.Vue;
    if (!Vue) { console.error(DYN_CFG.PREFIX + ' ' + DYN_CFG.MSG_NEED_VUE); return; }
    var $ = global.jQuery;

    var _uidSeq = 0;
    var _holder = null;
    var _appMap = (typeof WeakMap !== 'undefined') ? new WeakMap() : null;
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
        var reactiveModel = Vue.reactive(model);
        $(el).find('script').remove();

        // 3) 遮蔽嵌套 dyn-init（先于读取模板，外层不会编译到子应用内容）
        var nested = [];
        maskNested(el, nested);

        // 4) 组装组件：模板 = 容器现有 innerHTML；Model 响应式暴露为 model
        var component = {
            template: el.innerHTML,
            data: function () { return {}; },
            setup: function () {
                var exposed = { model: reactiveModel, element: el, dyn: dyn };
                if (cfg && typeof cfg.setup === 'function') {
                    var extra = cfg.setup({ model: reactiveModel, element: el }) || {};
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
        // 属性框内核组件（dyn-com.js 内置，dyn-init app 通用）
        if (global.DynCom) {
            ['NDynamicCom', 'DynPropItem', 'DynPropControl', 'DynPropContainer'].forEach(function (cn) {
                if (global.DynCom[cn]) app.component(cn, global.DynCom[cn]);
            });
        }
        app.config.globalProperties.$dyn = dyn;
        el.__dynApp = app;
        el.__dynModel = reactiveModel;
        app.__dynModel = reactiveModel;   // getApp(el) 返回的 app 上可直接取 model
        el.__dynLoaded = true;
        storeApp(el, app);
        // Vue 3.5 起 app._instance 不再被填充，直接取 mount 返回的 proxy 供 getModel 使用
        try { el.__dynProxy = app.mount(el) || null; } catch (e) { el.__dynProxy = null; throw e; }
        // getApp(el) 返回的 app 上挂响应式 model（__dynModel 是原始对象，改它不触发视图；model 是 Vue.reactive 代理）
        app.__dynProxy = el.__dynProxy;
        app.model = el.__dynProxy ? el.__dynProxy.model : Vue.reactive(model);

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
        // 根套根防护：el 自身是 dyn-init 根，且 html 首个元素也是 dyn-init 根 → 只取内部内容，避免嵌套 mount 删掉内层 dynmodel
        if (el.hasAttribute && el.hasAttribute('dyn-init') && typeof html === 'string') {
            var tmp = document.createElement('div');
            tmp.innerHTML = html;
            var root = tmp.firstElementChild;
            if (root && root.hasAttribute && root.hasAttribute('dyn-init')) html = root.innerHTML;
        }
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
        // 兼容直接传入 getApp(el) 返回的 app 实例
        if (el && !el.nodeType) {
            if (el.__dynProxy && el.__dynProxy.model) return el.__dynProxy.model;
            if (el.__dynModel) return el.__dynModel;
        }
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
        if (dyn._initActions) dyn._initActions(root);
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
                var fn = dyn.resolveAction ? dyn.resolveAction(name) : null;
                if (!fn) { console.warn('[dyn-lib] JSON 动作未注册: ' + name, item); return; }
                var fakeEl = document.createElement('div');
                fakeEl.style.display = 'none';
                if (root && root.nodeType === 1) root.appendChild(fakeEl);
                var ctx = dyn.buildCtx ? dyn.buildCtx(fakeEl, 'init', null, item.options || {}, name) : null;
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
                            // onEvent 扩展：窗体加载完成后执行 onopen 动作链（由 dyn-actionhelper 提供 _runEvents）
                            if (dyn._runEvents && opts.events && opts.events.onopen) {
                                dyn._runEvents(opts.events.onopen, { element: triggerEl, options: opts, holder: holder });
                            }
                        });
                    }).catch(function (err) {
                        self.error = (err && err.message) || '加载失败';
                        self.loading = false;
                    });
                },
                onClosed: function () {
                    // onEvent 扩展：窗体关闭时执行 onclose 动作链（由 dyn-actionhelper 提供 _runEvents）
                    if (dyn._runEvents && opts.events && opts.events.onclose) {
                        dyn._runEvents(opts.events.onclose, { element: triggerEl, options: opts, holder: holder });
                    }
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


    /* ---------------- 对外 API（核心部分；动作 API 由 dyn-actionhelper.js 注入） ---------------- */

    var dyn = {
        VERSION: '1.2.0',
        /* --- 配置/多语言 --- */
        CFG: DYN_CFG,
        i18n: { t: t, setLang: setLang, addLang: addLang, lang: function () { return _lang; }, dict: DYN_LANGS },
        t: t, setLang: setLang, addLang: addLang,
        /* --- 核心 --- */
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
        resolve: resolve,
        findAncestor: findAncestor,
        closestDynInit: closestDynInit,
        closestDataUrl: closestDataUrl,
        /* --- 其他 --- */
        updateEl: updateEl,
        serializeForm: serializeForm,
        setDynCfg: setDynCfg,
        setVueModel: function () {
            var f = (typeof dynActionHelper !== "undefined" && dynActionHelper.setVueModel) ? dynActionHelper.setVueModel : null;
            return f ? f.apply(this, arguments) : undefined;
        },
        getVueModel: getVueModel,
        /* --- 动作系统注入点（由 dyn-actionhelper.js 调用） --- */
        _actionApiInstalled: false,
        installActionApi: function (api) {
            if (!api || typeof api !== 'object') return;
            Object.keys(api).forEach(function (k) {
                if (k === 'i18n') return;
                dyn[k] = api[k];
            });
            dyn._actionApiInstalled = true;
            if (dyn._initActions && dyn.rebind) { try { dyn.rebind(); } catch (e) { } }
        }
    };

    global.dynCore = dyn;
    // 中间态：动作系统尚未加载时也保持 dyn 可用（dyn-lib.js 装配后为最终对象）
    global.dyn = dyn;
})(window);
