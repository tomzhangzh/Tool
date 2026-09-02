/**
 * designer.js - 低代码设计器核心逻辑
 * 参考 TUI.Web.Entry 架构：useDraggable + el-tree + provide/inject
 */
(function () {
    'use strict';

    const { createApp, reactive, ref, computed, watch, nextTick, onMounted, provide } = Vue;
    const S = window.LCDesignerStore;   // 全局共享 store（designer.core.js）
    const { ElMessage, ElMessageBox } = ElementPlus;

    // ===== 拆分出的公共工具（designer.utils.js / dyn-lib.js）=====
    const {
        dragState, deepClone, getByPath, setPathVal, isContainerComp, applyCompositeProps,
        createPaletteGhost, removePaletteGhost,
        computeInsertIndex, updateDropPlaceholder, clearDropPlaceholder
    } = (window.LCDesignerUtils || {});
    const eventBus = (window.dyn && window.dyn.eventBus);

    const DRAG_GROUP = 'lc-designer-group';

        // 组合组件配置 map（运行时渲染用；与 right 独立 app 共享）
        // 注意：LCDesignerCore 可能尚未初始化（初始化在其后 PaletteContent 导出处），必须先建容器
    if (!window.LCDesignerCore) window.LCDesignerCore = {};
    const compositeComponents = (window.LCDesignerCore.compositeComponents = window.LCDesignerCore.compositeComponents || {});

    // 全局 lcDesigner 引用（v-draggable 指令在 setup 外，接收 palette drop 时通过它调用 onPaletteDrop）
    let lcDesignerGlobal = null;


    // ===== 组件库面板（可复用：左侧 tab 与浮动弹出层共用）=====
    const PaletteContent = {
        name: 'PaletteContent',
        props: {
            components: Array,
            categories: Array,
            uiLibrary: String,
            category: String,
            onDragStart: Function,
            onDragEnd: Function
        },
        emits: ['update:uiLibrary', 'update:category'],
        template: `<div class="palette-inner">
            <div style="margin-bottom:8px;">
                <el-select :model-value="uiLibrary" size="small" style="width:100%;" @update:model-value="v => $emit('update:uiLibrary', v)">
                    <el-option label="全部组件" value="all"></el-option>
                    <el-option label="NutUI (移动端)" value="nutui"></el-option>
                    <el-option label="ElementUI (电脑端)" value="elementui"></el-option>
                    <el-option label="自定义" value="custom"></el-option>
                </el-select>
            </div>
            <el-radio-group :model-value="category" size="small" style="margin-bottom:8px;" @update:model-value="v => $emit('update:category', v)">
                <el-radio-button v-for="cat in categories" :key="cat.key" :label="cat.key">{{ cat.label }}</el-radio-button>
            </el-radio-group>
            <div class="component-grid">
                <div v-for="comp in components" :key="comp.componentName"
                     class="component-item" :data-comp-name="comp.componentName"
                     :class="{ 'is-composite': comp.isComposite || comp.IsComposite }"
                     :title="comp.description" draggable="true"
                     @dragstart="onDragStart($event, comp)" @dragend="onDragEnd">
                    <span class="comp-icon">{{ comp.icon }}</span>
                    <span class="comp-label">{{ comp.displayName || comp.label || comp.componentName }}</span>
                </div>
            </div>
        </div>`
    };

    // 导出供独立 left 面板 app 注册使用
    if (!window.LCDesignerCore) window.LCDesignerCore = {};
    window.LCDesignerCore.PaletteContent = PaletteContent;

    const app = createApp({
        components: { PaletteContent },
        setup() {
            // ===== 状态 =====
            const componentMetaList = S.componentMetaList;
            const pageList = S.pageList;
            const currentPageCode = S.currentPageCode;
            const currentPageId = S.currentPageId;
            const saving = S.saving;
            const showJson = S.showJson;
            const showNewPage = S.showNewPage;
            const showModelModal = S.showModelModal;
            const rightPanel = ref('open');
            const configJsonText = S.configJsonText;
            const designMode = S.designMode;
            function openRightPanel() {
                rightPanel.value = 'open';
            }

            // ===== 画布视图控制 =====
            const canvasPlatform = S.canvasPlatform; // mobile | desktop
            const canvasZoom = S.canvasZoom;
            const showRuler = S.showRuler;
            const canvasWidth = S.canvasWidth;
            const canvasHeight = S.canvasHeight;
            const zoomIn = () => { canvasZoom.value = Math.min(2, +(canvasZoom.value + 0.1).toFixed(2)); };
            const zoomOut = () => { canvasZoom.value = Math.max(0.25, +(canvasZoom.value - 0.1).toFixed(2)); };
            const zoomReset = () => { canvasZoom.value = 1; };
            const zoomPercent = S.zoomPercent;
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
            const currentCom = S.currentCom;
            const currentContainer = S.currentContainer;
            const currentPath = S.currentPath;
            const breadcrumbList = S.breadcrumbList;

            const newPageForm = S.newPageForm;

            // 页面配置根节点
            const configObj = S.configObj;   // 共享 store（designer.core.js）
            const modelObj = S.modelObj;

            const treeVersion = S.treeVersion;


            // ===== 保存为组合组件 =====
            const showCompositeDialog = S.showCompositeDialog;
            const compositeForm = S.compositeForm;

            // ===== 开放配置模式（所见即所得标记开放属性/容器）=====
            const openConfigMode = S.openConfigMode;
            function ensureOpen(node) {
                if (!node) return null;
                node.extendinfo = node.extendinfo || {};
                node.extendinfo.__open = node.extendinfo.__open || { props: {}, container: false };
                return node.extendinfo.__open;
            }
            // 当前选中组件的开放属性映射（属性面板开关显示用）
            const currentComOpenMap = computed(() => {
                const open = currentCom.value?.extendinfo?.__open;
                return open ? (open.props || {}) : {};
            });
            function toggleOpenProp(key, checked, node) {
                if (!node) return;
                const open = ensureOpen(node);
                if (checked) open.props[key] = true;
                else delete open.props[key];
            }
            function toggleOpenContainer(checked) {
                if (!currentCom.value) return;
                const open = ensureOpen(currentCom.value);
                open.container = !!checked;
            }
            // 当前选中组件是否已开放为容器
            const currentComOpenContainer = computed(() => {
                return !!(currentCom.value?.extendinfo?.__open?.container);
            });
            // 汇总当前页面所有开放标记（供列表查看/移除）
            const openSummary = computed(() => {
                const props = [];
                const containers = [];
                const walk = (node, relPath) => {
                    const open = node.extendinfo && node.extendinfo.__open;
                    if (open) {
                        if (open.container) {
                            containers.push({
                                kind: 'container', path: relPath || '(组合根)',
                                label: node.options?.labeloptions?.label || node.component,
                                component: node.component, node
                            });
                        }
                        if (open.props) {
                            for (const [propKey, on] of Object.entries(open.props)) {
                                if (!on) continue;
                                const fullPath = relPath ? relPath + '.' + propKey : propKey;
                                const curVal = getByPath(node, propKey);
                                props.push({ kind: 'prop', path: fullPath, field: propKey, label: propKey, value: curVal, node, propKey });
                            }
                        }
                    }
                    (node.childrenctrls || []).forEach((ch, i) => {
                        walk(ch, (relPath ? relPath + '.' : '') + 'childrenctrls[' + i + ']');
                    });
                };
                walk(configObj, '');
                return { props, containers };
            });
            function removeOpenItem(item) {
                if (item.kind === 'container') {
                    if (item.node.extendinfo && item.node.extendinfo.__open) {
                        item.node.extendinfo.__open.container = false;
                    }
                } else if (item.node.extendinfo && item.node.extendinfo.__open && item.node.extendinfo.__open.props) {
                    delete item.node.extendinfo.__open.props[item.propKey];
                }
            }

            // 遍历内部树：收集可开放属性候选（relPath 为节点相对根的路径，如 childrenctrls[0]）
            function collectNodeProps(node, relPath) {
                const out = [];
                const join = (f) => (relPath ? relPath + '.' : '') + f;
                const openProps = (node.extendinfo && node.extendinfo.__open && node.extendinfo.__open.props) || {};
                const defaultKey = (fieldPath) => {
                    const seg = fieldPath.split('.').pop();
                    return seg === 'label' ? 'label' : seg;
                };
                const add = (fieldPath, fieldLabel, value) => {
                    if (value === undefined || value === null || value === '') return;
                    out.push({ checked: !!openProps[fieldPath], path: join(fieldPath), field: fieldLabel, label: fieldLabel, value, key: join(fieldPath), type: guessPropType(value) }); // 完整路径作为开放属性 key（唯一标识，如 childrenctrls[0].childrenctrls[0].options.labeloptions.label）
                };
                if (node.modelname) add('modelname', '绑定字段', node.modelname);
                if (node.options?.labeloptions?.label) add('options.labeloptions.label', '标签文字', node.options.labeloptions.label);
                const co = node.options?.comoptions || {};
                for (const [k, v] of Object.entries(co)) {
                    if (typeof v === 'string' || typeof v === 'boolean' || typeof v === 'number') {
                        add('comoptions.' + k, '属性 ' + k, v);
                    }
                }
                return out;
            }
            function walkExpose(node, relPath) {
                let out = collectNodeProps(node, relPath);
                (node.childrenctrls || []).forEach((ch, i) => {
                    out = out.concat(walkExpose(ch, (relPath ? relPath + '.' : '') + 'childrenctrls[' + i + ']'));
                });
                return out;
            }
            function collectExposed(tree) {
                let out = collectNodeProps(tree, '');
                (tree.childrenctrls || []).forEach((ch, i) => {
                    out = out.concat(walkExpose(ch, 'childrenctrls[' + i + ']'));
                });
                return out;
            }
            // 遍历内部树：收集容器候选（可开放为插槽，仅收集子容器，根容器自身不开放）
            function walkContainers(node, relPath) {
                let out = [];
                if (isContainerComp(node.component)) {
                    const opened = !!(node.extendinfo && node.extendinfo.__open && node.extendinfo.__open.container);
                    out.push({ checked: opened, path: relPath, component: node.component,
                        label: node.options?.labeloptions?.label || node.component,
                        key: relPath }); // 开放容器 key 用完整路径（如 childrenctrls[0]）
                }
                (node.childrenctrls || []).forEach((ch, i) => {
                    out = out.concat(walkContainers(ch, (relPath ? relPath + '.' : '') + 'childrenctrls[' + i + ']'));
                });
                return out;
            }
            function collectContainers(tree) {
                let out = [];
                // 根自身若被标记为开放容器（target='' 表示组合根开放，外部组件直接拖入根内）
                if (tree.extendinfo && tree.extendinfo.__open && tree.extendinfo.__open.container) {
                    out.push({ checked: true, path: '', component: tree.component,
                        label: tree.options?.labeloptions?.label || tree.component, key: 'body' });
                }
                (tree.childrenctrls || []).forEach((ch, i) => {
                    out = out.concat(walkContainers(ch, 'childrenctrls[' + i + ']'));
                });
                return out;
            }
            function guessPropType(v) {
                if (typeof v === 'boolean') return 'switch';
                if (typeof v === 'number') return 'number';
                return 'input';
            }
            // 移除内部树节点的设计时开放标记（保存组合组件时清理）
            function stripOpenMarks(node) {
                if (!node) return;
                if (node.extendinfo && node.extendinfo.__open) delete node.extendinfo.__open;
                (node.childrenctrls || []).forEach(stripOpenMarks);
            }

            function openCompositeDialog() {
                // 内容来源默认"整个页面"（开放属性/容器候选也来自整个页面）；
                // 用户可在对话框中切换为"当前选中组件"，切换时通过 onCompositeSourceChange 重新收集候选
                compositeForm.source = 'page';
                const tree = configObj;
                if (!tree || !tree.component) { ElMessage.warning('请先打开页面'); return; }
                compositeForm.label = pageList.value.find(p => p.pageCode === currentPageCode.value)?.pageName || '';
                compositeForm.componentName = 'DynCom' + Math.random().toString(36).slice(2, 6);
                compositeForm.icon = '📦';
                compositeForm.description = '';
                compositeForm.exposedProps = collectExposed(tree);
                compositeForm.openContainers = collectContainers(tree);
                showCompositeDialog.value = true;
            }

            // 内容来源切换时重新收集开放属性/容器候选
            function onCompositeSourceChange() {
                if (compositeForm.source === 'selected' && !currentCom.value) {
                    compositeForm.source = 'page';
                    ElMessage.warning('未选中组件，已回退为整个页面');
                }
                const tree = compositeForm.source === 'page' ? configObj : currentCom.value;
                if (!tree) return;
                compositeForm.exposedProps = collectExposed(tree);
                compositeForm.openContainers = collectContainers(tree);
            }
            async function saveAsComposite() {
                const tree = compositeForm.source === 'page' ? deepClone(configObj) : deepClone(currentCom.value);
                if (!tree || !tree.component) { ElMessage.warning('无效的组合内容'); return; }
                if (!compositeForm.componentName.trim()) { ElMessage.warning('请填写组件名称'); return; }

                // 清理内部树中的设计时开放标记（__open），避免污染组合组件内部树
                stripOpenMarks(tree);
                const exposedProps = compositeForm.exposedProps
                    .filter(p => p.checked)
                    .map(p => ({
                        key: (p.key && p.key.trim()) || String(p.path || '').split('.').pop() || p.field || 'prop',
                        label: p.label || p.field,
                        type: p.type || 'input',
                        default: p.value,
                        target: p.path
                    }));
                const openContainers = compositeForm.openContainers
                    .filter(c => c.checked)
                    .map(c => ({
                        key: (c.key && c.key.trim()) || (c.path === '' ? 'body' : c.component),
                        label: c.label || c.component,
                        target: c.path
                    }));

                // 组合组件拖入画布时的默认实例配置（开放属性默认值 + 开放容器空数组）
                const defaultComoptions = {};
                exposedProps.forEach(p => { defaultComoptions[p.key] = p.default ?? ''; });
                const defaultSlots = {};
                openContainers.forEach(c => { defaultSlots[c.key] = []; });
                const defaultConfig = {
                    component: compositeForm.componentName,
                    modelname: '',
                    options: {
                        comoptions: defaultComoptions, comlisteners: {},
                        labeloptions: {}, itemoptions: { style: {}, class: '' }
                    },
                    validators: [], childrenctrls: [], slots: defaultSlots, extendinfo: {}
                };

                const meta = {
                    componentName: compositeForm.componentName.trim(),
                    componentType: 4,
                    category: '组合',
                    label: compositeForm.label || compositeForm.componentName,
                    icon: compositeForm.icon || '📦',
                    defaultConfigJson: JSON.stringify(defaultConfig),
                    defaultOptionsJson: null,
                    propertyConfigJson: null,
                    isComposite: true,
                    compositeConfigJson: JSON.stringify({ tree, exposedProps, openContainers }),
                    uiLibrary: 'custom',
                    loadUrl: '/NutComponent/Container/DivContainer',
                    description: compositeForm.description,
                    isEnabled: true, sortOrder: 0
                };

                try {
                    const resp = await fetch('/api/lowcode/component', {
                        method: 'POST', headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(meta)
                    });
                    const result = await resp.json();
                    if (!result.success) { ElMessage.error('保存失败: ' + result.message); return; }
                    // 本地注册组合组件 + 刷新面板
                    try { compositeComponents[meta.componentName] = JSON.parse(meta.compositeConfigJson); } catch (e) { }
                    app.component(meta.componentName, window.nutLoadCom(meta.componentName, meta.loadUrl));
                    await loadComponentMeta();
                    showCompositeDialog.value = false;
                    ElMessage.success('组合组件已保存，可在左侧「组合」分类使用');
                } catch (e) {
                    ElMessage.error('保存失败: ' + e.message);
                }
            }

            // ===== 计算属性 =====
            const hasOptionField = computed(() => {
                if (!currentCom.value) return false;
                return ['DynNRadio', 'DynNCheckbox', 'DynNPicker'].includes(currentCom.value.component);
            });

            // 当前选中组件的属性面板配置
            const currentPropertyConfig = computed(() => {
                if (!currentCom.value) return null;
                const compName = currentCom.value.component;

                // 组合组件：根据开放属性 + 开放容器动态生成属性配置
                if (compositeComponents[compName]) {
                    const config = compositeComponents[compName];
                    const groups = [];
                    // 开放属性
                    if (config.exposedProps && config.exposedProps.length) {
                        const fields = config.exposedProps.map(p => ({
                            key: '@@' + p.key, // @@ 前缀 = comoptions 字面键（key 是完整路径，不能走点号路径解析）
                            label: p.label || p.key,
                            type: p.type || 'input',
                            default: p.default,
                            options: p.options,
                            placeholder: p.placeholder
                        }));
                        groups.push({ title: '开放属性', fields });
                    }
                    // 开放容器（插槽）提示
                    if (config.openContainers && config.openContainers.length) {
                        const slotFields = config.openContainers.map(oc => ({
                            key: '@@slots:' + oc.key, // @@slots: 前缀 = slots 字面键,
                            label: oc.label || oc.key,
                            type: 'slot',
                            hint: oc.hint || '可拖入组件',
                            readonly: true
                        }));
                        groups.push({ title: '开放容器（可拖入）', fields: slotFields });
                    }
                    return { groups };
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
            const modelJsonText = S.modelJsonText;

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
                if (!currentCom.value) return false;
                const arr = findContainingArray(configObj, currentCom.value);
                if (!arr) return false;
                return arr.indexOf(currentCom.value) > 0;
            });

            const canMoveDown = computed(() => {
                if (!currentCom.value) return false;
                const arr = findContainingArray(configObj, currentCom.value);
                if (!arr) return false;
                const idx = arr.indexOf(currentCom.value);
                return idx >= 0 && idx < arr.length - 1;
            });

            // ===== 工具方法 =====
            function deepClone(obj) {
                return JSON.parse(JSON.stringify(obj));
            }

            // 递归查找所有节点的父映射
            function buildParentMapping(root) {
                const mapping = [];
                function walk(node, parent) {
                    if (!node || typeof node !== 'object') return;
                    // 组合组件：开放容器 slots 中的外部内容也纳入映射（删除/移动/面包屑可用）
                    const cc = compositeComponents[node.component];
                    if (cc && cc.openContainers) {
                        for (const oc of cc.openContainers) {
                            const slotArr = node.slots ? node.slots[oc.key] : null;
                            if (Array.isArray(slotArr)) {
                                slotArr.forEach(child => {
                                    if (!child) return;
                                    mapping.push({ child, parent: parent || root, parentObj: node });
                                    walk(child, node);
                                });
                            }
                        }
                    }
                    if (!Array.isArray(node.childrenctrls)) return;
                    node.childrenctrls.forEach(child => {
                        if (!child) return;
                        mapping.push({ child, parent: parent || root, parentObj: node });
                        walk(child, node);
                    });
                }
                walk(root, null);
                return mapping;
            }

            // 查找选中组件所在数组（兼容 childrenctrls 与组合开放容器 slots）
            function findContainingArray(root, target) {
                if (!root || !target) return null;
                const hit = { arr: null };
                function walk(node) {
                    if (!node || typeof node !== 'object' || hit.arr) return;
                    const cc = compositeComponents[node.component];
                    if (cc && cc.openContainers) {
                        for (const oc of cc.openContainers) {
                            const slotArr = node.slots ? node.slots[oc.key] : null;
                            if (!Array.isArray(slotArr)) continue;
                            if (slotArr.includes(target)) { hit.arr = slotArr; return; }
                            slotArr.forEach(walk);
                        }
                    }
                    if (!Array.isArray(node.childrenctrls)) return;
                    if (node.childrenctrls.includes(target)) { hit.arr = node.childrenctrls; return; }
                    node.childrenctrls.forEach(walk);
                }
                walk(root);
                return hit.arr;
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
                if (eventBus) eventBus.emit('setcurrent', comConfig);
            }

            // ===== 组件操作 =====
            function deleteCurrent() {
                if (!currentCom.value) {
                    ElMessage.warning('请先选中组件');
                    return;
                }
                const arr = findContainingArray(configObj, currentCom.value);
                if (!arr) {
                    ElMessage.warning('未找到选中组件的父容器');
                    return;
                }
                const idx = arr.indexOf(currentCom.value);
                if (idx >= 0) {
                    arr.splice(idx, 1);
                    currentCom.value = null;
                    currentContainer.value = null;
                    currentPath.value = '';
                    ElMessage.success('已删除');
                    window.dispatchEvent(new CustomEvent('lc-tree-refresh'));
                }
            }

            function moveUp() {
                if (!currentCom.value) return;
                const arr = findContainingArray(configObj, currentCom.value);
                if (!arr) return;
                const idx = arr.indexOf(currentCom.value);
                if (idx > 0) {
                    [arr[idx - 1], arr[idx]] = [arr[idx], arr[idx - 1]];
                    window.dispatchEvent(new CustomEvent('lc-tree-refresh'));
                }
            }

            function moveDown() {
                if (!currentCom.value) return;
                const arr = findContainingArray(configObj, currentCom.value);
                if (!arr) return;
                const idx = arr.indexOf(currentCom.value);
                if (idx >= 0 && idx < arr.length - 1) {
                    [arr[idx + 1], arr[idx]] = [arr[idx], arr[idx + 1]];
                    window.dispatchEvent(new CustomEvent('lc-tree-refresh'));
                }
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
                        if (eventBus) eventBus.emit('loaded', { pageCode: code, page: result.data });
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
                        if (eventBus) eventBus.emit('saved', { id: result.data, pageCode: currentPageCode.value });
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
            // 防重说明：画布容器会嵌套（如 Form ⊃ CellGroup），从左侧拖入组件时 drop 事件
            // 可能同时触发多个容器的 onAdd，导致"表单、布局等多个容器都添加组件"。
            // 这里统一走"延迟提交 + 最深（最内层）容器优先"，同一拖拽会话只添加一次到真正的目标容器。
            let pendingAdd = null;     // 当前拖拽待提交的添加（最内层容器）
            function depthOfDom(el) {
                let d = 0, n = el;
                while (n) { d++; n = n.parentNode; }
                return d;
            }
            function commitPendingAdd() {
                const p = pendingAdd;
                pendingAdd = null;
                if (!p || !p.parentConfig?.childrenctrls) return;
                const list = p.parentConfig.childrenctrls;
                const idx = p.newIndex ?? list.length;
                if (idx >= 0 && idx < list.length) list.splice(idx, 0, p.newConfig);
                else list.push(p.newConfig);
                setCurrentCom(p.newConfig);
                ElMessage.success(`已添加: ${p.newConfig.component}`);
            }
            function onContainerDragAdd(evt, parentConfig) {
                console.log('[Designer] onContainerDragAdd:', evt.newIndex, dragState.currentMenuCom?.componentName, 'session', dragState.sessionId);
                if (!dragState.currentMenuCom || !parentConfig?.childrenctrls) return;
                if (parentConfig.__locked) return; // 组合内部锁定容器禁止拖入（组合组件除非开放容器）
                // 创建正确的配置
                let config = {};
                try {
                    config = JSON.parse(dragState.currentMenuCom.defaultConfigJson || dragState.currentMenuCom.DefaultConfigJson || '{}');
                } catch (e) {
                    config = { component: dragState.currentMenuCom.componentName || dragState.currentMenuCom.ComponentName, childrenctrls: [] };
                }
                const newConfig = deepClone(config);
                // 同一拖拽会话内嵌套容器可能多次回调：只保留 drop 目标 DOM 最深（最内层）的容器，
                // 更浅（父级）容器回调直接忽略，避免重复添加。
                const depth = evt && evt.to ? depthOfDom(evt.to) : 0;
                if (pendingAdd && pendingAdd.session === dragState.sessionId && pendingAdd.depth > depth) {
                    return; // 已有更深的目标容器，父级忽略
                }
                if (pendingAdd && pendingAdd.timer) clearTimeout(pendingAdd.timer);
                pendingAdd = { parentConfig, newConfig, newIndex: evt?.newIndex ?? null, depth, session: dragState.sessionId };
                pendingAdd.timer = setTimeout(commitPendingAdd, 20);
            }
            function onContainerDragEnd() {
                dragState.currentMenuCom = null;
                if (pendingAdd && pendingAdd.timer) clearTimeout(pendingAdd.timer);
                pendingAdd = null;
            }

            // ===== provide 给子组件 =====
            const lcProvider = {
                designMode,
                currentCom,
                setCurrentCom,
                onContainerDragAdd,
                onContainerDragEnd,
                onPaletteDrop,
                dragGroup: DRAG_GROUP
            };
            lcDesignerGlobal = lcProvider;
            provide('lcDesigner', lcProvider);

            // 画布容器收到 palette drop：插入组件到目标容器（组合内部锁定容器除外）
            function onPaletteDrop(parentConfig, evt, insertIndex) {
                if (!dragState.currentMenuCom || !parentConfig) return;
                if (parentConfig.__locked) return; // 组合内部锁定容器禁止拖入（开放容器已解锁）
                let config = {};
                try {
                    config = JSON.parse(dragState.currentMenuCom.defaultConfigJson || dragState.currentMenuCom.DefaultConfigJson || '{}');
                } catch (e) {
                    config = { component: dragState.currentMenuCom.componentName || dragState.currentMenuCom.ComponentName, childrenctrls: [] };
                }
                const newConfig = deepClone(config);
                if (parentConfig.__openSlot && Array.isArray(parentConfig.__slotRef)) {
                    // 开放容器：追加到外部 slots（持久化目标），compositeTree 响应式重建合并数组
                    parentConfig.__slotRef.push(newConfig);
                } else if (Array.isArray(parentConfig.childrenctrls)) {
                    // 按占位符索引插入（无索引则追加）
                    if (typeof insertIndex === 'number' && insertIndex >= 0 && insertIndex <= parentConfig.childrenctrls.length) {
                        parentConfig.childrenctrls.splice(insertIndex, 0, newConfig);
                    } else {
                        parentConfig.childrenctrls.push(newConfig);
                    }
                }
                setCurrentCom(newConfig);
                ElMessage.success('已添加: ' + newConfig.component);
                window.dispatchEvent(new CustomEvent('lc-tree-refresh'));
                if (eventBus) eventBus.emit('dropin', { parent: parentConfig, config: newConfig, index: insertIndex });
            }

            // ===== 画布级兜底 drop =====
            // 新建空页面时，根容器（DynNDivContainer）只有顶部一小块高度，拖到画布空白处无法命中子容器。
            // 在画布元素上兜底接收 palette drop：未命中子容器时插入到页面根容器（configObj.childrenctrls）。
            function isPaletteDragEvent(evt) {
                if (evt.dataTransfer && evt.dataTransfer.types) {
                    return Array.from(evt.dataTransfer.types || []).indexOf('application/x-lc-comp') >= 0;
                }
                return dragState.draggingFromPalette === true;
            }
            function onCanvasDragOver(evt) {
                if (isPaletteDragEvent(evt)) {
                    evt.preventDefault(); // 允许 drop
                    if (evt.dataTransfer) evt.dataTransfer.dropEffect = 'copy';
                }
            }
            function onCanvasDrop(evt) {
                if (!isPaletteDragEvent(evt)) return;
                evt.preventDefault();
                evt.stopPropagation();
                // 兜底：插入页面根容器（子容器已处理过的 drop 会 stopPropagation，到不了这里）
                if (configObj && Array.isArray(configObj.childrenctrls)) {
                    onPaletteDrop(configObj, evt, undefined);
                }
            }

            // ===== 导出公共 API（供独立面板 app 通过 __lcApi 调用）=====
            Object.assign(window.__lcApi, {
                loadComponentMeta, loadPageList, loadPage, savePage, newPage, confirmNewPage,
                showModelData, openCompositeDialog, onCompositeSourceChange, saveAsComposite,
                openPreview, applyJson, setCurrentCom, showJsonEditor,
                deleteCurrent, moveUp, moveDown, copyCurrent,
                zoomIn, zoomOut, zoomReset
            });

            // ===== 初始化 =====
            onMounted(async () => {
                console.log('[Designer] mounted, useDraggable:', typeof window.__useDraggable);
                // 监听拖动排序完成事件，刷新组件树
                window.addEventListener('lc-tree-refresh', () => { treeVersion.value++; });
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
                configJsonText, newPageForm, designMode,
                currentCom, currentContainer, currentPath, breadcrumbList,
                configObj, modelObj, treeVersion,
                hasOptionField, canMoveUp, canMoveDown,
                currentPropertyConfig, modelJsonText, onPropertyUpdate, showJsonEditor, showModelData, copyModelJson,
                setCurrentCom,
                showCompositeDialog, compositeForm, openCompositeDialog, saveAsComposite,
                onCompositeSourceChange,
                openConfigMode, currentComOpenMap, currentComOpenContainer, toggleOpenProp,
                toggleOpenContainer, openSummary, removeOpenItem, isContainerComp,
                onCanvasDragOver, onCanvasDrop,
                deleteCurrent, moveUp, moveDown, copyCurrent,
                addValidator, removeValidator, needsValue, toggleRequired,
                loadPage, newPage, confirmNewPage, savePage, openPreview, applyJson,
                isContainerComp,
                canvasPlatform, canvasZoom, showRuler, zoomIn, zoomOut, zoomReset, zoomPercent,
                rulerHRef, rulerVRef, canvasWidth, canvasHeight,
                rightPanel, openRightPanel
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
                // 动态获取最新 list（页面加载时 Object.assign 会替换 childrenctrls 引用，不能缓存）
                const getList = () => {
                    const v = binding.value;
                    const a = Array.isArray(v) ? v : [v];
                    return unref(a[0]);
                };
                const options = unref(arr[1]) || {};
                if (!el) return;

                // 拖拽元素选择器：优先用 options.draggable（如左侧面板 .component-item），默认画布容器 .lc-node
                const dragSel = options.draggable || '.lc-node';
                let dragOldArrIndex = -1;
                // 开放容器：把拼接数组的"外部段"同步回 slots 引用（内部固定内容不写回）
                function syncOpenSlotArr() {
                    const jc = binding.instance?.props?.jsonconfig;
                    if (jc && Array.isArray(jc.__slotRef) && Array.isArray(jc.childrenctrls)) {
                        const fixedLen = jc.__fixedLen || 0;
                        const slotsArr = jc.childrenctrls.slice(fixedLen);
                        jc.__slotRef.splice(0, jc.__slotRef.length, ...slotsArr);
                    }
                }
                const merged = {
                    animation: 150,
                    group: 'lc-designer-group',
                    ghostClass: 'lc-ghost',
                    dragClass: 'lc-drag',
                    chosenClass: 'lc-chosen',
                    draggable: dragSel,
                    forceFallback: false,
                    ...options,
                    onStart(evt) {
                        document.body.style.userSelect = 'none';
                        // 记录被拖动元素在 draggable 子元素中的数组索引（过滤非 draggable 元素如标题）
                        const draggableChildren = Array.from(el.children).filter(c => c.matches(dragSel));
                        dragOldArrIndex = draggableChildren.indexOf(evt.item);
                        console.log('[Drag] onStart:', { itemText: evt.item.textContent.trim().substring(0,20), oldDOMIndex: evt.oldIndex, oldArrIndex: dragOldArrIndex, listLength: getList().length, listItems: getList().map(i => i.component || '?') });
                        if (typeof options.onStart === 'function') options.onStart(evt);
                    },
                    onAdd(evt) {
                        // palette 已改用原生 DnD（不走 Sortable），此 onAdd 仅处理画布内部容器间移动
                        if (typeof options.onAdd === 'function') options.onAdd(evt);
                        syncOpenSlotArr(); // 同步开放容器 slots（外部拖入持久化）
                        // 新元素渲染后补 draggable 属性
                        setTimeout(() => {
                            el.querySelectorAll(dragSel).forEach(c => c.setAttribute('draggable', 'true'));
                        }, 50);
                    },
                    onEnd(evt) {
                        document.body.style.userSelect = '';
                        // 内部排序：同步数组顺序（用过滤非 draggable 元素后的正确数组索引）
                        if (evt.from === evt.to && dragOldArrIndex >= 0) {
                            const targetDraggable = Array.from(evt.to.children).filter(c => c.matches(dragSel));
                            const newArrIndex = targetDraggable.indexOf(evt.item);
                            const safeList = getList();
                            const item = safeList[dragOldArrIndex];
                            console.log('[Drag] onEnd:', { newArrIndex, oldArrIndex: dragOldArrIndex, item: item?.component });
                            if (item && newArrIndex >= 0 && newArrIndex !== dragOldArrIndex) {
                                // 直接修改容器组件的原始数组 props.jsonconfig.childrenctrls
                                const originalArray = binding.instance?.props?.jsonconfig?.childrenctrls;
                                if (originalArray && Array.isArray(originalArray)) {
                                    const origIdx = originalArray.indexOf(item);
                                    if (origIdx >= 0) {
                                        originalArray.splice(origIdx, 1);
                                        let insertIdx = originalArray.length;
                                        let safeCount = 0;
                                        for (let i = 0; i < originalArray.length; i++) {
                                            const cc = originalArray[i];
                                            if (cc && typeof cc === 'object' && cc.component) {
                                                if (safeCount === newArrIndex) { insertIdx = i; break; }
                                                safeCount++;
                                            }
                                        }
                                        originalArray.splice(insertIdx, 0, item);
                                        console.log('[Drag] originalArray updated:', originalArray.map(i => i.component));
                                    }
                                } else {
                                    console.warn('[Drag] originalArray not found via binding.instance');
                                }
                            }
                        }
                        dragOldArrIndex = -1;
                        if (typeof options.onEnd === 'function') options.onEnd(evt);
                        syncOpenSlotArr(); // 同步开放容器 slots（内部排序持久化）
                        // 通知组件树刷新（el-tree 对深层数组顺序变化不自动更新）
                        window.dispatchEvent(new CustomEvent('lc-tree-refresh'));
                    }
                };
                const sortable = Sortable.create(el, merged);
                el.__sortable = sortable;
                el.__dragList = getList; // 调试：暴露获取数组的函数
                el.__vueInstance = binding.instance; // 调试：暴露组件实例
                el.__dragBinding = binding.value; // 调试：暴露绑定值
                el.__dragArr0 = Array.isArray(binding.value) ? binding.value[0] : binding.value; // 调试：暴露数组引用
                // 接收左侧 palette 的原生 HTML5 drop（palette 不参与 Sortable group，走原生 dragover/drop）
                // isPaletteDrag: 通过 dataTransfer 自定义 MIME 判断是否来自左侧组件库
                const isPaletteDrag = (evt) => {
                    if (evt.dataTransfer && evt.dataTransfer.types) {
                        return Array.from(evt.dataTransfer.types || []).indexOf('application/x-lc-comp') >= 0;
                    }
                    return dragState.draggingFromPalette === true;
                };
                el.addEventListener('dragover', (evt) => {
                    if (isPaletteDrag(evt)) {
                        evt.preventDefault(); // 允许 drop
                        if (evt.dataTransfer) evt.dataTransfer.dropEffect = 'copy';
                        el.classList.add('lc-drop-target');
                        updateDropPlaceholder(el, evt); // 显示插入位置占位线
                    }
                });
                el.addEventListener('dragleave', (evt) => {
                    if (!evt.relatedTarget || !el.contains(evt.relatedTarget)) {
                        el.classList.remove('lc-drop-target');
                        if (dragState.dropTargetInfo && dragState.dropTargetInfo.el === el) clearDropPlaceholder();
                    }
                });
                el.addEventListener('drop', (evt) => {
                    if (!isPaletteDrag(evt)) return;
                    evt.preventDefault();
                    evt.stopPropagation(); // 嵌套容器：只让最深（最内层）容器处理
                    el.classList.remove('lc-drop-target');
                    const parentConfig = binding.instance && binding.instance.props ? binding.instance.props.jsonconfig : null;
                    // 重算插入索引：dragover 冒泡会污染 dragState.dropTargetInfo，drop 时基于当前容器与鼠标位置重算最可靠
                    const insertIndex = computeInsertIndex(el, evt);
                    if (lcDesignerGlobal && parentConfig) lcDesignerGlobal.onPaletteDrop(parentConfig, evt, insertIndex);
                    clearDropPlaceholder();
                });
                // Sortable 1.15.2 原生模式下不自动加 draggable，手动添加
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
            nodePath: { type: String, default: 'root' },
            locked: { type: Boolean, default: false }
        },
        inject: {
            lcDesigner: { default: null },
            lcLocked: { default: null },
            lcCompositeRoot: { default: null }
        },
        provide() {
            return {
                lcLocked: computed(() => this.isLocked),
                // 组合组件根实例：供内部开放容器/节点点击时选中组合组件本身
                lcCompositeRoot: this.isComposite ? this.jsonconfig : null
            };
        },
        template: `
            <div v-if="!validConfig" class="lc-node lc-error" style="padding:8px;color:#f56c6c;font-size:12px;">
                [NDynamicCom] 无效配置: {{ nodePath }}
            </div>
            <div v-else-if="depthExceeded" class="lc-node lc-error" style="padding:8px;color:#f56c6c;font-size:12px;">
                [NDynamicCom] 递归深度超限: {{ nodePath }}
            </div>
            <div v-else class="lc-node"
                 :class="{ 'lc-selected': isSelected, 'lc-container': isContainer, 'lc-design': isDesign, 'lc-composite': isComposite, 'lc-wrapper': hasWrapper, 'lc-locked': isLocked, 'lc-open-slot': isOpenSlot }"
                 @click.stop="onClick"
                 @lc-sort-end="onSortEnd">
                <!-- 开放容器插槽标签条 -->
                <div v-if="isOpenSlot" class="lc-open-slot-tag" @click.stop="onClick">
                    <span class="lc-open-slot-icon">⊕</span>{{ openSlotLabel }}
                    <span v-if="openSlotHint" class="lc-open-slot-hint">{{ openSlotHint }}</span>
                </div>
                <!-- 有 Wrapper：用包装器包裹 -->
                <component v-if="hasWrapper" :is="wrapperComponent"
                           :jsonconfig="jsonconfig.options.wrapperoptions"
                           :parentmodelinfo="parentmodelinfo"
                           :node-path="nodePath + '.wrapper'">
                    <!-- Wrapper 插槽内容 -->
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
            // 父级锁定状态（可能为 boolean 或 computed ref）
            parentLocked() {
                const pl = this.lcLocked;
                if (pl == null) return false;
                return (typeof pl === 'object' && 'value' in pl) ? !!pl.value : !!pl;
            },
            // 锁定：自身 locked 或父级锁定，且不是开放容器
            isLocked() { return (this.locked || this.parentLocked) && !this.jsonconfig?.__unlocked; },
            isOpenSlot() { return !!(this.jsonconfig?.__openSlot); },
            openSlotLabel() { return this.jsonconfig?.__openSlot?.label || ''; },
            openSlotHint() { return this.jsonconfig?.__openSlot?.hint || ''; },
            compositeTree() {
                if (!this.isComposite) return null;
                const config = compositeComponents[this.jsonconfig.component];
                if (!config?.tree) return null;
                const externalProps = this.jsonconfig.options?.comoptions || {};
                const externalSlots = this.jsonconfig.slots || (this.jsonconfig.slots = {});
                // 建立对开放容器外部内容的响应式依赖：拖入组件 push/splice 改变数组 length 时，
                // 触发本 computed 重算 → 画布即时刷新（否则拖入后画布无反应）
                if (config.openContainers) {
                    for (const oc of config.openContainers) {
                        if (externalSlots[oc.key]) void externalSlots[oc.key].length;
                    }
                }
                return applyCompositeProps(config.tree, config, externalProps, externalSlots);
            }
        },
        methods: {
            onClick() {
                // 锁定节点不可选中（点击穿透由 CSS pointer-events 处理，这里兜底）
                if (this.isLocked) return;
                if (!this.lcDesigner?.setCurrentCom || !this.isDesign) return;
                // 开放容器/组合组件内部节点是 compositeTree 临时对象，点击应选中所属组合组件实例
                if (this.isOpenSlot && this.lcCompositeRoot) {
                    this.lcDesigner.setCurrentCom(this.lcCompositeRoot);
                    return;
                }
                this.lcDesigner.setCurrentCom(this.jsonconfig);
            },
            onMouseDown(e) {
                if (!this.isDesign) return;
                if (e.target.closest('.el-form-item__label, .nut-form-item__label, .nut-cell-group-title')) return;
                e.preventDefault();
            },
            onSortEnd(e) {
                const { item, newArrIndex } = e.detail || {};
                const children = this.jsonconfig?.childrenctrls;
                if (!item || !children || !Array.isArray(children)) return;
                const origIdx = children.indexOf(item);
                if (origIdx < 0) return;
                children.splice(origIdx, 1);
                let insertIdx = children.length;
                let safeCount = 0;
                for (let i = 0; i < children.length; i++) {
                    const cc = children[i];
                    if (cc && typeof cc === 'object' && cc.component) {
                        if (safeCount === newArrIndex) { insertIdx = i; break; }
                        safeCount++;
                    }
                }
                children.splice(insertIdx, 0, item);
                console.log('[NDynamicCom] onSortEnd:', children.map(c => c.component));
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
        try {
            app.mount('#designer-app');
        } catch (e) {
            console.error('[Designer] MOUNT ERROR:', e);
            window.__mountErr = (e && e.stack) || String(e);
        }
        // 挂载各独立面板 app（dyn-init：toolbar / breadcrumb 等）
        try {
            if (window.dyn && window.dyn.initAll) window.dyn.initAll();
        } catch (e) {
            console.error('[Designer] initAll ERROR:', e);
            window.__mountErr = (window.__mountErr ? window.__mountErr + ' | ' : '') + ((e && e.stack) || String(e));
        }
    })();

})();
