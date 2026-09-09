/**
 * VueLib core/utils：工具函数（统一使用 lodash 的 get/set）
 * 依赖：lodash(_.)
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};

    var Utils = {
        uid: (function () {
            var n = 0;
            return function (prefix) {
                n++;
                return (prefix || 'vl') + Date.now().toString(36) + '_' + n.toString(36);
            };
        })(),

        getByPath: function (obj, path) {
            if (!obj || !path) return undefined;
            return _.get(obj, path);
        },

        setByPath: function (obj, path, value) {
            if (!obj || !path) return;
            _.set(obj, path, value);
        },

        /** 解析 DOM：支持选择器 / Element / jQuery 风格 */
        resolveEl: function (el) {
            if (!el) return null;
            if (typeof el === 'string') return document.querySelector(el);
            if (el.length && el[0] && el[0].nodeType) return el[0];   // 类数组
            return el.nodeType ? el : null;
        },

        /** 深度合并（不修改源对象） */
        deepMerge: function (dst) {
            dst = dst || {};
            for (var i = 1; i < arguments.length; i++) {
                var src = arguments[i];
                if (!src || typeof src !== 'object') continue;
                for (var k in src) {
                    var sv = src[k];
                    if (sv && typeof sv === 'object' && !Array.isArray(sv)) {
                        dst[k] = Utils.deepMerge(_.isObject(dst[k]) ? dst[k] : {}, sv);
                    } else if (dst[k] === undefined) {
                        dst[k] = sv;
                    }
                }
            }
            return dst;
        },

        /** 模板占位符解析：将 { "a":"{{b.c}}" } 中的 {{path}} 替换为 model 实际值 */
        resolvePlaceholders: function (obj, model) {
            if (obj == null) return obj;
            if (typeof obj === 'string') {
                var m = obj.match(/^\{\{\s*([\w.[\]-]+)\s*\}\}$/);
                if (m) {
                    var v = Utils.getByPath(model, m[1].replace(/\[(\d+)\]/g, '.$1'));
                    return v === undefined ? obj : v;
                }
                return obj.replace(/\{\{\s*([\w.[\]-]+)\s*\}\}/g, function (_, path) {
                    var val = Utils.getByPath(model, path.replace(/\[(\d+)\]/g, '.$1'));
                    return val === undefined ? '' : String(val);
                });
            }
            if (Array.isArray(obj)) return obj.map(function (x) { return Utils.resolvePlaceholders(x, model); });
            if (typeof obj === 'object') {
                var out = {};
                for (var k in obj) out[k] = Utils.resolvePlaceholders(obj[k], model);
                return out;
            }
            return obj;
        },

        /** 读取 DOM 属性上的 JSON（自动容错） */
        parseAttrJson: function (el, attr) {
            var raw = el.getAttribute(attr);
            if (!raw) return null;
            try { return JSON.parse(raw); } catch (e) {
                console.error('[VueLib] 属性 JSON 解析失败', attr, raw);
                return null;
            }
        },

        /** 序列化 GET 参数 */
        toQuery: function (params) {
            if (!params) return '';
            return Object.keys(params).map(function (k) {
                return encodeURIComponent(k) + '=' + encodeURIComponent(params[k] == null ? '' : params[k]);
            }).join('&');
        },

        /** 读取 DOM 输入值（collect 动作用） */
        readInputValue: function (el) {
            if (!el) return '';
            if (el.tagName === 'SELECT' || el.type === 'radio' || el.type === 'checkbox') {
                if (el.type === 'checkbox') return el.checked;
                return el.value;
            }
            return el.value == null ? (el.textContent || '') : el.value;
        },

        /** 数组去重合并 */
        uniqueConcat: function (a, b) {
            return a.concat(b.filter(function (x) { return a.indexOf(x) < 0; }));
        }
    };

    window.VueLib = window.VueLib || {};
    window.VueLib.utils = Utils;
})();
