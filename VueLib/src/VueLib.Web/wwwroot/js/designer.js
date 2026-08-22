/**
 * designer.js - 低代码设计器核心逻辑
 */
(function () {
    'use strict';

    const { createApp, reactive, ref, computed, watch, nextTick, onMounted } = Vue;
    const { ElMessage, ElMessageBox } = ElementPlus;

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
            const activeCategory = ref('form');
            const selectedNode = ref(null);
            const selectedParent = ref(null);
            const selectedPath = ref('');
            const configJsonText = ref('');

            const newPageForm = reactive({ pageName: '', pageCode: '' });

            // 页面配置（根节点）
            const configObj = reactive({
                component: 'NDivContainer',
                modelname: '',
                options: {
                    comoptions: {},
                    comlisteners: {},
                    labeloptions: {},
                    itemoptions: { style: { padding: '12px', background: '#fff' }, class: '' }
                },
                validators: [],
                childrenctrls: [],
                slots: {},
                extendinfo: {}
            });

            // 数据模型
            const modelObj = reactive({});

            // ===== 分类 =====
            const categories = [
                { key: '表单', label: '表单' },
                { key: '布局', label: '布局' },
                { key: '展示', label: '展示' },
                { key: '通用', label: '通用' }
            ];

            const hasOptionField = computed(() => {
                if (!selectedNode.value) return false;
                return ['NRadio', 'NCheckbox', 'NPicker'].includes(selectedNode.value.component);
            });

            const canMoveUp = computed(() => {
                if (!selectedParent.value || !selectedNode.value) return false;
                const idx = selectedParent.value.childrenctrls.indexOf(selectedNode.value);
                return idx > 0;
            });

            const canMoveDown = computed(() => {
                if (!selectedParent.value || !selectedNode.value) return false;
                const idx = selectedParent.value.childrenctrls.indexOf(selectedNode.value);
                return idx >= 0 && idx < selectedParent.value.childrenctrls.length - 1;
            });

            // ===== 工具方法 =====
            function getComponentsByCategory(cat) {
                return componentMetaList.value.filter(c => c.category === cat);
            }

            function deepClone(obj) {
                return JSON.parse(JSON.stringify(obj));
            }

            function findParent(node, root, parent = null) {
                if (node === root) return parent;
                if (root.childrenctrls) {
                    for (const child of root.childrenctrls) {
                        if (child === node) return root;
                        const found = findParent(node, child, root);
                        if (found) return found;
                    }
                }
                return null;
            }

            function findNodePath(node, root, path = 'root') {
                if (node === root) return path;
                if (root.childrenctrls) {
                    for (let i = 0; i < root.childrenctrls.length; i++) {
                        const child = root.childrenctrls[i];
                        const childPath = `${path}.childrenctrls[${i}]`;
                        if (child === node) return childPath;
                        const found = findNodePath(node, child, childPath);
                        if (found) return found;
                    }
                }
                return '';
            }

            // ===== 组件操作 =====
            function addComponent(meta) {
                const defaultConfig = JSON.parse(meta.defaultConfigJson || '{}');
                const target = selectedNode.value && isContainer(selectedNode.value)
                    ? selectedNode.value
                    : configObj;

                if (!target.childrenctrls) target.childrenctrls = [];
                target.childrenctrls.push(deepClone(defaultConfig));

                // 选中新添加的组件
                const newNode = target.childrenctrls[target.childrenctrls.length - 1];
                selectNode(newNode);

                ElMessage.success(`已添加: ${meta.label}`);
                syncPreview();
            }

            function isContainer(node) {
                return ['NForm', 'NCellGroup', 'NDivContainer', 'NGrid'].includes(node.component);
            }

            function selectNode(node) {
                selectedNode.value = node;
                selectedParent.value = findParent(node, configObj);
                selectedPath.value = findNodePath(node, configObj);
            }

            function deleteNode() {
                if (!selectedNode.value || !selectedParent.value) {
                    ElMessage.warning('请先选中要删除的组件');
                    return;
                }
                const idx = selectedParent.value.childrenctrls.indexOf(selectedNode.value);
                if (idx >= 0) {
                    selectedParent.value.childrenctrls.splice(idx, 1);
                    selectedNode.value = null;
                    selectedParent.value = null;
                    selectedPath.value = '';
                    syncPreview();
                }
            }

            function moveUp() {
                if (!canMoveUp.value) return;
                const arr = selectedParent.value.childrenctrls;
                const idx = arr.indexOf(selectedNode.value);
                [arr[idx - 1], arr[idx]] = [arr[idx], arr[idx - 1]];
                syncPreview();
            }

            function moveDown() {
                if (!canMoveDown.value) return;
                const arr = selectedParent.value.childrenctrls;
                const idx = arr.indexOf(selectedNode.value);
                [arr[idx + 1], arr[idx]] = [arr[idx], arr[idx + 1]];
                syncPreview();
            }

            // ===== 验证器 =====
            function addValidator() {
                if (!selectedNode.value) return;
                if (!selectedNode.value.validators) selectedNode.value.validators = [];
                selectedNode.value.validators.push({ type: 'required', message: '此字段必填' });
            }

            function removeValidator(idx) {
                if (!selectedNode.value) return;
                selectedNode.value.validators.splice(idx, 1);
            }

            function needsValue(type) {
                return ['minLength', 'maxLength', 'min', 'max', 'pattern'].includes(type);
            }

            function toggleRequired(val) {
                if (!selectedNode.value) return;
                if (!selectedNode.value.validators) selectedNode.value.validators = [];
                const idx = selectedNode.value.validators.findIndex(v => v.type === 'required');
                if (val && idx < 0) {
                    selectedNode.value.validators.unshift({ type: 'required', message: '此字段必填' });
                } else if (!val && idx >= 0) {
                    selectedNode.value.validators.splice(idx, 1);
                }
            }

            // ===== 预览同步 =====
            let previewFrame = null;
            let previewReady = false;

            function syncPreview() {
                if (!previewFrame || !previewReady) {
                    console.log('[Designer] syncPreview skipped, previewFrame=', !!previewFrame, 'previewReady=', previewReady);
                    return;
                }
                const payload = {
                    type: 'designer-update',
                    config: deepClone(configObj),
                    model: deepClone(modelObj)
                };
                console.log('[Designer] 发送配置到预览, root component:', configObj.component, 'children:', (configObj.childrenctrls || []).length);
                previewFrame.contentWindow.postMessage(payload, '*');
            }

            function validateForm() {
                if (!previewFrame || !previewReady) {
                    ElMessage.warning('预览未就绪');
                    return;
                }
                previewFrame.contentWindow.postMessage({ type: 'designer-validate' }, '*');
            }

            // ===== 页面管理 =====
            async function loadComponentMeta() {
                try {
                    const resp = await fetch('/api/lowcode/components');
                    const result = await resp.json();
                    if (result.success) {
                        componentMetaList.value = result.data;
                    }
                } catch (e) {
                    ElMessage.error('加载组件元数据失败: ' + e.message);
                }
            }

            async function loadPageList() {
                try {
                    const resp = await fetch('/api/lowcode/pages');
                    const result = await resp.json();
                    if (result.success) {
                        pageList.value = result.data;
                    }
                } catch (e) {
                    ElMessage.error('加载页面列表失败: ' + e.message);
                }
            }

            async function loadPage(code) {
                if (!code) return;
                try {
                    const resp = await fetch(`/api/lowcode/page/${code}`);
                    const result = await resp.json();
                    if (result.success && result.data) {
                        const page = result.data;
                        currentPageId.value = page.id;
                        // 替换 configObj 内容
                        const config = JSON.parse(page.configJson || '{}');
                        Object.keys(configObj).forEach(k => delete configObj[k]);
                        Object.assign(configObj, config);
                        // 替换 modelObj
                        const model = JSON.parse(page.defaultModelJson || '{}');
                        Object.keys(modelObj).forEach(k => delete modelObj[k]);
                        Object.assign(modelObj, model);
                        selectedNode.value = null;
                        selectedParent.value = null;
                        ElMessage.success(`已加载页面: ${page.pageName}`);
                        nextTick(syncPreview);
                    }
                } catch (e) {
                    ElMessage.error('加载页面失败: ' + e.message);
                }
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
                // 清空配置
                Object.keys(configObj).forEach(k => delete configObj[k]);
                Object.assign(configObj, {
                    component: 'NDivContainer',
                    modelname: '',
                    options: { comoptions: {}, comlisteners: {}, labeloptions: {}, itemoptions: { style: { padding: '12px', background: '#fff' }, class: '' } },
                    validators: [], childrenctrls: [], slots: {}, extendinfo: {}
                });
                Object.keys(modelObj).forEach(k => delete modelObj[k]);
                currentPageId.value = null;
                currentPageCode.value = newPageForm.pageCode;
                showNewPage.value = false;
                selectedNode.value = null;
                ElMessage.success('已创建新页面');
                nextTick(syncPreview);
            }

            async function savePage() {
                if (!currentPageCode.value) {
                    ElMessage.warning('请先选择或新建页面');
                    return;
                }
                saving.value = true;
                try {
                    const pageData = {
                        id: currentPageId.value || 0,
                        pageName: pageList.value.find(p => p.pageCode === currentPageCode.value)?.pageName || currentPageCode.value,
                        pageCode: currentPageCode.value,
                        configJson: JSON.stringify(configObj),
                        defaultModelJson: JSON.stringify(modelObj),
                        isEnabled: true,
                        sortOrder: 0
                    };
                    const resp = await fetch('/api/lowcode/page', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(pageData)
                    });
                    const result = await resp.json();
                    if (result.success) {
                        currentPageId.value = result.data;
                        ElMessage.success('保存成功');
                        loadPageList();
                    } else {
                        ElMessage.error('保存失败: ' + result.message);
                    }
                } catch (e) {
                    ElMessage.error('保存失败: ' + e.message);
                } finally {
                    saving.value = false;
                }
            }

            function openPreview() {
                window.open(`/designer/preview?code=${currentPageCode.value}`, '_blank');
            }

            // ===== JSON 编辑 =====
            watch(showJson, (val) => {
                if (val) {
                    configJsonText.value = JSON.stringify(configObj, null, 2);
                }
            });

            function applyJson() {
                try {
                    const config = JSON.parse(configJsonText.value);
                    Object.keys(configObj).forEach(k => delete configObj[k]);
                    Object.assign(configObj, config);
                    ElMessage.success('JSON 已应用');
                    showJson.value = false;
                    syncPreview();
                } catch (e) {
                    ElMessage.error('JSON 格式错误: ' + e.message);
                }
            }

            // ===== 监听配置变化，同步预览 =====
            watch(configObj, () => {
                syncPreview();
            }, { deep: true });

            // ===== 消息监听 =====
            window.addEventListener('message', (event) => {
                const data = event.data;
                if (!data || !data.type) return;
                if (data.type === 'preview-loaded') {
                    previewReady = true;
                    syncPreview();
                } else if (data.type === 'preview-ready') {
                    previewReady = true;
                } else if (data.type === 'validate-result') {
                    if (data.data && data.data.valid) {
                        ElMessage.success('验证通过');
                    } else {
                        const msgs = Object.values(data.data?.errors || {}).flat().join('; ');
                        ElMessage.error('验证失败: ' + (msgs || '未知错误'));
                    }
                }
            });

            // ===== 初始化 =====
            onMounted(async () => {
                previewFrame = document.querySelector('.preview-iframe');
                await Promise.all([loadComponentMeta(), loadPageList()]);
                // 默认加载第一个页面
                if (pageList.value.length > 0) {
                    currentPageCode.value = pageList.value[0].pageCode;
                    loadPage(pageList.value[0].pageCode);
                }
            });

            return {
                componentMetaList, pageList, currentPageCode, saving, showJson, showNewPage,
                activeCategory, selectedNode, selectedPath, configJsonText, newPageForm,
                categories, hasOptionField, canMoveUp, canMoveDown,
                getComponentsByCategory, addComponent, selectNode, deleteNode, moveUp, moveDown,
                addValidator, removeValidator, needsValue, toggleRequired,
                loadPage, newPage, confirmNewPage, savePage, openPreview, validateForm,
                applyJson
            };
        }
    });

    // 注册 Element Plus 和图标
    app.use(ElementPlus);
    if (window.ElementPlusIconsVue) {
        for (const [key, comp] of Object.entries(window.ElementPlusIconsVue)) {
            app.component(key, comp);
        }
    }
    app.mount('#designer-app');

})();
