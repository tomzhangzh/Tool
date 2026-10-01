/**
 * dyn-layout-engine.js — 布局壳运行时（壳模型 v1）
 * ------------------------------------------------------------
 * 概念三分：
 *   - 布局壳 shell：真代码（数量天然收敛：堆叠/主从/页签/弹窗）。声明 slots（槽位期望的
 *       block 角色）、defaultWires（壳内置连线，页面可以不写）、commands（壳自身端口，
 *       如 shell.log）、onMount（壳自身 UI 状态机的挂载点，如分隔条拖拽——由壳自行实现，
 *       回传 dispose 即可，engine 不内置任何具体 UI 行为）。
 *   - 槽内 Block：标准 BlockApp（data-blk-role + __dynBlock 句柄），由服务端按
 *       spec.slots 渲染进 [data-layout-slot] 容器；Block 不知道自己在什么壳里。
 *   - 连线 wire：{ from:'node.event', to:'node.cmd' | ['n.c',...], with?: 模板 }
 *       node 名 = 槽位名（master/detail/...）或 'shell'；
 *       with 模板：'$event.x' 取事件载荷、'$provide.x' 取页面 provide、其余字面量；
 *       省略 with 时把整个事件 payload 透传。
 *
 * 页面规格（DynWebPage.ParamsJson）：
 *   { layout, layoutProps?, provide?, slots:{name:{block,settingId?,model?}}, wires?:[],
 *     replaceDefaultWires?: true }
 *
 * 只负责运行时；具体壳定义由壳视图（Views/DynLayouts/*.cshtml）在 mount 前 register。
 */
