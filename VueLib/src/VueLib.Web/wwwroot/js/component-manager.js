/**
 * component-manager.js - 组件管理后台逻辑
 * 组件列表、基本信息编辑、属性配置可视化编辑器
 */
(function () {
    'use strict';

    const { createApp, reactive, ref, computed, watch, onMounted } = Vue;

    const app = createApp({
        setup() {
            const componentList = ref([]);
            const current = ref(null);
            const searchKeyword = ref('');
            const activeTab = ref('basic');
            const saving = ref(false);
            const showPropertyJson = ref(false);

            // 组合组件相关
            const compositeTab = ref('tree');
            const compositeTreeText = ref('');
            const compositeTreeError = ref('');
            const exposedProps = ref([]);
            const compositeConfig = reactive({ tree: null, exposedProps: [] });

            // 属性配置（响应式对象，编辑时直接操作）
            const propertyConfig = reactive({ groups: [] });

            // 属性配置 JSON 文本（只读预览）
            const propertyConfigText = computed(() => {
                return JSON.stringify(propertyConfig, null, 2);
            });

            // 过滤后的组件列表
            const filteredList = computed(() => {
                if (!searchKeyword.value) return componentList.value;
                const kw = searchKeyword.value.toLowerCase();
                return componentList.value.filter(c =>
                    (c.componentName || '').toLowerCase().includes(kw) ||
                    (c.label || '').toLowerCase().includes(kw) ||
                    (c.category || '').includes(kw)
                );
            });

            // 分类对应的 tag 类型
            function categoryType(cat) {
                const map = { '表单': '', '布局': 'success', '展示': 'warning', '通用': 'info', '移动端': 'danger' };
                return map[cat] || '';
            }

            // ===== 加载组件列表 =====
            async function loadList() {
                try {
                    const resp = await fetch('/api/lowcode/components/all');
                    const result = await resp.json();
                    if (result.success) {
                        componentList.value = result.data || [];
                    }
                } catch (e) {
                    console.error('加载组件列表失败:', e);
                    ElementPlus.ElMessage.error('加载组件列表失败');
                }
            }

            // ===== 选择组件 =====
            function selectComponent(item) {
                // 深拷贝，避免直接修改列表中的对象
                current.value = JSON.parse(JSON.stringify(item));
                activeTab.value = 'basic';
                // 解析属性配置
                parsePropertyConfig();
                // 解析组合配置
                parseCompositeConfig();
            }

            // ===== 解析属性配置 JSON =====
            function parsePropertyConfig() {
                propertyConfig.groups = [];
                if (current.value?.propertyConfigJson) {
                    try {
                        const parsed = JSON.parse(current.value.propertyConfigJson);
                        if (parsed.groups && Array.isArray(parsed.groups)) {
                            propertyConfig.groups = parsed.groups;
                        }
                    } catch (e) {
                        console.error('解析属性配置失败:', e);
                        ElementPlus.ElMessage.warning('属性配置 JSON 解析失败，已重置为空');
                    }
                }
            }

            // ===== 解析组合配置 =====
            function parseCompositeConfig() {
                compositeConfig.tree = null;
                compositeConfig.exposedProps = [];
                exposedProps.value = [];
                compositeTreeText.value = '';
                compositeTreeError.value = '';

                if (current.value?.compositeConfigJson) {
                    try {
                        const parsed = JSON.parse(current.value.compositeConfigJson);
                        if (parsed.tree) {
                            compositeConfig.tree = parsed.tree;
                            compositeTreeText.value = JSON.stringify(parsed.tree, null, 2);
                        }
                        if (parsed.exposedProps && Array.isArray(parsed.exposedProps)) {
                            exposedProps.value = parsed.exposedProps;
                            compositeConfig.exposedProps = parsed.exposedProps;
                        }
                    } catch (e) {
                        console.error('解析组合配置失败:', e);
                        ElementPlus.ElMessage.warning('组合配置 JSON 解析失败');
                    }
                }
            }

            // 解析内部组件树（从文本框）
            function parseCompositeTree() {
                compositeTreeError.value = '';
                if (!compositeTreeText.value.trim()) {
                    compositeConfig.tree = null;
                    return;
                }
                try {
                    compositeConfig.tree = JSON.parse(compositeTreeText.value);
                } catch (e) {
                    compositeTreeError.value = 'JSON 格式错误: ' + e.message;
                }
            }

            // 格式化内部组件树
            function formatCompositeTree() {
                try {
                    const obj = JSON.parse(compositeTreeText.value);
                    compositeTreeText.value = JSON.stringify(obj, null, 2);
                    compositeConfig.tree = obj;
                    compositeTreeError.value = '';
                } catch (e) {
                    compositeTreeError.value = 'JSON 格式错误: ' + e.message;
                }
            }

            // 从设计器导入（提示用户）
            function loadFromDesigner() {
                ElementPlus.ElMessageBox.alert(
                    '请打开 /designer 页面，设计好组件后点击"JSON"开关，复制配置 JSON，然后粘贴到左侧文本框中。',
                    '从设计器导入',
                    { confirmButtonText: '我知道了' }
                );
            }

            // 添加开放属性
            function addExposedProp() {
                exposedProps.value.push({
                    key: '',
                    label: '',
                    type: 'input',
                    target: '',
                    default: ''
                });
            }

            // 添加开放属性选项
            function addExposedOption(prop) {
                if (!prop.options) prop.options = [];
                prop.options.push({ label: '选项' + (prop.options.length + 1), value: 'value' + (prop.options.length + 1) });
            }

            // ===== 新增组件 =====
            function addComponent() {
                current.value = {
                    id: 0,
                    componentName: '',
                    componentType: 1,
                    category: '表单',
                    label: '',
                    icon: '',
                    defaultConfigJson: '',
                    defaultOptionsJson: '',
                    propertyConfigJson: '',
                    loadUrl: '',
                    description: '',
                    isEnabled: true,
                    isComposite: false,
                    compositeConfigJson: '',
                    uiLibrary: '',
                    customScriptJson: '',
                    sortOrder: 0
                };
                propertyConfig.groups = [];
                compositeConfig.tree = null;
                compositeConfig.exposedProps = [];
                exposedProps.value = [];
                compositeTreeText.value = '';
                activeTab.value = 'basic';
            }

            // ===== 重置/取消 =====
            function resetCurrent() {
                current.value = null;
                propertyConfig.groups = [];
                compositeConfig.tree = null;
                compositeConfig.exposedProps = [];
                exposedProps.value = [];
                compositeTreeText.value = '';
            }

            // ===== 保存组件 =====
            async function saveComponent() {
                if (!current.value) return;
                if (!current.value.componentName) {
                    ElementPlus.ElMessage.warning('请输入组件名');
                    activeTab.value = 'basic';
                    return;
                }
                if (!current.value.loadUrl) {
                    ElementPlus.ElMessage.warning('请输入加载URL');
                    activeTab.value = 'basic';
                    return;
                }

                // 序列化属性配置
                current.value.propertyConfigJson = JSON.stringify(propertyConfig);

                // 序列化组合配置
                if (current.value.isComposite) {
                    // 确保内部树已解析
                    if (compositeTreeText.value.trim()) {
                        try {
                            compositeConfig.tree = JSON.parse(compositeTreeText.value);
                        } catch (e) {
                            ElementPlus.ElMessage.error('内部组件树 JSON 格式错误');
                            activeTab.value = 'composite';
                            compositeTab.value = 'tree';
                            return;
                        }
                    }
                    current.value.compositeConfigJson = JSON.stringify({
                        tree: compositeConfig.tree,
                        exposedProps: exposedProps.value
                    });
                } else {
                    current.value.compositeConfigJson = null;
                }

                saving.value = true;
                try {
                    const resp = await fetch('/api/lowcode/component', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(current.value)
                    });
                    const result = await resp.json();
                    if (result.success) {
                        ElementPlus.ElMessage.success('保存成功');
                        await loadList();
                        // 如果是新增，重新加载选中的组件
                        if (current.value.id === 0) {
                            const saved = componentList.value.find(c => c.componentName === current.value.componentName);
                            if (saved) selectComponent(saved);
                        }
                    } else {
                        ElementPlus.ElMessage.error(result.message || '保存失败');
                    }
                } catch (e) {
                    console.error('保存失败:', e);
                    ElementPlus.ElMessage.error('保存失败: ' + e.message);
                } finally {
                    saving.value = false;
                }
            }

            // ===== 属性配置：分组操作 =====
            function addPropertyGroup() {
                propertyConfig.groups.push({
                    title: '新分组',
                    fields: []
                });
            }

            function removePropertyGroup(gi) {
                ElementPlus.ElMessageBox.confirm('确定删除该分组吗？', '提示', { type: 'warning' })
                    .then(() => {
                        propertyConfig.groups.splice(gi, 1);
                    }).catch(() => {});
            }

            // ===== 属性配置：字段操作 =====
            function addPropertyField(gi) {
                propertyConfig.groups[gi].fields.push({
                    key: '',
                    label: '',
                    type: 'input',
                    default: ''
                });
            }

            function removePropertyField(gi, fi) {
                propertyConfig.groups[gi].fields.splice(fi, 1);
            }

            // 字段类型变化时初始化默认值
            function onFieldTypeChange(field) {
                if (field.type === 'switch') {
                    field.default = false;
                } else if (field.type === 'number' || field.type === 'slider') {
                    field.default = 0;
                    if (!field.min) field.min = 0;
                    if (!field.max) field.max = 100;
                    if (!field.step) field.step = 1;
                } else if (['select', 'radio', 'checkbox'].includes(field.type)) {
                    field.default = '';
                    if (!field.options) field.options = [{ label: '选项1', value: 'value1' }];
                } else {
                    field.default = '';
                }
            }

            // 添加选项
            function addOption(field) {
                if (!field.options) field.options = [];
                field.options.push({ label: '新选项', value: 'value' + (field.options.length + 1) });
            }

            onMounted(() => {
                loadList();
            });

            return {
                componentList, filteredList, current, searchKeyword,
                activeTab, saving, showPropertyJson,
                propertyConfig, propertyConfigText,
                compositeTab, compositeTreeText, compositeTreeError, exposedProps, compositeConfig,
                categoryType,
                loadList, selectComponent, addComponent, resetCurrent, saveComponent,
                addPropertyGroup, removePropertyGroup,
                addPropertyField, removePropertyField, onFieldTypeChange, addOption,
                parseCompositeTree, formatCompositeTree, loadFromDesigner,
                addExposedProp, addExposedOption
            };
        }
    });

    app.use(ElementPlus);
    if (window.ElementPlusIconsVue) {
        for (const [key, comp] of Object.entries(window.ElementPlusIconsVue)) {
            app.component(key, comp);
        }
    }
    app.mount('#app');

    console.log('[ComponentManager] 组件管理页面已初始化');
})();
