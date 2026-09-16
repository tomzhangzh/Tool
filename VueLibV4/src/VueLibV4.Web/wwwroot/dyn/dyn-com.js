/* ============================================================
 * VueLib 低代码平台 - 全局唯一动态组件渲染内核（dyn-com）
 * ------------------------------------------------------------
 * 设计目标：设计器 / 运行时 / 预览 / NutUI 运行时 共用同一个
 * NDynamicCom 渲染实现，确保"设计器显示 = 页面显示"。
 *
 * 包含：
 *   1. compositeComponents —— 组合组件配置表（全局唯一）
 *   2. isContainerComp / applyCompositeProps —— 渲染辅助
 *   3. NDynamicCom —— 唯一递归渲染器（设计态装饰 + 纯渲染双模式）
 *   4. registerComponents —— 组件注册（组合配置 + 自定义脚本 + 注册）
 *
 * 设计态判定（双通道，均可不提供）：
 *   - inject lcDesigner（设计器 store：designMode.value / currentCom.value）
 *   - inject lcDesignState（预览 state：mode / selectedPath / 拖拽状态）
 *   运行时都不 provide -> isDesign=false -> 纯渲染（组合组件仍展开）。
 * ============================================================ */
(function (global) {
    'use strict';

    if (global.DynCom) { console.log('[dyn-com] 已存在，跳过重复加载'); return; }

    var Vue = global.Vue;
    if (!Vue) { console.error('[dyn-com] 未找到 Vue'); return; }

    var computed = Vue.computed;

    // ===== 组合组件配置表（全局唯一）=====
    var compositeComponents = {};

    // ===== 结构性布局样式键（不复制到设计器 lc-node 装饰层）=====
    // 这些键描述"组件自身内部的布局"（如容器两列 grid），只应由组件本体渲染；
    // 若同时复制到 lc-node 装饰层，会出现"装饰层 + 组件本体"双重网格/双重布局，
    // 导致设计器里容器/网格类组件显示错乱（如教师细节屏两列 grid 被劈成一半）。
    var LAYOUT_DENY_KEYS = {
        'display': 1, 'float': 1, 'clear': 1,
        'gap': 1, 'column-gap': 1, 'row-gap': 1,
        'grid': 1, 'grid-template': 1, 'grid-template-columns': 1, 'grid-template-rows': 1, 'grid-template-areas': 1,
        'grid-auto-flow': 1, 'grid-auto-columns': 1, 'grid-auto-rows': 1,
        'grid-column': 1, 'grid-row': 1, 'grid-area': 1,
        'grid-column-start': 1, 'grid-column-end': 1, 'grid-row-start': 1, 'grid-row-end': 1,
        'flex': 1, 'flex-flow': 1, 'flex-direction': 1, 'flex-wrap': 1,
        'flex-grow': 1, 'flex-shrink': 1, 'flex-basis': 1,
        'justify-content': 1, 'justify-items': 1, 'justify-self': 1,
        'align-content': 1, 'align-items': 1, 'align-self': 1,
        'place-content': 1, 'place-items': 1, 'place-self': 1,
        'order': 1
    };

    // ===== 工具函数 =====
    function isContainerComp(name) {
        if (!name) return false;
        return /(DivContainer|Grid|Row|Col|Card|Form|CellGroup|Container|Tab|Step|Swipe|Table|List|Menu|Footer|Header|Aside|Main)$/i.test(name)
            || name === 'DynElTable'
            || /^DynEl(Container|Row|Col|Card|Form|Tabs|Steps)$/i.test(name)
            || /^DynN(CellGroup|DivContainer|Grid|Row|Col|Card|Form|Tabs|Swipe|Steps)$/i.test(name)
            || /^DynCom/.test(name) && !!compositeComponents[name];
    }

    // 将组合组件的开放属性/开放容器内容应用到模板树
    function applyCompositeProps(tree, config, externalProps, externalSlots) {
        if (!tree || typeof tree !== 'object') return tree;
        if (Array.isArray(tree)) return tree.map(function (t) { return applyCompositeProps(t, config, externalProps, externalSlots); });

        var node = Object.assign({}, tree);
        // 开放属性：实例 comoptions 的键 -> 模板节点对应路径
        // 兼容两种结构：exposedProps[{key,target}]（保存组合组件格式）/ openProps[{key}]
        var openProps = (config && (config.exposedProps || config.openProps)) || [];
        if (openProps.length) {
            for (var i = 0; i < openProps.length; i++) {
                var op = openProps[i];
                if (!op || !op.key) continue;
                var val = externalProps ? externalProps[op.key] : undefined;
                if (val !== undefined) {
                    // exposedProps 用 target 指定写入位置；openProps 的 key 即路径
                    var targetPath = op.target || op.key;
                    setPath(node, targetPath, val);
                }
            }
        }
        // 开放容器：slots 里的数组 -> 模板节点 __openSlot（外部 slots 无该 key 时保留模板默认 children）
        if (config && config.openContainers && config.openContainers.length) {
            for (var j = 0; j < config.openContainers.length; j++) {
                var oc = config.openContainers[j];
                if (!oc || !oc.key) continue;
                var hasSlot = !!(externalSlots && oc.key in externalSlots);
                if (!hasSlot) continue;
                var kids = externalSlots[oc.key] || [];
                setPath(node, oc.key, kids.map(function (k) {
                    return Object.assign({ __unlocked: true }, k);
                }));
            }
        }
        // 递归子节点
        if (node.childrenctrls && node.childrenctrls.length) {
            node.childrenctrls = node.childrenctrls.map(function (c) {
                return applyCompositeProps(c, config, externalProps, externalSlots);
            });
        }
        return node;
    }

    function setPath(obj, path, value) {
        if (!obj || !path) return;
        var keys = String(path).split('.');
        var last = keys.pop();
        var target = obj;
        for (var i = 0; i < keys.length; i++) {
            var k = keys[i];
            var m = k.match(/^(\w+)\[(\d+)\]$/);
            if (m) {
                var arrKey = m[1], idx = Number(m[2]);
                if (!target[arrKey]) target[arrKey] = [];
                if (!target[arrKey][idx]) target[arrKey][idx] = {};
                target = target[arrKey][idx];
            } else if (/^\d+$/.test(k)) {
                var nIdx = Number(k);
                if (!Array.isArray(target) || target[nIdx] === undefined) {
                    if (!Array.isArray(target)) target = [];
                    target[nIdx] = {};
                }
                target = target[nIdx];
            } else {
                if (!target[k] || typeof target[k] !== 'object') target[k] = {};
                target = target[k];
            }
        }
        target[last] = value;
    }

    // ===== 组件注册（组合配置 + 自定义脚本 + app.component）=====
    // DynPropControl：属性框内置控件（不依赖组件表异步注册）
    // - 按 jsonconfig.options.comoptions.kind 渲染 el 控件（input/number/switch/select/radio/textarea/color/slider）
    // - v-model 绑定：parentmodelinfo + modelname（~路径 直接读写被编辑组件 ConfigJson）
    // - 选项支持静态 optionValues / dict（DynDict）/ ajax（dataUrl，POST）
    function buildPropOptions(co) {
        var opts = [];
        if (co.sourceType === 'dict' || co.sourceType === 'ajax') {
            // 异步加载
            var url = co.sourceType === 'dict'
                ? '/DynRun/Dict?dictType=' + encodeURIComponent(co.dictType || '')
                : (co.dataUrl || '');
            if (url) {
                fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: '{}' })
                    .then(function (r) { return r.json(); })
                    .then(function (j) {
                        var arr = (j && (j.items || j.rows || j.data)) || [];
                        co._opts = arr.map(function (v) {
                            return { label: v.label ?? v.text ?? v.name ?? String(v.value ?? ''), value: v.value ?? v.code ?? String(v.label ?? '') };
                        });
                    }).catch(function () { co._opts = []; });
            }
            return co._opts || [];
        }
        var raw = co.optionValues;
        if (Array.isArray(raw)) {
            opts = raw.map(function (v) {
                return (typeof v === 'object' && v !== null)
                    ? { label: v.label ?? v.value, value: v.value ?? v.label }
                    : { label: String(v).trim(), value: String(v).trim() };
            });
        } else if (raw != null && raw !== '') {
            opts = String(raw).split(',').map(function (v) { return { label: v.trim(), value: v.trim() }; });
        }
        return opts;
    }
    var DynPropControl = {
        name: 'DynPropControl',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, default: null },
            nodePath: { type: String, default: 'root' }
        },
        setup: function (props) {
            var jc = props.jsonconfig || {};
            if (!jc.options) jc.options = {};
            if (!jc.options.comoptions) jc.options.comoptions = {};
            var co = jc.options.comoptions;
            var parentModelPrefix = inject('parentModelPrefix', computed(function () { return ''; }));
            function getByPath(obj, path) { return !obj || !path ? undefined : ((global._ && global._.get) ? global._.get(obj, path) : undefined); }
            function setByPath(obj, path, value) { if (!obj || !path) return; if (global._ && global._.set) global._.set(obj, path, value); }
            var fullModelName = computed(function () {
                var raw = jc.modelname || '';
                if (!raw) { var p = parentModelPrefix.value || ''; return p.indexOf('~') === 0 ? p.slice(1) : p; }
                if (raw.indexOf('~') === 0) return raw.slice(1);
                return raw;
            });
            var innerValue = ref(null);
            var modelinfo = computed({
                get: function () {
                    if (props.parentmodelinfo && fullModelName.value) {
                        var v = getByPath(props.parentmodelinfo, fullModelName.value);
                        return v === undefined ? (co.default !== undefined ? co.default : '') : v;
                    }
                    return innerValue.value;
                },
                set: function (val) {
                    if (props.parentmodelinfo && fullModelName.value) setByPath(props.parentmodelinfo, fullModelName.value, val);
                    else innerValue.value = val;
                }
            });
            var kind = computed(function () { return co.kind || 'input'; });
            var options = computed(function () { return buildPropOptions(co); });
            // ---- object ↔ JSON 显示转换（textarea / code 绑定 object 字段：显示 JSON 字符串，输入合法 JSON 字符串写回 object）----
            function isObjValue(v) { return v !== null && typeof v === 'object'; }
            function toDisplayText(v) {
                if (v === undefined || v === null) return '';
                return isObjValue(v) ? JSON.stringify(v, null, 2) : String(v);
            }
            var editBuf = ref('');
            var rawIsObj = ref(false);
            function refreshBuf(v) { editBuf.value = toDisplayText(v); rawIsObj.value = isObjValue(v); }
            function onTextInput(v) { editBuf.value = v; }
            function commitBuf() {
                var v = editBuf.value;
                if (rawIsObj.value) {
                    try { modelinfo.value = JSON.parse(v); }
                    catch (e) { editBuf.value = toDisplayText(modelinfo.value); }
                } else {
                    modelinfo.value = v;
                }
            }
            // ---- kind=code：CodeMirror 代码编辑器（全局库，同步渲染，适用于 JSON/JS/SQL/CSS/XML/HTML）----
            var taRef = Vue.ref(null);
            var cmInstance = Vue.ref(null);
            Vue.onMounted(function () {
                if (kind.value !== 'code') return;
                if (!global.CodeMirror || !taRef.value) return;
                var lang = co.language || 'json';
                var modeMap = { json: 'application/json', javascript: 'javascript', js: 'javascript', sql: 'text/x-sql', css: 'text/css', xml: 'application/xml', html: 'text/html' };
                cmInstance.value = global.CodeMirror.fromTextArea(taRef.value, {
                    lineNumbers: true,
                    lineWrapping: true,
                    readOnly: !!co.readonly,
                    mode: modeMap[lang] || lang,
                    extraKeys: { 'Ctrl-Space': 'autocomplete' }
                });
                if (co.height) { try { cmInstance.value.setSize(null, co.height); } catch (e) {} }
                cmInstance.value.on('change', function (inst) { editBuf.value = inst.getValue(); });
                cmInstance.value.on('blur', function (inst) {
                    var v = inst.getValue();
                    if (rawIsObj.value) {
                        try { modelinfo.value = JSON.parse(v); } catch (e) {}
                    } else {
                        modelinfo.value = v;
                    }
                });
            });
            Vue.watch(modelinfo, function (v) {
                refreshBuf(v);
                if (cmInstance.value) {
                    var shown = toDisplayText(v);
                    var cv = cmInstance.value.getValue();
                    if (shown !== cv) cmInstance.value.setValue(shown || '');
                }
            }, { immediate: true });
            Vue.onBeforeUnmount(function () {
                if (cmInstance.value) { try { cmInstance.value.toTextArea(); } catch (e) {} cmInstance.value = null; }
            });
            return { jc: jc, co: co, kind: kind, options: options, modelinfo: modelinfo, props: props, taRef: taRef, editBuf: editBuf, onTextInput: onTextInput, commitBuf: commitBuf };
        },
        template: [
            '<el-input v-if="kind === \'input\'" :model-value="modelinfo" @update:model-value="modelinfo = $event"',
            '           :placeholder="co.placeholder || \'\'" :type="co.type || \'text\'" clearable size="small" style="width:100%;" />',
            '<el-input v-else-if="kind === \'textarea\'" :model-value="editBuf" @update:model-value="onTextInput" @blur="commitBuf"',
            '           :placeholder="co.placeholder || \'\'" type="textarea" :rows="co.rows || 3" size="small" style="width:100%;" />',
            '<div v-else-if="kind === \'code\'" style="width:100%;border:1px solid #dcdfe6;border-radius:4px;overflow:hidden;">',
            '  <textarea ref="taRef" :value="editBuf" style="display:none;"></textarea>',
            '</div>',
            '<el-input-number v-else-if="kind === \'number\'" :model-value="modelinfo" @update:model-value="modelinfo = $event"',
            '           :min="co.min" :max="co.max" :step="co.step || 1" controls-position="right" size="small" style="width:100%;" />',
            '<el-switch v-else-if="kind === \'switch\'" :model-value="modelinfo" @update:model-value="modelinfo = $event" size="small" />',
            '<el-select v-else-if="kind === \'select\' || kind === \'radio\'" :model-value="modelinfo" @update:model-value="modelinfo = $event"',
            '           :placeholder="co.placeholder || \'请选择\'" filterable clearable size="small" style="width:100%;">',
            '  <el-option v-for="opt in options" :key="opt.value" :label="opt.label" :value="opt.value" />',
            '</el-select>',
            '<el-radio-group v-else-if="kind === \'radio\'" :model-value="modelinfo" @update:model-value="modelinfo = $event" size="small">',
            '  <el-radio v-for="opt in options" :key="opt.value" :label="opt.value">{{ opt.label }}</el-radio>',
            '</el-radio-group>',
            '<el-color-picker v-else-if="kind === \'color\'" :model-value="modelinfo" @update:model-value="modelinfo = $event" size="small" />',
            '<el-slider v-else-if="kind === \'slider\'" :model-value="modelinfo" @update:model-value="modelinfo = $event"',
            '           :min="co.min || 0" :max="co.max || 100" :step="co.step || 1" style="width:100%;" />',
            '<el-input v-else :model-value="modelinfo" @update:model-value="modelinfo = $event" placeholder="未支持控件类型" size="small" />'
        ].join('\n')
    };

    // DynPropContainer：属性框根/子容器（简单 div 渲染 children，内核内置）
    var DynPropContainer = {
        name: 'DynPropContainer',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, default: null },
            nodePath: { type: String, default: 'root' }
        },
        setup: function (props) {
            var jc = props.jsonconfig || {};
            if (!jc.options) jc.options = {};
            if (!jc.options.itemoptions) jc.options.itemoptions = { style: {} };
            function getStyle() { return Object.assign({}, jc.options.itemoptions.style || {}); }
            function getClass() { return String(jc.options.itemoptions.class || '').split(' ').filter(Boolean); }
            var safeChildren = computed(function () { return (jc.childrenctrls || []).filter(function (c) { return c && typeof c === 'object'; }); });
            return { jc: jc, props: props, getStyle: getStyle, getClass: getClass, safeChildren: safeChildren };
        },
        template: [
            '<div class="el-prop-container" :style="getStyle()" :class="getClass()">',
            '  <n-dynamic-com v-for="(child, idx) in safeChildren" :key="((child && child.component) || \'x\') + \'_\' + idx"',
            '                 :jsonconfig="child" :parentmodelinfo="props.parentmodelinfo"',
            '                 :node-path="props.nodePath + \'.childrenctrls[\' + idx + \']\'"></n-dynamic-com>',
            '</div>'
        ].join('\n')
    };

    // DynPropItem：属性框条目组件（el-form-item 外壳 + 绑定路径）
    // - 属性框渲染时，DynPropItem.modelname = 被编辑组件 ConfigJson 的绑定路径（如 options.labeloptions.label）
    // - 子控件（input/select/switch...）的 modelname 为空，通过 parentModelPrefix 继承该路径
    // - parentmodelinfo 由属性框容器注入 = 被编辑组件的 ConfigJson，子控件读写即更新 ConfigJson
    var DynPropItem = {
        name: 'DynPropItem',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, default: null },
            nodePath: { type: String, default: 'root' }
        },
        setup: function (props) {
            var jc = props.jsonconfig || {};
            if (!jc.options) jc.options = {};
            if (!jc.options.comoptions) jc.options.comoptions = {};
            var co = jc.options.comoptions;
            if (co.label === undefined) co.label = '';
            if (co.required === undefined) co.required = false;
            if (co.showLabel === undefined) co.showLabel = true;
            if (co.labelWidth === undefined) co.labelWidth = '110px';
            if (!jc.childrenctrls) jc.childrenctrls = [];
            // 把"绑定路径"作为前缀传给子控件：~ 前缀 = 直接用路径，不拼外层前缀
            provide('parentModelPrefix', computed(function () { return '~' + (jc.modelname || ''); }));
            provide('parent_jsonconfig', jc);
            provide('parent_com', { parent_Config: null, parent_Com: null });
            function getStyle() {
                var o = (jc.options && jc.options.itemoptions) || {};
                return Object.assign({}, o.style || {});
            }
            function getClass() {
                var o = (jc.options && jc.options.itemoptions) || {};
                return String(o.class || '').split(' ').filter(Boolean);
            }
            var safeChildren = computed(function () {
                var arr = (jc.childrenctrls || []).filter(function (c) { return c && typeof c === 'object'; });
                return arr;
            });
            return { jc: jc, co: co, props: props, getStyle: getStyle, getClass: getClass, safeChildren: safeChildren };
        },
        template: [
            '<div class="el-prop-item" :style="getStyle()" :class="getClass()">',
            '  <label v-if="co.showLabel !== false" class="el-form-item__label prop-item-label"',
            '         :style="{ width: co.labelWidth || \'110px\' }" :class="{ \'is-required\': co.required }">',
            '    {{ co.label }}',
            '  </label>',
            '  <div class="el-form-item__content prop-item-content">',
            '    <n-dynamic-com v-for="(child, idx) in safeChildren" :key="((child && child.component) || \'x\') + \'_\' + idx"',
            '                   :jsonconfig="child" :parentmodelinfo="props.parentmodelinfo"',
            '                   :node-path="props.nodePath + \'.childrenctrls[\' + idx + \']\'"></n-dynamic-com>',
            '  </div>',
            '</div>'
        ].join('\n')
    };

    async function registerComponents(app, metas) {
        var count = 0;
        var composites = 0;
        var registry = {};
        app.component('DynPropItem', DynPropItem); // 内核内置：属性框条目组件
        app.component('DynPropControl', DynPropControl); // 内核内置：属性框控件
        app.component('DynPropContainer', DynPropContainer); // 内核内置：属性框容器
        registry['DynPropItem'] = DynPropItem;
        registry['DynPropControl'] = DynPropControl;
        registry['DynPropContainer'] = DynPropContainer;
        if (!metas || !metas.length) {
            global.__lcCompRegistry = registry;
            return { count: count, composites: composites, registry: registry };
        }
        for (var i = 0; i < metas.length; i++) {
            var meta = metas[i];
            var name = meta.componentName || meta.ComponentName || meta.name;
            var url = meta.loadUrl || meta.LoadUrl;
            if (!name) continue;
            if (meta.isComposite && meta.compositeConfigJson) {
                try { compositeComponents[name] = JSON.parse(meta.compositeConfigJson); composites++; }
                catch (e) { console.error('[dyn-com] 解析组合组件配置失败:', name, e); }
            }
            if (meta.customScriptJson && global.nutRegisterCustomScript) {
                try { global.nutRegisterCustomScript(name, meta.customScriptJson); }
                catch (e) { console.error('[dyn-com] 注册自定义脚本失败:', name, e); }
            }
            if (name && url && typeof global.nutLoadCom === 'function' && name !== 'DynPropItem') {
                var comp = global.nutLoadCom(name, url);
                // 修复 V4：'Button' 不注册到 app 全局组件表（仅保留在 registry / __lcCompRegistry 中）。
                // 原因：ElementPlus ElButton 内部渲染使用 resolveDynamicComponent(tag)，tag 默认 'button'，
                // 会被 capitalize 解析为注册名 'Button'，若命中 V1 Button 组件（其模板又是 el-button）
                // 会造成 el-button -> Button -> el-button 无限递归（RangeError / 渲染成空注释）。
                // 保留 registry 引用，页面配置 { "component": "Button" } 仍可经 DynCom.get 正常渲染。
                if (name !== 'Button') {
                    app.component(name, comp);
                }
                registry[name] = comp;
                count++;
            }
        }
        global.__lcCompRegistry = registry;
        return { count: count, composites: composites, registry: registry };
    }

    // ===== 唯一 NDynamicCom =====
    var NDynamicCom = {
        name: 'NDynamicCom',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, default: function () { return {}; } },
            nodePath: { type: String, default: 'root' },
            locked: { type: Boolean, default: false }
        },
        inject: {
            lcDesigner: { default: null },
            lcDesignState: { default: null },
            lcLocked: { default: null },
            lcCompositeRoot: { default: null },
            lcLabelCtx: { default: null }
        },
        data: function () { return { tbNearTop: false }; },
        provide: function () {
            var self = this;
            return {
                lcLocked: computed(function () { return self.isLocked; }),
                lcCompositeRoot: self.isComposite ? self.jsonconfig : null,
                lcLabelCtx: computed(function () { return self.effectiveLabel; })
            };
        },
        template: [
            '<div v-if="!validConfig" class="lc-node lc-error" style="padding:8px;color:#f56c6c;font-size:12px;">',
            '  [NDynamicCom] 无效配置: {{ nodePath }}',
            '</div>',
            '<div v-else-if="depthExceeded" class="lc-node lc-error" style="padding:8px;color:#f56c6c;font-size:12px;">',
            '  [NDynamicCom] 递归深度超限: {{ nodePath }}',
            '</div>',
            // ============ 设计态：lc-node 装饰 ============
            '<div v-else-if="isDesign" class="lc-node"',
            '     :class="nodeClassObj"',
            '     :draggable="designDragEnabled"',
            '     @click.stop="onClick"',
            '     @dragstart="onDragStart" @dragover="onDragOver" @dragleave="onDragLeave" @drop="onDrop" @dragend="onDragEnd"',
            '     @lc-sort-end="onSortEnd"',
            '     :style="nodeStyle">',
            // 设计态工具条（designer）
            '  <div v-if="toolbarVisible" class="lc-node-toolbar" :class="{ \'lc-tb-below\': tbNearTop }" v-on:mousedown="onToolbarDown" v-on:click.stop>',
            '    <span class="lc-node-tb-drag" title="拖拽移动">⋮⋮</span>',
            '    <span class="lc-node-tb-label">{{ componentLabel }}</span>',
            '    <span v-if="isSelected" class="lc-node-tb-ops">',
            '      <span class="lc-node-tb-btn" title="上移" v-on:click.stop="moveNode(-1)">↑</span>',
            '      <span class="lc-node-tb-btn" title="下移" v-on:click.stop="moveNode(1)">↓</span>',
            '      <span class="lc-node-tb-btn" title="复制" v-on:click.stop="copyNode">⧉</span>',
            '      <span class="lc-node-tb-btn lc-node-tb-del" title="删除组件" v-on:click.stop="removeNode">✕</span>',
            '    </span>',
            '  </div>',
            // 开放容器插槽标签条
            '  <div v-if="isOpenSlot" class="lc-open-slot-tag" @click.stop="onClick">',
            '    <span class="lc-open-slot-icon">⊕</span>{{ openSlotLabel }}',
            '    <span v-if="openSlotHint" class="lc-open-slot-hint">{{ openSlotHint }}</span>',
            '  </div>',
            // 有 Wrapper
            '  <component v-if="hasWrapper" :is="wrapperComponent"',
            '             :jsonconfig="jsonconfig.options.wrapperoptions"',
            '             :parentmodelinfo="parentmodelinfo"',
            '             :node-path="nodePath + \'.wrapper\'">',
            '    <n-dynamic-com v-if="isComposite && compositeTree" :jsonconfig="compositeTree"',
            '                   :parentmodelinfo="parentmodelinfo" :node-path="nodePath + \'.composite\'" :locked="true"></n-dynamic-com>',
            '    <component v-else :is="jsonconfig.component" :jsonconfig="jsonconfig"',
            '               :parentmodelinfo="parentmodelinfo" :node-path="nodePath"></component>',
            '  </component>',
            // 无 Wrapper
            '  <template v-else>',
            '    <n-dynamic-com v-if="isComposite && compositeTree" :jsonconfig="compositeTree"',
            '                   :parentmodelinfo="parentmodelinfo" :node-path="nodePath + \'.composite\'" :locked="true"></n-dynamic-com>',
            '    <component v-else :is="jsonconfig.component" :jsonconfig="jsonconfig"',
            '               :parentmodelinfo="parentmodelinfo" :node-path="nodePath"></component>',
            '  </template>',
            // 插入位置指示器（HTML5 拖拽）
            '  <div v-if="showInsertBefore" class="lc-insert-indicator lc-insert-before"></div>',
            '  <div v-if="showInsertAfter" class="lc-insert-indicator lc-insert-after"></div>',
            '</div>',
            // ============ 运行态：纯渲染（组合组件仍展开）============
            '<component v-else-if="hasWrapper" :is="wrapperComponent"',
            '           :jsonconfig="jsonconfig.options.wrapperoptions"',
            '           :parentmodelinfo="parentmodelinfo"',
            '           :node-path="nodePath + \'.wrapper\'">',
            '  <n-dynamic-com v-if="isComposite && compositeTree" :jsonconfig="compositeTree"',
            '                 :parentmodelinfo="parentmodelinfo" :node-path="nodePath + \'.composite\'" :locked="true"></n-dynamic-com>',
            '  <component v-else :is="jsonconfig.component" :jsonconfig="jsonconfig"',
            '             :parentmodelinfo="parentmodelinfo" :node-path="nodePath" :class="runtimeClass" :style="runtimeStyle"></component>',
            '</component>',
            '<n-dynamic-com v-else-if="isComposite && compositeTree" :jsonconfig="compositeTree"',
            '               :parentmodelinfo="parentmodelinfo" :node-path="nodePath + \'.composite\'" :locked="true"></n-dynamic-com>',
            '<component v-else :is="jsonconfig.component" :jsonconfig="jsonconfig"',
            '           :parentmodelinfo="parentmodelinfo" :node-path="nodePath" :class="runtimeClass" :style="runtimeStyle"></component>'
        ].join('\n'),
        computed: {
            validConfig: function () { return this.jsonconfig && typeof this.jsonconfig === 'object' && this.jsonconfig.component; },
            depth: function () {
                var m = this.nodePath.match(/\.(childrenctrls\[|composite|wrapper)/g);
                return m ? m.length : 0;
            },
            depthExceeded: function () { return this.depth > 15; },
            // ===== 设计态判定（双通道）=====
            ds: function () { return this.lcDesignState || null; },
            isDesign: function () {
                if (this.lcDesigner && this.lcDesigner.designMode && this.lcDesigner.designMode.value === 'design') return true;
                return !!(this.ds && this.ds.mode === 'design');
            },
            designDragEnabled: function () {
                // 设计器与 preview 均启用 HTML5 原生拖拽（Sortable 方案已废弃）
                // 组合内部锁定节点不可拖
                if (this.jsonconfig && this.jsonconfig.__locked) return false;
                return this.isDesign;
            },
            toolbarVisible: function () {
                // 仅 designer 场景显示工具条（preview 无工具栏需求）
                return !!(this.lcDesigner && this.isDesign);
            },
            isSelected: function () {
                if (this.lcDesigner && this.lcDesigner.currentCom && this.lcDesigner.currentCom.value === this.jsonconfig) return true;
                return !!(this.ds && this.ds.selectedPath === this.nodePath);
            },
            isContainer: function () { return isContainerComp(this.jsonconfig.component); },
            isComposite: function () {
                // __expanded = 组合组件展开树根，避免 tree 根引用组合组件自身造成无限递归
                if (this.jsonconfig && this.jsonconfig.__expanded) return false;
                return !!compositeComponents[this.jsonconfig.component];
            },
            hasWrapper: function () { return !!(this.jsonconfig.options && this.jsonconfig.options.wrapperoptions && this.jsonconfig.options.wrapperoptions.component); },
            wrapperComponent: function () { return this.jsonconfig.options.wrapperoptions.component; },
            compositeTree: function () {
                if (!this.isComposite) return null;
                // 属性框条目（DynPropItem）：实例结构已完整（条目 + 控件），不展开组合模板，
                // 避免组合模板里的默认 leaf（kind=input）覆盖实例控件配置（textarea/select/code/switch 等）
                if (this.jsonconfig && this.jsonconfig.component === 'DynPropItem') return null;
                var config = compositeComponents[this.jsonconfig.component];
                if (!config || !config.tree) return null;
                // 开放属性取值：实例顶层字段（modelname 等）+ comoptions 合并
                var externalProps = Object.assign({}, this.jsonconfig);
                var instCo = this.jsonconfig.options && this.jsonconfig.options.comoptions || {};
                for (var eKey in instCo) externalProps[eKey] = instCo[eKey];
                var externalSlots = this.jsonconfig.slots || (this.jsonconfig.slots = {});
                if (config.openContainers) {
                    for (var i = 0; i < config.openContainers.length; i++) {
                        var oc = config.openContainers[i];
                        if (externalSlots[oc.key]) void externalSlots[oc.key].length;
                    }
                }
                var _tree = applyCompositeProps(config.tree, config, externalProps, externalSlots);
                if (_tree && typeof _tree === 'object') _tree.__expanded = true;
                return _tree;
            },
            parentLocked: function () {
                var pl = this.lcLocked;
                if (pl == null) return false;
                return (typeof pl === 'object' && 'value' in pl) ? !!pl.value : !!pl;
            },
            isLocked: function () { return (this.locked || this.parentLocked) && !(this.jsonconfig && this.jsonconfig.__unlocked); },
            isOpenSlot: function () { return !!(this.jsonconfig && this.jsonconfig.__openSlot); },
            // ===== 容器属性下探 =====
            ownLabel: function () {
                if (!this.isContainer) return null;
                var o = this.jsonconfig.options || {};
                var lc = o.labelcontext || {};
                var w = o.labelWidth !== undefined ? o.labelWidth : (o.labelwidth !== undefined ? o.labelwidth : lc.width);
                var a = o.labelAlign !== undefined ? o.labelAlign : (o.labelalign !== undefined ? o.labelalign : lc.align);
                if (w === undefined && a === undefined) return null;
                return { width: w, align: a };
            },
            effectiveLabel: function () {
                var own = this.ownLabel;
                if (own) return own;
                var p = this.lcLabelCtx;
                if (p && typeof p === 'object' && 'value' in p) return p.value || null;
                return p || null;
            },
            nodeStyle: function () {
                var st = {};
                var lb = this.effectiveLabel;
                if (lb && lb.width !== undefined) st['--lc-label-width'] = lb.width;
                if (lb && lb.align !== undefined) st['--lc-label-align'] = lb.align;
                var col = Number(this.jsonconfig.options && this.jsonconfig.options.colspan || 0);
                if (col >= 2 && col <= 12) st.gridColumn = 'span ' + col;
                // 用户 itemoptions.style（设计态实时显示；结构性布局键不下发到装饰层，避免与组件本体双重布局）
                var us = this.jsonconfig && this.jsonconfig.options && this.jsonconfig.options.itemoptions && this.jsonconfig.options.itemoptions.style;
                if (us && typeof us === 'object') {
                    for (var k in us) {
                        if (us[k] === undefined || us[k] === '') continue;
                        if (LAYOUT_DENY_KEYS[k]) continue;
                        st[k] = us[k];
                    }
                }
                return st;
            },
            runtimeClass: function () {
                var uc = (this.jsonconfig && this.jsonconfig.options && this.jsonconfig.options.itemoptions && this.jsonconfig.options.itemoptions.class) || '';
                return String(uc).split(/\s+/).filter(Boolean);
            },
            runtimeStyle: function () {
                var us = this.jsonconfig && this.jsonconfig.options && this.jsonconfig.options.itemoptions && this.jsonconfig.options.itemoptions.style;
                return (us && typeof us === 'object') ? us : {};
            },
            nodeClassObj: function () {
                var obj = {
                    'lc-selected': this.isSelected,
                    'lc-dragging': this.isDraggingSelf,
                    'lc-drop-target': this.isDropTarget,
                    'lc-container': this.isContainer,
                    'lc-composite': this.isComposite,
                    'lc-wrapper': this.hasWrapper,
                    'lc-locked': this.isLocked,
                    'lc-open-slot': this.isOpenSlot,
                    'lc-design': true
                };
                var uc = (this.jsonconfig && this.jsonconfig.options && this.jsonconfig.options.itemoptions && this.jsonconfig.options.itemoptions.class) || '';
                String(uc).split(/\s+/).filter(Boolean).forEach(function (c) { obj[c] = true; });
                return obj;
            },
            componentLabel: function () {
                var name = this.jsonconfig.component || '';
                var meta = null;
                if (global.dynCom && global.dynCom.get) meta = global.dynCom.get(name);
                if (meta && meta.label) return meta.label + ' · ' + name;
                return name;
            },
            openSlotLabel: function () { return this.jsonconfig.__openSlot ? (this.jsonconfig.__openSlot.label || '') : ''; },
            openSlotHint: function () { return this.jsonconfig.__openSlot ? (this.jsonconfig.__openSlot.hint || '') : ''; },
            // ===== HTML5 拖拽状态（preview）=====
            isDraggingSelf: function () {
                return !!(this.ds && this.ds.isDragging && this.ds.dragType === 'move' && this.ds.sourcePath === this.nodePath);
            },
            isDropTarget: function () {
                return !!(this.ds && this.ds.isDragging && this.ds.dropTargetPath === this.nodePath && this.ds.dropPosition === 'inside');
            },
            showInsertBefore: function () {
                return !!(this.ds && this.ds.isDragging && this.ds.dropTargetPath === this.nodePath && this.ds.dropPosition === 'before');
            },
            showInsertAfter: function () {
                return !!(this.ds && this.ds.isDragging && this.ds.dropTargetPath === this.nodePath && this.ds.dropPosition === 'after');
            }
        },
        mounted: function () {
            var self = this;
            if (this.toolbarVisible) {
                this._observeTb();
            }
            this._initSortable();
        },
        updated: function () {
            if (this.toolbarVisible) {
                this._observeTb();
            }
        },
        beforeUnmount: function () {
            if (this._sortable) { try { this._sortable.destroy(); } catch (e) { } this._sortable = null; }
            if (this._tbScrollEl) { this._tbScrollEl.removeEventListener('scroll', this._tbHandler, true); this._tbScrollEl = null; }
        },
        methods: {
            // ===== 选择 =====
            onClick: function () {
                if (this.isLocked) return;
                // 开放容器/组合内部节点点击 -> 选中所属组合组件实例
                if (this.isOpenSlot) {
                    if (this.lcDesigner && this.lcCompositeRoot) { this.lcDesigner.setCurrentCom(this.lcCompositeRoot); return; }
                    if (this.ds && this.lcCompositeRoot) {
                        this.ds.selectedPath = this.nodePath;
                        this.postSelect(this.nodePath);
                        return;
                    }
                }
                if (this.lcDesigner && this.lcDesigner.setCurrentCom && this.isDesign) { this.lcDesigner.setCurrentCom(this.jsonconfig); return; }
                if (this.ds && this.isDesign) {
                    this.ds.selectedPath = this.nodePath;
                    this.postSelect(this.nodePath);
                }
            },
            postSelect: function (path) {
                try { if (parent && parent.postMessage) parent.postMessage({ type: 'component-selected', path: path }, '*'); } catch (e) { /* 运行时不处理 */ }
            },
            // ===== HTML5 拖拽（设计器 + preview）=====
            onDragStart: function (e) {
                // 设计器：内部拖拽由容器 Sortable 接管（这里不干预，避免与 Sortable 冲突）
                if (this.isDesign && this.lcDesigner) { return; }
                if (!this.isDesign || !this.ds) return;
                e.stopPropagation();
                this.ds.isDragging = true;
                this.ds.dragType = 'move';
                this.ds.sourcePath = this.nodePath;
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('text/plain', this.nodePath);
            },
            onDragOver: function (e) {
                // 设计器：只处理 palette 拖入（内部拖拽由 Sortable 处理，这里不 preventDefault）
                if (this.isDesign && this.lcDesigner) {
                    if (!e.dataTransfer || !e.dataTransfer.types) return;
                    var U = window.LCDesignerUtils;
                    var types = Array.from(e.dataTransfer.types || []);
                    var isPalette = types.indexOf('application/x-lc-comp') >= 0;
                    if (!isPalette) return;
                    // 只有容器接收；非容器冒泡给父容器/画布兜底
                    var canReceive = (this.isContainer || (this.jsonconfig && this.jsonconfig.__openSlot));
                    if (!canReceive) return;
                    if (this.jsonconfig && this.jsonconfig.__locked) return; // 组合内部锁定容器不接收
                    e.preventDefault();
                    e.stopPropagation();
                    e.dataTransfer.dropEffect = 'copy';
                    if (U && U.updateDropPlaceholder && this.$el) U.updateDropPlaceholder(this.$el, e);
                    return;
                }
                if (!this.isDesign || !this.ds || !this.ds.isDragging) return;
                e.preventDefault();
                e.stopPropagation();
                var rect = this.$el.getBoundingClientRect();
                var y = e.clientY - rect.top;
                var h = rect.height;
                if (this.isContainer) {
                    if (y < h * 0.25) { this.ds.dropTargetPath = this.nodePath; this.ds.dropPosition = 'before'; }
                    else if (y > h * 0.75) { this.ds.dropTargetPath = this.nodePath; this.ds.dropPosition = 'after'; }
                    else { this.ds.dropTargetPath = this.nodePath; this.ds.dropPosition = 'inside'; }
                    e.dataTransfer.dropEffect = 'move';
                } else {
                    this.ds.dropTargetPath = this.nodePath;
                    this.ds.dropPosition = (y < h / 2) ? 'before' : 'after';
                    e.dataTransfer.dropEffect = 'move';
                }
            },
            onDragLeave: function () { /* 由 dragover 持续更新 */ },
            onDragEnd: function (e) {
                var U = window.LCDesignerUtils;
                if (U) {
                    U.dragState.draggingInternal = false;
                    U.dragState.sourcePath = null;
                    if (U.clearDropPlaceholder) U.clearDropPlaceholder();
                }
            },
            onDrop: function (e) {
                // 设计器：只处理 palette 插入（内部拖拽由 Sortable 的 onEnd 更新 json）
                if (this.isDesign && this.lcDesigner) {
                    if (!e.dataTransfer || !e.dataTransfer.types) return;
                    var U = window.LCDesignerUtils;
                    var types = Array.from(e.dataTransfer.types || []);
                    var isPalette = types.indexOf('application/x-lc-comp') >= 0;
                    if (!isPalette) return;
                    var canReceive = (this.isContainer || (this.jsonconfig && this.jsonconfig.__openSlot));
                    if (!canReceive) return;
                    if (this.jsonconfig && this.jsonconfig.__locked) return;
                    e.preventDefault();
                    e.stopPropagation();
                    var idx = U && U.computeInsertIndex ? U.computeInsertIndex(this.$el, e) : null;
                    if (U && U.clearDropPlaceholder) U.clearDropPlaceholder();
                    var d = this.lcDesigner;
                    if (d && typeof d.onPaletteDrop === 'function') d.onPaletteDrop(this.jsonconfig, e, idx);
                    return;
                }
                if (!this.isDesign || !this.ds || !this.ds.isDragging) return;
                e.preventDefault();
                e.stopPropagation();
                var targetPath = this.ds.dropTargetPath;
                var position = this.ds.dropPosition;
                try { if (parent && parent.postMessage) parent.postMessage({ type: 'component-drop', targetPath: targetPath, position: position, sourcePath: this.ds.sourcePath }, '*'); } catch (err) { }
                this.ds.isDragging = false;
                this.ds.dropTargetPath = null;
                this.ds.dropPosition = null;
            },
            // ===== Sortable 排序结束（designer）=====
            onSortEnd: function (e) {
                var detail = e && e.detail ? e.detail : (e || {});
                var item = detail.item;
                var newArrIndex = detail.newArrIndex;
                var children = this.jsonconfig && this.jsonconfig.childrenctrls;
                if (!item || !children || !Array.isArray(children)) return;
                var origIdx = children.indexOf(item);
                if (origIdx < 0) return;
                children.splice(origIdx, 1);
                var insertIdx = children.length;
                var safeCount = 0;
                for (var i = 0; i < children.length; i++) {
                    var cc = children[i];
                    if (cc && typeof cc === 'object' && cc.component) {
                        if (safeCount === newArrIndex) { insertIdx = i; break; }
                        safeCount++;
                    }
                }
                children.splice(insertIdx, 0, item);
            },
            // ===== Sortable 初始化（设计器内部拖拽，palette 拖入仍走 HTML5）=====
            _initSortable: function () {
                var self = this;
                if (!this.isDesign || !this.lcDesigner || !window.Sortable) return;
                if (!this.isContainer) return;
                if (this.jsonconfig && this.jsonconfig.__locked) return;
                var el = this.$el;
                if (!el || el.__lcSortable) return;
                var dragSel = '> .lc-node:not(.lc-locked)';
                var dragOldArrIndex = -1;
                el.__lcConfig = this.jsonconfig;
                try {
                    el.__lcSortable = window.Sortable.create(el, {
                        animation: 150,
                        ghostClass: 'lc-ghost',
                        dragClass: 'lc-drag',
                        chosenClass: 'lc-chosen',
                        draggable: dragSel,
                        filter: '.lc-node-toolbar, .lc-node-tb-drag, .lc-open-slot-tag, .lc-tb-btn',
                        preventOnFilter: false,
                        onStart: function (evt) {
                            document.body.style.userSelect = 'none';
                            var dragged = evt.item || el.children[0];
                            var draggableChildren = Array.from(el.children).filter(function (c) { return c.matches(dragSel); });
                            dragOldArrIndex = draggableChildren.indexOf(dragged);
                        },
                        onEnd: function (evt) {
                            document.body.style.userSelect = '';
                            var oldIdxSaved = dragOldArrIndex;
                            dragOldArrIndex = -1;
                            try {
                                var fromEl = evt.from || el, toEl = evt.to || el;
                                var srcConfig = fromEl.__lcConfig, tgtConfig = toEl.__lcConfig;
                                if (!srcConfig || !tgtConfig) return;
                                var srcArr = srcConfig.childrenctrls, tgtArr = tgtConfig.childrenctrls;
                                if (!Array.isArray(srcArr) || !Array.isArray(tgtArr)) return;
                                if (oldIdxSaved < 0 || oldIdxSaved >= srcArr.length) return;
                                var item = srcArr[oldIdxSaved];
                                if (!item || typeof item !== 'object') return;
                                var oldIdx = srcArr.indexOf(item);
                                if (oldIdx < 0) return;
                                var targetDraggable = Array.from(toEl.children).filter(function (c) { return c.matches(dragSel); });
                                var newArrIndex = targetDraggable.indexOf(evt.item);
                                srcArr.splice(oldIdx, 1);
                                if (srcArr === tgtArr && newArrIndex > oldIdx) newArrIndex--;
                                if (newArrIndex < 0) newArrIndex = tgtArr.length;
                                if (newArrIndex > tgtArr.length) newArrIndex = tgtArr.length;
                                tgtArr.splice(newArrIndex, 0, item);
                                window.dispatchEvent(new CustomEvent('lc-tree-refresh'));
                                var d = self.lcDesigner;
                                if (d && typeof d.setCurrentCom === 'function') d.setCurrentCom(item);
                            } catch (err) { console.error('[Sortable] onEnd error:', err); }
                        }
                    });
                } catch (err) { console.error('[Sortable] init error:', err); }
            },
            // ===== 工具条操作（designer）=====
            onToolbarDown: function () { /* 拖拽由容器 Sortable 处理 */ },
            removeNode: function () {
                var d = this.lcDesigner;
                if (!d) return;
                d.setCurrentCom(this.jsonconfig);
                if (typeof d.deleteCurrent === 'function') d.deleteCurrent();
                else if (typeof global.__lcDeleteCurrent === 'function') global.__lcDeleteCurrent();
            },
            moveNode: function (dir) {
                var d = this.lcDesigner;
                if (!d) return;
                d.setCurrentCom(this.jsonconfig);
                if (dir < 0 && typeof d.moveUp === 'function') d.moveUp();
                else if (dir > 0 && typeof d.moveDown === 'function') d.moveDown();
            },
            copyNode: function () {
                var d = this.lcDesigner;
                if (!d) return;
                d.setCurrentCom(this.jsonconfig);
                if (typeof d.copyCurrent === 'function') d.copyCurrent();
            },
            // ===== 工具条贴顶翻转 =====
            _observeTb: function () {
                var el = this.$el; if (!el) return;
                var scrollEl = el.closest('.canvas-scroll') || el.parentElement;
                if (this._tbScrollEl !== scrollEl) {
                    if (this._tbScrollEl) this._tbScrollEl.removeEventListener('scroll', this._tbHandler, true);
                    this._tbScrollEl = scrollEl;
                    if (scrollEl) scrollEl.addEventListener('scroll', this._tbHandler, true);
                }
                this._tbUpdate();
            },
            _tbUpdate: function () {
                var el = this.$el; if (!el) return;
                var scrollEl = this._tbScrollEl || (el.closest('.canvas-scroll') || el.parentElement);
                if (!scrollEl) return;
                var sr = scrollEl.getBoundingClientRect();
                var er = el.getBoundingClientRect();
                var near = (er.top - sr.top) < 26;
                if (near !== this.tbNearTop) this.tbNearTop = near;
            },
            _tbHandler: function () { this._tbUpdate(); }
        }
    };

    // ===== 导出（V1 完整内核）=====
    global.DynCom = {
        NDynamicCom: NDynamicCom,
        compositeComponents: compositeComponents,
        isContainerComp: isContainerComp,
        applyCompositeProps: applyCompositeProps,
        registerComponents: registerComponents,
        DynPropItem: DynPropItem,
        DynPropControl: DynPropControl,
        DynPropContainer: DynPropContainer
    };

    /* ================================================================
     * V4 兼容层（追加，不改 V1 内核）：
     *   - nutLoadCom / nutRegisterCustomScript 桥接到 V4 的 vue-loader.js
     *   - V3 API：register / get / meta / loadMeta / setupApp / registry / metaMap
     *     （V4 的 dyn-core.DynRender 与 dyn-designer 依赖这些 API）
     * ================================================================ */

    // 组件注册表（V3 API 共享同一份 registry）
    var _registry = {};
    var _metaMap = {};

    function v4Register(name, component, meta) {
        _registry[name] = component;
        if (meta) _metaMap[name] = meta;
    }
    function v4Get(name) {
        if (_registry[name]) return _registry[name];
        // 回退：V1 registerComponents 注册的组件（写入 __lcCompRegistry）
        var lc = global.__lcCompRegistry;
        if (lc && lc[name]) return lc[name];
        return undefined;
    }
    function v4Meta(name) { return _metaMap[name]; }

    /** 统一组件注册入口：首次调用时从后端拉取组件清单并注册（异步组件，注册后即可渲染） */
    var _regPromise = null;
    function v4EnsureRegistered(app) {
        if (_regPromise) return _regPromise;
        _regPromise = fetch('/api/component/list')
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (res && res.data && Array.isArray(res.data)) {
                    return registerComponents(app, res.data);
                }
                return { count: 0, composites: 0, registry: {} };
            })
            .catch(function (e) {
                console.error('[DynCom] 注册组件清单失败', e);
                return { count: 0, composites: 0, registry: {} };
            });
        return _regPromise;
    }

    /** 从后端加载组件元数据（ComponentMeta /all），供设计器组件库 + 默认值补全 */
    function v4LoadMeta() {
        return fetch('/api/platform/componentmeta/all')
            .then(function (r) { return r.json(); })
            .then(function (res) {
                if (res.code === 0) {
                    (res.data || []).forEach(function (m) {
                        if (m && m.ComponentName) {
                            try { m.PropsMeta = typeof m.PropsMeta === 'string' ? JSON.parse(m.PropsMeta) : (m.PropsMeta || []); } catch (e) { m.PropsMeta = []; }
                            try { m.AllowDrop = typeof m.AllowDrop === 'string' ? JSON.parse(m.AllowDrop) : (m.AllowDrop || []); } catch (e) { m.AllowDrop = []; }
                            try { m.CanDropInto = typeof m.CanDropInto === 'string' ? JSON.parse(m.CanDropInto) : (m.CanDropInto || []); } catch (e) { m.CanDropInto = []; }
                            try { m.SlotsDefine = typeof m.SlotsDefine === 'string' ? JSON.parse(m.SlotsDefine) : (m.SlotsDefine || []); } catch (e) { m.SlotsDefine = []; }
                            _metaMap[m.ComponentName] = m;
                        }
                    });
                }
                return _metaMap;
            })
            .catch(function (e) { console.error('[DynCom] 加载组件元数据失败', e); return _metaMap; });
    }

    /** 应用初始化：注册 ElementPlus + 图标 + V1 内核 + 已注册组件 */
    function v4SetupApp(app) {
        if (global.ElementPlus) {
            var locale = (global.ElementPlus.locale && global.ElementPlus.locale.zhCn) || undefined;
            app.use(global.ElementPlus, locale ? { locale: locale } : undefined);
        }
        if (global.ElementPlusIconsVue) {
            Object.keys(global.ElementPlusIconsVue).forEach(function (k) {
                app.component(k, global.ElementPlusIconsVue[k]);
            });
        }
        // V1 内核组件（NDynamicCom 渲染器 + 属性框组件）
        ['NDynamicCom', 'DynPropItem', 'DynPropControl', 'DynPropContainer'].forEach(function (cn) {
            if (global.DynCom[cn]) app.component(cn, global.DynCom[cn]);
        });
        // V4 统一递归渲染器：DynRender（dyn-core）——设计器/运行时一套代码，
        // 不包装 DOM（选中/拖拽事件直接落在组件根元素），容器组件 children 也用它递归
        if (global.DynCore && global.DynCore.DynRender) {
            app.component('DynRender', global.DynCore.DynRender);
        }
        Object.keys(_registry).forEach(function (k) {
            // 修复 V4：'Button' 不注册到 app 全局组件表（原因同上，避免 el-button 内部 tag='button' 递归）
            if (k === 'Button') return;
            app.component(k, _registry[k]);
        });
    }

    // nutLoadCom 桥接：V4 使用 vue-loader.js 的 vueLoadCom（Razor 代码优先 / DB 回退）
    if (!global.nutLoadCom && typeof global.vueLoadCom === 'function') {
        global.nutLoadCom = function (name, url) { return global.vueLoadCom(name); };
    }
    if (!global.nutRegisterCustomScript) {
        global.nutRegisterCustomScript = function (name, json) { /* V4 无自定义脚本注册，静默 */ };
    }

    // 合并导出：V1 内核 + V3 API
    global.DynCom = Object.assign(global.DynCom, {
        register: v4Register,
        get: v4Get,
        meta: v4Meta,
        loadMeta: v4LoadMeta,
        setupApp: v4SetupApp,
        ensureRegistered: v4EnsureRegistered,
        registry: _registry,
        metaMap: _metaMap
    });

    console.log('[dyn-com] 唯一动态组件渲染内核已加载（V1 完整内核 + V4 兼容层）');
})(window);
