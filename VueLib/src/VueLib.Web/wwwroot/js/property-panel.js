/* ============================================================
 * VueLib 低代码平台 - 动态属性面板
 * 根据 PropertyConfigJson 动态生成 DynElement Plus 表单
 * 支持: input/number/switch/select/textarea/color/slider/radio/checkbox/icon
 * ============================================================ */
(function () {
    'use strict';

    // ===== 工具函数 =====
    function getByPath(obj, path) {
        if (!obj || !path) return undefined;
        return path.split('.').reduce(function (o, k) {
            return (o == null) ? undefined : o[k];
        }, obj);
    }

    function setByPath(obj, path, value) {
        if (!obj || !path) return;
        var keys = path.split('.');
        var last = keys.pop();
        var target = keys.reduce(function (o, k) {
            if (o[k] == null || typeof o[k] !== 'object') o[k] = {};
            return o[k];
        }, obj);
        target[last] = value;
    }

    // ===== 动态属性面板组件（用 template 更可靠）=====
    var DynamicPropertyPanel = {
        name: 'DynamicPropertyPanel',
        props: {
            propertyConfig: { type: Object, default: function () { return { groups: [] }; } },
            componentConfig: { type: Object, required: true },
            componentName: { type: String, default: '' }
        },
        emits: ['update', 'reset'],
        data: function () {
            return {
                activeGroups: []
            };
        },
        computed: {
            groups: function () {
                var baseGroups = (this.propertyConfig && this.propertyConfig.groups) || [];
                // 自动添加数据绑定分组（所有组件通用）
                var bindingGroup = {
                    title: '数据绑定',
                    fields: [
                        { key: 'modelname', label: '绑定字段名', type: 'input', default: '', placeholder: '如: user.name' }
                    ]
                };
                return [bindingGroup].concat(baseGroups);
            }
        },
        watch: {
            groups: {
                immediate: true,
                handler: function (val) {
                    this.activeGroups = val.map(function (g) { return g.title; });
                }
            }
        },
        methods: {
            getFieldValue: function (field) {
                var val = getByPath(this.componentConfig, field.key);
                return (val === undefined) ? field.default : val;
            },
            setFieldValue: function (field, val) {
                setByPath(this.componentConfig, field.key, val);
                this.$emit('update', field.key, val, this.componentConfig);
            },
            needsValue: function (type) {
                return ['minLength', 'maxLength', 'min', 'max', 'pattern'].indexOf(type) >= 0;
            }
        },
        template: `
            <div class="dynamic-property-panel">
                <el-empty v-if="!groups.length" description="该组件暂无可配置属性" :image-size="60" />
                <el-collapse v-else v-model="activeGroups" accordion="false" style="border:none">
                    <el-collapse-item v-for="group in groups" :key="group.title" :title="group.title" :name="group.title">
                        <div class="pp-fields">
                            <div v-for="field in group.fields" :key="field.key" class="pp-field">
                                <label class="pp-label">{{ field.label }}</label>
                                <div class="pp-control">
                                    <!-- input -->
                                    <el-input v-if="field.type === 'input'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :placeholder="field.placeholder || field.label"
                                        clearable size="small" style="width:100%" />

                                    <!-- number -->
                                    <el-input-number v-else-if="field.type === 'number'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :min="field.min" :max="field.max" :step="field.step || 1"
                                        controls-position="right" size="small" />

                                    <!-- switch -->
                                    <el-switch v-else-if="field.type === 'switch'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :active-text="field.activeText" :inactive-text="field.inactiveText" />

                                    <!-- select -->
                                    <el-select v-else-if="field.type === 'select'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :placeholder="field.placeholder || '请选择'"
                                        filterable clearable size="small" style="width:100%">
                                        <el-option v-for="opt in field.options" :key="opt.value"
                                            :label="opt.label" :value="opt.value" :disabled="opt.disabled" />
                                    </el-select>

                                    <!-- textarea -->
                                    <el-input v-else-if="field.type === 'textarea'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        type="textarea" :rows="field.rows || 3"
                                        :placeholder="field.placeholder || field.label"
                                        clearable size="small" style="width:100%" />

                                    <!-- color -->
                                    <el-color-picker v-else-if="field.type === 'color'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :show-alpha="field.showAlpha !== false" />

                                    <!-- slider -->
                                    <el-slider v-else-if="field.type === 'slider'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :min="field.min || 0" :max="field.max || 100" :step="field.step || 1"
                                        :show-input="field.showInput" />

                                    <!-- radio -->
                                    <el-radio-group v-else-if="field.type === 'radio'"
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)" size="small">
                                        <el-radio v-for="opt in field.options" :key="opt.value"
                                            :label="opt.value" :disabled="opt.disabled">{{ opt.label }}</el-radio>
                                    </el-radio-group>

                                    <!-- checkbox -->
                                    <el-checkbox-group v-else-if="field.type === 'checkbox'"
                                        :model-value="getFieldValue(field) || []"
                                        @update:model-value="setFieldValue(field, $event)" size="small">
                                        <el-checkbox v-for="opt in field.options" :key="opt.value"
                                            :label="opt.value" :disabled="opt.disabled">{{ opt.label }}</el-checkbox>
                                    </el-checkbox-group>

                                    <!-- fallback -->
                                    <el-input v-else
                                        :model-value="getFieldValue(field)"
                                        @update:model-value="setFieldValue(field, $event)"
                                        :placeholder="field.placeholder || field.label"
                                        clearable size="small" style="width:100%" />
                                </div>
                            </div>
                        </div>
                    </el-collapse-item>
                </el-collapse>
            </div>
        `
    };

    // 暴露到全局
    window.DynamicPropertyPanel = DynamicPropertyPanel;
    window.ppGetByPath = getByPath;
    window.ppSetByPath = setByPath;

    console.log('[property-panel] 动态属性面板已加载');
})();
