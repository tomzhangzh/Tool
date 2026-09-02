/**
 * html-code-generator.js —— 页面 HTML 代码生成器
 * 根据当前设计页面的 configObj（组件配置树）递归生成 ElementUI / NutUI 的 Vue 模板 HTML 代码。
 * 暴露全局：window.LCHtmlCodeGen
 *   - generateHtml(configObj, modelObj) => { html: string, modelCode: string }
 *
 * 生成策略：
 *   - 容器组件（form/cell-group/div/grid/card/row/col/tabs...）→ 打开标签 + 递归 childrenctrls + 关闭标签
 *   - 表单组件（input/select/radio/...）→ <form-item label=".."> + 组件标签（v-model 绑定 model.xxx）
 *   - 普通组件（button/text/tag/...）→ 组件标签 + 文本内容
 *   - 组合组件（DynCom*）→ 递归其 childrenctrls（含开放容器 slots 外部内容）
 *   - 属性来自 options.comoptions（字符串→k="v"，数字/布尔/对象→:k="v"）
 *   - label 来自 options.labeloptions.label，必填来自 labeloptions.required
 *   - v-model 来自 node.modelname（自动加 model. 前缀，支持 user.name 嵌套路径）
 */
(function (global) {
    'use strict';

    // ===== 组件 → 标签/渲染形态映射 =====
    // tag       : 生成的 HTML 标签
    // container : 容器，递归 childrenctrls
    // formItem  : 表单组件，包一层 form-item（label + v-model）
    // grid      : 容器且子项需包一层 item 标签（nut-grid → nut-grid-item）
    // text      : 文本内容组件（内容取自 comoptions.text / labeloptions.label）
    // group     : 选项组组件（radio/checkbox），若配置了 optionValues 生成选项
    const TAG_MAP = {
        // ===== NutUI =====
        DynNForm:        { tag: 'nut-form', container: true },
        DynNCellGroup:   { tag: 'div', cls: 'nut-cell-group-wrapper', container: true, titleField: 'title' },
        DynNDivContainer:{ tag: 'div', container: true },
        DynNDivider:     { tag: 'nut-divider', text: true },
        DynNGrid:        { tag: 'nut-grid', container: true, grid: true },
        DynNGridMenu:    { tag: 'nut-grid', container: true, grid: true },
        DynNNavBar:      { tag: 'nut-navbar', container: true },
        DynNBottomNav:   { tag: 'nut-tabbar', container: true },
        DynNButton:      { tag: 'nut-button', text: true },
        DynNImage:       { tag: 'nut-image' },
        DynNIcon:        { tag: 'nut-icon', text: true },
        DynNText:        { tag: 'nut-text', text: true },
        DynNTag:         { tag: 'nut-tag', text: true },
        DynNNoticeBar:   { tag: 'nut-noticebar', text: true },
        DynNProgress:    { tag: 'nut-progress' },
        DynNEmpty:       { tag: 'nut-empty' },
        DynNStatCard:    { tag: 'div', cls: 'nut-stat-card', container: true },
        DynNHeroBanner:  { tag: 'div', cls: 'nut-hero-banner', container: true },
        DynNReportCard:  { tag: 'div', cls: 'nut-report-card', container: true },
        DynNReportFilter:{ tag: 'div', cls: 'nut-report-filter', container: true },
        DynNLoginCard:   { tag: 'div', cls: 'nut-login-card', container: true },
        DynNProfileHeader:{ tag: 'div', cls: 'nut-profile-header', container: true },
        DynNDataTable:   { tag: 'div', cls: 'nut-data-table', text: true },
        DynNEChart:      { tag: 'div', cls: 'nut-echart' },
        DynNMenuItem:    { tag: 'div', cls: 'nut-menu-item', text: true },
        DynNViewToggle:  { tag: 'div', cls: 'nut-view-toggle', container: true },
        // NutUI 表单
        DynNInput:       { tag: 'nut-input', formItem: true },
        DynNTextarea:    { tag: 'nut-textarea', formItem: true },
        DynNSwitch:      { tag: 'nut-switch', formItem: true },
        DynNRadio:       { tag: 'nut-radio', formItem: true, group: 'nut-radio-group', item: 'nut-radio', itemValue: 'value' },
        DynNCheckbox:    { tag: 'nut-checkbox', formItem: true, group: 'nut-checkbox-group', item: 'nut-checkbox', itemValue: 'value' },
        DynNStepper:     { tag: 'nut-stepper', formItem: true },
        DynNRate:        { tag: 'nut-rate', formItem: true },
        DynNSlider:      { tag: 'nut-slider', formItem: true },
        DynNPicker:      { tag: 'nut-picker', formItem: true },
        DynNDatePicker:  { tag: 'nut-datepicker', formItem: true },
        DynNUploader:    { tag: 'nut-uploader', formItem: true },

        // ===== ElementUI =====
        DynElForm:       { tag: 'el-form', container: true },
        DynElDivContainer: { tag: 'div', container: true },
        DynElCard:       { tag: 'el-card', container: true },
        DynElRow:        { tag: 'el-row', container: true },
        DynElCol:        { tag: 'el-col', container: true },
        DynElTabs:       { tag: 'el-tabs', container: true },
        DynElButton:     { tag: 'el-button', text: true },
        DynElImage:      { tag: 'el-image' },
        DynElTag:        { tag: 'el-tag', text: true },
        DynElBadge:      { tag: 'el-badge', text: true },
        DynElAvatar:     { tag: 'el-avatar', text: true },
        DynElProgress:   { tag: 'el-progress' },
        DynElAlert:      { tag: 'el-alert', text: true },
        DynElDivider:    { tag: 'el-divider', text: true },
        // ElementUI 表单
        DynElInput:      { tag: 'el-input', formItem: true },
        DynElInputNumber:{ tag: 'el-input-number', formItem: true },
        DynElSelect:     { tag: 'el-select', formItem: true, group: 'el-select', item: 'el-option', itemValue: 'value', optionLabel: 'label' },
        DynElSwitch:     { tag: 'el-switch', formItem: true },
        DynElRadio:      { tag: 'el-radio', formItem: true, group: 'el-radio-group', item: 'el-radio', itemValue: 'value' },
        DynElCheckbox:   { tag: 'el-checkbox', formItem: true, group: 'el-checkbox-group', item: 'el-checkbox', itemValue: 'value' },
        DynElDatePicker: { tag: 'el-date-picker', formItem: true },
        DynElTimePicker: { tag: 'el-time-picker', formItem: true },
        DynElSlider:     { tag: 'el-slider', formItem: true },
        DynElRate:       { tag: 'el-rate', formItem: true },
        DynElColorPicker:{ tag: 'el-color-picker', formItem: true }
    };

    // 运行时无对应独立标签、但本质是容器的组件（组合组件 / 兜底）
    function isCompositeName(name) {
        return !!name && (name.indexOf('DynCom') === 0);
    }

    /**
     * 收集节点 modelname（去重、支持 user.name 嵌套）
     */
    function collectModelNames(node, out) {
        if (!node || typeof node !== 'object') return;
        if (node.modelname) {
            out[node.modelname] = true;
        }
        (node.childrenctrls || []).forEach(function (c) { collectModelNames(c, out); });
        if (node.slots) {
            Object.keys(node.slots).forEach(function (k) {
                const s = node.slots[k];
                if (s && s.childrenctrls) s.childrenctrls.forEach(function (c) { collectModelNames(c, out); });
            });
        }
    }

    /**
     * 将 model 字段路径列表转成嵌套的 reactive 声明代码
     * ['user.name', 'age'] → "  user: { name: '' },\n  age: ''"
     */
    function modelFieldsToCode(modelNames) {
        const root = {};
        Object.keys(modelNames).forEach(function (path) {
            const parts = path.split('.');
            let cur = root;
            for (let i = 0; i < parts.length - 1; i++) {
                if (!cur[parts[i]] || typeof cur[parts[i]] !== 'object') cur[parts[i]] = {};
                cur = cur[parts[i]];
            }
            cur[parts[parts.length - 1]] = '';
        });
        function render(obj, indent) {
            const pad = '  '.repeat(indent);
            const pad2 = '  '.repeat(indent + 1);
            return Object.keys(obj).map(function (k) {
                const v = obj[k];
                if (v && typeof v === 'object') {
                    return pad + k + ': {\n' + render(v, indent + 1) + '\n' + pad + '}';
                }
                return pad + k + ': ' + JSON.stringify(v);
            }).join(',\n');
        }
        return render(root, 1);
    }

    /**
     * 将 options.comoptions 中的配置转成标签属性字符串
     * 排除键：text / title（这些作为文本内容）、label / value（选项用）
     */
    function attrsFrom(comoptions, excludeKeys) {
        const ex = excludeKeys || ['text', 'title'];
        return Object.keys(comoptions || {}).filter(function (k) {
            const v = comoptions[k];
            return ex.indexOf(k) < 0 && v !== undefined && v !== null && v !== '' && v !== false;
        }).map(function (k) {
            const v = comoptions[k];
            if (typeof v === 'boolean') return k;                       // true → 布尔属性
            if (typeof v === 'number') return ':' + k + '="' + v + '"'; // 数字 → 动态绑定
            if (typeof v === 'object') return ':' + k + "='" + JSON.stringify(v) + "'"; // 对象 → 动态绑定
            return k + '="' + String(v).replace(/"/g, '&quot;') + '"';
        }).join(' ');
    }

    /**
     * 获取组件的文本内容
     */
    function textOf(node, meta) {
        const opt = (node.options && node.options.comoptions) || {};
        const label = (node.options && node.options.labeloptions && node.options.labeloptions.label) || '';
        if (opt.text) return String(opt.text);
        if (opt.title) return String(opt.title);
        return label || '';
    }

    /**
     * 获取 optionValues（逗号分隔字符串 → 数组）
     */
    function optionValuesOf(node) {
        const raw = node.options && node.options.optionValues;
        if (typeof raw === 'string' && raw.trim()) {
            return raw.split(',').map(function (s) { return s.trim(); }).filter(Boolean);
        }
        if (Array.isArray(raw)) return raw;
        return null;
    }

    /**
     * 递归解析组合组件：从组合模板（compositeComponents）合并开放属性/开放容器，
     * 把"薄"的组合节点展开为包含内部组件的完整树（供 model 收集与渲染使用）。
     * 注意：调用方必须传入 deepClone 后的树，避免污染页面 config。
     */
    function resolveCompositeTree(node) {
        if (!node || typeof node !== 'object' || !node.component) return node;
        if (isCompositeName(node.component) && !TAG_MAP[node.component]) {
            let full = node;
            try {
                const comps = (window.LCDesignerCore && window.LCDesignerCore.compositeComponents) || {};
                const cc = comps[node.component];
                const utils = window.LCDesignerUtils || {};
                if (cc && cc.tree && typeof utils.applyCompositeProps === 'function') {
                    full = utils.applyCompositeProps(cc.tree, cc,
                        (node.options && node.options.comoptions) || {},
                        node.slots || {});
                }
            } catch (e) { /* 模板合并失败则保持原节点 */ }
            (full.childrenctrls || []).forEach(function (c, i) {
                full.childrenctrls[i] = resolveCompositeTree(c);
            });
            return full;
        }
        if (node.childrenctrls) {
            node.childrenctrls = node.childrenctrls.map(function (c) { return resolveCompositeTree(c); });
        }
        if (node.slots) {
            Object.keys(node.slots).forEach(function (k) {
                const s = node.slots[k];
                if (s && s.childrenctrls) {
                    s.childrenctrls = s.childrenctrls.map(function (c) { return resolveCompositeTree(c); });
                }
            });
        }
        return node;
    }

    /**
     * 递归渲染一个节点
     */
    function renderNode(node, depth) {
        if (!node || typeof node !== 'object' || !node.component) return '';
        const indent = '  '.repeat(depth);
        const comp = node.component;
        const children = (node.childrenctrls || []).filter(function (c) { return c && typeof c === 'object'; });
        const slots = node.slots || {};

        // 组合组件：从组合模板（compositeComponents）合并开放属性/开放容器后递归。
        // 页面 config 中组合节点是"薄"节点（仅外部 props + slots），内部内容需从模板 tree 重建。
        if (isCompositeName(comp) && !TAG_MAP[comp]) {
            let full = node;
            try {
                const comps = (window.LCDesignerCore && window.LCDesignerCore.compositeComponents) || {};
                const cc = comps[comp];
                const utils = window.LCDesignerUtils || {};
                if (cc && cc.tree && typeof utils.applyCompositeProps === 'function') {
                    const externalProps = (node.options && node.options.comoptions) || {};
                    const externalSlots = node.slots || {};
                    // applyCompositeProps 内部 deepClone 模板，不会污染组合定义
                    full = utils.applyCompositeProps(cc.tree, cc, externalProps, externalSlots);
                }
            } catch (e) { /* 模板合并失败则回退使用原始 children */ }
            const children2 = (full.childrenctrls || []).filter(function (c) { return c && typeof c === 'object'; });
            const inner = children2.map(function (c) { return renderNode(c, depth + 1); });
            if (!inner.length) return indent + '<!-- 组合组件 ' + comp + '（无内部内容） -->\n';
            return indent + '<!-- 组合组件 ' + comp + ' -->\n' + inner.join('');
        }

        const meta = TAG_MAP[comp];
        if (!meta) {
            // 未知组件：输出注释占位，仍递归子级
            const inner = children.map(function (c) { return renderNode(c, depth + 1); }).join('');
            return indent + '<!-- 组件 ' + comp + '（未配置代码生成映射） -->\n' +
                (inner ? inner : '');
        }

        const opt = (node.options && node.options.comoptions) || {};
        const labelOpt = (node.options && node.options.labeloptions) || {};
        const label = labelOpt.label || '';
        const required = !!labelOpt.required;
        const modelname = node.modelname || '';
        const vmodel = modelname ? 'v-model="model.' + modelname + '"' : '';
        const cls = meta.cls ? ' class="' + meta.cls + '"' : '';
        const options = optionValuesOf(node);

        // ===== 容器 =====
        if (meta.container) {
            const attr = [meta.cls ? 'class="' + meta.cls + '"' : ''].filter(Boolean).join(' ');
            const childrenHtml = children.map(function (c) { return renderNode(c, depth + 1); }).join('');
            const title = opt.title ? indent + '  <div class="' + (meta.cls ? meta.cls + '-title' : 'group-title') + '">' + opt.title + '</div>\n' : '';
            let body = childrenHtml;
            if (meta.grid) {
                // grid：每个子项包一层 item 标签
                body = children.map(function (c) {
                    return indent + '  <nut-grid-item>\n' + renderNode(c, depth + 2) + indent + '  </nut-grid-item>\n';
                }).join('');
            }
            return indent + '<' + meta.tag + (meta.cls ? ' class="' + meta.cls + '"' : '') + '>\n' +
                title + body +
                indent + '</' + meta.tag + '>\n';
        }

        // ===== 表单组件（包 form-item）=====
        if (meta.formItem) {
            const attrs = [vmodel, attrsFrom(opt)].filter(Boolean).join(' ');
            let innerTag = indent + '  <' + meta.tag + (attrs ? ' ' + attrs : '') + '></' + meta.tag + '>\n';
            // 选项组（radio / checkbox / select）
            if (meta.group && options && options.length) {
                const groupTag = meta.group;
                const itemTag = meta.item || meta.tag;
                const valueAttr = meta.itemValue || 'value';
                const optLabelAttr = meta.optionLabel || 'label';
                const isElOption = meta.group === 'el-select';
                const items = options.map(function (o) {
                    const optLabel = isElOption ? ' label="' + o + '"' : '';
                    const body = isElOption ? '' : o;
                    return indent + '    <' + itemTag + optLabel + ' ' + valueAttr + '="' + o + '">' + body + '</' + itemTag + '>\n';
                }).join('');
                innerTag = indent + '  <' + groupTag + (vmodel ? ' ' + vmodel : '') + '>\n' + items + indent + '  </' + groupTag + '>\n';
            }
            // form-item 包裹（按组件前缀区分 ElementUI / NutUI）
            const fiTag = comp.indexOf('DynEl') === 0 ? 'el-form-item' : 'nut-form-item';
            const fiAttr = 'label="' + label + '"' + (required ? ' required' : '');
            return indent + '<' + fiTag + ' ' + fiAttr + '>\n' +
                innerTag +
                indent + '</' + fiTag + '>\n';
        }

        // ===== 普通组件 =====
        const text = meta.text ? textOf(node, meta) : '';
        const attrs = [attrsFrom(opt, meta.text ? ['text', 'title'] : ['title'])].filter(Boolean).join(' ');
        const openTag = '<' + meta.tag + (attrs ? ' ' + attrs : '') + '>';
        if (text) {
            return indent + openTag + text + '</' + meta.tag + '>\n';
        }
        return indent + openTag + '></' + meta.tag + '>\n';
    }

    /**
     * 生成完整 HTML 代码（Vue SFC 风格：template + script setup 的 model 声明）
     */
    function generateHtml(configObj, modelObj) {
        // 深拷贝后先解析组合组件，得到包含内部组件的完整渲染树（避免污染页面 config）
        const root = JSON.parse(JSON.stringify(configObj || {}));
        const full = resolveCompositeTree(root);
        const names = {};
        collectModelNames(full, names);
        const body = renderNode(full, 0);
        const modelCode = modelFieldsToCode(names);
        const html = '<template>\n' + body + '</template>\n\n' +
            '<script setup>\n' +
            "import { reactive } from 'vue'\n\n" +
            'const model = reactive({\n' + modelCode + '\n})\n' +
            '</script>';
        return html;
    }

    global.LCHtmlCodeGen = {
        generateHtml: generateHtml,
        TAG_MAP: TAG_MAP
    };

})(window);
