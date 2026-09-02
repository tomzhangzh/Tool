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

    // 深拷贝工具
    function deepClone(obj) {
        if (window._) return window._.cloneDeep(obj);
        return JSON.parse(JSON.stringify(obj));
    }

    // 按路径取值（点号路径）
    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce(function (o, k) { return (o == null) ? undefined : o[k]; }, obj);
    }

    // 应用组合组件的外部属性 + 开放容器到内部树
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

    // 手动按路径设置值（lodash 不可用时）
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
            const treeVersion = ref(0);

            // ===== 分类 =====
            const categories = [
                { key: '表单', label: '表单' },
                { key: '布局', label: '布局' },
                { key: '展示', label: '展示' },
                { key: '通用', label: '通用' },
                { key: '组合', label: '组合' }
            ];

            // ===== 保存为组合组件 =====
            const showCompositeDialog = ref(false);
            const compositeForm = reactive({
                componentName: '', label: '', icon: '📦', source: 'selected', description: '',
                exposedProps: [], openContainers: []
            });

            // ===== 开放配置模式（所见即所得标记开放属性/容器）=====
            const openConfigMode = ref(false);
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
            const filteredComponents = computed(() => {
                return componentMetaList.value.filter(c => {
                    const isComp = !!(c.isComposite || c.IsComposite);
                    const lib = c.uiLibrary || c.UiLibrary || 'nutui';
                    const libMatch = activeUiLibrary.value === 'all' || lib === activeUiLibrary.value;
                    // 组合组件独立分类显示，不出现在其他分类
                    if (activeCategory.value === '组合') return isComp;
                    if (isComp) return false;
                    const catMatch = (c.category || c.Category) === activeCategory.value;
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
            // 防重说明：画布容器会嵌套（如 Form ⊃ CellGroup），从左侧拖入组件时 drop 事件
            // 可能同时触发多个容器的 onAdd，导致"表单、布局等多个容器都添加组件"。
            // 这里统一走"延迟提交 + 最深（最内层）容器优先"，同一拖拽会话只添加一次到真正的目标容器。
            let dragSessionId = 0;     // 拖拽会话计数（每次从左侧拖入自增）
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
                console.log('[Designer] onContainerDragAdd:', evt.newIndex, currentMenuCom?.componentName, 'session', dragSessionId);
                if (!currentMenuCom || !parentConfig?.childrenctrls) return;
                if (parentConfig.__locked) return; // 组合内部锁定容器禁止拖入（组合组件除非开放容器）
                // 创建正确的配置
                let config = {};
                try {
                    config = JSON.parse(currentMenuCom.defaultConfigJson || currentMenuCom.DefaultConfigJson || '{}');
                } catch (e) {
                    config = { component: currentMenuCom.componentName || currentMenuCom.ComponentName, childrenctrls: [] };
                }
                const newConfig = deepClone(config);
                // 同一拖拽会话内嵌套容器可能多次回调：只保留 drop 目标 DOM 最深（最内层）的容器，
                // 更浅（父级）容器回调直接忽略，避免重复添加。
                const depth = evt && evt.to ? depthOfDom(evt.to) : 0;
                if (pendingAdd && pendingAdd.session === dragSessionId && pendingAdd.depth > depth) {
                    return; // 已有更深的目标容器，父级忽略
                }
                if (pendingAdd && pendingAdd.timer) clearTimeout(pendingAdd.timer);
                pendingAdd = { parentConfig, newConfig, newIndex: evt?.newIndex ?? null, depth, session: dragSessionId };
                pendingAdd.timer = setTimeout(commitPendingAdd, 20);
            }
            function onContainerDragEnd() {
                currentMenuCom = null;
                if (pendingAdd && pendingAdd.timer) clearTimeout(pendingAdd.timer);
                pendingAdd = null;
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
                dragSessionId++;
                currentMenuCom = filteredComponents.value[evt.oldIndex];
            }

            function onPaletteDragEnd() {
                // 不立即清空，等容器的 onAdd 处理完
                setTimeout(() => { currentMenuCom = null; }, 100);
            }

            // 左侧组件库拖拽配置（v-draggable 指令用）
            // paletteStartSet: 记录拖拽前 palette 的 DOM 集合，拖拽结束后删除拖拽期间新插入的残留 clone
            //（Sortable pull:clone 拖到画布后不自动清理源里的 clone，若不清理，切换分类时残留项会出现在各分类首位）
            let paletteStartSet = null;
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
                    paletteStartSet = new Set(Array.from(evt.from.children));
                    currentMenuCom = filteredComponents.value[evt.oldIndex];
                },
                onEnd() {
                    console.log('[Designer] palette drag end');
                    setTimeout(() => {
                        // 清理 palette 中拖拽期间新插入的残留 clone（不在起始 DOM 集合里的 component-item）
                        const grid = document.querySelector('.component-grid');
                        if (grid && paletteStartSet) {
                            Array.from(grid.children).forEach(el => {
                                if (el.classList && el.classList.contains('component-item') && !paletteStartSet.has(el)) {
                                    el.parentNode && el.parentNode.removeChild(el);
                                }
                            });
                        }
                        paletteStartSet = null;
                        currentMenuCom = null;
                    }, 200);
                }
            }));

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
                activeCategory, activeUiLibrary, leftTab, configJsonText, newPageForm, designMode, treeFilter,
                currentCom, currentContainer, currentPath, breadcrumbList,
                configObj, modelObj, comListRef, treeRef, treeVersion,
                categories, hasOptionField, canMoveUp, canMoveDown,
                currentPropertyConfig, modelJsonText, onPropertyUpdate, showJsonEditor, showModelData, copyModelJson,
                filteredComponents, paletteDragOptions, selectFromTree, setCurrentCom,
                showCompositeDialog, compositeForm, openCompositeDialog, saveAsComposite,
                onCompositeSourceChange,
                openConfigMode, currentComOpenMap, currentComOpenContainer, toggleOpenProp,
                toggleOpenContainer, openSummary, removeOpenItem, isContainerComp,
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
                        // 从左侧面板(palette)拖入画布容器时，删除 Sortable 自动插入的克隆 DOM（避免图标和组件重复）。
                        // 注意：palette 自身的 pull:clone 也会触发 onAdd（evt.to 就是 palette），此时不能删 clone，
                        // 否则会干扰 Sortable 的 clone 生命周期，导致拖拽结束后源容器残留克隆 DOM，切分类时残留项出现在各分类首位。
                        const fromIsPalette = evt.from && evt.from.classList && evt.from.classList.contains('component-grid');
                        const toIsPalette = evt.to && evt.to.classList && evt.to.classList.contains('component-grid');
                        if (fromIsPalette && !toIsPalette) {
                            if (evt.item && evt.item.parentNode) {
                                evt.item.parentNode.removeChild(evt.item);
                            }
                        }
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
        app.mount('#designer-app');
    })();

})();
