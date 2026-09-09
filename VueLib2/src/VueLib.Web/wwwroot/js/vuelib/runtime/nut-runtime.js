/**
 * VueLib runtime/nut-runtime：运行时入口
 *  - createNutApp(el, opts)：创建唯一根 Vue 应用（ElementPlus + LCNode + 动态组件注册）
 *  - bootPage / renderPage：加载页面 JSON → 设置版本上下文 → 渲染配置树
 *  - bootEmbeddedPage / bootEmbeddedComponent：TagHelper 嵌入场景
 *  - 单根 App 实例，页面切换不重复 createApp（解决内存泄漏）
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var NDynamicCom = window.VueLib.runtime.NDynamicCom;

    var runtimeApp = null;
    var runtimeContainer = null;

    /** 创建唯一根应用 */
    async function createNutApp(el, options) {
        var container = window.VueLib.utils.resolveEl(el);
        if (!container) throw new Error('createNutApp: 挂载 DOM 不存在');

        if (runtimeApp) {
            try { runtimeApp.unmount(); } catch (e) { /* 忽略 */ }
            if (runtimeContainer) runtimeContainer.innerHTML = '';
            runtimeApp = null;
        }

        var app = Vue.createApp({
            name: 'VueLibRoot',
            template: '<div class="lc-runtime-root"><slot></slot></div>'
        });
        app.use(window.ElementPlus);
        // Vue 对 <lc-node> 的解析顺序为 lc-node → lcNode → LcNode，需注册标准 PascalCase 名
        app.component('LcNode', NDynamicCom);
        app.component('LCNode', NDynamicCom);
        window.VueLib.runtime.LCNodeName = 'LcNode';

        // 安装动态组件（Razor 快照 + 组合组件）
        await window.VueLib.services.component.installComponents(app);

        // 暴露应用上下文（dyn-init 等可继承全局组件/属性）
        window.VueLib._appContext = app._context;

        if (options && typeof options.onReady === 'function') options.onReady(app);
        runtimeApp = app;
        runtimeContainer = container;
        return app;
    }

    /** 在容器内渲染页面配置树（不新建 App） */
    function renderPage(container, config, model, versions) {
        if (!runtimeApp) throw new Error('请先调用 createNutApp');
        var reactiveModel = Vue.reactive(model || {});
        window.VueLib.runtime.currentModel = reactiveModel;
        window.VueLib.services.component.setPageContext({ versions: versions || {}, model: reactiveModel });

        container.innerHTML = '';
        container.__vlRuntimeRoot = true;

        var vnode = Vue.h(NDynamicCom, {
            jsonconfig: config || { component: 'DivContainer', childrenctrls: [] },
            parentmodelinfo: reactiveModel,
            design: false,
            nodePath: 'root',
            depth: 0
        });
        vnode.appContext = runtimeApp._context;
        Vue.render(vnode, container);
    }

    /** 加载并渲染页面 */
    async function bootPage(container, code) {
        if (!runtimeApp) await createNutApp(container);
        var page = await window.VueLib.services.page.getPage(code);
        renderPage(container, page.config, page.model, page.versions);
        return page;
    }

    /** TagHelper：嵌入低代码页面 */
    async function bootEmbeddedPage(container, code) {
        if (!runtimeApp) await createNutApp(container);
        var page = await window.VueLib.services.page.getPage(code);
        renderPage(container, page.config, page.model, page.versions);
        return page;
    }

    /** TagHelper：嵌入单组件 */
    async function bootEmbeddedComponent(container, config) {
        if (!runtimeApp) await createNutApp(container);
        var model = Vue.reactive({});
        window.VueLib.runtime.currentModel = model;
        window.VueLib.services.component.setPageContext({ versions: {}, model: model });
        container.innerHTML = '';
        container.__vlRuntimeRoot = true;
        var vnode = Vue.h(NDynamicCom, {
            jsonconfig: config || { component: 'ElementInput', childrenctrls: [] },
            parentmodelinfo: model,
            design: false,
            nodePath: 'root'
        });
        vnode.appContext = runtimeApp._context;
        Vue.render(vnode, container);
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.runtime = window.VueLib.runtime || {};
    window.VueLib.runtime.createNutApp = createNutApp;
    window.VueLib.runtime.renderPage = renderPage;
    window.VueLib.runtime.bootPage = bootPage;
    window.VueLib.runtime.bootEmbeddedPage = bootEmbeddedPage;
    window.VueLib.runtime.bootEmbeddedComponent = bootEmbeddedComponent;
    window.VueLib.runtime.getApp = function () { return runtimeApp; };
})();
