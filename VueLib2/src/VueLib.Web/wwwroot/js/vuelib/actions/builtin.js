/**
 * VueLib actions/builtin：内置动作集
 * setVar / collect(setvuefilter) / delay / notify / triggerEvent / openwindow / closewindow
 * postback / reload / updateEl / evaljs
 * 窗口服务（element dialog + lay 窗口）：URL 内容通过 updateEl 加载，支持参数。
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;
    var debug = window.VueLib.debug;
    var actions = window.VueLib.actions;

    /* ================= 窗口服务 ================= */
    var windowState = { app: null, mode: null, el: null };

    function openElementWindow(opts) {
        var div = document.createElement('div');
        document.body.appendChild(div);
        var app = Vue.createApp({
            data: function () {
                return { visible: true, title: opts.title || '窗口', width: opts.width || C.windowDefaults.width };
            },
            mounted: function () {
                var self = this;
                if (opts.url) {
                    window.VueLib.dyn.updateEl(this.$refs.content, opts.url, opts.params).then(function () {
                        if (opts.onLoaded) opts.onLoaded(self.$refs.content);
                    });
                }
            },
            methods: {
                onClose: function () {
                    this.visible = false;
                    var el = this.$el;
                    setTimeout(function () { app.unmount(); if (el && el.parentNode) el.parentNode.removeChild(el); }, 200);
                    window.VueLib.eventBus.emit('window:closed', opts);
                }
            },
            template: '' +
                '<el-dialog :model-value="visible" :title="title" :width="width" append-to-body @close="onClose">' +
                '  <div ref="content" class="lc-window-content"></div>' +
                '  <template #footer><el-button size="small" @click="onClose">关 闭</el-button></template>' +
                '</el-dialog>'
        });
        app.use(window.ElementPlus);
        app.mount(div);
        windowState = { app: app, mode: 'element', el: div };
    }

    function openLayWindow(opts) {
        var div = document.createElement('div');
        document.body.appendChild(div);
        var w = opts.width || C.windowDefaults.width;
        var h = opts.height || C.windowDefaults.height;
        var app = Vue.createApp({
            data: function () {
                return {
                    title: opts.title || '窗口',
                    style: { width: w, height: h, left: Math.max(20, (window.innerWidth - parseInt(w)) / 2) + 'px', top: Math.max(20, (window.innerHeight - parseInt(h)) / 3) + 'px', zIndex: 3000 },
                    dragging: false, dragStart: { x: 0, y: 0, left: 0, top: 0 }
                };
            },
            mounted: function () {
                var self = this;
                if (opts.url) {
                    window.VueLib.dyn.updateEl(this.$refs.content, opts.url, opts.params).then(function () {
                        if (opts.onLoaded) opts.onLoaded(self.$refs.content);
                    });
                }
                this._move = function (e) {
                    if (!self.dragging) return;
                    self.style.left = (self.dragStart.left + e.clientX - self.dragStart.x) + 'px';
                    self.style.top = (self.dragStart.top + e.clientY - self.dragStart.y) + 'px';
                };
                this._up = function () { self.dragging = false; };
                document.addEventListener('mousemove', this._move);
                document.addEventListener('mouseup', this._up);
            },
            beforeUnmount: function () {
                document.removeEventListener('mousemove', this._move);
                document.removeEventListener('mouseup', this._up);
            },
            methods: {
                startDrag: function (e) {
                    this.dragging = true;
                    this.dragStart = { x: e.clientX, y: e.clientY, left: parseInt(this.style.left), top: parseInt(this.style.top) };
                },
                onClose: function () {
                    var self = this;
                    var el = this.$el;
                    setTimeout(function () { app.unmount(); if (el && el.parentNode) el.parentNode.removeChild(el); }, 150);
                    window.VueLib.eventBus.emit('window:closed', opts);
                }
            },
            template: '' +
                '<div class="lc-lay-window" :style="style">' +
                '  <div class="lc-lay-window-header" @mousedown="startDrag">' +
                '    <span class="lc-lay-window-title">{{title}}</span>' +
                '    <span class="lc-lay-window-close" @click.stop="onClose">×</span>' +
                '  </div>' +
                '  <div ref="content" class="lc-lay-window-body"></div>' +
                '</div>'
        });
        app.use(window.ElementPlus);
        app.mount(div);
        windowState = { app: app, mode: 'lay', el: div };
    }

    function closeWindow() {
        if (windowState.app) {
            try { windowState.app.unmount(); } catch (e) { /* 忽略 */ }
            if (windowState.el && windowState.el.parentNode) windowState.el.parentNode.removeChild(windowState.el);
            windowState = { app: null, mode: null, el: null };
        }
    }

    window.VueLib.services = window.VueLib.services || {};
    window.VueLib.services.window = {
        open: function (opts) {
            closeWindow();
            opts = opts || {};
            if (opts.mode === 'lay') openLayWindow(opts);
            else openElementWindow(opts);
        },
        close: closeWindow,
        getState: function () { return windowState; }
    };

    /* ================= 内置动作 ================= */

    // 设置变量：{path, value} 或 {path, valueFrom:{selector,attr}}
    actions.register('setVar', async function (ctx, o) {
        var model = ctx.model;
        if (!model) return null;
        var value = o.value;
        if (o.valueFrom) {
            var srcEl = Utils.resolveEl(o.valueFrom.selector || o.valueFrom.el);
            if (srcEl) {
                value = o.valueFrom.attr && o.valueFrom.attr !== 'value'
                    ? srcEl.getAttribute(o.valueFrom.attr)
                    : Utils.readInputValue(srcEl);
            }
        }
        Utils.setByPath(model, o.path, value);
        return { path: o.path, value: value };
    });

    // 从 DOM 收集值到模型：{mapping: {path: {selector, attr}}}（兼容旧 setvuefilter {el,target,path}）
    actions.register('collect', async function (ctx, o) {
        var model = ctx.model;
        if (!model) return null;
        if (o.mapping) {
            for (var path in o.mapping) {
                var m = o.mapping[path] || {};
                var el = Utils.resolveEl(m.selector || m.el);
                if (!el) continue;
                var v = m.attr && m.attr !== 'value' ? el.getAttribute(m.attr) : Utils.readInputValue(el);
                Utils.setByPath(model, path, v);
            }
            return { mapping: o.mapping };
        }
        // 兼容旧 setvuefilter：把 el 容器的表单值拷到 target 模型
        if (o.el && o.target) {
            var root = Utils.resolveEl(o.el);
            if (root) {
                var inputs = root.querySelectorAll('input,select,textarea');
                for (var i = 0; i < inputs.length; i++) {
                    var inp = inputs[i];
                    var name = inp.name || inp.id;
                    if (name) Utils.setByPath(model, o.path ? o.path + '.' + name : name, Utils.readInputValue(inp));
                }
            }
        }
        return null;
    });
    actions.register('setvuefilter', actions._registry.collect);   // 别名

    // 延时：{ms}
    actions.register('delay', function (ctx, o) {
        return new Promise(function (resolve) { setTimeout(function () { resolve({ ms: o.ms }); }, o.ms || 0); });
    });

    // 消息提示：{type, message}  → ElementPlus ElMessage
    actions.register('notify', async function (ctx, o) {
        var ep = window.ElementPlus;
        if (ep && ep.ElMessage) {
            ep.ElMessage[o.type || 'info'](o.message || '');
        } else {
            window.alert(o.message || '');
        }
        return { type: o.type, message: o.message };
    });

    // 触发事件：{name, payload} → eventBus.emit
    actions.register('triggerEvent', async function (ctx, o) {
        var payload = Utils.resolvePlaceholders(o.payload || {}, ctx.model || {});
        window.VueLib.eventBus.emit(o.name, payload, ctx);
        return { name: o.name, payload: payload };
    });

    // 打开窗口：{mode:'element'|'lay', url, title, width, height, params}
    actions.register('openwindow', async function (ctx, o) {
        var params = Utils.resolvePlaceholders(o.params || {}, ctx.model || {});
        window.VueLib.services.window.open({
            mode: o.mode || 'element',
            url: o.url,
            title: o.title || '窗口',
            width: o.width, height: o.height,
            params: params,
            onLoaded: function (contentEl) {
                // 窗口内容里的 dyn-click-* 由全局委托接管
                debug.log('window', 'content loaded', o.url);
            }
        });
        return { mode: o.mode, url: o.url };
    });

    actions.register('closewindow', async function () {
        closeWindow();
        return null;
    });

    // postback：{url, method, params, body, target, renderTarget}
    actions.register('postback', async function (ctx, o) {
        var method = (o.method || 'GET').toUpperCase();
        var url = o.url;
        var params = Utils.resolvePlaceholders(o.params || {}, ctx.model || {});
        var body = null;
        var headers = {};
        if (method === 'GET' || method === 'DELETE') {
            url += (url.indexOf('?') >= 0 ? '&' : '?') + Utils.toQuery(params);
        } else {
            headers['Content-Type'] = 'application/json';
            body = JSON.stringify(o.body ? Utils.resolvePlaceholders(o.body, ctx.model || {}) : params);
        }
        var resp = await fetch(url, { method: method, headers: headers, body: body });
        var ct = resp.headers.get('content-type') || '';
        var data = ct.indexOf('json') >= 0 ? await resp.json() : await resp.text();
        if (o.target) {
            var target = Utils.resolveEl(o.target);
            if (target) {
                if (typeof data === 'string') target.innerHTML = data;
                else if (data && typeof data === 'object' && data.html) target.innerHTML = data.html;
            }
        }
        if (o.renderTarget) {
            var rt = Utils.resolveEl(o.renderTarget);
            if (rt) window.VueLib.dyn.reload(rt);
        }
        return data;
    });

    // reload：{target} 重新加载 dyn-init 容器
    actions.register('reload', async function (ctx, o) {
        if (!o.target) return null;
        var el = Utils.resolveEl(o.target);
        if (el) window.VueLib.dyn.reload(el);
        return { target: o.target };
    });

    // updateEl：{target, url, params} 原始 HTML 注入
    actions.register('updateEl', async function (ctx, o) {
        var params = Utils.resolvePlaceholders(o.params || {}, ctx.model || {});
        await window.VueLib.dyn.updateEl(o.target, o.url, params);
        return { target: o.target, url: o.url };
    });

    // evaljs：{code} 执行字符串代码（⚠️ 受信后端脚本，禁止开放给普通用户）
    actions.register('evaljs', async function (ctx, o) {
        if (C.evalJs === false) {
            console.warn('[VueLib.actions] evaljs 已被 VueLibConfig.evalJs=false 禁用');
            return null;
        }
        if (typeof o.code !== 'string') return null;
        debug.warn('evaljs', '执行受信脚本', o.code);
        // eslint-disable-next-line no-new-func
        var fn = new Function('model', 'el', 'VueLib', 'ctx', o.code);
        return fn(ctx.model, ctx.el, window.VueLib, ctx);
    });

    /* ================= 安装全局委托 ================= */
    actions.installDelegated();
})();
