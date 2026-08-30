/* ============================================================
 * VueLib 低代码平台 - 模板参数动态表单
 * 根据模板 ParamSchema（{groups:[{name,fields:[{key,label,type,default,options,placeholder}]}]}）
 * 动态渲染参数配置表单，绑定到 params 对象。
 * 支持: input/textarea/number/switch/select/color/json
 * 由模板管理与页面管理共用，实现"模板参数用 JSON 定义、用动态组件配置"。
 * ============================================================ */
(function () {
    'use strict';

    var TemplateParamPanel = {
        name: 'TemplateParamPanel',
        props: {
            schema: { type: Object, default: function () { return { groups: [] }; } },
            params: { type: Object, required: true }
        },
        emits: ['update'],
        computed: {
            groups: function () {
                var g = (this.schema && this.schema.groups) || [];
                return g.map(function (grp) {
                    return {
                        name: grp.name || '参数',
                        fields: grp.fields || []
                    };
                });
            },
            hasFields: function () {
                var self = this;
                return self.groups.some(function (g) { return g.fields.length > 0; });
            }
        },
        methods: {
            fieldVal: function (f) {
                var v = this.params[f.key];
                return (v === undefined || v === null) ? f.default : v;
            },
            setField: function (f, v) {
                this.params[f.key] = v;
                this.$emit('update', f.key, v);
            },
            // json 控件：显示字符串，失焦/输入时尝试解析
            jsonText: function (f) {
                var v = this.fieldVal(f);
                if (typeof v === 'string') return v;
                return v ? JSON.stringify(v, null, 2) : (f.default || '');
            },
            setJson: function (f, text) {
                var t = (text || '').trim();
                if (!t) { this.setField(f, ''); return; }
                try { this.setField(f, JSON.parse(t)); }
                catch (e) { this.params[f.key] = t; }
            }
        },
        template: `
            <div class="tpl-param-panel">
                <el-empty v-if="!hasFields" description="该模板未定义参数（可在模板管理中配置 ParamSchema）" :image-size="60" />
                <el-collapse v-else v-model="openGroups" style="border:none">
                    <el-collapse-item v-for="g in groups" :key="g.name" :title="g.name" :name="g.name">
                        <div class="tpl-fields">
                            <div v-for="f in g.fields" :key="f.key" class="tpl-field">
                                <label class="tpl-label">
                                    {{ f.label }}
                                    <span v-if="f.required" style="color:#f56c6c;margin-left:2px">*</span>
                                </label>
                                <div class="tpl-control">
                                    <el-input v-if="f.type==='input'" :model-value="fieldVal(f)"
                                        @update:model-value="setField(f,$event)" :placeholder="f.placeholder||f.label" clearable size="small" style="width:100%" />
                                    <el-input v-else-if="f.type==='textarea'" :model-value="fieldVal(f)"
                                        @update:model-value="setField(f,$event)" type="textarea" :rows="f.rows||3"
                                        :placeholder="f.placeholder||f.label" size="small" style="width:100%" />
                                    <el-input-number v-else-if="f.type==='number'" :model-value="fieldVal(f)"
                                        @update:model-value="setField(f,$event)" :min="f.min" :max="f.max" :step="f.step||1"
                                        controls-position="right" size="small" style="width:100%" />
                                    <el-switch v-else-if="f.type==='switch'" :model-value="fieldVal(f)"
                                        @update:model-value="setField(f,$event)" />
                                    <el-select v-else-if="f.type==='select'" :model-value="fieldVal(f)"
                                        @update:model-value="setField(f,$event)" :placeholder="f.placeholder||'请选择'" filterable clearable size="small" style="width:100%">
                                        <el-option v-for="o in (f.options||[])" :key="o.value" :label="o.label" :value="o.value" :disabled="o.disabled" />
                                    </el-select>
                                    <el-color-picker v-else-if="f.type==='color'" :model-value="fieldVal(f)"
                                        @update:model-value="setField(f,$event)" />
                                    <el-input v-else-if="f.type==='json'" :model-value="jsonText(f)"
                                        @update:model-value="setJson(f,$event)" type="textarea" :rows="f.rows||4"
                                        :placeholder="f.placeholder||'JSON 数组/对象'" size="small" style="width:100%;font-family:Consolas,monospace" />
                                    <el-input v-else :model-value="fieldVal(f)" @update:model-value="setField(f,$event)"
                                        :placeholder="f.placeholder||f.label" clearable size="small" style="width:100%" />
                                </div>
                                <div v-if="f.tip" class="tpl-tip">{{ f.tip }}</div>
                            </div>
                        </div>
                    </el-collapse-item>
                </el-collapse>
            </div>
        `,
        data: function () {
            return { openGroups: [] };
        },
        watch: {
            groups: {
                immediate: true,
                handler: function (val) {
                    this.openGroups = val.map(function (g) { return g.name; });
                }
            }
        }
    };

    window.TemplateParamPanel = TemplateParamPanel;
    console.log('[template-param-panel] 模板参数动态表单已加载');
})();
