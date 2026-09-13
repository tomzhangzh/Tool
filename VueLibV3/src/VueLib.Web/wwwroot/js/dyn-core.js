/**
 * dyn-core.js —— dyn 框架核心（V3，无 jQuery 依赖）
 * 静态变量集中在文件头部。
 */
(function (global) {
    'use strict';

    /* ===== DYN_CFG 全局静态配置（文件头部集中）===== */
    var DYN_CFG = {
        PREFIX: '[dyn-v3]',
        HOLDER_ID: 'dyn-holder',
        ATTR_INIT: 'dyn-init',
        ATTR_URL: 'data-dyn-url',
        API_COMPONENTS: '/api/components',
        API_ACTIONHELPERS: '/api/actionhelpers',
        API_DICT: '/api/dict/',
        DEFAULT_LANG: 'zh-CN'
    };

    /* ===== i18n ===== */
    var DYN_LANGS = {
        'zh-CN': { loading: '加载中...', opFail: '操作失败', confirm: '确认?', ok: '确定', cancel: '取消' },
        'en-US': { loading: 'Loading...', opFail: 'Operation failed', confirm: 'Confirm?', ok: 'OK', cancel: 'Cancel' }
    };
    var _lang = DYN_CFG.DEFAULT_LANG;

    function t(key) { return (DYN_LANGS[_lang] || {})[key] || key; }

    /* ===== 路径工具（不依赖 jQuery/lodash，纯原生）===== */
    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce(function (o, k) {
            if (o == null) return undefined;
            // 支持 array[index] 语法
            var m = k.match(/^(\w+)\[(\d+)\]$/);
            if (m) return (o[m[1]] || [])[parseInt(m[2], 10)];
            return o[k];
        }, obj);
    }

    function setPathVal(obj, path, val) {
        if (!obj || !path) return;
        var keys = path.split('.');
        var last = keys.pop();
        var target = keys.reduce(function (o, k) {
            var m = k.match(/^(\w+)\[(\d+)\]$/);
            if (m) { if (!o[m[1]]) o[m[1]] = []; return o[m[1]][parseInt(m[2], 10)] || (o[m[1]][parseInt(m[2], 10)] = {}); }
            if (!o[k]) o[k] = {};
            return o[k];
        }, obj);
        var lm = last.match(/^(\w+)\[(\d+)\]$/);
        if (lm) { if (!target[lm[1]]) target[lm[1]] = []; target[lm[1]][parseInt(lm[2], 10)] = val; }
        else target[last] = val;
    }

    /* ===== Model 获取/设置 ===== */
    function getApp(el) {
        while (el) {
            if (el.__dynApp) return el.__dynApp;
            el = el.parentElement;
        }
        return null;
    }

    function getModel(el) {
        var app = getApp(el);
        return app ? app._model : {};
    }

    function setVueModel(el, path, value) {
        var app = getApp(el);
        if (app && app._model) {
            setPathVal(app._model, path, value);
            if (app._vueInstance && app._vueInstance.$forceUpdate) app._vueInstance.$forceUpdate();
        }
    }

    /* ===== fetch / postback ===== */
    function postJSON(url, data) {
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        }).then(function (r) { return r.json(); });
    }

    /* ===== Message / Confirm ===== */
    function message(type, text) {
        if (global.ElementPlus && ElementPlus.ElMessage) {
            ElementPlus.ElMessage[type || 'info'](text);
        } else {
            console.log('[dyn]', type, text);
        }
    }

    function confirmAsync(text) {
        if (global.ElementPlus && ElementPlus.ElMessageBox) {
            return ElementPlus.ElMessageBox.confirm(text, t('confirm'), { type: 'warning' });
        }
        return Promise.resolve();
    }

    /* ===== Modal open/close ===== */
    function openModal(url, opts) {
        message('info', 'openModal: ' + url);
    }
    function closeModal(el) {
        message('info', 'closeModal');
    }

    /* ===== Reload / updateEl ===== */
    function reload(el, selector) {
        var target = selector ? document.querySelector(selector) : el;
        if (target && target.getAttribute('data-dyn-url')) {
            postJSON(target.getAttribute('data-dyn-url'), getModel(target)).then(function (html) {
                if (typeof html === 'string') target.innerHTML = html;
            });
        }
    }

    function updateEl(selector, html) {
        var el = document.querySelector(selector);
        if (el) el.innerHTML = html;
    }

    /* ===== EventBus ===== */
    var _listeners = {};
    function on(evt, fn) { (_listeners[evt] = _listeners[evt] || []).push(fn); }
    function off(evt, fn) { if (_listeners[evt]) _listeners[evt] = _listeners[evt].filter(function (f) { return f !== fn; }); }
    function emit(evt, data) { (_listeners[evt] || []).forEach(function (fn) { try { fn(data); } catch (e) { console.error(e); } }); }

    /* ===== nextId ===== */
    var _uid = 0;
    function nextId(prefix) { return (prefix || 'id') + '_' + (++_uid) + '_' + Date.now(); }

    /* ===== 导出全局 dyn ===== */
    global.dyn = {
        cfg: DYN_CFG,
        t: t, setLang: function (l) { _lang = l; },
        getByPath: getByPath, setPathVal: setPathVal,
        getApp: getApp, getModel: getModel, setVueModel: setVueModel,
        postJSON: postJSON,
        message: message, confirmAsync: confirmAsync,
        openModal: openModal, closeModal: closeModal,
        reload: reload, updateEl: updateEl,
        on: on, off: off, emit: emit,
        nextId: nextId,
        _actionHelpers: {}
    };

    console.log(DYN_CFG.PREFIX + ' dyn-core v3 loaded');
})(window);
