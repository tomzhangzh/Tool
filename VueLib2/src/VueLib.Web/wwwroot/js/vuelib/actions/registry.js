/**
 * VueLib actions/registry：动作注册表 + 链式执行器 + 声明式属性委托
 *
 * 声明式模板占位：动作 options 支持 {{path}} 占位符，执行时从 ctx.model 取值替换。
 * 完全兼容 JSON 配置，可放 <div style="display:none"><script type="application/json">...</script></div>
 * 由 dyn-click-ref 引用，或组件 extendinfo.actions.chain 直接使用。
 */
(function () {
    'use strict';
    var C = window.VueLibConfig || {};
    var Utils = window.VueLib.utils;
    var debug = window.VueLib.debug;

    var actions = Object.create(null);

    /* ================= 动作注册 ================= */
    function register(name, fn) {
        if (!name || typeof fn !== 'function') return;
        actions[name] = fn;
        // 同步生成编程式 helper：VueLib.actions.helpers[name]
        helpers[name] = function (options, ctx) {
            return run([{ action: name, options: options || {} }], ctx || {});
        };
    }

    /* ================= 链式执行 ================= */
    /**
     * 执行动作链
     * @param {Array} chain [{action, options, confirm}]
     * @param {object} ctx { model, el, extra }
     */
    async function run(chain, ctx, extra) {
        ctx = ctx || {};
        ctx.extra = extra || ctx.extra;
        if (!Array.isArray(chain)) chain = [chain];
        var output = {};
        for (var i = 0; i < chain.length; i++) {
            var step = chain[i];
            if (!step || !step.action) continue;
            var tag = (ctx.debugTag || 'DynChain');
            debug.chainStep(i, step.action, 'start');
            if (step.confirm) {
                var ok = window.confirm(step.confirm);
                debug.chainStep(i, step.action, ok ? 'ok' : 'cancel', { confirm: step.confirm });
                if (!ok) return { cancelled: true, output: output };
            }
            var fn = actions[step.action];
            if (!fn) {
                console.error('[VueLib.actions] 未知动作: ' + step.action);
                debug.chainStep(i, step.action, 'unknown');
                continue;
            }
            // 占位符解析：options 里的 {{path}} 替换为 model 值
            var options = Utils.resolvePlaceholders(step.options || {}, ctx.model || {});
            try {
                var result = await fn(ctx, options);
                output[step.action] = result;
                debug.chainStep(i, step.action, 'ok', result);
            } catch (e) {
                console.error('[VueLib.actions] 动作失败: ' + step.action, e);
                debug.chainStep(i, step.action, 'error', { message: e.message });
                throw e;
            }
        }
        return { cancelled: false, output: output };
    }

    /* ================= 从元素构建链 ================= */

    /** 读取元素上的声明式链配置（兼容内联 JSON / ref 引用 / dyn-click-xxx 单动作） */
    function buildChainFromEl(el) {
        if (el.hasAttribute('dyn-click-chain')) {
            var parsed = Utils.parseAttrJson(el, 'dyn-click-chain');
            if (!parsed) return null;
            // 兼容 {steps:[...]} 容器结构与裸数组两种写法
            return Array.isArray(parsed) ? parsed : (Array.isArray(parsed.steps) ? parsed.steps : parsed);
        }
        if (el.hasAttribute('dyn-click-ref')) {
            var ref = el.getAttribute('dyn-click-ref');
            var cfgEl = document.querySelector(ref) ||
                document.querySelector('[dyn-action-config="' + ref + '"]');
            if (!cfgEl) return null;
            var text = cfgEl.textContent || cfgEl.getAttribute('data-json') || '';
            try {
                var cfg = JSON.parse(text);
                return Array.isArray(cfg) ? cfg : (Array.isArray(cfg.steps) ? cfg.steps : cfg);
            } catch (e) { return null; }
        }
        // 收集所有 dyn-click-* 单动作属性（排除 chain/ref/debug）
        var steps = [];
        for (var i = 0; i < el.attributes.length; i++) {
            var a = el.attributes[i];
            var m = /^dyn-click-(.+)$/.exec(a.name);
            if (!m) continue;
            if (m[1] === 'chain' || m[1] === 'ref') continue;
            var opts = null;
            try { opts = JSON.parse(a.value); } catch (e) { opts = { _raw: a.value }; }
            steps.push({ action: m[1], options: opts || {} });
        }
        return steps.length ? steps : null;
    }

    /** 从元素执行（全局委托 / 组件内均可用） */
    async function runFromEl(el, extra) {
        el = Utils.resolveEl(el);
        if (!el) return null;
        var chain = buildChainFromEl(el);
        if (!chain) return null;
        var ctx = { model: window.VueLib.dyn.getModel(el), el: el };
        debug.log('actions', 'runFromEl', el, chain);
        if (el.hasAttribute(C.debugAttr)) debug.inspectEl(el);
        return run(chain, ctx, extra);
    }

    /* ================= 全局委托（document 捕获阶段） ================= */
    function hasDynClick(el) {
        if (!el || !el.getAttribute) return false;
        if (el.hasAttribute('dyn-click-chain') || el.hasAttribute('dyn-click-ref')) return true;
        var attrs = el.attributes;
        for (var i = 0; i < attrs.length; i++) {
            if (attrs[i].name.indexOf('dyn-click-') === 0) return true;
        }
        return false;
    }

    var installed = false;
    function installDelegated() {
        if (installed) return;
        installed = true;
        document.addEventListener('click', function (e) {
            var target = e.target && e.target.nodeType === 3 ? e.target.parentNode : e.target;
            var el = null;
            var node = target;
            while (node && node !== document) {
                if (hasDynClick(node)) { el = node; break; }
                node = node.parentNode;
            }
            if (!el) return;
            e.preventDefault();
            e.stopPropagation();
            runFromEl(el);
        }, true);
    }

    /* ================= 对外 ================= */
    var helpers = {};
    window.VueLib = window.VueLib || {};
    window.VueLib.actions = {
        register: register,
        run: run,
        runFromEl: runFromEl,
        buildChainFromEl: buildChainFromEl,
        installDelegated: installDelegated,
        helpers: helpers,
        _registry: actions
    };
})();
