/**
 * dyn-actionhelper.js —— 动作系统 V3
 * 动作直接挂在 window.actionHelper 对象上，自动进入委托。
 * 静态变量集中在文件头部。
 */
(function (global) {
    'use strict';

    /* ===== 静态配置（文件头部集中）===== */
    var CFG = {
        EVENTS: ['click', 'change', 'dblclick', 'focus', 'blur', 'mouseover', 'input'],
        INIT_EVENTS: ['init'],
        ATTR_PREFIX: 'dyn-',
        ACTION_BASE: 'data-dyn-args',
        DELEGATE_PREFIX: 'dyn-'
    };

    /* ===== actionHelper 命名空间（挂方法即动作）===== */
    var AH = global.actionHelper = global.actionHelper || {};

    /**
     * 构建 ctx 上下文对象
     */
    function buildCtx(el, eventName, nativeEvent, action, attrValue) {
        var options = {};
        if (attrValue) {
            try { options = JSON.parse(attrValue); } catch (e) { options = { raw: attrValue }; }
        }
        // 找祖先 dyn-init 元素拿 model 和 url
        var dynEl = el;
        while (dynEl && dynEl !== document) {
            if (dynEl.hasAttribute && dynEl.hasAttribute(CFG.DELEGATE_PREFIX + 'init')) break;
            dynEl = dynEl.parentElement;
        }
        var model = global.dyn.getModel(el);
        var url = '';
        if (dynEl) url = dynEl.getAttribute('data-dyn-url') || '';
        return {
            element: el,
            el: el,
            event: eventName,
            $event: nativeEvent,
            action: action,
            options: options,
            params: options,
            model: model,
            url: url,
            dyn: global.dyn
        };
    }

    /**
     * 从属性名解析出事件和动作
     * dyn-click-post -> { event:'click', action:'post' }
     * dyn-init-load -> { event:'init', action:'load' }
     */
    function parseAttrName(name) {
        if (!name || name.indexOf(CFG.DELEGATE_PREFIX) !== 0) return null;
        var rest = name.substring(CFG.DELEGATE_PREFIX.length);
        var dashIdx = rest.indexOf('-');
        if (dashIdx < 0) return null;
        var evt = rest.substring(0, dashIdx);
        var act = rest.substring(dashIdx + 1);
        return { event: evt, action: act };
    }

    /**
     * 执行一个动作
     */
    function run(el, eventName, action, attrValue, nativeEvent) {
        var fn = AH[action];
        if (!fn) {
            console.error('[actionHelper] 未定义动作: ' + action);
            global.dyn.message('error', '未定义动作: ' + action);
            return;
        }
        var ctx = buildCtx(el, eventName, nativeEvent, action, attrValue);
        try {
            var result = fn(ctx);
            if (result && typeof result.then === 'function') {
                result.catch(function (e) {
                    console.error('[actionHelper] 异步动作异常: ' + action, e);
                    global.dyn.message('error', '动作异常: ' + e.message);
                });
            }
        } catch (e) {
            console.error('[actionHelper] 执行异常: ' + action, e);
            global.dyn.message('error', '动作执行异常: ' + e.message);
        }
    }

    /**
     * 执行动作链
     */
    function runChain(el, steps) {
        (steps || []).forEach(function (step) {
            if (step && step.action) {
                run(el, 'click', step.action, JSON.stringify(step.args || step.params || {}), null);
            }
        });
    }

    /* ===== 事件委托（document capture，覆盖动态渲染）===== */
    function handleDelegate(e) {
        var evtName = e.type;
        var el = e.target;
        while (el && el !== document && el.nodeType === 1) {
            var attrs = el.attributes;
            if (attrs) {
                for (var i = 0; i < attrs.length; i++) {
                    var parsed = parseAttrName(attrs[i].name);
                    if (parsed && parsed.event === evtName) {
                        e.preventDefault();
                        e.stopPropagation();
                        run(el, parsed.event, parsed.action, attrs[i].value, e);
                        return;
                    }
                }
            }
            el = el.parentElement;
        }
    }

    /**
     * 执行 init 动作（DOM ready / innerHTML 更新后）
     */
    function runInitActions(root) {
        var scope = root || document;
        var els = scope.querySelectorAll('[dyn-init-]');
        // 也查 scope 本身
        var allEls = [].slice.call(els);
        if (scope.hasAttribute && scope.hasAttribute('dyn-init-load')) allEls.unshift(scope);
        allEls.forEach(function (el) {
            var attrs = el.attributes;
            for (var i = 0; i < attrs.length; i++) {
                var parsed = parseAttrName(attrs[i].name);
                if (parsed && parsed.event === 'init') {
                    run(el, 'init', parsed.action, attrs[i].value, null);
                }
            }
        });
    }

    /**
     * 从 DB 拉取动作并 eval 注册到 actionHelper
     */
    function loadFromServer() {
        return fetch(global.dyn.cfg.API_ACTIONHELPERS)
            .then(function (r) { return r.json(); })
            .then(function (list) {
                (list || []).forEach(function (a) {
                    if (!a.scriptContent) return;
                    try {
                        var fn = eval('(' + a.scriptContent + ')');
                        AH[a.code] = fn;
                    } catch (e) {
                        console.error('[actionHelper] 注册失败: ' + a.code, e);
                    }
                });
                console.log('[actionHelper] 已加载 ' + Object.keys(AH).length + ' 个动作');
            })
            .catch(function (e) { console.error('[actionHelper] 加载失败', e); });
    }

    /* ===== 初始化事件委托 ===== */
    function init() {
        CFG.EVENTS.forEach(function (evt) {
            document.addEventListener(evt, handleDelegate, true);
        });
        // DOM ready 后跑 init 动作
        runInitActions(document);
    }

    // 自动启动
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { init(); runInitActions(document); });
    } else {
        init();
        runInitActions(document);
    }

    console.log('[actionHelper] v3 loaded, EVENTS=' + CFG.EVENTS.join(','));

    /* ===== 导出 ===== */
    global.dynActionHelper = {
        AH: AH,
        run: run,
        runChain: runChain,
        runInitActions: runInitActions,
        loadFromServer: loadFromServer,
        buildCtx: buildCtx
    };

})(window);