(function (global) {
    'use strict';
    var DynLayouts = global.DynLayouts = global.DynLayouts || {};
    var shells = Object.create(null);

    // ---------------- 壳注册表 ----------------

    DynLayouts.register = function (name, def) { shells[name] = def; return def; };
    DynLayouts.get = function (name) { return shells[name] || null; };

    // ---------------- 小工具 ----------------

    function parseAttr(el, name) {
        var raw = el.getAttribute(name);
        if (!raw) return {};
        try { return JSON.parse(raw) || {}; }
        catch (e) { console.error('[DynLayouts] 非法 JSON 属性', name, e); return {}; }
    }

    function path(o, p) {
        if (o == null) return o;
        return p.split('.').reduce(function (a, k) { return (a == null) ? a : a[k]; }, o);
    }

    // with 模板实例化：字符串 '$event.x' / '$provide.x' 解析为值，对象深拷贝逐键解析
    function mapWith(tpl, ctx) {
        if (tpl === undefined || tpl === null) return ctx.event;
        if (typeof tpl !== 'object') {
            if (tpl === '$event') return ctx.event;
            if (tpl.indexOf('$event.') === 0) return path(ctx.event, tpl.slice(7));
            if (tpl.indexOf('$provide.') === 0) return path(ctx.provide, tpl.slice(9));
            return tpl;
        }
        var clone = Array.isArray(tpl) ? [] : {};
        Object.keys(tpl).forEach(function (k) {
            var v = tpl[k];
            clone[k] = (v && typeof v === 'object') ? mapWith(v, ctx) : mapWith(v, ctx);
        });
        return clone;
    }

    function splitRef(ref) {
        var i = String(ref || '').indexOf('.');
        if (i <= 0) return null;
        return { node: ref.slice(0, i), port: ref.slice(i + 1) };
    }

    // node 名 → 该槽 block 角色（shell 是壳自身端口）
    function roleOf(nodeName, spec) {
        if (nodeName === 'shell') return '__shell__';
        var s = spec.slots && spec.slots[nodeName];
        return s ? (s.block || null) : null;
    }

    function makeShellHandle(def, root, spec, handles) {
        var listeners = Object.create(null);
        var cmdNames = def.commands ? Object.keys(def.commands) : [];
        return {
            role: 'shell',
            send: function (cmd, payload) {
                var fn = def.commands && def.commands[cmd];
                if (typeof fn !== 'function') {
                    var msg = '壳命令未注册: "shell.' + cmd + '"（已注册=[' + cmdNames.join(',') + ']）';
                    console.warn('[DynLayouts] ' + msg);
                    if (global.DynDebug && DynDebug.note) DynDebug.note(msg, { source: 'shell' });
                    return Promise.reject(new Error('壳命令未注册: ' + cmd));
                }
                try { return Promise.resolve(fn(payload, { root: root, spec: spec, handles: handles })); }
                catch (e) { return Promise.reject(e); }
            },
            on: function (evt, fn) {
                (listeners[evt] = listeners[evt] || []).push(fn);
                return function () {
                    var arr = listeners[evt];
                    if (!arr) return;
                    var i = arr.indexOf(fn);
                    if (i > -1) arr.splice(i, 1);
                };
            },
            emit: function (evt, payload) {
                (listeners[evt] || []).slice().forEach(function (fn) {
                    try { fn(payload); } catch (e) { console.error('[shell event ' + evt + ']', e); }
                });
            },
            introspect: function () {
                return { commands: cmdNames, events: Object.keys(listeners) };
            },
            destroy: function () { listeners = Object.create(null); }
        };
    }

    // 绑定一批 wires，返回带 origin/off 的接线清单；端口未声明只警告不阻断（失败变吵，不致命）
    function bindWires(spec, handles, wires, warnings) {
        var contracts = (global.DynBlocks && global.DynBlocks.CONTRACTS) || {};
        var bound = [];
        wires.forEach(function (w) {
            var src = splitRef(w.from);
            if (!src) { warnings.push('wire 的 from 必须是 "节点.事件"：' + w.from); return; }
            var targets = (Array.isArray(w.to) ? w.to : [w.to]).map(splitRef);
            if (targets.some(function (t) { return !t; })) {
                warnings.push('wire 的 to 必须是 "节点.命令"：' + w.to); return;
            }

            // —— 端口静态校验（对 DynBlocks.CONTRACTS 已登记的角色生效）——
            var fromRole = roleOf(src.node, spec);
            var fromContract = fromRole && fromRole !== '__shell__' ? contracts[fromRole] : null;
            if (fromContract && fromContract.events.indexOf(src.port) < 0) {
                warnings.push('wire 源端口未声明：' + w.from + '（block=' + fromRole
                    + '，事件=[' + fromContract.events.join(',') + ']）');
            }
            targets.forEach(function (t) {
                var role = roleOf(t.node, spec);
                var contract = role && role !== '__shell__' ? contracts[role] : null;
                if (contract && contract.commands.indexOf(t.port) < 0) {
                    warnings.push('wire 目标端口未声明：' + t.node + '.' + t.port + '（block=' + role
                        + '，命令=[' + contract.commands.join(',') + ']）');
                }
            });

            var sourceHandle = handles[src.node];
            if (!sourceHandle || typeof sourceHandle.on !== 'function') {
                // 壳默认连线对槽位自适应：页面 spec 没放该槽（如无 filter 的壳页面）时安静跳过；
                // 页面自己写的 wire 必须吵闹——那是显式意图，节点缺失就是配置错误。
                if (w.origin !== 'default')
                    warnings.push('wire 源节点不存在或未挂载：' + src.node + '（' + w.from + '）');
                return;
            }
            var live = targets.filter(function (t) {
                if (!handles[t.node] || typeof handles[t.node].send !== 'function') {
                    if (w.origin !== 'default')
                        warnings.push('wire 目标节点不存在或未挂载：' + t.node + '（来自 ' + w.from + '）');
                    return false;
                }
                return true;
            });
            if (live.length === 0) return;

            var off = sourceHandle.on(src.port, function (payload) {
                var arg = mapWith(w.with, { event: payload, provide: spec.provide || {} });
                live.forEach(function (t) {
                    handles[t.node].send(t.port, arg).catch(function (e) {
                        console.error('[DynLayouts] wire 执行失败 ' + w.from + ' → ' + t.node + '.' + t.port, e);
                    });
                });
            });
            bound.push({ from: w.from, to: w.to, origin: w.origin || 'page', off: off });
        });
        return bound;
    }

    // ---------------- 主入口：挂载壳内全部 Block → 映射槽位句柄 → onMount → 接线 ----------------

    DynLayouts.mount = function (root) {
        if (root.__dynLayoutMounted) return Promise.resolve(root.__dynLayout);
        var spec = parseAttr(root, 'data-dyn-page-spec');
        var def = shells[spec.layout];
        if (!def) {
            console.error('[DynLayouts] 未注册的布局壳：' + spec.layout
                + '（已注册=[' + Object.keys(shells).join(',') + ']）');
            return Promise.resolve(null);
        }
        var warnings = [];

        // 1) 挂载壳内全部 BlockApp（mounted 只做自身初始化，不发对外事件，故并发挂载安全）
        var blockEls = root.querySelectorAll('[data-dyn-mode="createApp"]');
        var ready = [].map.call(blockEls, function (el) {
            return (global.dyn && global.dyn.mount) ? global.dyn.mount(el) : null;
        });
        return Promise.all(ready).then(function () {
            // 2) 槽位名 → 句柄
            var handles = { };
            // shell 命令上下文带 handles：壳命令可按 spec 把事件转发给其他槽（如 addChild 预填外键）
            handles.shell = makeShellHandle(def, root, spec, handles);
            [].forEach.call(root.querySelectorAll('[data-layout-slot]'), function (pane) {
                var blkEl = pane.hasAttribute('data-blk-role') ? pane : pane.querySelector('[data-blk-role]');
                var slotName = pane.getAttribute('data-layout-slot');
                if (blkEl && blkEl.__dynBlock) handles[slotName] = blkEl.__dynBlock;
                else warnings.push('槽位 "' + slotName + '" 缺少 Block 或挂载失败');
            });

            // 3) 槽位角色期望校验
            if (def.slots) Object.keys(def.slots).forEach(function (n) {
                var expect = (typeof def.slots[n] === 'string') ? def.slots[n] : def.slots[n].expect;
                var got = spec.slots && spec.slots[n] && spec.slots[n].block;
                if (expect && got && got !== expect) {
                    warnings.push('槽位 "' + n + '" 期望 block=' + expect + '，实际放置 ' + got);
                }
            });

            // 4) 壳自身布局状态机（分隔条等），与连线解耦
            var disposers = [];
            try {
                if (typeof def.onMount === 'function') {
                    var r = def.onMount(root, spec, handles);
                    if (r && typeof r.dispose === 'function') disposers.push(r.dispose);
                }
            } catch (e) {
                warnings.push('壳 onMount 异常：' + (e && e.message));
                console.error('[DynLayouts] onMount 异常', e);
            }

            // 5) 连线：壳默认连线在前，页面 wires 追加（replaceDefaultWires 时只用页面连线）
            var defaultWires = (def.defaultWires || []).map(function (w) {
                return Object.assign({}, w, { origin: 'default' });
            });
            var pageWires = (spec.wires || []).map(function (w) {
                return Object.assign({}, w, { origin: 'page' });
            });
            var wires = spec.replaceDefaultWires ? pageWires : defaultWires.concat(pageWires);
            var bound = bindWires(spec, handles, wires, warnings);

            var info = {
                layout: spec.layout, spec: spec, handles: handles,
                wires: bound, warnings: warnings,
                destroy: function () {
                    bound.forEach(function (b) { try { b.off(); } catch (e) { } });
                    disposers.forEach(function (d) { try { d(); } catch (e) { } });
                    if (handles.shell) handles.shell.destroy();
                    delete root.__dynLayout;
                    root.__dynLayoutMounted = false;
                }
            };
            root.__dynLayout = info;
            root.__dynLayoutMounted = true;

            var nDef = bound.filter(function (b) { return b.origin === 'default'; }).length;
            var nPage = bound.length - nDef;
            console.info('[DynLayouts] 壳就绪：' + spec.layout
                + '，连线 ' + bound.length + ' 条（壳默认 ' + nDef + ' / 页面 ' + nPage + '）'
                + (warnings.length ? '，告警 ' + warnings.length + ' 条' : ''), info);
            warnings.forEach(function (msg) {
                console.warn('[DynLayouts] ' + msg);
                if (global.DynDebug && DynDebug.note) DynDebug.note(msg, { source: 'layout' });
            });
            return info;
        });
    };
})(window);
