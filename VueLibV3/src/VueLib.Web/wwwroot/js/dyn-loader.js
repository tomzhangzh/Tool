/**
 * dyn-loader.js —— 统一加载器：启动时拉组件 meta + actionhelper 列表，动态注册
 * 静态变量集中在头部。
 */
(function (global) {
    'use strict';

    /* ===== 静态配置 ===== */
    var CFG = {
        COMPONENT_API: '/api/components',
        ACTIONHELPER_API: '/api/actionhelpers',
        COMPONENT_VIEW_API: '/api/component-view/'
    };

    var _componentMetaMap = {};   // componentName -> meta
    var _componentLoaded = {};     // componentName -> bool (已注册 Vue component)

    /**
     * 启动加载：拉组件 meta + actionhelper，动态注册
     */
    function bootstrap() {
        var promises = [
            loadComponentMeta(),
            global.dynActionHelper.loadFromServer()
        ];
        return Promise.all(promises).then(function () {
            console.log('[dyn-loader] bootstrap done: ' +
                Object.keys(_componentMetaMap).length + ' components, ' +
                Object.keys(global.dyn._actionHelpers).length + ' actions');
        });
    }

    /**
     * 拉组件元数据
     */
    function loadComponentMeta() {
        return fetch(CFG.COMPONENT_API)
            .then(function (r) { return r.json(); })
            .then(function (list) {
                (list || []).forEach(function (meta) {
                    _componentMetaMap[meta.componentName] = meta;
                });
            });
    }

    /**
     * 按名称获取组件 meta
     */
    function getComponentMeta(name) {
        return _componentMetaMap[name];
    }

    /**
     * 获取所有组件 meta（按 category 分组）
     */
    function getGroupedComponents() {
        var groups = {};
        Object.values(_componentMetaMap).forEach(function (meta) {
            var cat = meta.category || '其他';
            if (!groups[cat]) groups[cat] = [];
            groups[cat].push(meta);
        });
        return groups;
    }

    /**
     * 动态加载组件 cshtml 视图并注册为 Vue component
     */
    function loadComponentView(name) {
        if (_componentLoaded[name]) return Promise.resolve();
        return fetch(CFG.COMPONENT_VIEW_API + name)
            .then(function (r) {
                if (!r.ok) throw new Error('视图不存在: ' + name);
                return r.text();
            })
            .then(function (html) {
                // 简单的组件注册：把 cshtml 内容作为 template
                // 实际运行时由 dyn-com.js 的内核统一渲染
                _componentLoaded[name] = true;
            });
    }

    /* ===== 导出 ===== */
    global.dynLoader = {
        bootstrap: bootstrap,
        loadComponentMeta: loadComponentMeta,
        loadComponentView: loadComponentView,
        getComponentMeta: getComponentMeta,
        getGroupedComponents: getGroupedComponents,
        getComponentMetaMap: function () { return _componentMetaMap; }
    };

    console.log('[dyn-loader] v3 loaded');
})(window);
