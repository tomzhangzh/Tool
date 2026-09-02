/* ============================================================================
 * designer.utils.js —— 低代码设计器公共工具模块（从 designer.js 拆分）
 * 纯工具函数 + 拖拽共享状态，挂载到 window.LCDesignerUtils
 * 供 palette / canvas / property 等模块复用；不足可在 dyn-lib.js 继续扩展。
 * ============================================================================ */
(function (global) {
    'use strict';

    // ===== 拖拽共享状态（palette 拖出 / 画布接收 共用） =====
    const dragState = {
        currentMenuCom: null,        // 当前从左侧菜单拖拽的组件元数据
        draggingFromPalette: false,  // 是否正在从左侧组件库拖拽
        activeGhost: null,           // 拖拽 ghost 元素（组件 HTML 外观）
        sessionId: 0,              // 拖拽会话计数（从左侧拖入时自增）
        dropTargetInfo: null         // { el, insertIndex } 插入占位信息
    };

    // ===== 深拷贝 =====
    function deepClone(obj) {
        if (window._) return window._.cloneDeep(obj);
        return JSON.parse(JSON.stringify(obj));
    }

    // ===== 按点号路径取值 =====
    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce(function (o, k) { return (o == null) ? undefined : o[k]; }, obj);
    }

    // ===== 按点号路径设置值（lodash 不可用时） =====
    function setPathVal(obj, path, value) {
        const keys = path.split('.');
        const last = keys.pop();
        let target = obj;
        for (const k of keys) {
            if (target[k] == null || typeof target[k] !== 'object') target[k] = {};
            target = target[k];
        }
        target[last] = value;
    }

    // ===== 容器组件判断 =====
    const CONTAINER_COMPONENTS = ['DynNForm', 'DynNCellGroup', 'DynNDivContainer', 'DynNGrid', 'DynElDivContainer', 'DynElCard', 'DynElRow', 'DynElCol', 'DynElTabs'];
    function isContainerComp(name) {
        return CONTAINER_COMPONENTS.includes(name);
    }

    // ===== 应用组合组件的外部属性 + 开放容器到内部树 =====
    // externalProps: jsonconfig.options.comoptions（开放属性值）
    // externalSlots: jsonconfig.slots（开放容器内容，拖入的组件数组）
    function applyCompositeProps(innerTree, compositeConfig, externalProps, externalSlots) {
        if (!compositeConfig) return innerTree;
        const tree = deepClone(innerTree);
        // 0) 组合内部节点默认锁定（禁止拖入/编辑），开放容器在下方单独解锁
        (function markLocked(n) {
            if (!n || typeof n !== 'object') return;
            n.__locked = true;
            (n.childrenctrls || []).forEach(markLocked);
        })(tree);
        // 1) 注入开放属性（支持 target 单路径 / targets 数组路径）
        if (compositeConfig.exposedProps && externalProps) {
            for (const prop of compositeConfig.exposedProps) {
                if (externalProps[prop.key] === undefined) continue;
                const val = externalProps[prop.key];
                if (prop.targets && Array.isArray(prop.targets)) {
                    for (const t of prop.targets) {
                        if (window._) window._.set(tree, t, val);
                        else setPathVal(tree, t, val);
                    }
                } else if (prop.target) {
                    if (window._) window._.set(tree, prop.target, val);
                    else setPathVal(tree, prop.target, val);
                }
            }
        }
        // 2) 注入开放容器（插槽）：内部固定内容 + 外部拖入内容合并渲染
        if (compositeConfig.openContainers && externalSlots) {
            for (const oc of compositeConfig.openContainers) {
                const node = oc.target ? getByPath(tree, oc.target) : tree;   // 空 target 表示组合根自身开放
                if (node && typeof node === 'object') {
                    if (!externalSlots[oc.key]) externalSlots[oc.key] = [];
                    const internalChildren = node.childrenctrls || [];
                    node.childrenctrls = internalChildren.concat(externalSlots[oc.key]); // 内部固定内容在前，外部拖入追加在后
                    node.__openSlot = { key: oc.key, label: oc.label || oc.key, hint: oc.hint || '' };
                    node.__unlocked = true;                       // 开放容器不锁定
                    delete node.__locked;   // 开放容器解锁（容器本身可拖入，内部固定内容仍锁定）
                    node.__fixedLen = internalChildren.length;    // 内部固定内容数量（拖拽同步 slots 用）
                    node.__slotRef = externalSlots[oc.key];       // 外部拖入内容引用（拖拽持久化目标）
                }
            }
        }
        return tree;
    }

    // ===== 左侧 palette 拖拽 ghost（组件 HTML 外观）+ 插入占位符 =====
    const PALETTE_GHOST_SHAPES = {
        'DynNInput': '<div class="gh-field"><span class="gh-ph">请输入</span></div>',
        'DynNTextarea': '<div class="gh-field gh-textarea">请输入...</div>',
        'DynNButton': '<div class="gh-btn">按钮</div>',
        'DynNRadio': '<div class="gh-radio"><i class="gh-dot"></i><span>选项1</span></div>',
        'DynNCheckbox': '<div class="gh-check"><i class="gh-dot gh-square"></i><span>选项</span></div>',
        'DynNSwitch': '<div class="gh-switch is-on"></div>',
        'DynNCellGroup': '<div class="gh-box">容器 · CellGroup</div>',
        'DynNForm': '<div class="gh-box">容器 · Form</div>',
        'DynNDivider': '<div class="gh-divider"></div>',
        'DynNCell': '<div class="gh-cell">单元格</div>',
        'DynNSelect': '<div class="gh-field"><span class="gh-ph">请选择</span><span class="gh-arrow">▾</span></div>',
        'DynNDatePicker': '<div class="gh-field"><span class="gh-ph">选择日期</span><span class="gh-arrow">▾</span></div>',
        'DynNNumberInput': '<div class="gh-field"><span class="gh-ph">数字</span></div>',
        'DynNSlider': '<div class="gh-slider"><i class="gh-thumb"></i></div>',
        'DynNProgress': '<div class="gh-progress"><i></i></div>'
    };
    function ghostShape(name, comp) {
        const icon = (comp && comp.icon) || '📦';
        const label = (comp && (comp.displayName || comp.label)) || name;
        const inner = PALETTE_GHOST_SHAPES[name] || '<span class="gh-icon">' + icon + '</span><span class="gh-label">' + label + '</span>';
        return '<div class="gh-card">' + inner + '</div>';
    }
    function createPaletteGhost(comp) {
        const ghost = document.createElement('div');
        ghost.className = 'lc-palette-ghost';
        ghost.innerHTML = ghostShape(comp.componentName || comp.ComponentName || '', comp);
        ghost.style.cssText = 'position:fixed;top:-2000px;left:-2000px;pointer-events:none;z-index:9999;opacity:0.95;';
        document.body.appendChild(ghost);
        dragState.activeGhost = ghost;
        return ghost;
    }
    function removePaletteGhost() {
        if (dragState.activeGhost) { dragState.activeGhost.remove(); dragState.activeGhost = null; }
    }
    // 根据鼠标在容器内的位置计算插入索引（基于容器直接子节点 .lc-node 的顺序）
    function computeInsertIndex(containerEl, evt) {
        const nodes = Array.from(containerEl.querySelectorAll(':scope > .lc-node'));
        let idx = nodes.length;
        for (let i = 0; i < nodes.length; i++) {
            const r = nodes[i].getBoundingClientRect();
            if (evt.clientY < r.top + r.height / 2) { idx = i; break; }
        }
        return idx;
    }
    function updateDropPlaceholder(containerEl, evt) {
        // dragover 冒泡：只让最内层容器接管占位符（否则外层容器会把占位符/索引覆盖成自己的）
        if (evt.target && evt.target.closest) {
            const inner = evt.target.closest('.lc-container, .lc-open-slot');
            if (inner && inner !== containerEl && containerEl.contains(inner)) return;
        }
        const idx = computeInsertIndex(containerEl, evt);
        const nodes = Array.from(containerEl.querySelectorAll(':scope > .lc-node'));
        let ph = document.querySelector('.lc-drop-placeholder');
        if (!ph) {
            ph = document.createElement('div');
            ph.className = 'lc-drop-placeholder';
            document.body.appendChild(ph);
        }
        let rect;
        if (nodes.length > 0 && idx >= nodes.length) {
            const r = nodes[nodes.length - 1].getBoundingClientRect();
            rect = { left: r.left, top: r.bottom - 3, width: r.width };
        } else if (nodes.length > 0) {
            const r = nodes[idx].getBoundingClientRect();
            rect = { left: r.left, top: r.top - 3, width: r.width };
        } else {
            const r = containerEl.getBoundingClientRect();
            rect = { left: r.left, top: r.top, width: r.width };
        }
        ph.style.cssText = 'position:fixed;left:' + rect.left + 'px;top:' + rect.top + 'px;width:' + rect.width + 'px;height:6px;background:#409eff;border-radius:3px;z-index:9998;pointer-events:none;box-shadow:0 0 8px rgba(64,158,255,.7);';
        dragState.dropTargetInfo = { el: containerEl, insertIndex: idx };
    }
    function clearDropPlaceholder() {
        const ph = document.querySelector('.lc-drop-placeholder');
        if (ph) ph.remove();
        dragState.dropTargetInfo = null;
        document.querySelectorAll('.lc-drop-target').forEach(e => e.classList.remove('lc-drop-target'));
    }

    // ===== 对外导出 =====
    global.LCDesignerUtils = {
        dragState,
        deepClone, getByPath, setPathVal, isContainerComp, applyCompositeProps,
        ghostShape, createPaletteGhost, removePaletteGhost,
        computeInsertIndex, updateDropPlaceholder, clearDropPlaceholder,
        CONTAINER_COMPONENTS
    };

})(window);
