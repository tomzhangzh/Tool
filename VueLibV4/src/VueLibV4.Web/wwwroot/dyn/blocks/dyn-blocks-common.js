/**
 * dyn-blocks-common.js — 积木公共能力（统一 API 契约 + Block 实例句柄）
 * ------------------------------------------------------------
 * 必须在各 BlockApp（Apps/*.cshtml）的 dynconfig 执行前加载。
 * 依赖：DynCall（HTTP）。
 *
 * 架构约定：
 *   - 每个 Block 是独立 VueApp，自读容器上的 data-blk-config，不依赖任何根 app；
 *   - Block 间协作只通过挂在容器上的实例句柄 element.__dynBlock（见 createBlockHandle）；
 *   - 模板只负责摆放 UI 与 mount 后的显式接线，Block 内部不感知宿主形态。
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

    // ---------------- 数据 API：平台直连 vs 业务库，契约统一一处 ----------------

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

    // ---------------- Block 实例句柄（独立 app 协作契约） ----------------
    // 每个 Block 挂在自己的独立 VueApp 上，自读 data-blk-config，不依赖任何根 app。
    // 对外协作只通过挂在容器上的实例句柄 element.__dynBlock：
    //   handle.send(cmd, payload)  —— 编排方向 block 下命令（Promise，未注册/已销毁 reject）
    //   handle.on(evt, fn) -> off  —— 编排方订阅 block 事件（返回注销函数）
    //   handle.emit(evt, payload)  —— block 内部向外发事件
    //   handle.destroy()           —— unmount 时调用，之后全部静默（防悬垂订阅/失效命令）
    // 契约：Block 在 mounted 阶段只做自身初始化（首屏加载/回填），不得 emit 对外事件；
    //       外事件只能由用户交互触发——编排方必然已在 mount 完成后完成接线。
    DynBlocks.createBlockHandle = function (element) {
        var commands = Object.create(null);
        var listeners = Object.create(null);
        var destroyed = false;
        var handle = {
            role: element.getAttribute('data-blk-role') || '',
            destroyed: false,
            // block 内部注册命令处理器
            reg: function (cmd, fn) {
                if (!destroyed && typeof fn === 'function') commands[cmd] = fn;
                return handle;
            },
            // 编排方向 block 下发命令
            send: function (cmd, payload) {
                if (destroyed || !commands[cmd]) {
                    return Promise.reject(new Error('Block 命令未注册或实例已销毁: ' + cmd));
                }
                try { return Promise.resolve(commands[cmd](payload)); }
                catch (e) { return Promise.reject(e); }
            },
            // 编排方订阅 block 事件
            on: function (evt, fn) {
                if (destroyed || typeof fn !== 'function') return function () { };
                (listeners[evt] = listeners[evt] || []).push(fn);
                return function () {
                    var arr = listeners[evt];
                    if (!arr) return;
                    var i = arr.indexOf(fn);
                    if (i > -1) arr.splice(i, 1);
                };
            },
            // block 内部向外发事件
            emit: function (evt, payload) {
                if (destroyed) return;
                (listeners[evt] || []).slice().forEach(function (fn) {
                    try { fn(payload); } catch (e) { console.error('[block event ' + evt + ']', e); }
                });
            },
            // 编排方排查用：当前已注册命令/已订阅事件
            introspect: function () {
                return { destroyed: destroyed, commands: Object.keys(commands), events: Object.keys(listeners) };
            },
            destroy: function () {
                destroyed = true;
                handle.destroyed = true;
                commands = Object.create(null);
                listeners = Object.create(null);
            }
        };
        element.__dynBlock = handle;
        return handle;
    };

    // 扫描容器内全部已挂载 Block，返回 { role: handle }（容器自身是 Block 时也包含）。
    // 用于 updateEl/open 注入片段后的接线：谁注入，谁编排。
    DynBlocks.scan = function (container) {
        var out = {};
        var add = function (el) {
            if (el && el.__dynBlock) {
                var role = el.getAttribute('data-blk-role') || 'block';
                if (!out[role]) out[role] = el.__dynBlock;
            }
        };
        if (container && container.getAttribute) add(container);
        if (container && container.querySelectorAll) {
            [].forEach.call(container.querySelectorAll('[data-blk-role]'), add);
        }
        return out;
    };
})(window);
