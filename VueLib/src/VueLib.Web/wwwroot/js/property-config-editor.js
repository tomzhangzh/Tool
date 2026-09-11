/* ============================================================
 * VueLib 低代码平台 - 属性框配置器（PropertyConfigEditor）
 * ------------------------------------------------------------
 * 在设计器中可视化编辑组件的 PropertyConfigJson：
 *   - 分组管理（增删分组、分组名）
 *   - 字段管理（增删/排序字段；key=绑定路径、label、type、选项）
 *   - 字段类型：input/number/switch/select/textarea/color/slider/radio/checkbox/icon
 *               dict（字典下拉：DynDict）/ dbquery（数据库查询下拉）/ group/slot
 * 保存后通过 POST /api/lowcode/component 更新 ComponentMeta.PropertyConfigJson。
 * ============================================================ */
(function () {
    'use strict';

    var FIELD_TYPES = [
        { value: 'input', label: '文本框' },
        { value: 'number', label: '数字' },
        { value: 'switch', label: '开关' },
        { value: 'select', label: '下拉选择' },
        { value: 'textarea', label: '多行文本' },
        { value: 'color', label: '颜色' },
        { value: 'slider', label: '滑块' },
        { value: 'radio', label: '单选' },
        { value: 'checkbox', label: '多选' },
        { value: 'icon', label: '图标' },
        { value: 'dict', label: '字典下拉(DynDict)' },
        { value: 'dbquery', label: '数据库查询下拉' },
        { value: 'group', label: '分组标题' },
        { value: 'slot', label: '开放容器(只读)' }
    ];

    var PropertyConfigEditor = {
        name: 'PropertyConfigEditor',
        props: {
            visible: { type: Boolean, default: false },
            meta: { type: Object, default: null }
        },
        emits: ['update:visible', 'saved'],
        data: function () {
            return {
                config: { groups: [] },
                openGroups: [],
                saving: false,
                fieldTypes: FIELD_TYPES,
                errorMsg: ''
            };
        },
        watch: {
            visible: function (v) { if (v) this.load(); }
        },
        computed: {
            groups: function () { return this.config.groups || []; }
        },
        methods: {
            load: function () {
                this.errorMsg = '';
                if (!this.meta) { this.config = { groups: [] }; return; }
                var raw = this.meta.propertyConfigJson || this.meta.PropertyConfigJson || '';
                try {
                    var parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
                    this.config = (parsed && parsed.groups) ? parsed : { groups: [] };
                } catch (e) {
                    this.config = { groups: [] };
                    this.errorMsg = 'PropertyConfigJson 解析失败：' + e.message;
                }
            },
            addGroup: function () {
                this.groups.push({ title: '新分组', fields: [] });
            },
            removeGroup: function (gi) {
                this.groups.splice(gi, 1);
            },
            addField: function (group) {
                group.fields.push({ key: '', label: '', type: 'input', placeholder: '', options: [], dictType: '', queryUrl: '', rows: 3 });
            },
            removeField: function (group, fi) {
                group.fields.splice(fi, 1);
            },
            moveField: function (group, fi, dir) {
                var idx = fi + dir;
                if (idx < 0 || idx >= group.fields.length) return;
                var t = group.fields[fi];
                group.fields[fi] = group.fields[idx];
                group.fields[idx] = t;
            },
            // 类型特定选项是否需要显示
            needsOptions: function (type) { return ['select', 'radio', 'checkbox'].indexOf(type) >= 0; },
            needsDictType: function (type) { return type === 'dict'; },
            needsQueryUrl: function (type) { return type === 'dbquery'; },
            needsPlaceholder: function (type) { return ['input', 'textarea', 'select'].indexOf(type) >= 0; },
            needsRows: function (type) { return type === 'textarea'; },
            parseOptions: function (field) {
                // options 允许文本编辑（JSON 数组）
                if (typeof field.options === 'string') {
                    try {
                        var arr = JSON.parse(field.options);
                        if (Array.isArray(arr)) field.options = arr;
                        else field.options = [];
                    } catch (e) { field.options = []; }
                }
                if (!Array.isArray(field.options)) field.options = [];
            },
            optionsText: function (field) {
                return JSON.stringify(field.options || []);
            },
            setOptionsText: function (field, text) {
                try {
                    var arr = JSON.parse(text);
                    field.options = Array.isArray(arr) ? arr : [];
                } catch (e) { field.options = []; }
            },
            save: function () {
                if (!this.meta) return;
                var self = this;
                this.saving = true;
                this.errorMsg = '';
                var meta = Object.assign({}, this.meta);
                meta.propertyConfigJson = JSON.stringify(this.config);
                fetch('/api/lowcode/component', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(meta)
                }).then(function (r) { return r.json(); }).then(function (res) {
                    self.saving = false;
                    if (res && res.success) {
                        self.$emit('saved', meta);
                        self.$emit('update:visible', false);
                    } else {
                        self.errorMsg = '保存失败：' + ((res && res.message) || '未知错误');
                    }
                }).catch(function (err) {
                    self.saving = false;
                    self.errorMsg = '保存失败：' + err.message;
                });
            },
            cancel: function () {
                this.$emit('update:visible', false);
            }
        },
        template: `
        <el-dialog :model-value="visible" :title="'配置属性框 - ' + (meta ? (meta.componentName || meta.ComponentName || '') : '')"
                   width="880px" top="6vh" :close-on-click-modal="false" @@update:model-value="cancel">
            <div v-if="errorMsg" style="color:#f56c6c;font-size:12px;margin-bottom:8px;">{{ errorMsg }}</div>
            <div v-if="!meta" class="empty-tip" style="color:#909399;font-size:12px;padding:12px;">请先选择一个组件</div>
            <template v-else>
                <el-collapse v-model="openGroups" style="--el-collapse-header-height:34px;">
                    <el-collapse-item v-for="(group, gi) in groups" :key="gi" :name="gi">
                        <template #title>
                            <div style="display:flex;align-items:center;gap:8px;width:100%;">
                                <el-input v-model="group.title" placeholder="分组名称" size="small" style="width:220px;" @@click.stop />
                                <span style="font-size:12px;color:#909399;">{{ (group.fields||[]).length }} 个字段</span>
                                <el-button size="small" text type="danger" @@click.stop="removeGroup(gi)" style="margin-left:auto;">删除分组</el-button>
                            </div>
                        </template>
                        <div v-for="(field, fi) in group.fields" :key="fi"
                             style="display:flex;flex-wrap:wrap;gap:6px;align-items:center;margin-bottom:8px;padding:8px;background:#f7f8fa;border-radius:6px;">
                            <el-input v-model="field.key" placeholder="绑定路径，如 options.labeloptions.label" size="small" style="width:300px;" />
                            <el-input v-model="field.label" placeholder="显示名" size="small" style="width:110px;" />
                            <el-select v-model="field.type" size="small" style="width:150px;" placeholder="类型">
                                <el-option v-for="t in fieldTypes" :key="t.value" :label="t.label" :value="t.value" />
                            </el-select>
                            <el-input v-if="needsPlaceholder(field.type)" v-model="field.placeholder" placeholder="占位符" size="small" style="width:110px;" />
                            <el-input-number v-if="needsRows(field.type)" v-model="field.rows" :min="1" :max="10" size="small" style="width:90px;" />
                            <el-input v-if="needsDictType(field.type)" v-model="field.dictType" placeholder="字典类型，如 Gender" size="small" style="width:140px;" />
                            <el-input v-if="needsQueryUrl(field.type)" v-model="field.queryUrl" placeholder="查询URL（POST返回 {items:[{label,value}]}）" size="small" style="width:230px;" />
                            <el-input v-if="needsOptions(field.type)" :model-value="optionsText(field)" @@update:model-value="setOptionsText(field, $event)"
                                      placeholder='选项JSON，如 [{"label":"男","value":"M"}]' size="small" style="width:230px;" />
                            <span style="flex:1;"></span>
                            <el-button size="small" text @@click="moveField(group, fi, -1)">↑</el-button>
                            <el-button size="small" text @@click="moveField(group, fi, 1)">↓</el-button>
                            <el-button size="small" text type="danger" @@click="removeField(group, fi)">删除</el-button>
                        </div>
                        <el-button size="small" type="primary" plain @@click="addField(group)" icon="Plus" style="margin-top:4px;">添加字段</el-button>
                    </el-collapse-item>
                </el-collapse>
                <el-button size="small" type="primary" plain @@click="addGroup" icon="FolderAdd" style="margin-top:10px;">添加分组</el-button>
                <div style="margin-top:10px;font-size:12px;color:#909399;line-height:1.6;">
                    提示：绑定路径为组件 ConfigJson 中的 JSON 路径（如 options.labeloptions.label / options.comoptions.placeholder /
                    modelname / childrenctrls[0].modelname）。dict 从设计库 DynDict 读取；dbquery 需后端返回 {"items":[{"label","value"}]}。
                </div>
            </template>
            <template #footer>
                <el-button size="small" @@click="cancel">取消</el-button>
                <el-button size="small" type="primary" :loading="saving" @@click="save">保存属性配置</el-button>
            </template>
        </el-dialog>
        `
    };

    window.PropertyConfigEditor = PropertyConfigEditor;
    console.log('[property-config-editor] 属性框配置器已加载');
})();
