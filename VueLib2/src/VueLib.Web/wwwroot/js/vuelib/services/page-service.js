/**
 * VueLib services/page-service：页面加载 / 保存 / 列表
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};

    async function pages() {
        var resp = await fetch(C.pagesApi);
        return resp.json();
    }

    /** 加载页面：返回 {config, model, versions}，并合并 config.extendinfo.componentVersions */
    async function getPage(code) {
        var resp = await fetch(C.pageApi + '/' + encodeURIComponent(code));
        var json = await resp.json();
        if (!json.success) throw new Error(json.message || '页面加载失败');
        var d = json.data;
        var config = d.config || {};
        var model = d.model || {};
        var versions = d.componentVersions || {};
        // 页面 JSON 顶层 extendinfo.componentVersions 也作为版本锁来源（Demo 页用）
        if (config.extendinfo && config.extendinfo.componentVersions) {
            versions = Object.assign({}, config.extendinfo.componentVersions, versions);
        }
        return { code: d.code, name: d.name, config: config, model: model, versions: versions };
    }

    async function savePage(payload) {
        var resp = await fetch(C.pageApi, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        return resp.json();
    }

    window.VueLib = window.VueLib || {};
    window.VueLib.services = window.VueLib.services || {};
    window.VueLib.services.page = { pages: pages, getPage: getPage, savePage: savePage };
    window.VueLib.services.pages = pages;      // 快捷入口
})();
