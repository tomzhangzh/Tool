/**
 * VueLib services/component-service：动态组件加载 + 版本解析 + 缓存
 *
 * 加载链路：
 *   1) ensureMeta() 拉取组件元数据（name / currentVersion / versions[]）
 *   2) 页面渲染时确定版本：页面锁（extendinfo.componentVersions）> 全局开关 > 当前版本
 *   3) 懒加载：请求未注册组件 → GET componentApi?name=&version= → 快照文本
 *      全量模式：config.componentLoadMode='all' 启动时全部加载
 *   4) 解析快照文本：DOMParser 提取 <template> + <script tag="comconfig">，
 *      new Function 注入 (Vue, _, axios, VueLib, ElementPlus) 执行得到 comConfig
 *   5) 组合组件：由 compositeConfigJson 动态构建（开放属性 + 开放插槽）
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;

    var metaCache = null;                 // 元数据列表
    var metaMap = {};                    // name → meta
    var componentCache = new Map();      // key: name@version → 组件定义/构建函数
    var loadingPromises = new Map();     // key: name@version → Promise（防抖并发）

    /* ---------- 页面上下文（版本锁定 + 当前模型） ---------- */
    var pageContext = { versions: {}, model: null };

    function setPageContext(ctx) {
        pageContext.versions = (ctx && ctx.versions) || {};
        pageContext.model = (ctx && ctx.model) || null;
    }

    function getPageContext() { return pageContext; }

    /* ---------- 元数据 ---------- */
    async function ensureMeta(force) {
        if (metaCache && !force) return metaCache;
        var resp = await fetch(C.componentMetaApi);
        var json = await resp.json();
        metaCache = json.success ? (json.data || []) : [];
        metaMap = {};
        metaCache.forEach(function (m) { metaMap[m.name] = m; });
        return metaCache;
    }

    function getMeta(name) { return metaMap[name] || null; }

    /* ---------- 版本解析 ---------- */
    function resolveVersion(name) {
        var meta = metaMap[name];
        if (!meta) return 0;
        // 1) 页面锁定的版本
        if (C.componentVersionLock && pageContext.versions && pageContext.versions[name]) {
            return pageContext.versions[name];
        }
        // 2) 元数据中当前版本
        return meta.version || 0;
    }

    /* ---------- 快照文本解析 ---------- */
    function parseComponentText(text) {
        var doc = new DOMParser().parseFromString(text, 'text/html');
        var tplEl = doc.querySelector('template');
        if (!tplEl) throw new Error('组件缺少 <template>');
        var template = tplEl.innerHTML;
        var scriptEl = doc.querySelector("script[tag='comconfig']");
        if (!scriptEl) throw new Error('组件缺少 <script tag="comconfig">');
        var scriptText = scriptEl.textContent.trim();
        // eslint-disable-next-line no-new-func
        var factory = new Function('Vue', '_', 'axios', 'VueLib', 'ElementPlus',
            scriptText + ';\nreturn typeof comConfig !== "undefined" ? comConfig : null;');
        var comConfig = factory(window.Vue, window._, window.axios, window.VueLib, window.ElementPlus);
        if (!comConfig) throw new Error('comConfig 解析为空');
        comConfig.template = template;
        return comConfig;
    }

    /* ---------- 组件加载（含缓存） ---------- */
    function cacheKey(name, version) { return name + '@' + version; }

    function lruPut(key, value) {
        componentCache.set(key, value);
        if (componentCache.size > C.componentCacheSize) {
            var first = componentCache.keys().next().value;
            componentCache.delete(first);
        }
    }

    async function fetchComponentText(name, version) {
        var q = 'name=' + encodeURIComponent(name) + '&version=' + (version || 0);
        var resp = await fetch(C.componentApi + '?' + q);
        var json = await resp.json();
        if (!json.success) throw new Error(json.message || '组件加载失败: ' + name);
        return json.data;
    }

    /** 加载组件定义（Razor 快照 / 组合组件统一入口） */
    async function loadComponent(name, version) {
        var meta = getMeta(name);
        if (!meta) throw new Error('组件未注册: ' + name);
        var v = version || resolveVersion(name) || meta.version || 1;
        var key = cacheKey(name, v);

        if (componentCache.has(key)) return componentCache.get(key);
        if (loadingPromises.has(key)) return loadingPromises.get(key);

        var p = (async function () {
            var data = await fetchComponentText(name, v);
            var def;
            if (data.sourceType === 'composite') {
                def = buildCompositeComponent(data);
            } else {
                def = parseComponentText(data.componentText || '');
            }
            if (def && !def.name) def.name = name;
            lruPut(key, def);
            if (window.VueLib.debug) {
                window.VueLib.debug.log('component', 'loaded', key, def);
            }
            return def;
        })();
        loadingPromises.set(key, p);
        try {
            return await p;
        } finally {
            loadingPromises.delete(key);
        }
    }

    /* ---------- 组合组件 ---------- */
    function deepClone(o) { return JSON.parse(JSON.stringify(o)); }

    /**
     * 组合组件：开放属性（instance comoptions 覆盖）+ 开放插槽（instance children 进入 slots[key]）
     */
    function buildCompositeComponent(meta) {
        var NDynamicCom = window.VueLib.runtime.NDynamicCom;
        return {
            name: 'LC_' + meta.name,
            props: {
                jsonconfig: { type: Object, required: true },
                parentmodelinfo: { type: Object, required: false },
                design: { type: Boolean, default: false },
                designApi: { type: Object, default: null },
                nodePath: { type: String, default: 'root' }
            },
            setup: function (props) {
                return function () {
                    var raw = meta.compositeConfigJson || '';
                    if (!raw) {
                        console.error('[VueLib] 组合组件缺少 compositeConfigJson: ' + meta.name);
                        return window.Vue.h('div', { class: 'lc-error', style: 'padding:8px;color:#ee0a24;font-size:12px;border:1px dashed #ee0a24;border-radius:4px;margin:4px' },
                            '组合组件配置缺失: ' + meta.name);
                    }
                    var inner = deepClone(JSON.parse(raw));
                    var inst = props.jsonconfig || {};
                    // 开放属性：实例 comoptions 覆盖组合内部
                    var instOpts = (inst.options && inst.options.comoptions) || {};
                    inner.options = inner.options || {};
                    inner.options.comoptions = Utils.deepMerge({}, inner.options.comoptions || {}, instOpts);
                    // 开放插槽：实例 children 进入 open slot
                    var instChildren = (inst.childrenctrls || []).filter(function (c) { return c && c.component; });
                    inner.slots = inner.slots || {};
                    Object.keys(inner.slots).forEach(function (k) {
                        if (inner.slots[k] && inner.slots[k].open) {
                            inner.slots[k].childrenctrls = instChildren;
                        }
                    });
                    return window.Vue.h(NDynamicCom, {
                        jsonconfig: inner,
                        parentmodelinfo: props.parentmodelinfo,
                        design: props.design,
                        designApi: props.designApi,
                        nodePath: props.nodePath
                    });
                };
            }
        };
    }

    /* ---------- 注册到应用 ---------- */
    var installedApps = {};

    function isInstalled(app) { return !!installedApps[app._uid]; }
    function resetApp(app) { delete installedApps[app._uid]; }

    async function installComponents(app, metas) {
        if (isInstalled(app)) return;
        installedApps[app._uid] = true;
        var list = metas || await ensureMeta();
        var razorList = list.filter(function (m) { return m.sourceType !== 'composite'; });
        var compositeList = list.filter(function (m) { return m.sourceType === 'composite'; });

        // 组合组件：同步注册（渲染时按实例配置构建）
        compositeList.forEach(function (m) {
            var meta = getMeta(m.name);
            app.component('LC_' + m.name, buildCompositeComponent(meta));
        });

        // Razor 组件：defineAsyncComponent 懒加载
        razorList.forEach(function (m) {
            app.component('LC_' + m.name, Vue.defineAsyncComponent({
                loader: function () { return loadComponent(m.name); },
                delay: C.asyncDelay,
                timeout: C.asyncTimeout,
                loadingComponent: {
                    template: '<div class="lc-loading" style="padding:16px;color:#909399;text-align:center;font-size:12px">组件加载中…</div>'
                },
                errorComponent: {
                    props: ['error'],
                    template: '<div class="lc-error" style="padding:8px;color:#ee0a24;font-size:12px;border:1px dashed #ee0a24;border-radius:4px;margin:4px">组件加载失败: {{ error && error.message }}</div>'
                }
            }));
        });

        // 'all' 模式：启动时全量加载文本
        if (C.componentLoadMode === 'all') {
            await Promise.all(razorList.map(function (m) {
                return loadComponent(m.name).catch(function (e) {
                    console.warn('[VueLib] 全量加载失败', m.name, e);
                });
            }));
        }
    }

    function resolveComponentName(name) { return 'LC_' + name; }

    /* ---------- 刷新元数据（设计器保存组合组件后） ---------- */
    async function refreshMeta() {
        metaCache = null;
        metaMap = {};
        return ensureMeta(true);
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.services = window.VueLib.services || {};
    window.VueLib.services.component = {
        ensureMeta: ensureMeta,
        getMeta: getMeta,
        getMetaMap: function () { return metaMap; },
        resolveVersion: resolveVersion,
        parseComponentText: parseComponentText,
        loadComponent: loadComponent,
        installComponents: installComponents,
        resetApp: resetApp,
        isInstalled: isInstalled,
        resolveComponentName: resolveComponentName,
        refreshMeta: refreshMeta,
        setPageContext: setPageContext,
        getPageContext: getPageContext,
        cache: componentCache
    };
})();
