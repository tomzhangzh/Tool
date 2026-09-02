/**
 * preview.js - 手机预览页面逻辑
 * 支持设计模式拖拽：拖到指定容器、容器内排序、从左侧添加新组件
 */
(function () {
    'use strict';

    // 暴露 Vue 全局 API
    const _vueGlobals = ['computed', 'reactive', 'ref', 'watch', 'onMounted', 'onUnmounted', 'nextTick', 'defineAsyncComponent', 'markRaw', 'h', 'provide', 'inject'];
    _vueGlobals.forEach(name => { if (Vue[name] && !window[name]) window[name] = Vue[name]; });

    const { createApp, reactive } = Vue;

    let appInstance = null;
    let appReady = false;

    const state = reactive({
        config: { component: 'DynNDivContainer', childrenctrls: [] },
        model: {}
    });

    // 设计模式状态
    const designState = reactive({
        mode: 'design',
        selectedPath: 'root',
        // 拖拽状态
        isDragging: false,
        dragType: null, // 'move' | 'add'
        sourcePath: null,
        addConfig: null, // 从左侧拖入的新组件配置
        // 放置目标
        dropTargetPath: null,
        dropIndex: -1, // -1 表示追加到容器末尾
        dropPosition: null // 'before' | 'after' | 'inside'
    });

    const CONTAINER_COMPONENTS = ['DynNForm', 'DynNCellGroup', 'DynNDivContainer', 'DynNGrid', 'DynElDivContainer', 'DynElCard', 'DynElRow', 'DynElCol', 'DynElTabs'];

    function isContainer(compName) {
        return CONTAINER_COMPONENTS.includes(compName);
    }

    // 组合组件配置 map
    const compositeComponents = {};

        // 按路径取值
    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce(function (o, k) { return (o == null) ? undefined : o[k]; }, obj);
    }

    // 应用组合组件的外部属性 + 开放容器到内部树
    function applyCompositeProps(innerTree, compositeConfig, externalProps, externalSlots) {
        if (!compositeConfig) return innerTree;
        const tree = JSON.parse(JSON.stringify(innerTree));
        // 组合内部节点默认锁定（预览不拖入，仅保持与设计器一致的锁定标记）
        (function markLocked(n) {
            if (!n || typeof n !== 'object') return;
            n.__locked = true;
            (n.childrenctrls || []).forEach(markLocked);
        })(tree);
        if (compositeConfig.exposedProps && externalProps) {
            for (const prop of compositeConfig.exposedProps) {
                if (externalProps[prop.key] === undefined) continue;
                const val = externalProps[prop.key];
                if (prop.targets && Array.isArray(prop.targets)) {
                    for (const t of prop.targets) {
                        if (window._) window._.set(tree, t, val);
                    }
                } else if (prop.target && window._) {
                    window._.set(tree, prop.target, val);
                }
            }
        }
        // 开放容器：内部固定内容 + 外部拖入内容合并渲染（空 target 表示组合根自身开放）
        if (compositeConfig.openContainers && externalSlots) {
            for (const oc of compositeConfig.openContainers) {
                const node = oc.target ? getByPath(tree, oc.target) : tree;
                if (node && typeof node === 'object') {
                    if (!externalSlots[oc.key]) externalSlots[oc.key] = [];
                    const internalChildren = node.childrenctrls || [];
                    node.childrenctrls = internalChildren.concat(externalSlots[oc.key]);
                    node.__openSlot = { key: oc.key, label: oc.label || oc.key, hint: oc.hint || '' };
                    node.__unlocked = true;
                    delete node.__locked;
                    node.__fixedLen = internalChildren.length;
                    node.__slotRef = externalSlots[oc.key];
                }
            }
        }
        return tree;
    }

function getUrlParam(name) {
        const match = window.location.search.match(new RegExp('[?&]' + name + '=([^&]+)'));
        return match ? decodeURIComponent(match[1]) : null;
    }

    const pageCode = getUrlParam('code');
    const isStandalone = !!pageCode;

    // ========== 组件注册 ==========
    function registerNutUI(app) {
        if (window.nutui) {
            const nutuiObj = window.nutui.default || window.nutui;
            app.use(nutuiObj);
            for (const key in nutuiObj) {
                const comp = nutuiObj[key];
                if (comp && (typeof comp === 'object' || typeof comp === 'function') && comp.name && comp.render) {
                    const kebab = comp.name.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
                    if (!app._context.components[kebab]) app.component(kebab, comp);
                }
            }
        }
    }

    function registerDynamicCom(app) {
        app.component('NDynamicCom', {
            name: 'NDynamicCom',
            props: {
                jsonconfig: { type: Object, required: true },
                parentmodelinfo: { type: Object, default: () => ({}) },
                nodePath: { type: String, default: 'root' },
                locked: { type: Boolean, default: false }
            },
            inject: {
                lcLocked: { default: null },
                lcCompositeRoot: { default: null }
            },
            provide() {
                return {
                    lcLocked: computed(() => this.isLocked),
                    lcCompositeRoot: this.isComposite ? this.jsonconfig : null
                };
            },
            template: `
                <div v-if="!validConfig" style="padding:8px;color:#f56c6c;font-size:12px;">
                    [NDynamicCom] 无效配置: {{ nodePath }}
                </div>
                <div v-else-if="depthExceeded" style="padding:8px;color:#f56c6c;font-size:12px;">
                    [NDynamicCom] 递归深度超限: {{ nodePath }}
                </div>
                <template v-else>
                <div v-if="isDesign" class="lc-node"
                     :class="{
                        'lc-selected': isSelected,
                        'lc-dragging': isDraggingSelf,
                        'lc-drop-target': isDropTarget,
                        'lc-container': isContainerComp,
                        'lc-composite': isComposite,
                        'lc-wrapper': hasWrapper,
                        'lc-locked': isLocked,
                        'lc-open-slot': isOpenSlot
                     }"
                     :draggable="true"
                     @@dragstart.stop="onDragStart"
                     @@dragover.stop.prevent="onDragOver"
                     @@dragleave="onDragLeave"
                     @@drop.stop.prevent="onDrop"
                     @@click.stop="onClick">
                    <!-- 有 Wrapper -->
                    <component v-if="hasWrapper" :is="wrapperComponent"
                               :jsonconfig="jsonconfig.options.wrapperoptions"
                               :parentmodelinfo="parentmodelinfo"
                               :node-path="nodePath + '.wrapper'">
                        <n-dynamic-com v-if="isComposite && compositeTree"
                                       :jsonconfig="compositeTree"
                                       :parentmodelinfo="parentmodelinfo"
                                       :node-path="nodePath + '.composite'"
                                   :locked="true"></n-dynamic-com>
                        <component v-else :is="jsonconfig.component"
                                   :jsonconfig="jsonconfig"
                                   :parentmodelinfo="parentmodelinfo"
                                   :node-path="nodePath"></component>
                    </component>
                    <!-- 无 Wrapper -->
                    <template v-else>
                        <n-dynamic-com v-if="isComposite && compositeTree"
                                       :jsonconfig="compositeTree"
                                       :parentmodelinfo="parentmodelinfo"
                                       :node-path="nodePath + '.composite'"
                                   :locked="true"></n-dynamic-com>
                        <component v-else :is="jsonconfig.component"
                                   :jsonconfig="jsonconfig"
                                   :parentmodelinfo="parentmodelinfo"
                                   :node-path="nodePath"></component>
                    </template>
                    <!-- 插入位置指示器 -->
                    <div v-if="showInsertBefore" class="lc-insert-indicator lc-insert-before"></div>
                    <div v-if="showInsertAfter" class="lc-insert-indicator lc-insert-after"></div>
                </div>
                <!-- 非设计模式 -->
                <component v-if="!isDesign && hasWrapper" :is="wrapperComponent"
                           :jsonconfig="jsonconfig.options.wrapperoptions"
                           :parentmodelinfo="parentmodelinfo"
                           :node-path="nodePath + '.wrapper'">
                    <n-dynamic-com v-if="isComposite && compositeTree"
                                   :jsonconfig="compositeTree"
                                   :parentmodelinfo="parentmodelinfo"
                                   :node-path="nodePath + '.composite'"
                                   :locked="true"></n-dynamic-com>
                    <component v-else :is="jsonconfig.component"
                               :jsonconfig="jsonconfig"
                               :parentmodelinfo="parentmodelinfo"
                               :node-path="nodePath"></component>
                </component>
                <n-dynamic-com v-else-if="!isDesign && isComposite && compositeTree"
                               :jsonconfig="compositeTree"
                               :parentmodelinfo="parentmodelinfo"
                               :node-path="nodePath + '.composite'"
                                   :locked="true"></n-dynamic-com>
                <component v-else-if="!isDesign" :is="jsonconfig.component"
                           :jsonconfig="jsonconfig"
                           :parentmodelinfo="parentmodelinfo"
                           :node-path="nodePath"></component>
                </template>
            `,
            computed: {
                validConfig() { return this.jsonconfig && typeof this.jsonconfig === 'object' && this.jsonconfig.component; },
                depth() {
                    const m = this.nodePath.match(/\.(childrenctrls\[|composite|wrapper)/g);
                    return m ? m.length : 0;
                },
                depthExceeded() { return this.depth > 15; },
                isDesign() { return designState.mode === 'design'; },
                isSelected() { return designState.selectedPath === this.nodePath; },
                isContainerComp() { return isContainer(this.jsonconfig.component); },
                isComposite() { return !!compositeComponents[this.jsonconfig.component]; },
                hasWrapper() { return !!(this.jsonconfig.options?.wrapperoptions?.component); },
                wrapperComponent() { return this.jsonconfig.options?.wrapperoptions?.component; },
                compositeTree() {
                    if (!this.isComposite) return null;
                    const config = compositeComponents[this.jsonconfig.component];
                    if (!config?.tree) return null;
                    const externalProps = this.jsonconfig.options?.comoptions || {};
                    const externalSlots = this.jsonconfig.slots || (this.jsonconfig.slots = {});
                    return applyCompositeProps(config.tree, config, externalProps, externalSlots);
                },
                parentLocked() {
                    const pl = this.lcLocked;
                    if (pl == null) return false;
                    return (typeof pl === 'object' && 'value' in pl) ? !!pl.value : !!pl;
                },
                isLocked() { return (this.locked || this.parentLocked) && !this.jsonconfig?.__unlocked; },
                isOpenSlot() { return !!(this.jsonconfig?.__openSlot); },
                isDraggingSelf() {
                    return designState.isDragging && designState.dragType === 'move' && designState.sourcePath === this.nodePath;
                },
                isDropTarget() {
                    return designState.isDragging && designState.dropTargetPath === this.nodePath && designState.dropPosition === 'inside';
                },
                showInsertBefore() {
                    return designState.isDragging && designState.dropTargetPath === this.nodePath && designState.dropPosition === 'before';
                },
                showInsertAfter() {
                    return designState.isDragging && designState.dropTargetPath === this.nodePath && designState.dropPosition === 'after';
                }
            },
            methods: {
                onClick() {
                    if (this.isLocked) return;
                    if (this.isOpenSlot && this.lcCompositeRoot) {
                        designState.selectedPath = this.lcCompositeRoot.__path || this.nodePath;
                        try { parent.postMessage({ type: 'component-selected', path: this.nodePath }, '*'); } catch (e) {}
                        return;
                    }
                    designState.selectedPath = this.nodePath;
                    try { parent.postMessage({ type: 'component-selected', path: this.nodePath }, '*'); } catch (e) {}
                },
                onDragStart(e) {
                    if (designState.mode !== 'design') return;
                    designState.isDragging = true;
                    designState.dragType = 'move';
                    designState.sourcePath = this.nodePath;
                    e.dataTransfer.effectAllowed = 'move';
                    e.dataTransfer.setData('text/plain', this.nodePath);
                },
                onDragOver(e) {
                    if (!designState.isDragging) return;
                    e.preventDefault();
                    e.stopPropagation();

                    const rect = this.$el.getBoundingClientRect();
                    const y = e.clientY - rect.top;
                    const h = rect.height;

                    if (this.isContainerComp) {
                        // 容器：判断是放在容器内还是在容器前后
                        if (y < h * 0.25) {
                            designState.dropTargetPath = this.nodePath;
                            designState.dropPosition = 'before';
                        } else if (y > h * 0.75) {
                            designState.dropTargetPath = this.nodePath;
                            designState.dropPosition = 'after';
                        } else {
                            designState.dropTargetPath = this.nodePath;
                            designState.dropPosition = 'inside';
                        }
                        e.dataTransfer.dropEffect = 'move';
                    } else {
                        // 非容器：只能在前后插入
                        if (y < h / 2) {
                            designState.dropTargetPath = this.nodePath;
                            designState.dropPosition = 'before';
                        } else {
                            designState.dropTargetPath = this.nodePath;
                            designState.dropPosition = 'after';
                        }
                        e.dataTransfer.dropEffect = 'move';
                    }
                },
                onDragLeave(e) {
                    // 不清除，由 dragover 持续更新
                },
                onDrop(e) {
                    if (!designState.isDragging) return;
                    e.preventDefault();
                    e.stopPropagation();

                    const targetPath = designState.dropTargetPath;
                    const position = designState.dropPosition;

                    if (designState.dragType === 'move') {
                        // 移动组件
                        try {
                            parent.postMessage({
                                type: 'component-move',
                                sourcePath: designState.sourcePath,
                                targetPath: targetPath,
                                position: position
                            }, '*');
                        } catch (err) {}
                    } else if (designState.dragType === 'add' && designState.addConfig) {
                        // 添加新组件
                        try {
                            parent.postMessage({
                                type: 'component-add',
                                config: designState.addConfig,
                                targetPath: targetPath,
                                position: position
                            }, '*');
                        } catch (err) {}
                    }

                    // 重置拖拽状态
                    designState.isDragging = false;
                    designState.dragType = null;
                    designState.sourcePath = null;
                    designState.addConfig = null;
                    designState.dropTargetPath = null;
                    designState.dropPosition = null;
                }
            }
        });
    }

    // 全局 dragend 监听，确保拖拽状态重置
    window.addEventListener('dragend', () => {
        designState.isDragging = false;
        designState.dragType = null;
        designState.sourcePath = null;
        designState.addConfig = null;
        designState.dropTargetPath = null;
        designState.dropPosition = null;
    });

    // document capture 阶段监听 dragover：确保从外部拖入时 isDragging 先被设置
    document.addEventListener('dragover', (e) => {
        if (designState.mode === 'design' && !designState.isDragging) {
            designState.isDragging = true;
            designState.dragType = 'add';
        }
    }, true);

    // ========== 组件元数据加载 ==========
    let componentsLoaded = false;
    async function loadAndRegisterComponents(app) {
        if (componentsLoaded) return;
        try {
            const resp = await fetch('/api/lowcode/components');
            const result = await resp.json();
            if (result.success && result.data) {
                let registered = 0;
                for (const meta of result.data) {
                    const name = meta.componentName || meta.ComponentName;
                    const url = meta.loadUrl || meta.LoadUrl;
                    // 保存组合组件配置
                    if (meta.isComposite && meta.compositeConfigJson) {
                        try {
                            compositeComponents[name] = JSON.parse(meta.compositeConfigJson);
                        } catch (e) {
                            console.error('[Preview] 解析组合组件配置失败:', name, e);
                        }
                    }
                    // 注册自定义脚本
                    if (meta.customScriptJson && window.nutRegisterCustomScript) {
                        try {
                            window.nutRegisterCustomScript(name, meta.customScriptJson);
                        } catch (e) {
                            console.error('[Preview] 注册自定义脚本失败:', name, e);
                        }
                    }
                    if (name && url) {
                        // 组件名已在数据库中统一为 Dyn 前缀（DynElInput / DynNInput），避免与 UI 库全局组件冲突
                        app.component(name, window.nutLoadCom(name, url));
                        registered++;
                    }
                }
                console.log(`[Preview] 注册了 ${registered} 个组件, 其中组合组件: ${Object.keys(compositeComponents).length}`);
                componentsLoaded = true;
            }
        } catch (err) {
            console.error('[Preview] 加载组件元数据失败:', err);
        }
    }

    // ========== 初始化 app ==========
    async function initApp() {
        const container = document.getElementById('preview-app');
        if (!container) return;

        const app = createApp({
            data() { return { state, designState, showModelModal: false }; },
            computed: {
                modelJsonText() {
                    return JSON.stringify(state.model, null, 2);
                }
            },
            template: `<div class="preview-page" :class="{ 'design-mode': designState.mode === 'design' }"
                          @@dragover.prevent="onRootDragOver"
                          @@drop.prevent="onRootDrop">
                <n-dynamic-com :jsonconfig="state.config" :parentmodelinfo="state.model" node-path="root"></n-dynamic-com>
                <div v-if="isStandalone" class="standalone-actions">
                    <button class="btn-primary" @@click="handleSubmit">提交</button>
                    <button class="btn-default" @@click="handleReset">重置</button>
                    <button class="btn-default" @@click="showModelData">查看数据</button>
                </div>
                <!-- Model JSON 弹窗 -->
                <div v-if="showModelModal" class="model-modal-overlay" @@click.self="showModelModal = false">
                    <div class="model-modal">
                        <div class="model-modal-header">
                            <span>页面数据模型 (Model JSON)</span>
                            <button class="model-modal-close" @@click="showModelModal = false">×</button>
                        </div>
                        <div class="model-modal-body">
                            <textarea class="model-json-textarea" readonly>{{ modelJsonText }}</textarea>
                        </div>
                        <div class="model-modal-footer">
                            <button class="btn-primary" @@click="copyModelJson">复制</button>
                            <button class="btn-default" @@click="showModelModal = false">关闭</button>
                        </div>
                    </div>
                </div>
            </div>`,
            methods: {
                validate() { return window.nutValidate(state.config, state.model); },
                handleSubmit() {
                    const result = this.validate();
                    if (result.valid) {
                        alert('验证通过！\n数据:\n' + JSON.stringify(state.model, null, 2));
                    } else {
                        alert('验证失败：\n' + Object.values(result.errors).flat().join('\n'));
                    }
                },
                handleReset() { Object.keys(state.model).forEach(k => delete state.model[k]); },
                showModelData() { this.showModelModal = true; },
                copyModelJson() {
                    navigator.clipboard.writeText(this.modelJsonText).then(() => {
                        alert('已复制到剪贴板');
                    }).catch(() => {
                        alert('复制失败，请手动选择复制');
                    });
                },
                onRootDragOver(e) {
                    if (designState.mode !== 'design') return;
                    e.preventDefault();
                    // 如果还没进入拖拽状态（从外部拖入），标记为待添加
                    if (!designState.isDragging) {
                        designState.isDragging = true;
                        designState.dragType = 'add';
                    }
                    // 默认放置目标为根容器内部
                    designState.dropTargetPath = 'root';
                    designState.dropPosition = 'inside';
                },
                onRootDrop(e) {
                    if (designState.mode !== 'design') return;
                    e.preventDefault();
                    // 如果有具体的 dropTargetPath，由 lc-node 的 drop 处理
                    // 这里只处理兜底：添加到根容器
                    if (designState.isDragging && designState.dragType === 'add' && designState.addConfig) {
                        try {
                            parent.postMessage({
                                type: 'component-add',
                                config: designState.addConfig,
                                targetPath: 'root',
                                position: 'inside'
                            }, '*');
                        } catch (err) {}
                    }
                    // 重置拖拽状态
                    designState.isDragging = false;
                    designState.dragType = null;
                    designState.sourcePath = null;
                    designState.addConfig = null;
                    designState.dropTargetPath = null;
                    designState.dropPosition = null;
                }
            }
        });

        app.config.globalProperties.isStandalone = isStandalone;
        registerNutUI(app);
        registerDynamicCom(app);
        await loadAndRegisterComponents(app);

        appInstance = app.mount(container);
        appReady = true;
        console.log('[Preview] app 初始化完成');

        if (!isStandalone) {
            try { parent.postMessage({ type: 'preview-ready' }, '*'); } catch (e) {}
        }
    }

    // ========== 更新配置 ==========
    function renderConfig(config, model) {
        Object.keys(state.config).forEach(k => delete state.config[k]);
        if (config) Object.assign(state.config, config);
        Object.keys(state.model).forEach(k => delete state.model[k]);
        if (model) Object.assign(state.model, model);
    }

    // ========== 独立模式 ==========
    async function loadPageByCode(code) {
        try {
            const resp = await fetch(`/api/lowcode/page/${code}`);
            const result = await resp.json();
            if (result.success && result.data) {
                renderConfig(JSON.parse(result.data.configJson || '{}'), JSON.parse(result.data.defaultModelJson || '{}'));
                document.title = result.data.pageName || '页面预览';
            }
        } catch (err) {
            console.error('加载页面失败:', err);
        }
    }

    // ========== 消息监听 ==========
    window.addEventListener('message', (event) => {
        const data = event.data;
        if (!data || !data.type) return;

        switch (data.type) {
            case 'designer-update':
                if (data.config) {
                    if (data.designMode) designState.mode = data.designMode;
                    if (data.selectedPath) designState.selectedPath = data.selectedPath;
                    if (appReady) {
                        renderConfig(data.config, data.model || {});
                    } else {
                        const wait = setInterval(() => {
                            if (appReady) { clearInterval(wait); renderConfig(data.config, data.model || {}); }
                        }, 50);
                    }
                }
                break;
            case 'designer-mode-change':
                designState.mode = data.designMode || 'design';
                break;
            case 'start-add-component':
                // 从左侧组件库拖入新组件
                designState.isDragging = true;
                designState.dragType = 'add';
                designState.addConfig = data.config;
                designState.sourcePath = null;
                break;
            case 'designer-validate':
                if (appInstance) {
                    const result = appInstance.validate();
                    try { parent.postMessage({ type: 'validate-result', data: result }, '*'); } catch (e) {}
                }
                break;
        }
    });

    // ========== 启动 ==========
    initApp();
    if (isStandalone) {
        designState.mode = 'preview';  // 独立预览页使用非设计模式
        loadPageByCode(pageCode);
    } else { try { parent.postMessage({ type: 'preview-loaded' }, '*'); } catch (e) {} }

})();
