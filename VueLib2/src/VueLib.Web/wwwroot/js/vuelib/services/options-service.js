/**
 * VueLib services/options-service：下拉选项解析
 * 支持 static（静态 items / optionValues 逗号串）、dict（后端字典）、sql（业务库查询）、ajax（前端请求）
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;

    /**
     * 解析选项源
     * @param {object} source {kind:'static'|'dict'|'sql'|'ajax', ...}
     * @param {object} model 当前模型（ajax params 占位符用）
     */
    async function resolveOptions(source, model) {
        if (!source || !source.kind) return [];
        switch (source.kind) {
            case 'static': {
                if (Array.isArray(source.items)) {
                    return source.items.map(function (it) {
                        return { label: it.label != null ? it.label : it.text, value: it.value != null ? it.value : it.id };
                    }).filter(function (x) { return x.label != null; });
                }
                if (typeof source.optionValues === 'string') {
                    return source.optionValues.split(',').map(function (v) { v = v.trim(); return { label: v, value: v }; });
                }
                return [];
            }
            case 'dict':
            case 'sql': {
                var resp = await fetch(C.optionsResolveApi, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        kind: source.kind,
                        typeCode: source.typeCode,
                        sql: source.sql,
                        valueField: source.valueField || 'value',
                        labelField: source.labelField || 'label'
                    })
                });
                var json = await resp.json();
                return json.success ? (json.data || []) : [];
            }
            case 'ajax': {
                var url = source.url || '';
                var params = Utils.resolvePlaceholders(source.params || {}, model || {});
                if (params && Object.keys(params).length) {
                    url += (url.indexOf('?') >= 0 ? '&' : '?') + Utils.toQuery(params);
                }
                if (source.params && source.params._appendKeyword) {
                    var kw = Utils.getByPath(model, source.params._appendKeyword);
                    if (kw) url += (url.indexOf('?') >= 0 ? '&' : '?') + 'keyword=' + encodeURIComponent(kw);
                }
                var resp = await fetch(url);
                var json = await resp.json();
                var list = json.data || (Array.isArray(json) ? json : []);
                var lf = source.labelField || 'label';
                var vf = source.valueField || 'value';
                return list.map(function (it) {
                    return { label: it[lf] != null ? it[lf] : it.label, value: it[vf] != null ? it[vf] : it.value };
                });
            }
            default:
                return [];
        }
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.services = window.VueLib.services || {};
    window.VueLib.services.resolveOptions = resolveOptions;
})();
