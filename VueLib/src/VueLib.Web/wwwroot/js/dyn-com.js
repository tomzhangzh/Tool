/**
 * dyn-com.js —— 动态组件统一注册表（dyncom 统一管理）
 * ----------------------------------------------------------------------------
 * 目标（重构第 6 项）：dyncom 在 view 中定义、由统一的地方管理，而不是各页面散落。
 *
 * 数据流：
 *   服务端 view（Views/Shared/Components/DynComRegistry/Default.cshtml）把数据库中的
 *   组件清单渲染为 <script type="application/json" id="dyncom-registry">，
 *   本模块读取后作为统一注册表；运行时组件定义由 vueLoadCom（vue-loader.js）按需加载，
 *   加载成功后回注到本注册表。
 *
 * API（挂 window.dynCom）：
 *   dynCom.registry()          → 全部注册项 [{name,label,category,type,icon,loadUrl,version}]
 *   dynCom.get(name)           → 单项注册信息
 *   dynCom.register(name, def) → 注册组件定义（vue-loader 加载成功后调用）
 *   dynCom.load(name)          → 加载组件（未加载则走 vueLoadCom，缓存）
 *   dynCom.meta(name)          → 已注册组件定义
 *   dynCom.has(name)           → 是否已加载
 *   dynCom.events              → 组件注册事件（注册/加载完成时 eventBus 广播 com:registered）
 * ----------------------------------------------------------------------------
 */
(function (global) {
    'use strict';

    var COMPONENT_REGISTRY_ID = 'dyncom-registry';

    /* ========================================================================
     * 静态配置（集中头部，js 不 hardcode）
     * ======================================================================== */
    var DYN_COM_CFG = {
        PREFIX: '[dyn-com]',
        LIST_API: '/api/component/list',
        EVENT_REGISTERED: 'com:registered',
        EVENT_LOADED: 'com:loaded'
    };

    // 注册表：name -> { name, label, category, type, icon, loadUrl, version }
    var _registry = {};
    // 已加载组件定义：name -> vue component options
    var _defs = {};

    /* ---------------- 读取服务端 view 注入的注册表 ---------------- */
    function loadRegistryFromView() {
        var node = document.getElementById(COMPONENT_REGISTRY_ID);
        if (!node) return 0;
        var arr = [];
        try { arr = JSON.parse(node.textContent || '[]'); } catch (e) { console.warn(DYN_COM_CFG.PREFIX + ' 注册表 JSON 解析失败', e); }
        var n = 0;
        (arr || []).forEach(function (item) {
            if (!item || !item.name) return;
            _registry[item.name] = item;
            n++;
        });
        return n;
    }

    /* ---------------- 注册 API ---------------- */
    function register(name, def) {
        if (!name) return;
        _defs[name] = def;
        if (global.dyn && dyn.eventBus) dyn.eventBus.emit(DYN_COM_CFG.EVENT_REGISTERED, { name: name });
        if (global.__DYN_DEBUG) console.log(DYN_COM_CFG.PREFIX + ' 组件已注册: ' + name);
    }

    function get(name) { return _registry[name] || null; }

    function meta(name) { return _defs[name] || null; }

    function has(name) { return !!_defs[name]; }

    function registry() { return Object.keys(_registry).map(function (k) { return _registry[k]; }); }

    /* ---------------- 统一加载入口：有定义直接返回；否则走 vueLoadCom ---------------- */
    function load(name) {
        if (_defs[name]) return Promise.resolve(_defs[name]);
        if (global.vueLoadCom) {
            try {
                var comp = global.vueLoadCom(name);
                // vueLoadCom 是异步组件（defineAsyncComponent），加载成功后由 vue-loader 回调 register()
                return Promise.resolve(comp);
            } catch (e) {
                console.error(DYN_COM_CFG.PREFIX + ' 加载组件失败: ' + name, e);
                return Promise.reject(e);
            }
        }
        return Promise.reject(new Error(DYN_COM_CFG.PREFIX + ' 缺少 vueLoadCom（vue-loader.js 未加载）'));
    }

    /* ---------------- 从 API 刷新注册表（服务端 view 未注入时兜底） ---------------- */
    function refresh() {
        if (!global.fetch) return Promise.resolve(loadRegistryFromView());
        return fetch(DYN_COM_CFG.LIST_API, { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                var arr = (res && Array.isArray(res.data)) ? res.data : null;
                if (!arr) return;
                arr.forEach(function (item) {
                    if (item && item.componentName) {
                        _registry[item.componentName] = {
                            name: item.componentName,
                            label: item.label,
                            category: item.category,
                            type: item.componentType,
                            icon: item.icon,
                            loadUrl: item.loadUrl,
                            version: '1'
                        };
                    }
                });
            })
            .catch(function () { /* 静默 */ });
    }

    // 初始：先读 view 注入的注册表，再异步 refresh 合并最新
    var n = loadRegistryFromView();
    if (global.__DYN_DEBUG) console.log(DYN_COM_CFG.PREFIX + ' 初始注册表: ' + n + ' 项');
    refresh();

    global.dynCom = {
        registry: registry,
        get: get,
        meta: meta,
        has: has,
        load: load,
        register: register,
        refresh: refresh,
        cfg: DYN_COM_CFG
    };
})(window);
