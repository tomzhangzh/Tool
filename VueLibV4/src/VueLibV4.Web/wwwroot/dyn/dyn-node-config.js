/**
 * dyn-node-config —— 组件节点配置的「纯函数机制层」。
 *
 * _BaseLayout 只保留 Vue 生命周期与响应式接线（computed/watch/provide），
 * 配置合并、表达式求值、属性穿透、选项加载、校验这五类机制全部在此实现：
 *   - 无响应式依赖：入参显式传入（model / ctx / options），出参为普通对象/数组；
 *   - 无 DOM 依赖（选项加载只用 fetch）；
 *   - 因此可被 node 直接单测（见 tests/dyn/run.js）。
 *
 * 对外唯一入口：window.DynNodeConfig（别名 DynNode / NC）。
 */
(function (global) {
    'use strict';

    var NC = {};

    /** lodash 由 dyn-lib 先行加载；缺失时按需回退，避免模块加载顺序变化导致整页报错 */
    function L() { return global._ || {}; }

    /**
     * 裸标签 JSON 属性归一：对象直接用；字符串按 JSON 解析（生成视图的 comoptions 属性到浏览器是 JSON 字符串）；
     * null/空串/解析失败回落到 dft，且不抛异常——手写模板写错一个属性不该让整页挂掉。
     */
    function parseAttrJson(v, dft) {
        if (v === null || v === undefined) return dft;
        if (typeof v === 'object') return v;
        var s = String(v).trim();
        if (!s) return dft;
        try {
            var o = JSON.parse(s);
            return (o && typeof o === 'object') ? o : dft;
        } catch (e) {
            console.warn('[裸标签属性 JSON 解析失败]', e, s.slice(0, 120));
            return dft;
        }
    }

    /** 按 key 覆盖合并：既更新 origin 已有键，也新增 patch 独有的键（如 bindings 注入的 disabled） */
    function mergePatch(origin, patch) {
        var res = Object.assign({}, origin);
        if (!patch || typeof patch !== "object") return res;
        for (var key in patch) {
            if (patch[key] !== undefined) {
                res[key] = patch[key];
            }
        }
        return res;
    }

    /** labeloptions 合并：父默认打底，子显式值覆盖（null 表示显式清空，undefined 表示不参与） */
    function mergeLabelOptions(selfOpt, parentDefaultOpt) {
        var result = Object.assign({}, parentDefaultOpt || {});
        for (var key in selfOpt) {
            var v = selfOpt[key];
            if (v === null) {
                result[key] = null;
            } else if (v !== undefined) {
                result[key] = v;
            }
        }
        return result;
    }

    /** itemoptions.style 合并（后者覆盖前者） */
    function getStyle() {
        var opts = Array.prototype.slice.call(arguments);
        return Object.assign({}, opts.map(function (o) { return (o && o.style) || {}; }).reduce(function (a, b) { return Object.assign(a, b); }, {}));
    }

    /** itemoptions.class 合并为 class 数组 */
    function getClass() {
        var opts = Array.prototype.slice.call(arguments);
        return opts.map(function (o) { return (o && o.class) || ''; })
            .reduce(function (a, s) { return a.concat(s.split(' ')); }, [])
            .filter(Boolean);
    }

    /** 把任意形态（字符串或 {value,label}）归一为 {value,label}，支持 valueKey/labelKey 映射 */
    function normOptionList(list, valueKey, labelKey) {
        return (Array.isArray(list) ? list : []).map(function (x) {
            if (x !== null && typeof x === 'object') {
                var v = valueKey ? x[valueKey] : x.value;
                var l = labelKey ? x[labelKey] : x.label;
                return { value: v !== undefined ? v : x.value, label: l !== undefined ? l : x.label };
            }
            return { value: x, label: String(x) };
        });
    }

    /** static 数据源：优先 optionValuesText（每行“值,标签”，无逗号时值即标签），否则读 optionValues 数组 */
    function optionsFromText(text) {
        if (typeof text === 'string' && text.trim()) {
            return text.split(/\r?\n/).map(function (line) {
                var s = line.trim();
                if (!s) return null;
                var parts = s.split(/[,，]/);
                var v = parts[0].trim();
                var lab = parts.length > 1 ? parts.slice(1).join(',').trim() : v;
                return { value: v, label: lab || v };
            }).filter(Boolean);
        }
        return null;
    }

    /**
     * 合并父容器穿透配置（白名单模式）。
     * @param {object} localOptions 当前组件 options
     * @param {object} parentPass 父注入的 comExtraPass（含 passValues：路径→值）
     * @param {string} passStr 子组件 compassthrough 白名单字符串
     * @param {boolean} whiteListMode true=启用白名单（取交集）；false=父清单字段全收
     */
    function applyPassThrough(localOptions, parentPass, passStr, whiteListMode) {
        var _ = L();
        var opts = _.cloneDeep ? _.cloneDeep(localOptions || {}) : JSON.parse(JSON.stringify(localOptions || {}));
        if (!parentPass) return opts;
        var parentVals = parentPass.passValues || {};
        var paths = Object.keys(parentVals);
        var allowed = paths;
        if (whiteListMode) {
            var childKeys = (passStr || '').split(',').map(function (i) { return i.trim(); }).filter(function (path) { return !!path; });
            allowed = paths.filter(function (p) { return childKeys.indexOf(p) >= 0; });
        }
        for (var i = 0; i < allowed.length; i++) {
            var path = allowed[i];
            var localVal = _.get ? _.get(opts, path) : undefined;
            // 子组件该路径未显式定义，才继承父的值
            if (localVal === undefined) {
                var pVal = parentVals[path];
                if (pVal !== undefined && _.set) _.set(opts, path, pVal);
            }
        }
        return opts;
    }

    /**
     * 表达式求值：{{ ... }} 包裹的按 JS 表达式求值，否则原样返回（字面量）。
     * ctx 字段：model / $self / value / row / fullModelName / getByPath / safeGet
     */
    function evalExpr(raw, ctx) {
        if (raw === null || raw === undefined) return raw;
        if (typeof raw !== 'string') return raw;
        var s = raw.trim();
        if (s.startsWith('{{') && s.endsWith('}}')) {
            var body = s.slice(2, -2).trim();
            if (!body) return undefined;
            try {
                // 参数顺序与 ctx 对齐：model, $self, value, row, fullModelName, getByPath, safeGet
                var fn = new Function('model', '$self', 'value', 'row', 'fullModelName', 'getByPath', 'safeGet', 'return (' + body + ');');
                return fn(ctx.model, ctx.$self, ctx.value, ctx.row, ctx.fullModelName, ctx.getByPath, ctx.safeGet);
            } catch (e) {
                console.warn('[bindings/modelexpr 表达式求值失败]', body, e);
                return undefined;
            }
        }
        return raw;
    }

    /**
     * 模板行内替换：把字符串中所有 {{ expr }} 用同一套 ctx 求值后替换。
     * valueTransform 可选，对每个求值结果做后处理（SQL 转义单引号 / AJAX URL 编码）。
     */
    function resolveTemplate(str, ctx, valueTransform) {
        if (typeof str !== 'string') return str;
        return str.replace(/\{\{\s*([\s\S]+?)\s*\}\}/g, function (match, body) {
            var val;
            try {
                var fn = new Function('model', '$self', 'value', 'row', 'fullModelName', 'getByPath', 'safeGet', 'return (' + body + ');');
                val = fn(ctx.model, ctx.$self, ctx.value, ctx.row, ctx.fullModelName, ctx.getByPath, ctx.safeGet);
            } catch (e) {
                console.warn('[template 表达式求值失败]', body, e);
                val = '';
            }
            var s = val == null ? '' : String(val);
            return valueTransform ? valueTransform(s) : s;
        });
    }

    /**
     * bindings 求值：键为 comoptions.x / labeloptions.x / itemoptions.x / shell.x，值为 {{ expr }} 或字面量。
     * 返回按组拆好的补丁：{ labeloptions, comoptions, itemoptions, shell }。
     */
    function bindingsPatch(bindings, ctx) {
        var _ = L();
        var patch = { labeloptions: {}, comoptions: {}, itemoptions: {}, shell: {} };
        if (!bindings || typeof bindings !== 'object') return patch;
        for (var key in bindings) {
            var val = evalExpr(bindings[key], ctx);
            var dot = key.indexOf('.');
            if (dot < 0) continue;
            var group = key.slice(0, dot);
            var sub = key.slice(dot + 1);
            if (group === 'labeloptions' || group === 'comoptions' || group === 'itemoptions' || group === 'shell') {
                if (_.set) _.set(patch[group], sub, val);
            }
        }
        return patch;
    }

    /**
     * evalcondition 脚本执行：脚本入参 ctx（与 bindings 同构），返回 {shell,labeloptions,comoptions} 补丁。
     * 全局开关 DYN_LIB_CONFIG.disableEvalJs=true 时直接返回 null。
     */
    function evalConditionPatch(scriptText, ctx) {
        if (!scriptText) return null;
        if (global.DYN_LIB_CONFIG && global.DYN_LIB_CONFIG.disableEvalJs) {
            console.warn("[BaseLayout] evalCondition全局关闭");
            return null;
        }
        try {
            var fn = new Function("ctx", scriptText);
            var ret = fn(ctx);
            if (ret && typeof ret === "object") {
                return {
                    shell: ret.shell != null ? ret.shell : null,
                    labeloptions: ret.labeloptions != null ? ret.labeloptions : null,
                    comoptions: ret.comoptions != null ? ret.comoptions : null
                };
            }
        } catch (e) {
            console.error("[evalCondition脚本执行异常]", e, scriptText);
        }
        return null;
    }

    /** 补齐节点配置的框架默认结构（原地写入，保持引用不变；jsonconfig 直连 reactive 时不可重建对象） */
    function ensureDefaults(jc, defaultPassThrough) {
        if (!jc.options) jc.options = {};
        if (!jc.options.comoptions) jc.options.comoptions = {};
        if (!jc.options.labeloptions) jc.options.labeloptions = { label: "", required: false, show: true };
        if (!jc.options.itemoptions) jc.options.itemoptions = { style: {}, class: "" };
        if (!jc.options.compassthrough) jc.options.compassthrough = defaultPassThrough;
        if (!Array.isArray(jc.childrenctrls)) jc.childrenctrls = [];
        if (!Array.isArray(jc.validators)) jc.validators = [];
        if (!jc.slots) jc.slots = {};
        if (!jc.extendinfo) jc.extendinfo = {};
        return jc;
    }

    /**
     * 校验执行：返回错误消息数组（空数组 = 通过）。
     * 支持的 type：required / min / max / length / pattern / email / phone / number / custom。
     * enableWhen（可选）为一段返回布尔的 JS 脚本，入参 ctx={value,model,getByPath}。
     */
    function runValidators(rules, val, model, getByPath) {
        var errors = [];
        var list = Array.isArray(rules) ? rules : [];
        var map = {
            required: function (v) { return v !== null && v !== undefined && v !== '' && !(Array.isArray(v) && v.length === 0); },
            min: function (v, m) { return v === '' || v === null || v === undefined || Number(v) >= Number(m); },
            max: function (v, m) { return v === '' || v === null || v === undefined || Number(v) <= Number(m); },
            length: function (v, m) { return !v || String(v).length >= Number(m); },
            pattern: function (v, p) { return !v || new RegExp(p).test(String(v)); },
            email: function (v) { return !v || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v)); },
            phone: function (v) { return !v || /^1[3-9]\d{9}$/.test(String(v)); },
            number: function (v) { return v === '' || v === null || v === undefined || !isNaN(Number(v)); },
            // custom：rule.value 为返回布尔值的 JS 表达式脚本（参数 v=当前值, model=表单模型）
            custom: function (v, script) {
                if (!script) return true;
                try { return !!new Function('v', 'model', 'return (' + script + ');')(val, model); }
                catch (e) { console.warn('[validator custom error]', e); return false; }
            }
        };
        for (var i = 0; i < list.length; i++) {
            var rule = list[i];
            if (!rule) continue;
            if (rule.enableWhen !== undefined) {
                try {
                    var en = new Function("ctx", rule.enableWhen)({ value: val, model: model, getByPath: getByPath });
                    if (!en) continue;
                } catch (e) {
                    console.warn("[validator enableWhen error]", e);
                }
            }
            var fn = map[rule.type];
            if (!fn) continue;
            if (!fn(val, rule.value)) errors.push(rule.message || ('验证失败: ' + rule.type));
        }
        return errors;
    }

    /**
     * 按 comoptions.sourceType 加载下拉选项，结果通过 onOptions 回写。
     *   static - 静态 optionValues / optionValuesText
     *   dict   - 平台字典 DynDict（dictType）
     *   table  - 工程表（projectId/table/valueField/textField，parentField+parentValuePath 联动）
     *   sql / ajax - 任意接口（url/sql 支持 {{model.field}} 占位，valueKey/labelKey 映射）
     * opts: { forceType, ctx, parentModel, onOptions }
     */
    function loadOptions(co, opts) {
        var s = co || {};
        var o = opts || {};
        var apply = o.onOptions || function () { };
        var t = o.forceType || s.sourceType || 'static';
        var text = optionsFromText(s.optionValuesText);
        if (t === 'static') {
            apply(text || normOptionList(s.optionValues));
            return;
        }
        if (t === 'dict') {
            if (!s.dictType) return;
            fetch('/api/dyncommon/dicts?type=' + encodeURIComponent(s.dictType))
                .then(function (r) { return r.json(); })
                .then(function (res) { if (res && res.code === 0) apply(normOptionList(res.data)); })
                .catch(function (e) { console.warn('[dict options]', e); });
            return;
        }
        if (t === 'table') {
            if (!s.projectId || !s.table) return;
            var qs = 'projectId=' + encodeURIComponent(s.projectId)
                + '&table=' + encodeURIComponent(s.table)
                + '&valueField=' + encodeURIComponent(s.valueField || 'Id')
                + '&textField=' + encodeURIComponent(s.textField || 'Name');
            if (s.parentField) {
                var pv = '';
                if (s.parentValuePath) pv = getPath(o.parentModel, s.parentValuePath);
                else if (s.parentValue !== undefined) pv = s.parentValue;
                if (pv !== undefined && pv !== null && pv !== '') {
                    qs += '&parentField=' + encodeURIComponent(s.parentField)
                        + '&parentValue=' + encodeURIComponent(pv);
                }
            }
            fetch('/api/dyncommon/options?' + qs)
                .then(function (r) { return r.json(); })
                .then(function (res) { if (res && res.code === 0) apply(normOptionList(res.data)); })
                .catch(function (e) { console.warn('[table options]', e); });
            return;
        }
        if (t === 'sql') {
            if (!s.sql) return;
            // {{ expr }} 用 bindings 同套 ctx 求值，值转义单引号防 SQL 语法破坏
            var resolvedSql = resolveTemplate(s.sql, o.ctx, function (v) { return v.replace(/'/g, "''"); });
            var q = 'projectId=' + encodeURIComponent(s.projectId || '') + '&sql=' + encodeURIComponent(resolvedSql);
            fetch('/api/dyncommon/sqloptions?' + q)
                .then(function (r) { return r.json(); })
                .then(function (res) { if (res && res.code === 0) apply(normOptionList(res.data)); })
                .catch(function (e) { console.warn('[sql options]', e); });
            return;
        }
        if (t === 'ajax') {
            if (!s.url) return;
            // {{ expr }} 用 bindings 同套 ctx 求值，值做 URL 编码
            var url = resolveTemplate(s.url, o.ctx, encodeURIComponent);
            fetch(url)
                .then(function (r) { return r.json(); })
                .then(function (res) {
                    var list = res && res.code === 0 ? res.data : (Array.isArray(res) ? res : (res && res.data) || []);
                    apply(normOptionList(list, s.valueKey, s.labelKey));
                })
                .catch(function (e) { console.warn('[ajax options]', e); });
        }
    }

    function getPath(obj, path) {
        var _ = L();
        return (!obj || !path || !_.get) ? undefined : _.get(obj, path);
    }

    /** 只保留函数型监听器（comlisteners 里可能混入配置键，模板按需绑定） */
    function getComlisteners(listeners) {
        var result = {};
        var src = listeners || {};
        for (var key in src) {
            if (typeof src[key] === 'function') result[key] = src[key];
        }
        return result;
    }

    /** 取插槽：优先真实具名插槽（context.slots），其次配置声明的 slots 定义 */
    function slotOf(contextSlots, jcSlots, name) {
        if (contextSlots && contextSlots[name]) return contextSlots[name];
        var def = jcSlots ? jcSlots[name] : undefined;
        return def !== undefined ? def : null;
    }

    /** 设计模式下容器为空时的占位样式类（容器类组件且无子节点才提示） */
    function designRootClass(component, childrenctrls, designMode) {
        if (!designMode) return "";
        var c = component || "";
        var isContainer = /Container|Form|Card|Tabs|Collapse|Row|Col|Panel/.test(c);
        if (isContainer && (!childrenctrls || childrenctrls.length === 0)) return "designer-empty-container";
        return "";
    }

    /**
     * 设计器交互绑定：点击选中 + 画布 Sortable 生命周期。
     * 画布拖放（库→画布 / 排序 / 跨容器移动）统一走 dyn-designer-sortable（SortableJS），
     * 不再挂任何原生 HTML5 拖放事件，避免双通道竞争。
     * cfg: { jc, props, designMode, pageCtx, designUid, designerScope, isTransparent(), isContainer(name) }
     * isTransparent 传函数：透明包装标记可能在 setupScripts 期间才写入，必须在使用时读取。
     */
    function bindDesignerNode(cfg) {
        var jc = cfg.jc, props = cfg.props, pageCtx = cfg.pageCtx;
        var sortableIns = null;

        function design() { return !!(cfg.designMode && cfg.designMode.value); }

        var handlers = {
            click: function (e) {
                if (!design() || !pageCtx || !pageCtx.onSelect) return;
                var selfUid = cfg.designUid && cfg.designUid.value;
                if (selfUid && e.target && e.target.closest) {
                    var inner = e.target.closest('[data-dyn-uid]');
                    if (inner && inner.getAttribute('data-dyn-uid') !== selfUid) return;
                }
                pageCtx.onSelect(props.jsonconfig, e);
            }
        };

        function destroy() {
            if (sortableIns) {
                if (global.DesignerSortable) global.DesignerSortable.destroy(sortableIns);
                sortableIns = null;
            }
        }

        /**
         * 初始化画布拖放。注意：DynDynamicCom 透明包装层与真实容器组件挂在同一根元素、
         * 都能看到同一个 .dyn-children-wrap，若两层都初始化会在同一元素上重复绑定 Sortable，
         * 嵌套容器时残留的旧实例 mousedown 监听器会与内层实例争抢，导致子组件拖不动。
         */
        function setup(rootEl) {
            if (!design() || (cfg.isTransparent && cfg.isTransparent()) || !global.DesignerSortable || !rootEl) return;
            if (sortableIns) return;
            var compName = jc.component || "";
            if (!(cfg.isContainer && cfg.isContainer(compName))) return;
            var isGrid = compName === "DynGridContainer";
            var listEl = (rootEl.classList && rootEl.classList.contains("dyn-children-wrap"))
                ? rootEl
                : (typeof rootEl.querySelector === "function" ? rootEl.querySelector(".dyn-children-wrap") : null);
            if (listEl) sortableIns = global.DesignerSortable.createCanvasContainer(listEl, jc, cfg.designerScope, isGrid);
        }

        return { handlers: handlers, setup: setup, destroy: destroy };
    }

    NC.parseAttrJson = parseAttrJson;
    NC.mergePatch = mergePatch;
    NC.mergeLabelOptions = mergeLabelOptions;
    NC.getStyle = getStyle;
    NC.getClass = getClass;
    NC.normOptionList = normOptionList;
    NC.optionsFromText = optionsFromText;
    NC.applyPassThrough = applyPassThrough;
    NC.evalExpr = evalExpr;
    NC.resolveTemplate = resolveTemplate;
    NC.bindingsPatch = bindingsPatch;
    NC.evalConditionPatch = evalConditionPatch;
    NC.ensureDefaults = ensureDefaults;
    NC.runValidators = runValidators;
    NC.loadOptions = loadOptions;
    NC.getComlisteners = getComlisteners;
    NC.designRootClass = designRootClass;
    // 设计态交互（选中/拖放/Sortable 生命周期）：_BaseLayout.setup 只接线，实现全在本文件
    NC.bindDesignerNode = bindDesignerNode;

    global.DynNodeConfig = NC;
    global.DynNode = NC;
})(window);
