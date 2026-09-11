/* ============================================================
 * VueLib 低代码平台 - 属性框设计器（PropertyFormDesigner）
 * ------------------------------------------------------------
 * 用"条目列表 + 属性编辑 + DynCom 实时预览"设计组件的 PropertyConfigJson：
 *   - 每个条目 = DynPropItem（el-form-item：标签 + 绑定路径）+ 子控件（input/select/switch...）
 *   - 绑定路径（modelname）为被编辑组件 ConfigJson 的 JSON 路径（如 options.labeloptions.label）
 *   - 保存后写入 ComponentMeta.PropertyConfigJson，右侧属性面板用 DynCom 渲染
 * ============================================================ */
(function (global) {
    'use strict';
    if (global.PropertyFormDesigner) { console.log('[property-form-designer] 已存在，跳过'); return; }

    var LIB_TYPES = [
        { label: '文本框', type: 'input', comp: 'DynNInput' },
        { label: '数字', type: 'number', comp: 'DynNInputNumber' },
        { label: '开关', type: 'switch', comp: 'DynNSwitch' },
        { label: '下拉', type: 'select', comp: 'DynNSelect' },
        { label: '单选', type: 'radio', comp: 'DynNRadio' },
        { label: '多行文本', type: 'textarea', comp: 'DynNInput' },
        { label: '颜色', type: 'color', comp: 'DynNColorPicker' },
        { label: '滑块', type: 'slider', comp: 'DynNSlider' },
        { label: '字典下拉', type: 'dict', comp: 'DynNSelect' },
        { label: '库查询下拉', type: 'dbquery', comp: 'DynNSelect' },
        { label: '代码编辑器', type: 'codemirror', comp: 'DynNCodeMirror' }
    ];

    function emptyConfig() {
        return {
            component: 'DynPropContainer',
            modelname: '',
            options: { itemoptions: { style: {} } },
            childrenctrls: [], slots: {}, validators: [], extendinfo: {}
        };
    }

    function buildLeaf(type) {
        // 属性框内置控件（DynPropControl），按 kind 渲染 el 控件，不依赖组件表注册
        var co = { kind: type || 'input' };
        if (type === 'textarea') co.type = 'textarea';
        if (type === 'dict') { co.sourceType = 'dict'; co.dictType = ''; }
        if (type === 'dbquery') { co.sourceType = 'ajax'; co.dataUrl = ''; }
        if (type === 'codemirror') {
            // CodeMirror 代码编辑器：DynPropControl kind=code（面板同步渲染，使用全局 CodeMirror 库，无需 LoadUrl 异步解析）
            var co2 = { kind: 'code', language: 'json', height: '220px', readonly: false, placeholder: '' };
            return {
                component: 'DynPropControl', modelname: '',
                options: { comoptions: co2, labeloptions: { show: false }, itemoptions: { style: {} } },
                childrenctrls: [], slots: {}, validators: [], extendinfo: {}
            };
        }
        return {
            component: 'DynPropControl', modelname: '',
            options: { comoptions: co, labeloptions: { show: false }, itemoptions: { style: {} } },
            childrenctrls: [], slots: {}, validators: [], extendinfo: {}
        };
    }

    function buildItem(label, leaf) {
        return {
            component: 'DynPropItem',
            modelname: '',
            options: { comoptions: { label: label || '', required: false, showLabel: true, labelWidth: '110px' }, itemoptions: { style: { marginBottom: '8px' } } },
            childrenctrls: [leaf], slots: {}, validators: [], extendinfo: {}
        };
    }

    var PropertyFormDesigner = {
        name: 'PropertyFormDesigner',
        props: {
            visible: { type: Boolean, default: false },
            meta: { type: Object, default: null }
        },
        emits: ['update:visible', 'saved'],
        data: function () {
            return {
                config: emptyConfig(),
                selectedIdx: -1,
                libTypes: LIB_TYPES,
                saving: false,
                errorMsg: '',
                previewTarget: { options: { labeloptions: { label: '', required: false, show: true }, comoptions: {} } }
            };
        },
        watch: {
            visible: { handler: function (v) { if (v) this.load(); }, immediate: true }
        },
        computed: {
            items: function () { return this.config.childrenctrls || []; },
            selectedItem: function () {
                var it = this.items[this.selectedIdx];
                return it || null;
            },
            selectedLeaf: function () {
                var it = this.selectedItem;
                return (it && it.childrenctrls && it.childrenctrls[0]) || null;
            },
            leafType: {
                get: function () {
                    var leaf = this.selectedLeaf;
                    if (!leaf) return 'input';
                    return ((leaf.options && leaf.options.comoptions && leaf.options.comoptions.kind) || 'input');
                },
                set: function (type) {
                    var it = this.selectedItem;
                    if (!it) return;
                    var path = it.modelname || '';
                    var label = (it.options && it.options.comoptions && it.options.comoptions.label) || '';
                    var leaf = buildLeaf(type);
                    it.childrenctrls = [leaf];
                }
            },
            leafOptionsText: {
                get: function () {
                    var leaf = this.selectedLeaf;
                    if (!leaf) return '';
                    var opts = (leaf.options && leaf.options.comoptions && leaf.options.comoptions.optionValues);
                    return Array.isArray(opts) ? JSON.stringify(opts) : (opts || '');
                },
                set: function (text) {
                    var leaf = this.selectedLeaf;
                    if (!leaf) return;
                    try {
                        var arr = JSON.parse(text);
                        if (Array.isArray(arr)) { if (!leaf.options) leaf.options = {}; if (!leaf.options.comoptions) leaf.options.comoptions = {}; leaf.options.comoptions.optionValues = arr; }
                    } catch (e) { /* 非法 JSON 忽略 */ }
                }
            }
        },
        methods: {
            load: function () {
                this.errorMsg = '';
                if (!this.meta) { this.config = emptyConfig(); return; }
                var raw = this.meta.propertyConfigJson || this.meta.PropertyConfigJson || '';
                try {
                    var parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
                    if (parsed && parsed.component && Array.isArray(parsed.childrenctrls)) {
                        this.config = parsed;
                    } else {
                        this.config = emptyConfig();
                        // 兼容旧 groups/fields schema：迁移为条目
                        if (parsed && parsed.groups && Array.isArray(parsed.groups)) {
                            var items = [];
                            parsed.groups.forEach(function (g) {
                                (g.fields || []).forEach(function (f) {
                                    if (f.key && f.key.indexOf('@') === 0) f.key = f.key.slice(2);
                                    var leaf = buildLeaf(f.type || 'input');
                                    if (f.placeholder) leaf.options.comoptions.placeholder = f.placeholder;
                                    if (Array.isArray(f.options)) leaf.options.comoptions.optionValues = f.options;
                                    if (f.dictType) { leaf.component = 'DynNSelect'; leaf.options.comoptions.sourceType = 'dict'; leaf.options.comoptions.dictType = f.dictType; }
                                    if (f.queryUrl) { leaf.component = 'DynNSelect'; leaf.options.comoptions.sourceType = 'ajax'; leaf.options.comoptions.dataUrl = f.queryUrl; }
                                    items.push(buildItem(f.label || f.key, leaf));
                                });
                            });
                            this.config.childrenctrls = items;
                        }
                    }
                } catch (e) {
                    this.config = emptyConfig();
                    this.errorMsg = 'PropertyConfigJson 解析失败：' + e.message;
                }
                this.selectedIdx = -1;
            },
            addItem: function (type) {
                var leaf = buildLeaf(type);
                var item = buildItem(LIB_TYPES.find(function (t) { return t.type === type; }).label, leaf);
                this.config.childrenctrls.push(item);
                this.selectedIdx = this.config.childrenctrls.length - 1;
            },
            removeItem: function (idx) {
                this.config.childrenctrls.splice(idx, 1);
                if (this.selectedIdx >= this.config.childrenctrls.length) this.selectedIdx = this.config.childrenctrls.length - 1;
            },
            moveItem: function (idx, dir) {
                var j = idx + dir;
                if (j < 0 || j >= this.config.childrenctrls.length) return;
                var t = this.config.childrenctrls[idx];
                this.config.childrenctrls[idx] = this.config.childrenctrls[j];
                this.config.childrenctrls[j] = t;
                this.selectedIdx = j;
            },
            selectItem: function (idx) { this.selectedIdx = idx; },
            isContainerLeaf: function () { return false; },
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
            cancel: function () { this.$emit('update:visible', false); },
            emptyPreview: function () { return this.previewTarget; }
        },
        template: [
            '<el-dialog :model-value="visible" :title="\'设计属性框 - \' + (meta ? (meta.componentName || meta.ComponentName || \'\') : \'\')"',
            '           width="1000px" top="5vh" :close-on-click-modal="false" @update:model-value="cancel">',
            '  <div v-if="errorMsg" style="color:#f56c6c;font-size:12px;margin-bottom:8px;">{{ errorMsg }}</div>',
            '  <div v-if="!meta" style="color:#909399;font-size:12px;padding:12px;">请先选中组件</div>',
            '  <div v-else style="display:flex;gap:12px;height:520px;">',
            '    <!-- 左：组件库 + 条目列表 -->',
            '    <div style="width:260px;flex:0 0 260px;display:flex;flex-direction:column;border-right:1px solid #ebeef5;padding-right:10px;">',
            '      <div style="font-size:12px;color:#606266;margin-bottom:6px;">添加条目（标签 + 控件 + 绑定路径）</div>',
            '      <div style="display:flex;flex-wrap:wrap;gap:6px;margin-bottom:12px;">',
            '        <el-button v-for="t in libTypes" :key="t.type" size="small" @click="addItem(t.type)" style="margin:0;">{{ t.label }}</el-button>',
            '      </div>',
            '      <div style="font-size:12px;color:#606266;margin-bottom:6px;">条目列表（{{ items.length }}）</div>',
            '      <div style="flex:1;overflow:auto;border:1px solid #ebeef5;border-radius:6px;">',
            '        <div v-for="(it, idx) in items" :key="idx"',
            '             style="display:flex;align-items:center;gap:4px;padding:6px 8px;cursor:pointer;border-bottom:1px solid #f0f2f5;font-size:12px;"',
            '             :style="idx === selectedIdx ? \'background:#ecf5ff;\' : \'\'" @click="selectItem(idx)">',
            '          <span style="flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">',
            '            {{ (it.options && it.options.comoptions && it.options.comoptions.label) || \'（未命名）\' }}',
            '            <span v-if="it.modelname" style="color:#909399;"> → {{ it.modelname }}</span>',
            '          </span>',
            '          <el-button size="mini" text @click.stop="moveItem(idx, -1)">↑</el-button>',
            '          <el-button size="mini" text @click.stop="moveItem(idx, 1)">↓</el-button>',
            '          <el-button size="mini" text type="danger" @click.stop="removeItem(idx)">✕</el-button>',
            '        </div>',
            '        <div v-if="!items.length" style="padding:16px;color:#c0c4cc;font-size:12px;text-align:center;">暂无条目，点击上方按钮添加</div>',
            '      </div>',
            '    </div>',
            '    <!-- 右：条目属性 + 预览 -->',
            '    <div style="flex:1;display:flex;flex-direction:column;gap:10px;min-width:0;">',
            '      <div v-if="selectedItem" style="border:1px solid #ebeef5;border-radius:6px;padding:10px;">',
            '        <div style="font-size:12px;color:#606266;margin-bottom:8px;">条目属性</div>',
            '        <el-form label-width="90px" size="small">',
            '          <el-form-item label="标签文字">',
            '            <el-input v-model="selectedItem.options.comoptions.label" placeholder="如：绑定字段、显示名称" clearable></el-input>',
            '          </el-form-item>',
            '          <el-form-item label="绑定路径">',
            '            <el-input v-model="selectedItem.modelname" placeholder="如 options.labeloptions.label / modelname" clearable></el-input>',
            '          </el-form-item>',
            '          <el-form-item label="必填"><el-switch v-model="selectedItem.options.comoptions.required"></el-switch></el-form-item>',
            '          <el-form-item label="控件类型">',
            '            <el-select v-model="leafType" size="small" style="width:100%;">',
            '              <el-option v-for="t in libTypes" :key="t.type" :label="t.label" :value="t.type" />',
            '            </el-select>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && (leafType === \'input\' || leafType === \'textarea\' || leafType === \'select\')" label="占位符">',
            '            <el-input v-model="selectedLeaf.options.comoptions.placeholder" placeholder="placeholder" clearable></el-input>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && (leafType === \'select\' || leafType === \'radio\')" label="选项JSON">',
            '            <el-input v-model="leafOptionsText" type="textarea" :rows="2" placeholder=\'[{"label":"男","value":"M"}]\' clearable></el-input>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && leafType === \'codemirror\'" label="语言">',
            '            <el-select v-model="selectedLeaf.options.comoptions.language" size="small" style="width:100%;">',
            '              <el-option label="JSON" value="json" />',
            '              <el-option label="JavaScript" value="javascript" />',
            '              <el-option label="SQL" value="sql" />',
            '              <el-option label="CSS" value="css" />',
            '              <el-option label="XML" value="xml" />',
            '              <el-option label="HTML" value="html" />',
            '            </el-select>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && leafType === \'codemirror\'" label="只读">',
            '            <el-switch v-model="selectedLeaf.options.comoptions.readonly"></el-switch>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && leafType === \'codemirror\'" label="高度">',
            '            <el-input v-model="selectedLeaf.options.comoptions.height" placeholder="如 220px" clearable></el-input>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && leafType === \'dict\'" label="字典类型">',
            '            <el-input v-model="selectedLeaf.options.comoptions.dictType" placeholder="如 Gender" clearable></el-input>',
            '          </el-form-item>',
            '          <el-form-item v-if="selectedLeaf && leafType === \'dbquery\'" label="查询URL">',
            '            <el-input v-model="selectedLeaf.options.comoptions.dataUrl" placeholder="POST 返回 {items:[{label,value}]}" clearable></el-input>',
            '          </el-form-item>',
            '        </el-form>',
            '      </div>',
            '      <div v-else style="border:1px dashed #dcdfe6;border-radius:6px;padding:16px;color:#c0c4cc;font-size:12px;text-align:center;">',
            '        选中左侧条目后可编辑标签、绑定路径与控件属性',
            '      </div>',
            '      <div style="flex:1;border:1px solid #ebeef5;border-radius:6px;padding:10px;overflow:auto;background:#fff;">',
            '        <div style="font-size:12px;color:#606266;margin-bottom:8px;">实时预览（绑定到示例对象）</div>',
            '        <n-dynamic-com v-if="config && config.component" :jsonconfig="config" :parentmodelinfo="previewTarget" :node-path="\'pf-root\'"></n-dynamic-com>',
            '        <div v-else style="color:#c0c4cc;font-size:12px;">空属性框</div>',
            '      </div>',
            '    </div>',
            '  </div>',
            '  <template #footer>',
            '    <span style="float:left;font-size:12px;color:#909399;line-height:32px;">提示：绑定路径为选中组件 ConfigJson 的 JSON 路径</span>',
            '    <el-button size="small" @click="cancel">取消</el-button>',
            '    <el-button size="small" type="primary" :loading="saving" @click="save">保存属性框配置</el-button>',
            '  </template>',
            '</el-dialog>'
        ].join('\n')
    };

    global.PropertyFormDesigner = PropertyFormDesigner;
    console.log('[property-form-designer] 属性框设计器已加载');
})(window);
