/**
 * VueLib 入口：聚合导出 window.VueLib
 */
(function () {
    'use strict';
    window.VueLib = window.VueLib || {};

    var api = {
        config: window.VueLibConfig || {},
        utils: window.VueLib.utils,
        eventBus: window.VueLib.eventBus,
        debug: window.VueLib.debug,
        dyn: window.VueLib.dyn,
        actions: window.VueLib.actions,
        services: window.VueLib.services,
        runtime: window.VueLib.runtime,
        designer: window.VueLib.designer
    };

    Object.keys(api).forEach(function (k) {
        if (api[k] !== undefined) window.VueLib[k] = api[k];
    });

    window.VueLib.version = '2.0.0';
    if (window.__DYN_DEBUG) {
        console.log('%c[VueLib] v2.0.0 ready', 'color:#2563eb;font-weight:bold', '加载模块: core/actions/services/debug/runtime/designer');
    }
})();
