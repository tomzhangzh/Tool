/**
 * dyn-lib.js —— dyn 入口装配（拆分后的兼容层）
 * ----------------------------------------------------------------------------
 * 拆分结构：
 *   dyn-core.js       框架核心（工具/挂载/刷新/回发/模态/事件总线/i18n/静态配置）
 *   dyn-actionhelper.js 动作系统 + 动作助手（委托/内置动作/新动作/DB 助手/META）
 *   dyn-lib.js        入口装配（本文件）
 *
 * 兼容性：
 *   - 新页面按顺序引入 dyn-core.js → dyn-actionhelper.js → dyn-lib.js
 *   - 旧页面只引入 dyn-lib.js 也能工作（document.write 同步注入两个依赖）
 * ----------------------------------------------------------------------------
 */
(function (global) {
    'use strict';

    var SRC_DIR = (function () {
        var s = document.currentScript || (function () {
            var sc = document.getElementsByTagName('script');
            return sc[sc.length - 1];
        })();
        var src = (s && s.src) || '';
        return src.substring(0, src.lastIndexOf('/') + 1);
    })();

    function ensure(globalName, fileName) {
        if (global[globalName]) return;
        document.write('<script src="' + SRC_DIR + fileName + '.js"><\/script>');
    }

    // 依赖缺失时同步注入（document.write 仅用于解析期同步加载）
    ensure('dynCore', 'dyn-core');
    ensure('dynActionHelper', 'dyn-actionhelper');

    // 最终装配：window.dyn = dynCore（动作系统已由 dyn-actionhelper.js 注入）
    if (global.dynCore) global.dyn = global.dynCore;

    // ==================== 新架构：PageSetting 组件树局部渲染（ElementUI 版） ====================
    // 依赖：window.dynCom（ComponentMeta 元数据列表）+ window.nutLoadCom（nut-runtime.js 组件加载缓存）
    // 用法：dyn.renderPageConfig(container, config, model) → { app, proxy, model(响应式) }
    // 挂载后容器带 __dynApp / __dynProxy，dyn-lib 动作（setVueModel 等）可直接操作其 model
    var renderPageConfig = async function (container, config, model) {
        var Vue = global.Vue;
        if (!Vue || !global.nutLoadCom) {
            console.error('[dyn.renderPageConfig] 缺少 Vue 或 nutLoadCom');
            return null;
        }
        var reactiveModel = Vue.reactive(model || {});

        // 1) 收集组件元数据（ComponentMeta → nutLoadCom 注册；nutLoadCom 返回 defineAsyncComponent，注册后按需加载）
        var metas = global.dynCom || [];
        var regs = [];
        for (var i = 0; i < metas.length; i++) {
            var m = metas[i];
            var name = m.componentName || m.ComponentName;
            var url = m.loadUrl || m.LoadUrl;
            if (name && url && global.nutLoadCom) regs.push({ name: name, def: global.nutLoadCom(name, url) });
        }

        // 2) 创建局部 app：递归渲染组件树
        var pageApp = Vue.createApp({
            data: function () { return { model: reactiveModel, config: config || {} }; },
            template: '<n-dynamic-com :jsonconfig="config" :parentmodelinfo="model"></n-dynamic-com>',
            methods: {
                getModel: function () { return this.model; }
            }
        });

        // 3) 注册 ElementPlus + 图标 + 递归组件
        if (global.ElementPlus) pageApp.use(global.ElementPlus);
        if (global.ElementPlusIconsVue) {
            Object.keys(global.ElementPlusIconsVue).forEach(function (k) { pageApp.component(k, global.ElementPlusIconsVue[k]); });
        }
        // 3.1) 注册动态组件（DynEl*/DynCom* 等，按需加载）
        for (var ri = 0; ri < regs.length; ri++) {
            var rn = regs[ri].name;
            if (!pageApp._context.components[rn]) pageApp.component(rn, regs[ri].def);
        }

        // NDynamicCom：全局唯一渲染内核（dyn-com.js）
        if (window.DynCom && window.DynCom.NDynamicCom) {
            pageApp.component('NDynamicCom', window.DynCom.NDynamicCom);
        } else {
            console.error('[dyn-lib] DynCom.NDynamicCom 未加载（dyn-com.js 缺失）');
        }

        // 4) 挂载并暴露给 dyn-lib 动作系统
        var proxy = pageApp.mount(container);
        container.__dynApp = pageApp;
        container.__dynProxy = proxy;
        pageApp.model = reactiveModel;
        pageApp.__dynModel = reactiveModel;
        pageApp.getModel = function () { return reactiveModel; };
        return { app: pageApp, proxy: proxy, model: reactiveModel };
    };

    // 挂到 dyn 对象
    if (global.dynCore) {
        global.dynCore.renderPageConfig = renderPageConfig;
        global.dynCore.renderPage = renderPageConfig;
    }
    global.dynRenderPageConfig = renderPageConfig;
})(window);
