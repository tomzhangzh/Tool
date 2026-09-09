/**
 * VueLib debug：结构化调试日志
 *  - 全局开关 window.__DYN_DEBUG（由 config.js 头部控制）
 *  - 元素带 dyn-debug 属性时，点击/初始化打印该元素详细解析信息
 */
(function () {
    'use strict';

    function enabled() { return window.__DYN_DEBUG === true; }

    function log(tag) {
        if (!enabled()) return;
        var args = Array.prototype.slice.call(arguments, 1);
        var prefix = '%c[' + tag + ']';
        console.log(prefix, 'color:#2563eb;font-weight:bold', ...args);
    }

    function warn(tag) {
        if (!enabled()) return;
        var args = Array.prototype.slice.call(arguments, 1);
        console.warn('[' + tag + ']', ...args);
    }

    /** 动作链逐步日志： [DynChain] step0 confirm ok → output: {...} */
    function chainStep(index, stepName, state, extra) {
        if (!enabled()) return;
        var arrow = state === 'ok' ? '→' : (state === 'start' ? '▶' : '✘');
        console.log('%c[DynChain]', 'color:#7c3aed;font-weight:bold',
            'step' + index, stepName, arrow, state,
            extra !== undefined ? 'output: ' + JSON.stringify(extra) : '');
    }

    /** 元素 dyn-debug 详情 */
    function inspectEl(el) {
        if (!el) return;
        var attrs = {};
        for (var i = 0; i < el.attributes.length; i++) {
            var a = el.attributes[i];
            if (a.name.indexOf('dyn-') === 0) attrs[a.name] = a.value;
        }
        var model = null;
        if (window.VueLib && window.VueLib.dyn) model = window.VueLib.dyn.getModel(el);
        console.log('%c[dyn-debug]', 'color:#dc2626;font-weight:bold', el);
        console.log('  属性:', attrs);
        console.log('  模型:', model);
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.debug = { enabled: enabled, log: log, warn: warn, chainStep: chainStep, inspectEl: inspectEl };
})();
