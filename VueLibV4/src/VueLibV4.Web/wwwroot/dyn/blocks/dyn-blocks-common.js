/**
 * dyn-blocks-common.js — 积木公共能力（#6 根上下文解析 + #8 统一 API 契约）
 * ------------------------------------------------------------
 * 必须在各 Block（FilterBlock/ListBlock/DetailBlock）的 dynconfig 执行前加载。
 * 依赖：DynCall（HTTP）、dyn.getApp（根应用注册表）。
 */
(function (global) {
    'use strict';
    var DynBlocks = global.DynBlocks = global.DynBlocks || {};

    // ---------------- 小工具 ----------------

    // 读取元素上的 JSON 属性，失败/无值返回 null
    DynBlocks.parseAttr = function (el, name) {
        try { return JSON.parse(el.getAttribute(name) || 'null') || null; }
        catch (e) { return null; }
    };

    // ---------------- #6 组合模式根应用解析（绝不静默降级） ----------------

    // 元素未声明 data-root-ext-id → 返回 null（独立模式）；
    // 声明了 → { rid, el, app }，app 为 null 表示根 app 尚未就绪/配置错误。
    DynBlocks.getRoot = function (element) {
        var rid = element.getAttribute('data-root-ext-id');
        if (!rid) return null;
        var el = document.getElementById(rid);
        var app = el && global.dyn && typeof dyn.getApp === 'function' ? dyn.getApp(el) : null;
        return { rid: rid, el: el || null, app: app };
    };

    // 组合模式等待根 app 就绪：50ms 轮询、最多约 2 秒。
    // done(app) 成功；done(null, message) 超时/不存在（调用方渲染显式错误，不自建消息中心）。
    DynBlocks.whenRootReady = function (element, done) {
        var root = DynBlocks.getRoot(element);
        if (!root) { done(null, '元素未声明 data-root-ext-id'); return; }
        if (root.app) { done(root.app); return; }
        var tries = 0;
        var timer = setInterval(function () {
            var cur = DynBlocks.getRoot(element);
            if (cur && cur.app) { clearInterval(timer); done(cur.app); }
            else if (++tries >= 40) {
                clearInterval(timer);
                done(null, '组合模式根应用未就绪：' + root.rid + '（请检查 RootExtId 与挂载顺序）');
            }
        }, 50);
    };

    // 组合模式：读取根 app model 上由 data-model-key 指定的配置对象与 data-msg-center-key 指定的消息中心。
    // 成功 {cfg,msg}；缺配置/缺消息中心 {error:文案}（调用方渲染显式错误，不降级）。
    DynBlocks.combinedCtx = function (element, app) {
        var mk = element.getAttribute('data-model-key');
        var msgKey = element.getAttribute('data-msg-center-key') || 'msgCenter';
        var cfg = mk ? app.model[mk] : null;
        var msg = app.model[msgKey] || null;
        if (mk && !(cfg && typeof cfg === 'object')) {
            return { error: '根 model 不存在配置 key【' + mk + '】或值非对象，请检查模板 Partial 传参 ModelKey' };
        }
        if (!msg) return { error: '根 model 不存在消息中心【' + msgKey + '】，请检查模板初始化' };
        return { cfg: cfg || {}, msg: msg };
    };

    // ---------------- #8 数据 API：平台直连 vs 业务库，契约统一一处 ----------------

    DynBlocks.URLS = {
        bizSearch: '/api/business/dyndata/search',
        bizInsert: '/api/business/dyndata/insert',
        bizUpdate: '/api/business/dyndata/update',
        bizDelete: '/api/business/dyndata/delete',
        bizGet: '/api/business/dyndata/get',
        platSearch: '/api/platform/dyndata/search',
        platSave: '/api/platform/dyndata/save',
        platDelete: '/api/platform/dyndata/delete',
        platGet: '/api/platform/dyndata/get'
    };

    // /api/platform/ 前缀 → 平台直连（query table、扁平实体）；其余按业务库契约（body 带 table/project）
    DynBlocks.apiStyle = function (url) {
        return (url || '').indexOf('/api/platform/') === 0 ? 'platform' : 'business';
    };

    // 列表查询：两种风格 POST body 同构 {table,page,size,filter,sort,project}
    DynBlocks.search = function (url, payload, scopeEl) {
        return global.DynCall.post(url, payload || {}, scopeEl ? { scopeEl: scopeEl } : undefined);
    };

    // 明细 GET 地址：平台 /api/platform/dyndata/get?table=&id=；业务多 project
    DynBlocks.getUrl = function (configuredOrStyleBase, table, id, project) {
        var style = DynBlocks.apiStyle(configuredOrStyleBase);
        var base = style === 'platform' ? DynBlocks.URLS.platGet : DynBlocks.URLS.bizGet;
        var q = 'table=' + encodeURIComponent(table) + '&id=' + encodeURIComponent(id);
        if (style !== 'platform') q += '&project=' + encodeURIComponent(project || '');
        return base + '?' + q;
    };

    // 保存 body：平台=扁平实体（端点自动判别增改）；业务={table,data,project}
    DynBlocks.savePayload = function (style, table, form, project) {
        return style === 'platform' ? form : { table: table, data: form, project: project };
    };

    // 保存 URL：平台统一 save 端点（必要时补 query table）；业务按 isEdit 取 update/insert
    DynBlocks.saveUrl = function (style, configuredUrl, table, isEdit) {
        if (style === 'platform') {
            var base = configuredUrl || DynBlocks.URLS.platSave;
            if (base.indexOf('table=') === -1) {
                base += (base.indexOf('?') > -1 ? '&' : '?') + 'table=' + encodeURIComponent(table);
            }
            return base;
        }
        if (configuredUrl) return configuredUrl;
        return isEdit ? DynBlocks.URLS.bizUpdate : DynBlocks.URLS.bizInsert;
    };

    // 删除请求：平台 delete?table=&id=（空 body）；业务 body {table,keys,project}
    DynBlocks.deleteRequest = function (deleteUrl, table, id, keyField, project) {
        var style = DynBlocks.apiStyle(deleteUrl);
        if (style === 'platform') {
            var base = deleteUrl || DynBlocks.URLS.platDelete;
            var url = base + (base.indexOf('?') > -1 ? '&' : '?')
                + 'table=' + encodeURIComponent(table) + '&id=' + encodeURIComponent(id);
            return { url: url, body: {} };
        }
        var keys = {};
        keys[keyField || 'Id'] = id;
        return { url: deleteUrl || DynBlocks.URLS.bizDelete, body: { table: table, keys: keys, project: project } };
    };
})(window);
