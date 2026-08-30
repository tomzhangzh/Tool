/**
 * designer.js - 低代码设计器核心逻辑
 * 参考 TUI.Web.Entry 架构：useDraggable + el-tree + provide/inject
 */
(function () {
    'use strict';

    const { createApp, reactive, ref, computed, watch, nextTick, onMounted, provide } = Vue;
    const { ElMessage, ElMessageBox } = ElementPlus;

    const CONTAINER_COMPONENTS = ['DynNForm', 'DynNCellGroup', 'DynNDivContainer', 'DynNGrid', 'DynElDivContainer', 'DynElCard', 'DynElRow', 'DynElCol', 'DynElTabs'];
    const isContainerComp = (name) => CONTAINER_COMPONENTS.includes(name);
    const DRAG_GROUP = 'lc-designer-group';

    // 组合组件配置 map（运行时渲染用）
    const compositeComponents = {};

    // 应用组合组件的外部属性到内部树
    function applyCompositeProps(innerTree, compositeConfig, externalProps) {
        if (!compositeConfig?.exposedProps || !externalProps) return innerTree;
        const tree = JSON.parse(JSON.stringify(innerTree));
        for (const prop of compositeConfig.exposedProps) {
            if (externalProps[prop.key] !== undefined && prop.target) {
                // 用 lodash 设置值（如果可用），否则手动解析
                if (window._) {
                    window._.set(tree, prop.target, externalProps[prop.key]);
                }
            }
        }
        return tree;
    }

    // 当前从左侧菜单拖拽的组件元数据
    let currentMenuCom = null;

    const app = createApp({
        setup() {
            // ===== 状态 =====
            const componentMetaList = ref([]);
            const pageList = ref([]);
            const currentPageCode = ref('');
            const currentPageId = ref(null);
            const saving = ref(false);
            const showJson = ref(false);
            const showNewPage = ref(false);
            const showModelModal = ref(false);
            const activeCategory = ref('表单');
            const activeUiLibrary = ref('all');
            const leftTab = ref('palette');
            const configJsonText = ref('');
            const designMode = ref('design');
            const treeFilter = ref('');

            // ===== 画布视图控制 =====
            const canvasPlatform = ref('mobile'); // mobile | desktop
            const canvasZoom = ref(1);
            const showRuler = ref(true);
            const canvasWidth = ref(1200);
            const canvasHeight = ref(800);
            const zoomIn = () => { canvasZoom.value = Math.min(2, +(canvasZoom.value + 0.1).toFixed(2)); };
            const zoomOut = () => { canvasZoom.value = Math.max(0.25, +(canvasZoom.value - 0.1).toFixed(2)); };
            const zoomReset = () => { canvasZoom.value = 1; };
            const zoomPercent = computed(() => Math.round(canvasZoom.value * 100) + '%');
            // 生成标尺刻度
            const generateRulerMarks = (maxPx, zoom) => {
                const marks = [];
                const step = zoom >= 1 ? 50 : (zoom >= 0.5 ? 100 : 200);
                for (let i = 0; i <= maxPx; i += step) {
                    marks.push({ pos: i * zoom, label: i % (step * 2) === 0 ? i + '' : '' });
                }
                return marks;
            };
            const rulerHMarks = computed(() => generateRulerMarks(1600, canvasZoom.value));
            const rulerVMarks = computed(() => generateRulerMarks(1200, canvasZoom.value));

            // ===== daybrush/ruler 标尺 =====
            const rulerHRef = ref(null);
            const rulerVRef = ref(null);
            let rulerH = null, rulerV = null;
            const initRulers = () => {
                if (!window.Ruler) return;
                if (rulerHRef.value && !rulerH) {
                    rulerH = new Ruler(rulerHRef.value, { type: 'horizontal', zoom: canvasZoom.value, unit: 50, backgroundColor: '#fafafa', lineColor: '#c0c4cc', textColor: '#909399' });
                }
                if (rulerVRef.value && !rulerV) {
                    rulerV = new Ruler(rulerVRef.value, { type: 'vertical', zoom: canvasZoom.value, unit: 50, backgroundColor: '#fafafa', lineColor: '#c0c4cc', textColor: '#909399' });
                }
            };
            const resizeRulers = () => { rulerH?.resize(); rulerV?.resize(); };
            const setRulerZoom = (z) => { if (rulerH) rulerH.zoom = z; if (rulerV) rulerV.zoom = z; resizeRulers(); };

            // 选中状态
            const currentCom = ref(null);
            const currentContainer = ref(null);
            const currentPath = ref('');
            const breadcrumbList = ref([]);

            const newPageForm = reactive({ pageName: '', pageCode: '' });

            // 页面配置根节点
            const configObj = reactive({
                component: 'DynNDivContainer', modelname: '',
                options: {
                    comoptions: {}, comlisteners: {}, labeloptions: {},
                    itemoptions: { style: { padding: '12px', background: '#fff' }, class: '' }
                },
                validators: [], childrenctrls: [], slots: {}, extendinfo: {}
            });
            const modelObj = reactive({});

            // 左侧组件库 ref
            const comListRef = ref(null);
            const paletteRef = ref(null);
            const treeRef = ref(null);

            // ===== 分类 =====
            const categories = [
                { key: '表单', label: '表单' },
                { key: '布局', label: '布局' },
                { key: '展示', label: '展示' },
                { key: '通用', label: '通用' }
            ];

            // ===== 计算属性 =====
            const filteredComponents = computed(() => {
                return componentMetaList.value.filter(c => {
                    const catMatch = (c.category || c.Category) === activeCategory.value;
                    const lib = c.uiLibrary || c.UiLibrary || 'nutui';
                    const libMatch = activeUiLibrary.value === 'all' || lib === activeUiLibrary.value;
                    return catMatch && libMatch;
                });
            });

            const hasOptionField = computed(() => {
                if (!currentCom.value) return false;
                return ['DynNRadio', 'DynNCheckbox', 'DynNPicker'].includes(currentCom.value.component);
            });

            // 当前选中组件的属性面板配置
            const currentPropertyConfig = computed(() => {
                if (!currentCom.value) return null;
                const compName = currentCom.value.component;

                // 组合组件：根据开放属性动态生成属性配置
                if (compositeComponents[compName]) {
                    const config = compositeComponents[compName];
                    if (config.exposedProps && config.exposedProps.length) {
                        const fields = config.exposedProps.map(p => ({
                            key: 'options.comoptions.' + p.key,
                            label: p.label || p.key,
                            type: p.type || 'input',
                            default: p.default,
                            options: p.options
                        }));
                        return { groups: [{ title: '开放属性', fields }] };
                    }
                    return { groups: [] };
                }

                const meta = componentMetaList.value.find(c =>
                    (c.componentName || c.ComponentName) === compName
                );
                if (!meta) {
                    console.log('[Designer] 未找到组件元数据:', compName);
                    return null;
                }
                const propConfig = meta.propertyConfigJson || meta.PropertyConfigJson;
                if (!propConfig) {
                    console.log('[Designer] 组件无属性配置:', compName, 'meta keys:', Object.keys(meta));
                    return null;
                }
                try {
                    const parsed = typeof propConfig === 'string' ? JSON.parse(propConfig) : propConfig;
                    console.log('[Designer] 属性配置加载成功:', compName, 'groups:', parsed.groups?.length);
                    return parsed;
                } catch (e) {
                    console.error('[Designer] 解析属性配置失败:', e, propConfig);
                    return null;
                }
            });

            // 当前页面 Model JSON
            const modelJsonText = computed(() => JSON.stringify(modelObj, null, 2));

            // 打开 Model 数据查看
            function showModelData() {
                showModelModal.value = true;
            }

            // 复制 Model JSON
            function copyModelJson() {
                navigator.clipboard.writeText(modelJsonText.value).then(() => {
                    alert('已复制到剪贴板');
                }).catch(() => {
                    alert('复制失败，请手动选择复制');
                });
            }

            // 属性更新回调
            function onPropertyUpdate(key, value, localModel) {
                if (!currentCom.value) return;
                // 直接修改 currentCom（响应式）
                // localModel 是 DynamicPropertyPanel 内部的模型，已被修改
                // 需要同步回 currentCom
                if (window.ppSetByPath) {
                    window.ppSetByPath(currentCom.value, key, value);
                }
                console.log('[Designer] 属性更新:', key, '=', value);
            }

            // 打开 JSON 编辑器
            function showJsonEditor() {
                if (currentCom.value) {
                    configJsonText.value = JSON.stringify(currentCom.value, null, 2);
                }
                showJson.value = true;
            }

            const canMoveUp = computed(() => {
                if (!currentContainer.value || !currentCom.value) return false;
                return currentContainer.value.childrenctrls.indexOf(currentCom.value) > 0;
            });

            const canMoveDown = computed(() => {
                if (!currentContainer.value || !currentCom.value) return false;
                const idx = currentContainer.value.childrenctrls.indexOf(currentCom.value);
                return idx >= 0 && idx < currentContainer.value.childrenctrls.length - 1;
            });

            // ===== 工具方法 =====
            function getComponentsByCategory(cat) {
                return componentMetaList.value.filter(c => {
                    const catMatch = (c.category || c.Category) === cat;
                    const lib = c.uiLibrary || c.UiLibrary || 'nutui';
                    const libMatch = activeUiLibrary.value === 'all' || lib === activeUiLibrary.value;
                    return catMatch && libMatch;
                });
            }

            function deepClone(obj) {
                return JSON.parse(JSON.stringify(obj));
            }

            // 递归查找所有节点的父映射
            function buildParentMapping(root) {
                const mapping = [];
                function walk(node, parent) {
                    if (!node || !node.childrenctrls) return;
                    node.childrenctrls.forEach(child => {
                        if (!child) return;
                        mapping.push({ child, parent: parent || root, parentObj: node });
                        walk(child, node);
                    });
                }
                walk(root, null);
                return mapping;
            }

            function findParent(node) {
                const mapping = buildParentMapping(configObj);
                const found = mapping.find(m => m.child === node);
                return found ? found.parentObj : null;
            }

            function findNodePath(node, root = configObj, path = 'root') {
                if (node === root) return path;
                if (root.childrenctrls) {
                    for (let i = 0; i < root.childrenctrls.length; i++) {
                        if (root.childrenctrls[i] === node) return `${path}.childrenctrls[${i}]`;
                        const found = findNodePath(node, root.childrenctrls[i], `${path}.childrenctrls[${i}]`);
                        if (found) return found;
                    }
                }
                return '';
            }

            // 获取祖先链（用于面包屑）
            function getAncestors(node) {
                const mapping = buildParentMapping(configObj);
                const ancestors = [];
                let current = node;
                while (current) {
                    ancestors.unshift(current);
                    const found = mapping.find(m => m.child === current);
                    current = found ? found.parent : null;
                }
                return ancestors;
            }

            // ===== 选中组件 =====
            function setCurrentCom(comConfig) {
                if (!comConfig) return;
                currentCom.value = comConfig;
                currentContainer.value = findParent(comConfig);
                currentPath.value = findNodePath(comConfig);
                breadcrumbList.value = getAncestors(comConfig);
            }

            function selectFromTree(data) {
                setCurrentCom(data);
            }

            // ===== 组件操作 =====
            function deleteCurrent() {
                if (!currentCom.value || !currentContainer.value) {
                    ElMessage.warning('请先选中组件');
                    return;
                }
                const idx = currentContainer.value.childrenctrls.indexOf(currentCom.value);
                if (idx >= 0) {
                    currentContainer.value.childrenctrls.splice(idx, 1);
                    currentCom.value = null;
                    currentContainer.value = null;
                    currentPath.value = '';
                    ElMessage.success('已删除');
                }
            }

            function moveUp() {
                if (!canMoveUp.value) return;
                const arr = currentContainer.value.childrenctrls;
                const idx = arr.indexOf(currentCom.value);
                [arr[idx - 1], arr[idx]] = [arr[idx], arr[idx - 1]];
            }

            function moveDown() {
                if (!canMoveDown.value) return;
                const arr = currentContainer.value.childrenctrls;
                const idx = arr.indexOf(currentCom.value);
                [arr[idx + 1], arr[idx]] = [arr[idx], arr[idx + 1]];
            }

            function copyCurrent() {
                if (!currentCom.value || !currentContainer.value) return;
                const idx = currentContainer.value.childrenctrls.indexOf(currentCom.value);
                if (idx >= 0) {
                    const copy = deepClone(currentCom.value);
                    currentContainer.value.childrenctrls.splice(idx + 1, 0, copy);
                    setCurrentCom(copy);
                    ElMessage.success('已复制');
                }
            }

            // ===== 验证器 =====
            function addValidator() {
                if (!currentCom.value) return;
                if (!currentCom.value.validators) currentCom.value.validators = [];
                currentCom.value.validators.push({ type: 'required', message: '必填', value: '' });
            }

            function removeValidator(idx) {
                if (currentCom.value?.validators) currentCom.value.validators.splice(idx, 1);
            }

            function needsValue(type) {
                return ['minLength', 'maxLength', 'min', 'max', 'pattern'].includes(type);
            }

            function toggleRequired(checked) {
                if (!currentCom.value) return;
                if (!currentCom.value.options) currentCom.value.options = {};
                if (!currentCom.value.options.labeloptions) currentCom.value.options.labeloptions = {};
                currentCom.value.options.labeloptions.required = checked;
                if (checked) {
                    if (!currentCom.value.validators) currentCom.value.validators = [];
                    if (!currentCom.value.validators.find(v => v.type === 'required')) {
                        currentCom.value.validators.push({ type: 'required', message: '必填' });
                    }
                } else if (currentCom.value.validators) {
                    currentCom.value.validators = currentCom.value.validators.filter(v => v.type !== 'required');
                }
            }

            // ===== 页面管理 =====
            async function loadComponentMeta() {
                try {
                    const resp = await fetch('/api/lowcode/components');
                    const result = await resp.json();
                    console.log('[Designer] components API result:', result);
                    if (result.success && result.data) {
                        componentMetaList.value = result.data;
                        console.log('[Designer] loaded', result.data.length, 'components');
                        console.log('[Designer] first component:', result.data[0]);
                        console.log('[Designer] categories:', [...new Set(result.data.map(c => c.category || c.Category))]);
                    }
                } catch (e) { ElMessage.error('加载组件元数据失败: ' + e.message); }
            }

            async function loadPageList() {
                try {
                    const resp = await fetch('/api/lowcode/pages');
                    const result = await resp.json();
                    if (result.success) pageList.value = result.data;
                } catch (e) { ElMessage.error('加载页面列表失败: ' + e.message); }
            }

            async function loadPage(code) {
                if (!code) return;
                try {
                    const resp = await fetch(`/api/lowcode/page/${code}`);
                    const result = await resp.json();
                    console.log('[Designer] page API result:', result);
                    if (result.success && result.data) {
                        currentPageId.value = result.data.id;
                        const config = JSON.parse(result.data.configJson || '{}');
                        console.log('[Designer] page config:', config);
                        Object.keys(configObj).forEach(k => delete configObj[k]);
                        Object.assign(configObj, config);
                        const model = JSON.parse(result.data.defaultModelJson || '{}');
                        Object.keys(modelObj).forEach(k => delete modelObj[k]);
                        Object.assign(modelObj, model);
                        currentCom.value = null;
                        currentContainer.value = null;
                        ElMessage.success(`已加载: ${result.data.pageName}`);
                    }
                } catch (e) { ElMessage.error('加载页面失败: ' + e.message); }
            }

            function newPage() {
                newPageForm.pageName = '';
                newPageForm.pageCode = '';
                showNewPage.value = true;
            }

            function confirmNewPage() {
                if (!newPageForm.pageName || !newPageForm.pageCode) {
                    ElMessage.warning('请填写页面名称和编码');
                    return;
                }
                Object.keys(configObj).forEach(k => delete configObj[k]);
                Object.assign(configObj, {
                    component: 'DynNDivContainer', modelname: '',
                    options: { comoptions: {}, comlisteners: {}, labeloptions: {}, itemoptions: { style: { padding: '12px' }, class: '' } },
                    validators: [], childrenctrls: [], slots: {}, extendinfo: {}
                });
                Object.keys(modelObj).forEach(k => delete modelObj[k]);
                currentPageId.value = null;
                currentPageCode.value = newPageForm.pageCode;
                showNewPage.value = false;
                currentCom.value = null;
                ElMessage.success('已创建新页面');
            }

            async function savePage() {
                if (!currentPageCode.value) { ElMessage.warning('请先选择或新建页面'); return; }
                saving.value = true;
                try {
                    const pageData = {
                        id: currentPageId.value || 0,
                        pageName: pageList.value.find(p => p.pageCode === currentPageCode.value)?.pageName || currentPageCode.value,
                        pageCode: currentPageCode.value,
                        configJson: JSON.stringify(configObj),
                        defaultModelJson: JSON.stringify(modelObj),
                        isEnabled: true, sortOrder: 0
                    };
                    const resp = await fetch('/api/lowcode/page', {
                        method: 'POST', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(pageData)
                    });
                    const result = await resp.json();
                    if (result.success) {
                        currentPageId.value = result.data;
                        ElMessage.success('保存成功');
                        loadPageList();
                    } else { ElMessage.error('保存失败: ' + result.message); }
                } catch (e) { ElMessage.error('保存失败: ' + e.message); }
                finally { saving.value = false; }
            }

            function openPreview() {
                window.open(`/designer/preview?code=${currentPageCode.value}`, '_blank');
            }

            // ===== JSON 编辑 =====
            watch(showJson, (val) => { if (val) configJsonText.value = JSON.stringify(configObj, null, 2); });

            function applyJson() {
                try {
                    const config = JSON.parse(configJsonText.value);
                    Object.keys(configObj).forEach(k => delete configObj[k]);
                    Object.assign(configObj, config);
                    ElMessage.success('JSON 已应用');
                    showJson.value = false;
                } catch (e) { ElMessage.error('JSON 格式错误: ' + e.message); }
            }

            // ===== 拖拽回调（供容器组件调用）=====
            function onContainerDragAdd(evt, parentConfig) {
                console.log('[Designer] onContainerDragAdd:', evt.newIndex, currentMenuCom?.componentName);
                if (!currentMenuCom || !parentConfig?.childrenctrls) return;
                const list = parentConfig.childrenctrls;
                const idx = evt.newIndex ?? list.length;
                // 创建正确的配置
                let config = {};
                try {
                    config = JSON.parse(currentMenuCom.defaultConfigJson || currentMenuCom.DefaultConfigJson || '{}');
                } catch (e) {
                    config = { component: currentMenuCom.componentName || currentMenuCom.ComponentName, childrenctrls: [] };
                }
                const newConfig = deepClone(config);
                // 替换 vue-draggable-plus 在该位置添加的克隆对象
                if (idx >= 0 && idx < list.length) {
                    list.splice(idx, 0, newConfig);
                } else {
                    list.push(newConfig);
                }
                setCurrentCom(newConfig);
                ElMessage.success(`已添加: ${newConfig.component}`);
            }

            function onContainerDragEnd() {
                currentMenuCom = null;
            }

            // ===== provide 给子组件 =====
            provide('lcDesigner', {
                designMode,
                currentCom,
                setCurrentCom,
                onContainerDragAdd,
                onContainerDragEnd,
                dragGroup: DRAG_GROUP
            });

            // ===== 左侧组件库拖拽 =====
            function onPaletteDragStart(evt) {
                currentMenuCom = filteredComponents.value[evt.oldIndex];
            }

            function onPaletteDragEnd() {
                // 不立即清空，等容器的 onAdd 处理完
                setTimeout(() => { currentMenuCom = null; }, 100);
            }

            // 左侧组件库拖拽配置（v-draggable 指令用）
            const paletteDragOptions = computed(() => ({
                animation: 250,
                swapThreshold: 0.1,
                group: { name: 'lc-designer-group', pull: 'clone', put: false },
                sort: false,
                draggable: '.component-item',
                ghostClass: 'lc-ghost',
                dragClass: 'lc-drag',
                chosenClass: 'lc-chosen',
                onStart(evt) {
                    console.log('[Designer] palette drag start:', evt.oldIndex);
                    currentMenuCom = filteredComponents.value[evt.oldIndex];
                },
                onEnd() {
                    console.log('[Designer] palette drag end');
                    setTimeout(() => { currentMenuCom = null; }, 200);
                }
            }));

            // ===== 初始化 =====
            onMounted(async () => {
                console.log('[Designer] mounted, useDraggable:', typeof window.__useDraggable);
                await Promise.all([loadComponentMeta(), loadPageList()]);
                if (pageList.value.length > 0) {
                    currentPageCode.value = pageList.value[0].pageCode;
                    loadPage(pageList.value[0].pageCode);
                }
                nextTick(() => { initRulers(); resizeRulers(); });
            });

            // 缩放变化时同步标尺
            watch(canvasZoom, (z) => { setRulerZoom(z); });
            // 标尺显示切换时重新初始化
            watch(showRuler, (show) => {
                if (show) {
                    rulerH = null; rulerV = null;
                    nextTick(() => { initRulers(); resizeRulers(); });
                }
            });
            // 平台切换时重新调整标尺
            watch(canvasPlatform, () => { nextTick(() => resizeRulers()); });

            return {
                componentMetaList, pageList, currentPageCode, saving, showJson, showNewPage, showModelModal,
                activeCategory, activeUiLibrary, leftTab, configJsonText, newPageForm, designMode, treeFilter,
                currentCom, currentContainer, currentPath, breadcrumbList,
                configObj, modelObj, comListRef, treeRef,
                categories, hasOptionField, canMoveUp, canMoveDown,
                currentPropertyConfig, modelJsonText, onPropertyUpdate, showJsonEditor, showModelData, copyModelJson,
                filteredComponents, paletteDragOptions, selectFromTree, setCurrentCom,
                onPaletteDragStart, onPaletteDragEnd,
                deleteCurrent, moveUp, moveDown, copyCurrent,
                addValidator, removeValidator, needsValue, toggleRequired,
                loadPage, newPage, confirmNewPage, savePage, openPreview, applyJson,
                isContainerComp,
                canvasPlatform, canvasZoom, showRuler, zoomIn, zoomOut, zoomReset, zoomPercent,
                rulerHRef, rulerVRef, canvasWidth, canvasHeight
            };
        }
    });

    // ========== 注册全局组件 ==========
    app.use(ElementPlus);
    if (window.ElementPlusIconsVue) {
        for (const [key, comp] of Object.entries(window.ElementPlusIconsVue)) {
            app.component(key, comp);
        }
    }

    // NutUI
    if (window.nutui) {
        const nutuiObj = window.nutui.default || window.nutui;
        app.use(nutuiObj);
        for (const key in nutuiObj) {
            const comp = nutuiObj[key];
            if (comp && comp.name && comp.render) {
                const kebab = comp.name.replace(/([a-z0-9])([A-Z])/g, '$1-$2').toLowerCase();
                if (!app._context.components[kebab]) app.component(kebab, comp);
            }
        }
    }

        // v-draggable 指令（基于独立 Sortable.js，修复 vue-draggable-plus onMounted 不触发的 bug）
    if (window.Sortable) {
        app.directive('draggable', {
            mounted(el, binding) {
                const value = binding.value;
                const arr = Array.isArray(value) ? value : [value];
                const unref = v => (v && v.__v_isRef) ? v.value : v;
                const list = unref(arr[0]);
                const options = unref(arr[1]) || {};
                if (!el || !list) return;

                const merged = {
                    animation: 150,
                    group: 'lc-designer-group',
                    ghostClass: 'lc-ghost',
                    dragClass: 'lc-drag',
                    chosenClass: 'lc-chosen',
                    draggable: '.lc-node',
                    forceFallback: false,
                    ...options,
                    onStart(evt) {
                        document.body.style.userSelect = 'none';
                        if (typeof options.onStart === 'function') options.onStart(evt);
                    },
                    onAdd(evt) {
                        // 从左侧面板 clone 拖入时，删除 Sortable 自动插入的克隆 DOM（避免图标和组件重复）
                        if (evt.from && evt.from.classList && evt.from.classList.contains('component-grid')) {
                            if (evt.item && evt.item.parentNode) {
                                evt.item.parentNode.removeChild(evt.item);
                            }
                        }
                        if (typeof options.onAdd === 'function') options.onAdd(evt);
                        // 新元素渲染后补 draggable 属性
                        setTimeout(() => {
                            el.querySelectorAll(dragSel).forEach(c => c.setAttribute('draggable', 'true'));
                        }, 50);
                    },
                    onEnd(evt) {
                        document.body.style.userSelect = '';
                        // 内部排序：同步数组顺序
                        if (evt.from === evt.to && evt.oldIndex !== evt.newIndex && evt.oldIndex != null) {
                            const item = list.splice(evt.oldIndex, 1)[0];
                            list.splice(evt.newIndex, 0, item);
                        }
                        if (typeof options.onEnd === 'function') options.onEnd(evt);
                    }
                };
                const sortable = Sortable.create(el, merged);
                el.__sortable = sortable;
                // Sortable 1.15.2 原生模式下不自动加 draggable，手动添加
                const dragSel = merged.draggable || '.lc-node';
                const applyDraggable = () => el.querySelectorAll(dragSel).forEach(c => c.setAttribute('draggable', 'true'));
                applyDraggable();
                setTimeout(applyDraggable, 150); // 延迟再执行，覆盖动态渲染的子元素
            },
            unmounted(el) {
                if (el.__sortable) {
                    el.__sortable.destroy();
                    el.__sortable = null;
                }
            }
        });
        console.log('[Designer] v-draggable directive registered (Sortable.js)');
    } else {
        console.error('[Designer] Sortable.js not loaded!');
    }

    // 动态属性面板
    if (window.DynamicPropertyPanel) {
        app.component('DynamicPropertyPanel', window.DynamicPropertyPanel);
        console.log('[Designer] DynamicPropertyPanel registered');
    } else {
        console.warn('[Designer] DynamicPropertyPanel not loaded');
    }

    // NDynamicCom（递归渲染配置树，支持组合组件）
    app.component('NDynamicCom', {
        name: 'NDynamicCom',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, default: () => ({}) },
            nodePath: { type: String, default: 'root' }
        },
        inject: { lcDesigner: { default: null } },
        template: `
            <div v-if="!validConfig" class="lc-node lc-error" style="padding:8px;color:#f56c6c;font-size:12px;">
                [NDynamicCom] 无效配置: {{ nodePath }}
            </div>
            <div v-else-if="depthExceeded" class="lc-node lc-error" style="padding:8px;color:#f56c6c;font-size:12px;">
                [NDynamicCom] 递归深度超限: {{ nodePath }}
            </div>
            <div v-else class="lc-node"
                 :class="{ 'lc-selected': isSelected, 'lc-container': isContainer, 'lc-design': isDesign, 'lc-composite': isComposite, 'lc-wrapper': hasWrapper }"
                 @click.stop="onClick">
                <!-- 有 Wrapper：用包装器包裹 -->
                <component v-if="hasWrapper" :is="wrapperComponent"
                           :jsonconfig="jsonconfig.options.wrapperoptions"
                           :parentmodelinfo="parentmodelinfo"
                           :node-path="nodePath + '.wrapper'">
                    <!-- Wrapper 插槽内容 -->
                    <n-dynamic-com v-if="isComposite && compositeTree"
                                   :jsonconfig="compositeTree"
                                   :parentmodelinfo="parentmodelinfo"
                                   :node-path="nodePath + '.composite'"></n-dynamic-com>
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
                                   :node-path="nodePath + '.composite'"></n-dynamic-com>
                    <component v-else :is="jsonconfig.component"
                               :jsonconfig="jsonconfig"
                               :parentmodelinfo="parentmodelinfo"
                               :node-path="nodePath"></component>
                </template>
            </div>
        `,
        computed: {
            validConfig() { return this.jsonconfig && typeof this.jsonconfig === 'object' && this.jsonconfig.component; },
            depth() {
                const m = this.nodePath.match(/\.(childrenctrls\[|composite|wrapper)/g);
                return m ? m.length : 0;
            },
            depthExceeded() { return this.depth > 15; },
            isSelected() { return this.lcDesigner?.currentCom?.value === this.jsonconfig; },
            isContainer() { return isContainerComp(this.jsonconfig.component); },
            isDesign() { return this.lcDesigner?.designMode?.value === 'design'; },
            isComposite() { return !!compositeComponents[this.jsonconfig.component]; },
            hasWrapper() { return !!(this.jsonconfig.options?.wrapperoptions?.component); },
            wrapperComponent() { return this.jsonconfig.options?.wrapperoptions?.component; },
            compositeTree() {
                if (!this.isComposite) return null;
                const config = compositeComponents[this.jsonconfig.component];
                if (!config?.tree) return null;
                const externalProps = this.jsonconfig.options?.comoptions || {};
                return applyCompositeProps(config.tree, config, externalProps);
            }
        },
        methods: {
            onClick() {
                if (this.lcDesigner?.setCurrentCom && this.isDesign) {
                    this.lcDesigner.setCurrentCom(this.jsonconfig);
                }
            }
        }
    });

    // 异步加载自定义组件并 mount
    (async function init() {
        try {
            const resp = await fetch('/api/lowcode/components');
            const result = await resp.json();
            if (result.success && result.data) {
                let count = 0;
                for (const meta of result.data) {
                    const name = meta.componentName || meta.ComponentName;
                    const url = meta.loadUrl || meta.LoadUrl;
                    // 保存组合组件配置
                    if (meta.isComposite && meta.compositeConfigJson) {
                        try {
                            compositeComponents[name] = JSON.parse(meta.compositeConfigJson);
                        } catch (e) {
                            console.error('[Designer] 解析组合组件配置失败:', name, e);
                        }
                    }
                    // 注册自定义脚本
                    if (meta.customScriptJson && window.nutRegisterCustomScript) {
                        try {
                            window.nutRegisterCustomScript(name, meta.customScriptJson);
                        } catch (e) {
                            console.error('[Designer] 注册自定义脚本失败:', name, e);
                        }
                    }
                    if (name && url) {
                        // 组件名已在数据库中统一为 Dyn 前缀（DynElInput / DynNInput），避免与 UI 库全局组件冲突
                        app.component(name, window.nutLoadCom(name, url));
                        count++;
                    }
                }
                console.log(`[Designer] 注册了 ${count} 个自定义组件, 其中组合组件: ${Object.keys(compositeComponents).length}`);
            }
        } catch (e) { console.error('[Designer] 加载组件元数据失败:', e); }
        app.mount('#designer-app');
    })();

})();
