/**
 * VueLib core/eventbus：全局事件总线（支持命名空间前缀）
 */
(function () {
    'use strict';

    function EventBus() {
        this._map = Object.create(null);
    }

    EventBus.prototype.on = function (name, fn) {
        if (!name || typeof fn !== 'function') return this;
        (this._map[name] = this._map[name] || []).push(fn);
        return this;
    };

    EventBus.prototype.once = function (name, fn) {
        var self = this;
        var wrap = function () {
            self.off(name, wrap);
            fn.apply(null, arguments);
        };
        wrap._raw = fn;
        return this.on(name, wrap);
    };

    EventBus.prototype.off = function (name, fn) {
        if (!name) { this._map = Object.create(null); return this; }
        var list = this._map[name];
        if (!list) return this;
        this._map[name] = fn ? list.filter(function (f) { return f !== fn && f._raw !== fn; }) : [];
        return this;
    };

    EventBus.prototype.emit = function (name) {
        var list = this._map[name];
        if (!list || !list.length) return this;
        var args = Array.prototype.slice.call(arguments, 1);
        list.slice().forEach(function (fn) {
            try { fn.apply(null, args); } catch (e) { console.error('[VueLib.eventBus]', name, e); }
        });
        return this;
    };

    /** 按命名空间批量解绑（如 'demo:' 解绑所有 demo: 前缀事件） */
    EventBus.prototype.offByPrefix = function (prefix) {
        var self = this;
        Object.keys(this._map).forEach(function (name) {
            if (name.indexOf(prefix) === 0) self.off(name);
        });
        return this;
    };

    window.VueLib = window.VueLib || {};
    window.VueLib.eventBus = new EventBus();
})();
