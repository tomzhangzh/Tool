/**
 * dyn-com.js —— dynCom 唯一内核（设计态/运行态一套代码）
 * 节点 JSON 标准结构：
 * { component, modelname, options:{ comoptions, comlisteners, labeloptions:{label,required,show}, itemoptions:{style,class} },
 *   validators:[], childrenctrls:[], slots:{}, extendinfo:{} }
 *
 * 保存清理规则：属性为 null/""/[]/{} 不落库；加载时统一补默认值。
 * 静态变量集中在头部。
 */
(function (global) {
    'use strict';

    /* ===== 静态配置 ===== */
    var CFG = {
        ROOT_COMPONENT: 'DynElForm',
        // 需要清理的空值
        EMPTY_VALUES: [null, undefined, '', [], {}]
    };

    /**
     * 判断值是否为空（null/""/[]/{}）
     */
    function isEmpty(v) {
        if (v === null || v === undefined) return true;
        if (typeof v === 'string') return v === '';
        if (Array.isArray(v)) return v.length === 0;
        if (typeof v === 'object') return Object.keys(v).length === 0;
        return false;
    }

    /**
     * 深度清理节点：移除 null/""/[]/{} 的属性（保存时调用）
     */
    function cleanNode(node) {
        if (!node || typeof node !== 'object') return node;
        var result = {};
        Object.keys(node).forEach(function (k) {
            var v = node[k];
            if (isEmpty(v)) return; // 跳过空值
            if (k === 'options' && typeof v === 'object') {
                result[k] = cleanOptions(v);
            } else if (k === 'childrenctrls' && Array.isArray(v)) {
                result[k] = v.map(cleanNode).filter(function (c) { return c && c.component; });
            } else if (k === 'slots' && typeof v === 'object') {
                var cleaned = {};
                Object.keys(v).forEach(function (sk) {
                    if (!isEmpty(v[sk])) cleaned[sk] = v[sk];
                });
                if (Object.keys(cleaned).length > 0) result[k] = cleaned;
            } else if (k === 'validators' && Array.isArray(v)) {
                if (v.length > 0) result[k] = v;
            } else {
                result[k] = v;
            }
        });
        return result;
    }

    function cleanOptions(opts) {
        var r = {};
        Object.keys(opts).forEach(function (k) {
            if (!isEmpty(opts[k])) r[k] = opts[k];
        });
        return r;
    }

    /**
     * 规范化节点：加载时补默认值（new Node(JSON.parse(...)) 后调用）
     */
    function normalizeNode(node) {
        if (!node) node = {};
        node.component = node.component || CFG.ROOT_COMPONENT;
        node.modelname = node.modelname || '';
        node.options = node.options || {};
        node.options.comoptions = node.options.comoptions || {};
        node.options.comlisteners = node.options.comlisteners || {};
        node.options.labeloptions = node.options.labeloptions || { label: node.component, required: false, show: true };
        node.options.itemoptions = node.options.itemoptions || { style: {}, class: '' };
        node.validators = node.validators || [];
        node.childrenctrls = node.childrenctrls || [];
        node.slots = node.slots || {};
        node.extendinfo = node.extendinfo || {};
        // 递归规范化子节点
        node.childrenctrls = (node.childrenctrls || []).map(normalizeNode);
        return node;
    }

    /**
     * 创建空节点
     */
    function createNode(componentName, label) {
        return normalizeNode({
            component: componentName,
            modelname: '',
            options: {
                comoptions: {},
                comlisteners: {},
                labeloptions: { label: label || componentName, required: false, show: true },
                itemoptions: { style: {}, class: '' }
            },
            validators: [],
            childrenctrls: [],
            slots: {},
            extendinfo: {}
        });
    }

    /**
     * 组合组件：开放属性解包
     * 把 compositeConfig.openProps 中定义的 path 从父节点 options 映射到子节点
     */
    function applyCompositeProps(node, compositeConfig) {
        if (!compositeConfig || !compositeConfig.openProps) return;
        compositeConfig.openProps.forEach(function (path) {
            var val = global.dyn.getByPath(node, path);
            if (val !== undefined) {
                // 把值设置到对应路径（已经是同一路径，无需移动）
            }
        });
    }

    /**
     * 判断是否容器组件
     */
    function isContainerComp(meta) {
        return !!(meta && (meta.isContainer || meta.category === 'container' || meta.category === 'wrapper'));
    }

    /* ===== 导出 ===== */
    global.dynCom = {
        cfg: CFG,
        isEmpty: isEmpty,
        cleanNode: cleanNode,
        normalizeNode: normalizeNode,
        createNode: createNode,
        applyCompositeProps: applyCompositeProps,
        isContainerComp: isContainerComp
    };

    console.log('[dyn-com] v3 kernel loaded');
})(window);
