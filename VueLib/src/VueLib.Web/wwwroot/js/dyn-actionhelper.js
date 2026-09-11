/**
 * dyn-actionhelper.js —— dyn 动作系统 + 动作助手（拆分自 dyn-lib.js v1.1.0）
 * ----------------------------------------------------------------------------
 * 职责：
 *   1. 动作注册表（约定式：actionHelper 挂方法即动作）+ 通用事件委托
 *   2. 内置动作 + 扩展动作（openwindow/setwindow/setdyncom/toast/copy/download/setattr/grid...）
 *   3. 新增动作：switch / setvar / delay / notify / triggerevent
 *   4. chain 动作链 onEvent 扩展（onopen/onclose）
 *   5. 从数据库加载自定义动作助手（/api/dynactionhelper），META 统一管理
 *   6. 动作 META 目录（actionMeta / getMeta / actionList）
 *
 * 依赖：dyn-core.js（必须先行加载）；由 dyn-lib.js 装配为最终 window.dyn
 * ----------------------------------------------------------------------------
 */
(function (global) {
    'use strict';

    if (!global.dynCore) {
        console.error('[dyn-actionhelper] 请先加载 dyn-core.js');
        return;
    }
    var dyn = global.dynCore;
    var Vue = global.Vue;
    var $ = global.jQuery;

    /* ========================================================================
     * DYN_CFG —— 动作系统静态配置（集中头部，js 不 hardcode）
     * ======================================================================== */
    var DYN_CFG = {
        PREFIX: '[dyn]',
        ACTION_API_BASE: '/api/dynactionhelper',
        ACTION_EVENTS: ['change', 'click', 'dblclick', 'error', 'focus', 'select', 'mouseover'],
        DEFAULT_CONFIRM: '确定执行该操作吗？',
        MSG_CHAIN_NEED_STEPS: 'chain 需要 steps 数组',
        MSG_CHAIN_STEP_MISSING: 'chain 步骤缺少 action: ',
        MSG_CHAIN_ACTION_NOT_FOUND: 'chain 步骤动作不存在: ',
        MSG_CHAIN_ABORT: 'chain 中止于步骤 ',
        MSG_ACTION_NOT_FOUND: '动作未注册: ',
        MSG_GRID_HOST_NOT_FOUND: '未找到 3 屏管理组件容器（.dyn-grid）',
        MSG_SETATTR_NO_EL: 'setattr 未找到元素',
        MSG_COPY_DONE: '已复制：',
        MSG_DOWNLOAD_NO_URL: 'download 缺少 url',
        MSG_OPEN_NO_URL: '打开窗体缺少 url',
        CHAIN_EDITOR_URL: '/Designer/ActionChainEditor'
    };

    /* ========================================================================
     * 核心函数别名（跨文件拆分：动作代码直接复用 dyn-core 内部能力）
     * ======================================================================== */
    var resolve = dyn.resolve;
    var findAncestor = dyn.findAncestor;
    var closestDynInit = dyn.closestDynInit;
    var parseModel = dyn.parseModel;
    var fetchPartial = dyn.fetchPartial;
    var showMessage = dyn.showMessage;
    var confirmAsync = dyn.confirmAsync;
    var init = dyn.init;
    var getApp = dyn.getApp;
    var getModel = dyn.getModel;
    var postback = dyn.postback;
    var open = dyn.open;
    var close = dyn.close;
    var reload = dyn.reload;
    var updateEl = dyn.updateEl;
    var serializeForm = dyn.serializeForm;
    var setPathVal = dyn.setPathVal;
    var getVueModel = dyn.getVueModel;
    var getByPath = dyn.getByPath;
    var eventBus = dyn.eventBus;
    var deepClone = dyn.deepClone;
    var isContainerComp = dyn.isContainerComp;
    var nextId = dyn.nextId;

    /* ============================================================================
     * 动作统一挂在 actionHelper 上（挂方法即动作），自动进入委托选择器，
     * 扩展新动作无需改委托代码。动作统一收一个 ctx 上下文对象（比 common.js 的函数
     * 签名反射更安全、更显式）：
     *   ctx = { element, event, $event, targetInfo, action, options, params,
     *           model(最近 dyn-init 祖先), url(祖先 data-dyn-url) }
     * 兼容旧属性 dyn-click-postback/open/close/reload（作为内置动作注册）。
     * ============================================================================ */

    var ACTION_EVENTS = ['change', 'click', 'dblclick', 'error', 'focus', 'select', 'mouseover'];
    var _actions = {};
    var _selCache = {};
    var _actionMeta = {};

    // ===== 约定式动作（actionHelper）：唯一的动作注册方式，无需 registerAction / registerInitAction =====
    // 用法（参考 common.js 的「挂方法即动作」）：
    //   dyn.actionHelper.post = function (ctx) { ... }      → 挂上即可：
    //       1) 被任意 dyn-{event}-post 属性触发（事件由属性名决定）
    //       2) 被 dyn-init-post 属性在初始化（页面/Vue init 或 innerHTML 更新）时触发
    //   fn._events = ['click']                              → 可选白名单：事件委托只认这些事件（不设 = 全部事件属性均可）
    //   fn._skip   = true                                   → 跳过（辅助方法用下划线前缀或此标记隔离）
    // 触发规则：事件是否触发由元素上的 dyn-{event}-{action} 属性名决定（与 common.js 的 t-{event}-{fn} 一致）；
    //           初始化是否触发由 dyn-init-{action} / dyn-{action}-init 属性决定；没有对应动作函数时静默忽略。
    // 运行时新增动作后调用 dyn.rebind()（= autoBindActions()）重新生成委托选择器。
    var actionHelper = {};
    function autoBindActions() {
        Object.keys(actionHelper).forEach(function (name) {
            var fn = actionHelper[name];
            if (typeof fn !== 'function' || fn._skip) return;
            // 挂上即注册：同一函数既可用于事件委托（dyn-{event}-{name}），也可用于初始化扫描（dyn-init-{name}）
            _actions[name] = fn;
            // 登记自描述元数据（供 dyn.actionList() 枚举 / 工具面板 / 自动生成文档）
            _actionMeta[name] = {
                name: name,
                label: fn._label || name,
                doc: fn._doc || '',
                events: fn._events ? fn._events.slice() : ACTION_EVENTS.slice()
            };
        });
        _selCache = {};
        return dyn;
    }
    // 内置/自定义动作的便捷定义：挂到 actionHelper。事件与初始化均由属性名驱动，无需任何标记。
    function defineAction(name, fn) {
        actionHelper[name] = fn;
        return fn;
    }

    // 构造统一上下文（动作方法的唯一入参）
    // 占位符替换：把 options（已解析 JSON）所有字符串中的 {{key}} 用 params（按钮 data-* 属性）替换
    function applyTpl(val, params) {
        if (typeof val === 'string') {
            // 循环替换：{{detail-url}} 替换出的 URL 里可能还有 {{id}}，一轮不够
            var guard = 0;
            while (guard++ < 5 && /\{\{/.test(val)) {
                val = val.replace(/\{\{([\w.-]+)\}\}/g, function (_, k) {
                    return params && params[k] !== undefined ? params[k] : _;
                });
            }
            return val;
        }
        if (val && typeof val === 'object') {
            Object.keys(val).forEach(function (k) {
                var v = val[k];
                if (typeof v === 'string' || (v && typeof v === 'object')) val[k] = applyTpl(v, params);
            });
        }
        return val;
    }

    function buildCtx(el, eventName, $event, options, actionName) {
        options = options || {};
        var params = {};
        if (el && el.attributes) {
            [].forEach.call(el.attributes, function (a) {
                if (a.name.indexOf('data-') === 0 && a.name.indexOf('data-dyn') !== 0 && a.name.indexOf('data-v-') !== 0) {
                    params[a.name.substring(5)] = a.value;
                }
            });
        }
        // 先替换占位符（用 data-* 原始值），再合并显式 options.params（显式值覆盖占位符结果）
        applyTpl(options, params);
        options.params = Object.assign({}, params, options.params || {});
        var ancEl = closestDynInit(el) || el;
        var app = getApp(ancEl);
        return {
            element: el, el: el,
            event: eventName, $event: $event, targetInfo: $event,
            action: actionName,
            options: options,
            params: options.params,
            model: getModel(ancEl) || parseModel(ancEl) || {},
            vm: app && app._instance ? app._instance.proxy : null,  // Vue 组件实例
            url: options.url || (ancEl && ancEl.getAttribute ? ancEl.getAttribute('data-dyn-url') : '') || ''
        };
    }

    // 为某事件生成委托选择器（由注册表动态生成，注册动作时失效缓存）
    function selectorFor(eventName) {
        if (_selCache[eventName]) return _selCache[eventName];
        var sels = [];
        Object.keys(_actions).forEach(function (name) {
            var fn = _actions[name];
            // 动作声明了 _events 白名单时只认这些事件；未声明 = 全部事件属性均可触发
            if (fn && fn._events && fn._events.indexOf(eventName) < 0) return;
            // DOM 属性名一律被 HTML 解析器小写化，这里用小写生成选择器保证匹配
            sels.push('[dyn-' + eventName + '-' + name.toLowerCase() + ']');
        });
        _selCache[eventName] = sels.join(',');
        return _selCache[eventName];
    }

    // 解析元素属性：找 dyn-{event}-{action}，返回 { action, raw }
    function resolveActionAttr(el, eventName) {
        var prefix = 'dyn-' + eventName + '-';
        if (!el || !el.attributes) return null;
        var hit = null;
        [].forEach.call(el.attributes, function (a) {
            if (a.name.indexOf(prefix) === 0) hit = { action: a.name.substring(prefix.length), raw: a.value };
        });
        return hit;
    }

    // 解析 options：JSON 优先；裸字符串兼容旧 dyn-click-reload 的选择器写法
    function parseActionOptions(raw) {
        if (!raw || !raw.trim()) return {};
        var t = raw.trim();
        var c = t.charAt(0);
        if (c === '{' || c === '[') { try { return JSON.parse(t); } catch (e) { return { selector: t }; } }
        return { selector: t };
    }

    // 大小写不敏感查找动作：HTML 属性名会被浏览器转成小写（如 dyn-click-setVueModel → setvuemodel），
    // 而动作可能以驼峰注册（setVueModel），这里做兼容匹配。
    function resolveAction(name) {
        if (_actions[name]) return _actions[name];
        var lower = name.toLowerCase();
        var keys = Object.keys(_actions);
        for (var i = 0; i < keys.length; i++) {
            if (keys[i].toLowerCase() === lower) return _actions[keys[i]];
        }
        return null;
    }

    // 通用事件委托：每个事件一个 document capture 监听器，覆盖动态渲染出的所有元素
    var _delegationBound = false;
    function bindDelegation() {
        if (_delegationBound) return;
        _delegationBound = true;
        var doBind = function () {
    ACTION_EVENTS.forEach(function (ev) {
        document.addEventListener(ev, function (e) {
            var sel = selectorFor(ev);
            if (!sel) return;
            var el = e.target && e.target.closest ? e.target.closest(sel) : null;
            if (!el) return;
            var hit = resolveActionAttr(el, ev);
            if (!hit) return;
            var fn = resolveAction(hit.action);
            if (!fn) return;
            var ctx = buildCtx(el, ev, e, parseActionOptions(hit.raw), hit.action);
            // P5: 提供 prevent 选项（默认阻止）
            var prevent = ctx.options.prevent !== false;
            if (prevent) {
                e.preventDefault();
                e.stopPropagation();
            }
            // 异步执行动作（支持 async / 返回 Promise）；reject 统一提示，不阻断后续
            Promise.resolve(fn(ctx)).catch(function (err) {
                console.error('[dyn-lib] 动作执行失败: ' + hit.action, err);
                showMessage('操作失败：' + ((err && err.message) || err), 'error');
            });
        }, true);
    });
        };
        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', doBind);
        else setTimeout(doBind, 0);
    }
    bindDelegation();

    // ===== 内置动作（全部挂 actionHelper，约定式自动绑定；兼容旧 dyn-click-postback/open/close/reload） =====
    defineAction('postback', function (ctx) {
        var o = ctx.options || {};
        if (o.confirm) {
            var msg = o.confirm === true ? '确定执行该操作吗？' : o.confirm;
            return confirmAsync(msg).then(function (ok) { if (ok) return postback(ctx.element, o); });
        }
        return postback(ctx.element, o);
    });
    defineAction('postdata', function (ctx) {
        return _actions.postback(ctx);
    });
    defineAction('confirm-postdata', function (ctx) {
        var o = Object.assign({}, ctx.options || {});
        if (!o.confirm) o.confirm = true;
        return _actions.postback(Object.assign({}, ctx, { options: o }));
    });
    defineAction('reload', function (ctx) {
        var o = ctx.options || {};
        return reload(o.selector || ctx.element);
    });
    defineAction('open', function (ctx) {
        return open(ctx.options || {}, ctx.element);
    });
    defineAction('close', function (ctx) {
        close(ctx.element);
        return true;
    });
    defineAction('updateel', function (ctx) {
        var o = ctx.options || {};
        return updateEl(o.selector || ctx.element, o.url, o.params);
    });
    // evaljs：事件动作 + 初始化动作共用同一实现
    defineAction('evaljs', function (ctx) {
        var code = ctx.options;
        if (typeof code === 'object') code = code.js || code.code || code.selector || code;
        if (!code) return;
        try {
            // 使用 new Function 执行代码
            // eslint-disable-next-line no-new-func
            var result = new Function("ctx", `return ${code}`)(ctx);
            return result;
        } catch (err) {
            console.error('[dyn-lib] evalJS 执行失败', err);
            showMessage('执行失败：' + ((err && err.message) || err), 'error');
        }
    });
    // ===== 内置动作（含初始化动作，均挂 actionHelper，事件/初始化均由属性名驱动） =====
    // dyn-init-load='{"url":"/x"}'：请求后端，由后端 HTML 填充本 div，随后 init(div)，
    // 并将 url 写入 div 的 data-url（后续可被 reload 动作读取，作为数据源）。
    defineAction('load', function (ctx) {
        var o = ctx.options || {};
        var url = o.url || ctx.url;
        if (!url) { console.warn('[dyn-lib] dyn-init-load 缺少 url', ctx.element); return; }
        var el = ctx.element;
        // 完整请求配置存 element 上（url + 固定参数 + method），data-url 存 url 供声明式读取/reload 兜底
        el.__dynCfg = { url: url, params: o.params || {}, method: o.method || 'POST' };
        if (o.writeUrl !== false) el.setAttribute('data-url', url);

        // P0: 检测是否在 Vue 管理下（dyn-init app 内部）
        var isVueManaged = closestDynInit(el);
        if (isVueManaged) {
            console.warn('[dyn-lib] dyn-init-load 元素在 Vue 管理下，建议用 Vue 方式更新（通过 model 数据驱动）', el);
            // 仍然尝试 innerHTML，但可能破坏 Vue
        }

        return fetchPartial(url, o.params || {}, o.method || 'POST').then(function (html) {
            el.innerHTML = html;
            return init(el);
        }).catch(function (err) {
            showMessage('加载失败：' + ((err && err.message) || err), 'error');
            return null;
        });
    });

    // dyn-init-evaljs='alert("Hello World")'：页面/Vue 初始化完毕立即执行 JavaScript 代码
    // （evaljs 已在上方 defineAction 挂载，init 扫描自动命中 dyn-init-evaljs）

    // ===== setVueModel：设置 Vue model 值（支持 dyn-init/click/change-setVueModel） =====
    // 属性约定：
    //   dyn-click-setVueModel='{"modelName":"user.name","model":"张三","settimeout":100}'
    //   dyn-change-setVueModel='{"modelName":"user.age","model":30}'
    //   dyn-init-setVueModel='{"modelName":"page.title","model":"首页"}'
    //   dyn-click-setVueModel='{"TargetEl":"#other-container","modelName":"items[0].name","model":"x"}'
    // 参数：
    //   modelName  —— model 路径，支持点路径与数组下标（如 "user.name"、"items[0].name"）
    //   model      —— 要设置的值（JSON 字符串自动解析为对象/数组）；change 事件缺省时取元素 value
    //   TargetEl   —— 可选，指定目标 Vue 容器（选择器字符串或 DOM 元素）；缺省用当前元素向上找最近的 VueApp
    //   settimeout —— 延迟毫秒（可选）
    function setVueModel(ctx, modelName, value, delay) {
        var o = ctx.options || {};
        if (modelName == null) modelName = o.modelName || o.name || o.path;
        // change/input 事件且未显式给 model 时，从元素取值（input/select/textarea 等）
        if (value === undefined) {
            value = ('model' in o) ? getVueModel(ctx.element, o.model) : o.value;
            if (value === undefined && ctx.element && 'value' in ctx.element) {
                value = ctx.element.value;
            }
        }
        if (delay == null) delay = o.settimeout || o.delay || 0;
        if (!modelName) { console.warn('[dyn-lib] setVueModel 缺少 modelName', ctx.element); return; }
        // 定位目标 Vue model：TargetEl 指定容器；缺省用当前元素向上查找
        var target = getVueModel(ctx.element, o.TargetEl || o.targetEl || o.target);
        if (!target) { console.warn('[dyn-lib] setVueModel 未找到 Vue model（需在 dyn-init 容器内或指定 TargetEl）', ctx.element); return; }
        // value 若是 JSON 字符串则尝试解析为对象/数组（失败保持原字符串）
        if (typeof value === 'string') {
            var t = value.trim();
            if (t.charAt(0) === '{' || t.charAt(0) === '[') {
                try { value = JSON.parse(t); } catch (e) { /* 保持字符串 */ }
            }
        }
        var doSet = function () { setPathVal(target, modelName, value); };
        if (delay > 0) { setTimeout(doSet, delay); return; }
        doSet();
        return target;
    }
    // setVueModel：挂上即支持 dyn-{event}-setVueModel（事件）+ dyn-init-setVueModel（初始化）
    defineAction('setVueModel', function (ctx) {
        return setVueModel(ctx);
    });

    // ===== 初始化动作扫描 =====
    // 属性约定：dyn-init-{action}（页面/Vue 初始化完毕立即执行，推荐）
    //           兼容旧命名 dyn-{action}-init
    // 说明：初始化动作与事件动作共用 actionHelper（挂上即注册），扫描所有已挂载动作，
    //       命中 dyn-init-{name} / dyn-{name}-init 属性即执行，无需 _init 标记。
    // 注意：DOM 属性名一律被 HTML 解析器小写化，且部分环境 qsa/hasAttribute 大小写敏感，
    //       因此用 name.toLowerCase() 生成属性名保证匹配。
    function initActions(root) {
        root = resolve(root) || document.body;
        if (!root) return;
        Object.keys(_actions).forEach(function (name) {
            var lower = name.toLowerCase();
            ['dyn-init-' + lower, 'dyn-' + lower + '-init'].forEach(function (attrName) {
                var targets = [];
                if (root.nodeType === 1 && root.hasAttribute && root.hasAttribute(attrName)) targets.push(root);
                if (root.querySelectorAll) targets = targets.concat([].slice.call(root.querySelectorAll('[' + attrName + ']')));
                targets.forEach(function (el) {
                    if (el.__dynInitDone) return;
                    el.__dynInitDone = true;
                    var raw = el.getAttribute(attrName);
                    var ctx = buildCtx(el, 'init', null, parseActionOptions(raw), name);
                    Promise.resolve(_actions[name](ctx)).catch(function (err) {
                        console.error('[dyn-lib] 初始化动作失败: ' + name, err);
                    });
                });
            });
        });
    }

    /* ============================================================================
     * 扩展动作集：openwindow / setwindow / setdyncom / toast / copy / download / setattr
     * 全部挂 actionHelper（挂方法即动作、属性驱动），零注册自动进入委托与初始化扫描。
     * 触发：dyn-click-openwindow='{...}'（事件）、dyn-init-openwindow='{...}'（初始化）
     * 每个动作带 _doc 自描述（dyn.actionList() 可枚举，便于工具面板/自动生成文档）。
     * ============================================================================ */

    // ---- 轻量独立窗口（无桌面系统时 openwindow type=window 使用；setwindow 可控制） ----
    function createDynWindow(o) {
        var win = document.createElement('div');
        win.className = 'dyn-window';
        var width = o.width || 800, height = o.height || 600;
        var x = Math.max(20, Math.round((window.innerWidth - width) / 2));
        var y = Math.max(20, Math.round((window.innerHeight - height) / 2));
        win.style.cssText = 'position:fixed;z-index:3000;left:' + x + 'px;top:' + y + 'px;width:' + width + 'px;height:' + height + 'px;'
            + 'background:#fff;border:1px solid #dcdfe6;border-radius:8px;box-shadow:0 8px 30px rgba(0,0,0,.18);'
            + 'display:flex;flex-direction:column;overflow:hidden;';
        win.innerHTML =
            '<div class="dyn-window-bar" style="display:flex;align-items:center;height:36px;background:#f5f7fa;border-bottom:1px solid #e4e7ed;cursor:move;flex:0 0 auto;user-select:none;">'
            + '<span class="dyn-window-title" style="flex:1;padding:0 12px;font-size:13px;color:#303133;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;">' + (o.title || '窗口') + '</span>'
            + '<span class="dyn-window-btn" data-act="fullscreen" title="最大化" style="padding:0 8px;cursor:pointer;color:#909399;">⛶</span>'
            + '<span class="dyn-window-btn" data-act="min" title="最小化" style="padding:0 8px;cursor:pointer;color:#909399;">—</span>'
            + '<span class="dyn-window-btn" data-act="close" title="关闭" style="padding:0 10px;cursor:pointer;color:#909399;">✕</span>'
            + '</div>'
            + '<div class="dyn-window-body" style="flex:1;position:relative;overflow:hidden;background:#fff;">'
            + (o.url ? '<iframe src="' + o.url + '" style="width:100%;height:100%;border:none;"></iframe>' : '')
            + '</div>';
        document.body.appendChild(win);
        var bar = win.querySelector('.dyn-window-bar');
        var dragging = false, dx = 0, dy = 0;
        bar.addEventListener('mousedown', function (e) {
            if (e.target.closest && e.target.closest('.dyn-window-btn')) return;
            dragging = true; dx = e.clientX - win.offsetLeft; dy = e.clientY - win.offsetTop;
            var onMove = function (ev) { if (!dragging) return; win.style.left = (ev.clientX - dx) + 'px'; win.style.top = (ev.clientY - dy) + 'px'; };
            var onUp = function () { dragging = false; document.removeEventListener('mousemove', onMove); document.removeEventListener('mouseup', onUp); };
            document.addEventListener('mousemove', onMove); document.addEventListener('mouseup', onUp);
        });
        win.querySelectorAll('.dyn-window-btn').forEach(function (b) {
            b.addEventListener('click', function () {
                var act = b.getAttribute('data-act');
                if (act === 'close') win.remove();
                else if (act === 'fullscreen') {
                    var full = win.classList.toggle('dyn-window-full');
                    if (full) { win.style.width = '100vw'; win.style.height = '100vh'; win.style.left = '0'; win.style.top = '0'; }
                    else { win.style.width = width + 'px'; win.style.height = height + 'px'; }
                } else if (act === 'min') { win.style.display = (win.style.display === 'none') ? 'flex' : 'none'; }
            });
        });
        win.__dynWin = { width: width, height: height };
        return win;
    }

    // 查找窗体宿主：轻量窗口 → ElementUI 模态 → LayUI 弹层 → 桌面窗口
    // 优先从元素向上找（元素在窗口内部时）；否则回退取页面最上层（最后创建的）窗口宿主，
    // 便于窗口外的按钮也能控制"当前活动窗口"。
    function findWindowHost(el) {
        if (el) {
            var up = findAncestor(el, '.dyn-window') || findAncestor(el, '.dyn-modal-host')
                || findAncestor(el, '.layui-layer') || findAncestor(el, '.window');
            if (up) return up;
        }
        if (typeof document === 'undefined') return null;
        var wins = document.querySelectorAll('.dyn-window');
        if (wins.length) return wins[wins.length - 1];
        var dlgs = document.querySelectorAll('.dyn-modal-host');
        if (dlgs.length) return dlgs[dlgs.length - 1];
        var layers = document.querySelectorAll('.layui-layer');
        if (layers.length) return layers[layers.length - 1];
        var desk = document.querySelectorAll('.window');
        if (desk.length) return desk[desk.length - 1];
        return null;
    }

    // ---- openwindow：打开窗体（自动探测可用 UI 库） ----
    // dyn-click-openwindow='{"url":"/x","title":"标题","type":"auto|modal|layer|newtab|window","width":800,"height":600,"params":{}}'
    function openwindow(ctx) {
        var o = ctx.options || {};
        var type = (o.type || 'auto').toLowerCase();
        if (type === 'auto') type = (window.layui && layui.layer) ? 'layer' : (window.ElementPlus ? 'modal' : 'window');
        if (type === 'newtab') { window.open(o.url || o.href || 'about:blank', '_blank'); return; }
        if (type === 'layer') {
            if (window.layui && layui.layer) {
                // 返回 Promise<layerIndex>，供 chain 步骤拿到弹层索引
                return new Promise(function (resolve) {
                    layui.use(['layer'], function () {
                        var idx = layui.layer.open({
                            type: 2, title: o.title || '窗口',
                            area: [(o.width || 800) + 'px', (o.height || 600) + 'px'],
                            content: o.url || 'about:blank'
                        });
                        resolve(idx);
                    });
                });
            }
            showMessage('LayUI layer 不可用，已回退模态', 'warning');
        }
        if (type === 'window') { return createDynWindow(o); }
        // modal（默认）：复用 el-dialog 模态
        return open({ url: o.url, title: o.title, width: (o.width || 800) + 'px', params: o.params, method: o.method }, ctx.element);
    }
    openwindow._events = ['click'];
    openwindow._label = '打开窗体';
    openwindow._doc = '打开窗体：type=auto(自动)/modal(ElementPlus 模态)/layer(LayUI 弹层)/newtab(新标签)/window(轻量窗口)，支持 url/title/width/height/params';

    // ---- setwindow：设置所在窗体的标题/尺寸/全屏/最小化/关闭 ----
    // dyn-click-setwindow='{"title":"新标题","width":1000,"height":700,"fullscreen":true,"minimize":false,"close":false}'
    function setwindow(ctx) {
        var o = ctx.options || {};
        var host = findWindowHost(ctx.element);
        if (!host) { showMessage('未找到所在窗口', 'warning'); return; }
        if (host.classList.contains('dyn-window')) {
            if (o.title) { var t = host.querySelector('.dyn-window-title'); if (t) t.textContent = o.title; }
            if (o.width || o.height) { if (o.width) host.style.width = o.width + 'px'; if (o.height) host.style.height = o.height + 'px'; }
            if (o.fullscreen) { host.style.width = '100vw'; host.style.height = '100vh'; host.style.left = '0'; host.style.top = '0'; }
            if (o.close) host.remove();
            return;
        }
        if (host.classList.contains('dyn-modal-host') && host.__dynApp && host.__dynApp._instance) {
            var p = host.__dynApp._instance.proxy;
            if (o.title) p.title = o.title;
            if (o.width) p.width = (typeof o.width === 'number' ? o.width + 'px' : o.width);
            if (o.close) p.visible = false;
            return;
        }
        if (host.classList.contains('layui-layer') && window.layui && layui.layer) {
            if (o.close) { layui.layer.close(layui.layer.index || 0); return; }
            if (o.title) { var tt = host.querySelector('.layui-layer-title'); if (tt) tt.textContent = o.title; }
            if (o.width) host.style.width = o.width + 'px';
            return;
        }
        // 桌面窗口（DOM 兜底）
        if (o.title) { var t2 = host.querySelector('.window-title, .title'); if (t2) t2.textContent = o.title; }
        if (o.width || o.height) { if (o.width) host.style.width = o.width + 'px'; if (o.height) host.style.height = o.height + 'px'; }
        if (o.close) host.remove();
    }
    setwindow._events = ['click'];
    setwindow._label = '设置窗口';
    setwindow._doc = '设置所在窗口：title/width/height/fullscreen/minimize/close（支持轻量窗口/ElementPlus 模态/LayUI 弹层/桌面窗口）';

    // ---- setdyncom：设置目标 DynCom 组件配置（configjson/modeljson） ----
    // dyn-click-setdyncom='{"configjson":{...},"modeljson":{...},"selector":"#com","mode":"merge|replace"}'
    function setdyncom(ctx) {
        var o = ctx.options || {};
        var target = o.selector ? (typeof o.selector === 'string' ? document.querySelector(o.selector) : o.selector) : ctx.element;
        if (!target) { showMessage('setdyncom 未找到目标组件', 'warning'); return; }
        var parse = function (v) {
            if (v === undefined || v === null) return null;
            if (typeof v === 'string') { var t = v.trim(); if (t.charAt(0) === '{' || t.charAt(0) === '[') { try { return JSON.parse(t); } catch (e) { return v; } } return v; }
            return v;
        };
        var cfg = parse(o.configjson);
        var mdl = parse(o.modeljson);
        var mode = o.mode || 'merge';
        // 1) 更新 data-* 属性（声明式，供宿主读取）
        if (cfg !== null) target.setAttribute('data-config', (typeof cfg === 'object' ? JSON.stringify(cfg) : String(cfg)));
        if (mdl !== null) target.setAttribute('data-model', (typeof mdl === 'object' ? JSON.stringify(mdl) : String(mdl)));
        // 2) 更新元素上的组件节点存储（__dyncom）
        if (target.__dyncom && typeof target.__dyncom === 'object') {
            if (cfg !== null) target.__dyncom.config = mode === 'replace' ? cfg : Object.assign(target.__dyncom.config || {}, cfg);
            if (mdl !== null) target.__dyncom.model = mode === 'replace' ? mdl : Object.assign(target.__dyncom.model || {}, mdl);
        }
        // 3) 尝试更新 Vue 组件实例（__vueParentComponent.props / setupState）
        var inst = target.__vueParentComponent;
        if (inst) {
            try {
                if (cfg !== null && inst.props) {
                    if (mode === 'replace') Object.keys(inst.props).forEach(function (k) { delete inst.props[k]; });
                    Object.assign(inst.props, cfg);
                }
                if (mdl !== null && inst.setupState) Object.assign(inst.setupState, mdl);
                if (inst.update) inst.update();
            } catch (e) { console.warn('[dyn-lib] setdyncom 更新 Vue 实例失败', e); }
        }
        // 4) 派发自定义事件，宿主可监听重渲染
        target.dispatchEvent(new CustomEvent('dyn:comchange', { detail: { config: cfg, model: mdl, mode: mode }, bubbles: true }));
        return { target: target, config: cfg, model: mdl };
    }
    setdyncom._events = ['click'];
    setdyncom._label = '设置组件配置';
    setdyncom._doc = '设置目标 DynCom 组件配置：configjson/modeljson（对象或 JSON 字符串）+ selector 定位，更新 data-config/data-model/组件实例并派发 dyn:comchange';

    // ---- toast：统一提示（ElementPlus.ElMessage / NutUI.toast / layui layer.msg 自动探测） ----
    function toast(ctx) {
        var o = ctx.options || {};
        var text = o.text || o.message || o.msg || '';
        var type = o.type || 'success';
        if (!text) return;
        if (window.layui && layui.layer && layui.use && (!window.ElementPlus || o.layui)) {
            try { layui.use(['layer'], function () { layui.layer.msg(text); }); return; } catch (e) { }
        }
        showMessage(text, type);
    }
    toast._events = ['click'];
    toast._label = '提示消息';
    toast._doc = '统一提示：text/message + type(success/error/warning)，ElementPlus/NutUI/layui 自动探测';

    // ---- copy：复制文本到剪贴板 ----
    // dyn-click-copy='{"text":"要复制的文本"}'；不带 text 时取元素 value/textContent
    function copy(ctx) {
        var o = ctx.options || {};
        var text = o.text;
        if (text === undefined) text = (ctx.element && 'value' in ctx.element) ? ctx.element.value : (ctx.element ? ctx.element.textContent : '');
        if (text === undefined || text === null) return;
        var s = String(text);
        function done() { showMessage('已复制：' + s.slice(0, 20) + (s.length > 20 ? '…' : ''), 'success'); }
        function fallbackCopy() {
            var ta = document.createElement('textarea');
            ta.value = s; ta.style.position = 'fixed'; ta.style.opacity = '0';
            document.body.appendChild(ta); ta.select();
            try { document.execCommand('copy'); } catch (e) { }
            document.body.removeChild(ta);
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(s).then(done).catch(function () { fallbackCopy(); done(); });
        } else { fallbackCopy(); done(); }
    }
    copy._events = ['click'];
    copy._label = '复制文本';
    copy._doc = '复制文本到剪贴板：text 选项，或取元素 value/textContent';

    // ---- download：下载文件 ----
    // dyn-click-download='{"url":"/files/x.pdf","filename":"x.pdf"}'
    function download(ctx) {
        var o = ctx.options || {};
        var url = o.url || (ctx.element && (ctx.element.href || ctx.element.getAttribute('data-url')));
        if (!url) { showMessage('download 缺少 url', 'warning'); return; }
        var a = document.createElement('a');
        a.href = url; a.download = o.filename || o.name || '';
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
    }
    download._events = ['click'];
    download._label = '下载文件';
    download._doc = '下载文件：url + filename/name';

    // ---- setattr：设置元素属性/样式/文本/HTML ----
    // dyn-click-setattr='{"selector":"#x","attr":{"title":"新标题"},"style":{"color":"red"},"text":"新文本","html":"<b>x</b>"}'
    function setattr(ctx) {
        var o = ctx.options || {};
        var el = o.selector ? (typeof o.selector === 'string' ? document.querySelector(o.selector) : o.selector) : ctx.element;
        if (!el) { showMessage('setattr 未找到元素', 'warning'); return; }
        if (o.attr) Object.keys(o.attr).forEach(function (k) { el.setAttribute(k, o.attr[k]); });
        if (o.style) Object.keys(o.style).forEach(function (k) { el.style[k] = o.style[k]; });
        if (o.text !== undefined) el.textContent = o.text;
        if (o.html !== undefined) el.innerHTML = o.html;
    }
    setattr._events = ['click'];
    setattr._label = '设置元素';
    setattr._doc = '设置元素：attr(属性)/style(样式)/text/html(内容)，selector 定位';

    // ---- confirm：确认框（供 chain 链做分支；返回 Promise<true|false>） ----
    // dyn-click-confirm='{"message":"确定执行？"}'   （单独用仅弹确认框，无副作用）
    function confirmAction(ctx) {
        var o = ctx.options || {};
        var msg = o.message || o.msg || o.text || '确定执行该操作吗？';
        return confirmAsync(msg);
    }
    confirmAction._events = ['click'];
    confirmAction._label = '确认框';
    confirmAction._doc = '确认框：message/msg/text，返回 true(确定)/false(取消)——供 chain 链做中止分支';

    // ---- chain：动作链（按序 await 执行 steps；步骤返回 false 中止；上一步返回值注入 ctx.$result） ----
    // dyn-click-chain='{"steps":[{"action":"confirm","options":{...}},{"action":"openwindow","options":{...}},...]}'
    // 每个步骤可写 {action, options}；options 缺省时步骤对象本身即 options；字符串步骤 = {action: 字符串}
    function chain(ctx) {
        var steps = ctx.options && ctx.options.steps;
        if (Array.isArray(ctx.options)) steps = ctx.options;
        if (!Array.isArray(steps)) { showMessage('chain 需要 steps 数组', 'warning'); return; }
        var last;
        var run = function (i) {
            if (i >= steps.length) return Promise.resolve(last);
            var s = steps[i];
            if (!s) return run(i + 1);
            var actionName = (typeof s === 'string') ? s : (s.action || '');
            var opt = (typeof s === 'object' && s.options) ? s.options : (typeof s === 'string' ? {} : s);
            if (!actionName) { console.warn('[dyn-lib] chain 步骤缺少 action: ', i, s); return run(i + 1); }
            var fn = resolveAction(actionName);
            if (!fn) { showMessage('chain 步骤动作不存在: ' + actionName, 'error'); return run(i + 1); }
            // 步骤 ctx：以链起点元素为上下文，注入链信息 $step/$result/$chain
            var stepCtx = buildCtx(ctx.element, 'chain', ctx.$event || null, opt, actionName);
            stepCtx.$step = i;
            stepCtx.$result = last;
            stepCtx.$chain = steps;
            stepCtx.$chainAction = actionName;
            // onEvent 扩展：步骤可携带 onEvent:{onopen:[...],onclose:[...]}，
            // 当步骤是 openwindow/open 等窗口动作时自动注入 events 钩子
            if (s && s.onEvent && (actionName === 'openwindow' || actionName === 'open')) {
                opt = Object.assign({}, opt, { events: Object.assign({}, opt.events || {}, s.onEvent) });
                stepCtx.options = opt;
            }
            if (ctx.$event) stepCtx.$event = ctx.$event;
            return Promise.resolve(fn(stepCtx)).then(function (r) {
                if (r === false) { console.log('[dyn-lib] chain 中止于步骤 ' + i + ' (' + actionName + ')'); return last; }
                last = r;
                return run(i + 1);
            });
        };
        return run(0);
    }
    chain._events = ['click'];
    chain._label = '动作链';
    chain._doc = '动作链：steps 数组按序 await 执行；步骤返回 false 中止；上一步返回值注入下一步 ctx.$result，返回最后一步结果';


    // 注册扩展动作到 actionHelper（挂方法即动作，autoBindActions 自动纳入委托与元数据登记）
    actionHelper.confirm = confirmAction;
    actionHelper.chain = chain;
    actionHelper.openwindow = openwindow;
    actionHelper.setwindow = setwindow;
    actionHelper.setdyncom = setdyncom;
    actionHelper.toast = toast;
    actionHelper.copy = copy;
    actionHelper.download = download;
    actionHelper.setattr = setattr;
    // ---- showmessage：显示消息提示（复用 dyn 内部 showMessage，自动探测 ElementPlus/NutUI/LayUI） ----
    // dyn-click-showmessage='{"message":"保存成功","type":"success","title":"提示"}'
    function showmessage(ctx) {
        var o = ctx.options || {};
        showMessage(o.message || '操作成功', o.type || 'success');
    }
    showmessage._events = ['click'];
    showmessage._label = '消息提示';
    showmessage._doc = '显示消息提示：message / type(success|error|warning|info)';

    // ---- redirect：页面跳转 ----
    // dyn-click-redirect='{"url":"/Home/Index"}'
    function redirect(ctx) {
        var o = ctx.options || {};
        var url = o.url || o.href;
        if (url) window.location.href = url;
    }
    redirect._events = ['click'];
    redirect._label = '页面跳转';
    redirect._doc = '跳转到指定 URL：url';

    actionHelper.showmessage = showmessage;
    actionHelper.redirect = redirect;

    // ---- grid：3屏管理组件动作（Filter + Grid + Detail） ----
    // 用法：dyn-click-grid='{"action":"search|clear|add|edit|delete|load","confirm":"..","title":"..","width":"..","id":1}'
    // 容器约定：组件根元素 class="dyn-grid"，并带 data-filter-url / data-list-url / data-add-url / data-edit-url / data-delete-url
    // 行为：search=序列化 filter 区域 → 设置到 list 的 model.Filter → list 重新 load；clear=清空 filter 后同上；
    //       add=open 模态加载 addUrl（带 id=0）；edit=open 模态加载 editUrl（带行 id）；
    //       delete=confirm 后 POST deleteUrl 并刷新 list；load=仅刷新 list
    function grid(ctx) {
        var o = ctx.options || {};
        var el = ctx.element;
        var host = el && el.closest ? el.closest('.dyn-grid') : null;
        if (!host) { showMessage('未找到 3 屏管理组件容器（.dyn-grid）', 'error'); return; }
        var act = String(o.action || 'search').toLowerCase();
        function attr(n) { return host.getAttribute('data-' + n + '-url') || ''; }
        var urls = { filter: attr('filter'), list: attr('list'), add: attr('add'), edit: attr('edit'), del: attr('delete') };
        var filterEl = host.querySelector('.dyn-grid-filter');
        var listEl = host.querySelector('.dyn-grid-list');
        var rowId = o.id != null ? o.id
            : (ctx.params && ctx.params.id != null ? ctx.params.id
                : (ctx.params && ctx.params['data-id'] != null ? ctx.params['data-id'] : null));

        // 序列化 filter 区域输入（name 驱动）→ 过滤空值 → 设置到 list 的 model.Filter
        function applyFilter() {
            var f = {};
            if (filterEl) {
                var ser = serializeForm(filterEl) || {};
                Object.keys(ser).forEach(function (k) {
                    var v = ser[k];
                    if (v !== '' && v != null && !(Array.isArray(v) && v.length === 0)) f[k] = v;
                });
            }
            if (listEl) setVueModel({ element: listEl, options: {} }, 'Filter', f);
            return f;
        }
        function loadList(f) {
            if (!urls.list) { showMessage('未配置 listUrl（data-list-url）', 'error'); return; }
            // 筛选值包成 { Filter: f } 匹配后端 DynSummaryPost.Filter；无筛选时传空对象触发默认查询
            return reload(listEl, { url: urls.list, params: f && Object.keys(f).length ? { Filter: f } : {} });
        }
        function clearFilterInputs(root) {
            if (!root) return;
            var inputs = [].slice.call(root.querySelectorAll('input, select, textarea'));
            inputs.forEach(function (inp) {
                var t = (inp.type || '').toLowerCase();
                if (t === 'checkbox' || t === 'radio') { inp.checked = false; }
                else if (inp.tagName === 'SELECT') { inp.selectedIndex = 0; }
                else { inp.value = ''; }
            });
            inputs.forEach(function (inp) {
                inp.dispatchEvent(new Event('input', { bubbles: true }));
                inp.dispatchEvent(new Event('change', { bubbles: true }));
            });
        }

        if (act === 'search') return loadList(applyFilter());
        if (act === 'clear') { clearFilterInputs(filterEl); return loadList(applyFilter()); }
        if (act === 'load') return loadList();
        if (act === 'add') {
            if (!urls.add) { showMessage('未配置 addUrl（data-add-url）', 'error'); return; }
            return open({ url: urls.add, params: { id: 0 }, title: o.title || '新增', width: o.width || '720px' }, el);
        }
        if (act === 'edit') {
            if (!urls.edit) { showMessage('未配置 editUrl（data-edit-url）', 'error'); return; }
            if (rowId == null) { showMessage('缺少记录 ID', 'warning'); return; }
            return open({ url: urls.edit, params: { id: rowId }, title: o.title || '编辑', width: o.width || '720px' }, el);
        }
        if (act === 'delete') {
            if (!urls.del) { showMessage('未配置 deleteUrl（data-delete-url）', 'error'); return; }
            if (rowId == null) { showMessage('缺少记录 ID', 'warning'); return; }
            var doDel = function () {
                // id 走 URL query（后端 int id 参数从 query 绑定），body 仅保留筛选参数
                var delUrl = urls.del + (urls.del.indexOf('?') >= 0 ? '&' : '?') + 'id=' + encodeURIComponent(rowId);
                return postback(el, { url: delUrl, reload: listEl, message: o.message || '已删除' });
            };
            if (o.confirm) return confirmAsync(o.confirm).then(function (ok) { if (ok) return doDel(); });
            return doDel();
        }
        showMessage('未知 grid 动作: ' + act, 'error');
    }
    grid._events = ['click'];
    grid._label = '3屏管理';
    grid._doc = '3屏管理组件（Filter+Grid+Detail）：action=search|clear|add|edit|delete|load；容器需 .dyn-grid + data-filter/list/add/edit/delete-url；行 id 取 data-id';
    actionHelper.grid = grid;
    // 动作清单函数：返回所有已注册动作的 {name,label,doc,events}
    function actionList() {
        return Object.keys(_actionMeta).map(function (k) { return Object.assign({}, _actionMeta[k]); });
    }


    // ============================================================================
    // 新动作：setvar / delay / notify / triggerevent / switch（2026-09 重构新增）
    // ============================================================================

    // ---- setvar：设置变量（目标 Vue model 路径） ----
    // dyn-click-setvar='{"modelName":"form.name","value":"张三","delay":0}'
    function setvar(ctx) {
        var o = ctx.options || {};
        var modelName = o.modelName || o.path || '';
        var value = o.value;
        if (value !== undefined && typeof value === 'string' && value.indexOf('{{') >= 0) {
            value = applyTpl(value, ctx.params || {});
        }
        var done = function () {
            if (!modelName) return;
            var target = getVueModel(ctx.element, o.target || null);
            if (!target) return;
            setPathVal(target, modelName, value);
        };
        var ms = Number(o.delay || 0);
        if (ms > 0) { setTimeout(done, ms); return; }
        done();
    }
    setvar._events = ['click', 'change'];
    setvar._label = '设置变量';
    setvar._doc = '设置目标 Vue model 变量：modelName 点路径/数组下标，value 支持 {{params}} 占位，delay 延迟毫秒';

    // ---- delay：延时（供 chain 步骤） ----
    // dyn-click-delay='{"ms":500}'
    function delay(ctx) {
        var o = ctx.options || {};
        var ms = Number(o.ms != null ? o.ms : (o.delay != null ? o.delay : 300));
        return new Promise(function (r) { setTimeout(r, ms); });
    }
    delay._events = [];
    delay._label = '延时';
    delay._doc = '等待指定毫秒后继续（常用于 chain 步骤）';

    // ---- notify：消息通知（ElementPlus ElNotification） ----
    // dyn-click-notify='{"type":"success","title":"提示","message":"已保存","duration":3000}'
    function notify(ctx) {
        var o = ctx.options || {};
        var message = o.message || o.msg || '';
        if (!message) return;
        var title = o.title || '';
        var type = o.type || 'success';
        if (global.ElementPlus && ElementPlus.ElNotification) {
            ElementPlus.ElNotification({ title: title, message: message, type: type, duration: Number(o.duration || 3000) });
            return;
        }
        showMessage(message, type);
    }
    notify._events = ['click', 'change'];
    notify._label = '消息通知';
    notify._doc = 'ElementPlus 通知：type=success|warning|error|info，title/message 支持 {{params}} 占位，duration 毫秒';

    // ---- triggerevent：触发事件（eventBus / window CustomEvent） ----
    // dyn-click-triggerevent='{"type":"eventBus","name":"refresh","payload":{"x":1}}'
    function triggerevent(ctx) {
        var o = ctx.options || {};
        var name = o.name || o.event || '';
        if (!name) return;
        var payload = o.payload || o.params || {};
        if (o.type === 'window' || o.window) {
            try { global.dispatchEvent(new CustomEvent(name, { detail: payload })); } catch (e) { }
            return;
        }
        eventBus.emit(name, payload);
    }
    triggerevent._events = ['click', 'change'];
    triggerevent._label = '触发事件';
    triggerevent._doc = '触发全局/窗口事件总线：type=eventBus|window，name 事件名，payload 参数对象';

    // ---- switch：条件分支（按表达式值匹配 cases，执行对应动作/动作链） ----
    // dyn-click-switch='{"expr":"model.status","cases":[{"value":"draft","action":"openwindow","options":{...}},
    //                   {"value":"done","chain":{"steps":[...]}}],"default":{"action":"toast","options":{...}}}'
    function switchAction(ctx) {
        var o = ctx.options || {};
        var expr = o.expr || o.expression || '';
        var value;
        if (expr.indexOf('model.') === 0) value = getByPath(ctx.model, expr.substring(6));
        else if (expr.indexOf('params.') === 0) value = ctx.params ? ctx.params[expr.substring(7)] : undefined;
        else if (expr) value = (ctx.params && ctx.params[expr] !== undefined) ? ctx.params[expr] : getByPath(ctx.model, expr);
        else value = o.value;
        var hit = null;
        var cases = o.cases || [];
        for (var i = 0; i < cases.length; i++) {
            var c = cases[i] || {};
            if (String(c.value) === String(value)) { hit = c; break; }
        }
        if (!hit && o.default) hit = o.default;
        if (!hit || typeof hit !== 'object') return Promise.resolve(null);
        var actName = hit.action || 'switch';
        var subCtx = buildCtx(ctx.element, ctx.event, ctx.$event || null, hit.options || {}, actName);
        if (hit.chain) {
            subCtx.options = Array.isArray(hit.chain) ? { steps: hit.chain } : (hit.chain.steps ? hit.chain : { steps: hit.chain });
            return chain(subCtx);
        }
        if (!hit.action) return Promise.resolve(null);
        var fn = resolveAction(hit.action);
        if (!fn) { console.warn(DYN_CFG.PREFIX + ' ' + DYN_CFG.MSG_ACTION_NOT_FOUND + hit.action); return Promise.resolve(null); }
        return Promise.resolve(fn(subCtx));
    }
    switchAction._events = ['click', 'change'];
    switchAction._label = '条件分支';
    switchAction._doc = '按表达式值匹配 cases 执行对应动作/动作链；命中 default 走兜底；无匹配静默';

    // ============================================================================
    // onEvent 支持：_runEvents —— 执行一组动作链步骤（供 core open 的 onopen/onclose 钩子调用）
    // ============================================================================
    function _runEvents(steps, baseCtx) {
        if (!Array.isArray(steps) || !steps.length) return Promise.resolve(null);
        var fakeCtx = {
            element: baseCtx.element || document.body,
            model: baseCtx.model || null,
            options: { steps: steps },
            $event: null,
            $eventCtx: baseCtx
        };
        try { return chain(fakeCtx); } catch (e) { console.error('[dyn] onEvent 链执行异常', e); return Promise.resolve(null); }
    }

    // ============================================================================
    // DB 动作助手：/api/dynactionhelper 返回自定义动作（META + ScriptContent）→ 动态注册
    // ============================================================================
    function registerActionHelper(row) {
        if (!row || !row.code || !row.scriptContent) return false;
        var fn = null;
        try {
            // eslint-disable-next-line no-new-func
            fn = new Function('dyn', 'return (' + row.scriptContent + ')')(window.dynCore || dyn);
        } catch (e) {
            console.error('[dyn] 注册动作助手失败: ' + row.code, e);
            return false;
        }
        if (typeof fn !== 'function') return false;
        var meta = null;
        try { meta = typeof row.metaJson === 'string' ? JSON.parse(row.metaJson) : (row.metaJson || null); } catch (e) { }
        fn._label = (meta && meta.label) || row.name || row.code;
        fn._doc = (meta && meta.doc) || '';
        fn._events = (meta && Array.isArray(meta.events) && meta.events.length) ? meta.events : ['click'];
        fn._meta = meta;
        fn._db = { id: row.id, code: row.code, isBuiltin: !!row.isBuiltin };
        actionHelper[row.code] = fn;
        return true;
    }

    function loadDbActionHelpers() {
        if (!global.fetch) return;
        fetch(DYN_CFG.ACTION_API_BASE, { headers: { 'Accept': 'application/json' } })
            .then(function (r) { return r.json(); })
            .then(function (res) {
                var rows = null;
                if (Array.isArray(res)) rows = res;
                else if (res && Array.isArray(res.data)) rows = res.data;
                if (!rows) return;
                var added = 0;
                rows.forEach(function (row) {
                    if (row && row.isBuiltin) return; // 内置动作已编译在 dyn-actionhelper.js
                    if (registerActionHelper(row)) added++;
                });
                if (added > 0) { try { autoBindActions(); } catch (e) { } }
                if (global.__DYN_DEBUG) console.log('[dyn] 已加载 DB 动作助手 ' + added + ' 个');
            })
            .catch(function () { /* 静默：后端未提供时不影响页面 */ });
    }

    // ============================================================================
    // 动作 META 目录：actionMeta / getMeta（供属性编辑器下拉、设计器展示）
    // ============================================================================
    function getMeta(name) {
        return _actionMeta[name] || null;
    }

    var ACTION_I18N = {
        'zh-CN': { '动作': '动作', '选择动作助手': '选择动作助手' },
        'en-US': { '动作': 'Action', '选择动作助手': 'Select Action Helper' }
    };

    // ============================================================================
    // openChainEditor —— 动作链编辑器（独立 view + dyn.openwindow 弹窗，不在主 js 里堆 UI）
    // 用法：dyn.openChainEditor({ steps: [...], callback: function(steps){...} })
    // ============================================================================
    function openChainEditor(opts) {
        opts = opts || {};
        var steps = opts.steps || [];
        var mode = opts.mode || 'main';
        // 回调契约：编辑器保存后调用 callback(steps)，随后自动关闭弹窗
        var key = (mode === 'sub') ? '__dynChainSubEditor' : '__dynChainEditor';
        window[key] = { callback: opts.callback, mode: mode };
        var url = DYN_CFG.CHAIN_EDITOR_URL + '?steps=' + encodeURIComponent(JSON.stringify(steps)) + '&mode=' + encodeURIComponent(mode);
        return openwindow({
            url: url,
            title: opts.title || '动作链编辑器',
            type: opts.type || 'modal',
            width: opts.width || '72%',
            height: opts.height || '70%',
            events: opts.events || null
        }, opts.element || null);
    }

    // ============================================================================
    // 装配：注入 dynCore + 暴露 _initActions/_runEvents + 注册新动作 + 自动绑定 + 加载 DB 助手
    // ============================================================================
    actionHelper.setvar = setvar;
    actionHelper.delay = delay;
    actionHelper.notify = notify;
    actionHelper.triggerevent = triggerevent;
    actionHelper.switch = switchAction;

    var actionApi = {
        /* --- 动作注册表 + 通用委托 --- */
        initActions: initActions,
        actions: _actions,
        initActionList: _actions,
        buildCtx: buildCtx,
        resolveAction: resolveAction,
        actionEvents: ACTION_EVENTS.slice(),
        actionHelper: actionHelper,
        actionList: actionList,
        actionMeta: _actionMeta,
        getMeta: getMeta,
        actionI18n: ACTION_I18N,
        autoBindActions: autoBindActions,
        rebind: autoBindActions,
        bindDelegation: bindDelegation,
        /* --- DB 动作助手 --- */
        registerActionHelper: registerActionHelper,
        loadDbActionHelpers: loadDbActionHelpers,
        /* --- 动作链编辑器 --- */
        openChainEditor: openChainEditor
    };
    dyn._initActions = initActions;
    dyn._runEvents = _runEvents;
    dyn.installActionApi(actionApi);

    // 初始绑定 + 加载数据库自定义动作助手
    try { autoBindActions(); } catch (e) { }
    loadDbActionHelpers();

    // 暴露全局名（兼容旧入口 dyn-lib.js shim 的 ensure 检查，以及 dyn-core 惰性包装）
    global.dynActionHelper = {
        setVueModel: setVueModel,
        actionHelper: actionHelper,
        openChainEditor: openChainEditor,
        registerActionHelper: registerActionHelper
    };
    // 最终装配：window.dyn = dynCore（已含动作系统）
    global.dyn = dyn;
})(window);