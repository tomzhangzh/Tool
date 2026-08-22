/**
 * designer.js - 低代码设计器核心逻辑
 * 参考 TUI.Web.Entry 架构：useDraggable + el-tree + provide/inject
 */
(function () {
    'use strict';

    const { createApp, reactive, ref, computed, watch, nextTick, onMounted, provide } = Vue;
    const { ElMessage, ElMessageBox } = ElementPlus;

    const CONTAINER_COMPONENTS = ['NForm', 'NCellGroup', 'NDivContainer', 'NGrid'];
    const isContainerComp = (name) => CONTAINER_COMPONENTS.includes(name);
    const DRAG_GROUP = 'lc-designer-group';

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
            const activeCategory = ref('表单');
            const leftTab = ref('palette');
            const configJsonText = ref('');
            const designMode = ref('design');
            const treeFilter = ref('');

            // 选中状态
            const currentCom = ref(null);
            const currentContainer = ref(null);
            const currentPath = ref('');
            const breadcrumbList = ref([]);

            const newPageForm = reactive({ pageName: '', pageCode: '' });

            // 页面配置根节点
            const configObj = reactive({
                component: 'NDivContainer', modelname: '',
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
                return componentMetaList.value.filter(c => (c.category || c.Category) === activeCategory.value);
            });

            const hasOptionField = computed(() => {
                if (!currentCom.value) return false;
                return ['NRadio', 'NCheckbox', 'NPicker'].includes(currentCom.value.component);
            });

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
                return componentMetaList.value.filter(c => (c.category || c.Category) === cat);
            }

            function deepClone(obj) {
                return JSON.parse(JSON.stringify(obj));
            }

            // 递归查找所有节点的父映射
            function buildParentMapping(root) {
                const mapping = [];
                function walk(node, parent) {
                    if (node.childrenctrls) {
                        node.childrenctrls.forEach(child => {
                            mapping.push({ child, parent: parent || root, parentObj: node });
                            walk(child, node);
                        });
                    }
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
                    component: 'NDivContainer', modelname: '',
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
                    list.splice(idx, 1, newConfig);
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
            });

            return {
                componentMetaList, pageList, currentPageCode, saving, showJson, showNewPage,
                activeCategory, leftTab, configJsonText, newPageForm, designMode, treeFilter,
                currentCom, currentContainer, currentPath, breadcrumbList,
                configObj, modelObj, comListRef, treeRef,
                categories, hasOptionField, canMoveUp, canMoveDown,
                filteredComponents, paletteDragOptions, selectFromTree, setCurrentCom,
                onPaletteDragStart, onPaletteDragEnd,
                deleteCurrent, moveUp, moveDown, copyCurrent,
                addValidator, removeValidator, needsValue, toggleRequired,
                loadPage, newPage, confirmNewPage, savePage, openPreview, applyJson,
                isContainerComp
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

    // vue-draggable-plus
    if (window.VueDraggablePlus) {
        const VDP = window.VueDraggablePlus.default || window.VueDraggablePlus;
        console.log('[Designer] VueDraggablePlus keys:', Object.keys(VDP));
        if (VDP.VueDraggable) app.component('VueDraggable', VDP.VueDraggable);
        if (VDP.vDraggable) {
            app.directive('draggable', VDP.vDraggable);
            console.log('[Designer] v-draggable directive registered');
        }
        if (VDP.useDraggable) window.__useDraggable = VDP.useDraggable;
    } else {
        console.error('[Designer] VueDraggablePlus not loaded!');
    }

    // NDynamicCom（递归渲染配置树）
    app.component('NDynamicCom', {
        name: 'NDynamicCom',
        props: {
            jsonconfig: { type: Object, required: true },
            parentmodelinfo: { type: Object, default: () => ({}) },
            nodePath: { type: String, default: 'root' }
        },
        inject: { lcDesigner: { default: null } },
        template: `
            <div class="lc-node"
                 :class="{ 'lc-selected': isSelected, 'lc-container': isContainer, 'lc-design': isDesign }"
                 @@click.stop="onClick">
                <component :is="jsonconfig.component"
                           :jsonconfig="jsonconfig"
                           :parentmodelinfo="parentmodelinfo"
                           :node-path="nodePath"></component>
            </div>
        `,
        computed: {
            isSelected() { return this.lcDesigner?.currentCom?.value === this.jsonconfig; },
            isContainer() { return isContainerComp(this.jsonconfig.component); },
            isDesign() { return this.lcDesigner?.designMode?.value === 'design'; }
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
                    if (name && url) {
                        app.component(name, window.nutLoadCom(name, url));
                        count++;
                    }
                }
                console.log(`[Designer] 注册了 ${count} 个自定义组件`);
            }
        } catch (e) { console.error('[Designer] 加载组件元数据失败:', e); }
        app.mount('#designer-app');
    })();

})();
