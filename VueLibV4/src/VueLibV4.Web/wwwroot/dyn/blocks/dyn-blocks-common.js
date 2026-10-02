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
var __plugin = {
    name:'blocks-common', stage:'blocks', requires:[],
    setup:function(ctx){
    'use strict';
    var DynBlocks = global.DynBlocks = global.DynBlocks || {};

    // ---------------- Block 端口契约（静态声明） ----------------
    // 布局壳 wire 运行时（dyn-layout-engine）据此在挂载时校验连线：
    // 连到不存在的事件/命令立即 console.error + 进浮层，而不是等用户点了没反应。
    // 新增 Block 时必须在此登记端口，否则壳页面 spec.wires 引用它会被判为未声明端口。
    DynBlocks.CONTRACTS = {
        filter: { commands: ['submit'], events: ['changed'] },
        list:   { commands: ['loadData', 'reload'], events: ['add', 'edit', 'addChild'] },
        detail: { commands: ['newForm', 'editForm'], events: ['saved', 'cancel'] },
        tree:   { commands: ['reload'], events: ['nodeClick', 'addRoot', 'addChild'] }
    };

    // ---------------- 小工具 ----------------

    // 读取元素上的 JSON 属性，失败/无值返回 null
    DynBlocks.parseAttr = function (el, name) {
        try { return JSON.parse(el.getAttribute(name) || 'null') || null; }
        catch (e) { return null; }
    };

    // ---------------- 数据 API：全系统唯一一套端点，数据域只由 project 坐标决定 ----------------
    //   /api/dyndata/{search,save,insert,update,delete,get}
    //   project 缺省                 → 默认业务库 BusinessDb
    //   project="__platform__"       → 平台元数据库 PlatformDb
    //   project=DynProject.Code/Id   → 项目独立库
    // Block 不再需要知道/配置任何 URL：无配置时全部走下面的缺省；configuredUrl 参数仅为
    // 将来“单个 Block 指向自定义处理器”保留，正常页面规格里不应该出现任何 *Url。
    DynBlocks.URLS = {
        search: '/api/dyndata/search',
        save: '/api/dyndata/save',
        insert: '/api/dyndata/insert',
        update: '/api/dyndata/update',
        delete: '/api/dyndata/delete',
        get: '/api/dyndata/get'
    };

    // 平台库数据域坐标（与后端 ProjectDbResolver.PlatformProjectKey 对应）
    DynBlocks.PLATFORM_PROJECT = '__platform__';

    // 列表查询：POST body {table,page,size,filter,sort?,project?}
    DynBlocks.search = function (url, payload, scopeEl) {
        return global.DynCall.post(url || DynBlocks.URLS.search, payload || {},
            scopeEl ? { scopeEl: scopeEl } : undefined);
    };

    // 单行 GET 地址：/api/dyndata/get?table=&id=&project=
    DynBlocks.getUrl = function (table, id, project) {
        return DynBlocks.URLS.get
            + '?table=' + encodeURIComponent(table)
            + '&id=' + encodeURIComponent(id)
            + '&project=' + encodeURIComponent(project || '');
    };

    // 保存请求：POST {table,data,project?}（服务端按主键自动判别增/改）
    DynBlocks.saveRequest = function (configuredUrl, table, data, project) {
        return { url: configuredUrl || DynBlocks.URLS.save, body: { table: table, data: data, project: project } };
    };

    // 删除请求：POST {table,keys:{Id:...},project?}
    DynBlocks.deleteRequest = function (configuredUrl, table, id, keyField, project) {
        var keys = {};
        keys[keyField || 'Id'] = id;
        return {
            url: configuredUrl || DynBlocks.URLS.delete,
            body: { table: table, keys: keys, project: project }
        };
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
                if (destroyed) {
                    return Promise.reject(new Error('Block 实例已销毁，命令被拒绝: ' + cmd));
                }
                if (!commands[cmd]) {
                    // 失败变吵：命令名拼写错/Block 未挂载/角色不匹配，过去只得到一个静默 reject，
                    // 现象是"点了没反应"。这里把角色、已注册命令与元素位置一起打出来。
                    var registered = Object.keys(commands);
                    var warnMsg = '命令未注册: "' + cmd + '"（role=' + (handle.role || '?')
                        + '，已注册=[' + registered.join(',') + ']）。检查命令名拼写/scan 时机/Block 是否挂载成功。';
                    console.warn('[DynBlocks] ' + warnMsg, element);
                    if (global.DynDebug && DynDebug.note) DynDebug.note(warnMsg, { source: 'block:' + (handle.role || '?') });
                    return Promise.reject(new Error('Block 命令未注册: ' + cmd));
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
    }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);
