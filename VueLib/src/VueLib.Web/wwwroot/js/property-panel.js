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
            componentName: { type: String, default: '' },
            openConfigMode: { type: Boolean, default: false },
            openMap: { type: Object, default: function () { return {}; } }
        },
        emits: ['update', 'reset', 'toggle-open'],
        data: function () {
            return {
                activeGroups: []
            };
        },
        computed: {
            groups: function () {
                var baseGroups = (this.propertyConfig && this.propertyConfig.groups) || [];
                // 兼容：支持 group.name 和 group.title
                var normalizedGroups = baseGroups.map(function (g) {
                    return {
                        title: g.title || g.name || '未命名分组',
                        fields: (g.fields || []).map(function (f) {
                            // 自动补全字段路径：如果 key 不包含点号，默认添加 options.comoptions. 前缀
                            // 特殊字段：modelname 保持根级别
                            var key = f.key || '';
                            // at 前缀字面键（@@/@ = 开放属性/开放容器）不补 options.comoptions. 前缀，
                            // 否则 @@username 会被误改为 options.comoptions.@@username 导致读写错位
                            if (key && key.indexOf('@') !== 0 && key.indexOf('.') < 0 && key !== 'modelname') {
                                key = 'options.comoptions.' + key;
                            }
                            return Object.assign({}, f, { key: key });
                        })
                    };
                });
                // 自动添加数据绑定分组（所有组件通用）
                var bindingGroup = {
                    title: '数据绑定',
                    fields: [
                        { key: 'modelname', label: '绑定字段名', type: 'input', default: '', placeholder: '如: user.name' }
                    ]
                };
                return [bindingGroup].concat(normalizedGroups);
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
            // 字面键前缀长度：@@ 双前缀（官方）返回 2，@ 单前缀（历史/手动数据兼容）返回 1，非字面键返回 0
            literalPrefixLen: function (k) {
                if (typeof k !== 'string') return 0;
                if (k.indexOf('@@') === 0) return 2;
                if (k.indexOf('@') === 0) return 1;
                return 0;
            },
            isLiteralKey: function (k) {
                return this.literalPrefixLen(k) > 0;
            },
            getFieldValue: function (field) {
                var pl = this.literalPrefixLen(field.key);
                // @@ / @ 字面键：开放属性存 comoptions[完整路径]，开放容器存 slots[路径]（键含点号/方括号，不能按点号路径解析）
                if (pl > 0) {
                    var body = field.key.slice(pl);
                    if (body.indexOf('slots:') === 0) {
                        var sk = body.slice(6);
                        var sl = (this.componentConfig && this.componentConfig.slots) || {};
                        return sl[sk];
                    }
                    var co = (this.componentConfig && this.componentConfig.options && this.componentConfig.options.comoptions) || {};
                    return co[body] !== undefined ? co[body] : field.default;
                }
                var val = getByPath(this.componentConfig, field.key);
                return (val === undefined) ? field.default : val;
            },
            setFieldValue: function (field, val) {
                var pl = this.literalPrefixLen(field.key);
                if (pl > 0) {
                    var body = field.key.slice(pl);
                    if (body.indexOf('slots:') === 0) {
                        var sk = body.slice(6);
                        if (!this.componentConfig.slots) this.componentConfig.slots = {};
                        this.componentConfig.slots[sk] = val;
                    } else {
                        if (!this.componentConfig.options.comoptions) this.componentConfig.options.comoptions = {};
                        this.componentConfig.options.comoptions[body] = val;
                    }
                    this.$emit('update', field.key, val, this.componentConfig);
                    return;
                }
                setByPath(this.componentConfig, field.key, val);
                this.$emit('update', field.key, val, this.componentConfig);
            },
            needsValue: function (type) {
                return ['minLength', 'maxLength', 'min', 'max', 'pattern'].indexOf(type) >= 0;
            },
            onToggleOpen: function (field, val) {
                this.$emit('toggle-open', field.key, val, this.componentConfig);
            }
        },
        template: `
            <div class="dynamic-property-panel">
                <el-empty v-if="!groups.length" description="该组件暂无可配置属性" :image-size="60" />
                <el-collapse v-else v-model="activeGroups" accordion="false" style="border:none">
                    <el-collapse-item v-for="group in groups" :key="group.title" :title="group.title" :name="group.title">
                        <div class="pp-fields">
                            <div v-for="field in group.fields" :key="field.key" class="pp-field">
                                <div class="pp-field-head">
                                    <label class="pp-label">{{ field.label }}</label>
                                    <el-switch v-if="openConfigMode && field.type !== 'slot'"
                                        class="pp-open-switch"
                                        :model-value="!!(openMap && openMap[field.key])"
                                        @update:model-value="onToggleOpen(field, $event)"
                                        size="small" inline-prompt active-text="开" inactive-text="开" />
                                    <span v-if="openConfigMode && field.type !== 'slot'" class="pp-open-tip">{{ !!(openMap && openMap[field.key]) ? '已开放' : '开放' }}</span>
                                </div>
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

                                    <!-- slot: 开放容器（只读提示） -->
                                    <div v-else-if="field.type === 'slot'" class="pp-slot-tip">
                                        <el-tag size="small" type="success" style="margin-right:4px;">⊕ 插槽</el-tag>
                                        <span style="font-size:12px;color:#909399;">{{ field.hint || '从左侧拖入组件到画布中该绿色区域' }}（当前 {{ (getFieldValue(field)||[]).length }} 个组件）</span>
                                    </div>

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
