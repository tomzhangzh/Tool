/**
 * detail-block.js — Detail 积木（详情/表单区）
 * ------------------------------------------------------------
 * 独立 Vue 应用（自有 createApp），渲染动态表单（dyn-dynamic-com），
 * 负责：新增/编辑表单；表单校验；保存到 saveUrl；发布保存/取消事件。
 * 完全不知道自己被放在哪（页面内嵌面板 / 弹窗 / 行内展开），由模板决定。
 *
 * 入参 opts：
 *   msgCenter, blockId, setting:{config:表单组件树, default:model骨架}
 *   saveAddUrl  : 新增保存接口（EffectiveParams.AddUrl）
 *   saveEditUrl : 编辑保存接口（EffectiveParams.EditUrl）
 *   tableName   : 业务表名
 *   mountTo     : 挂载容器
 *
 * 命令处理 handleCommand：
 *   {cmd:'newForm', preFill:{...}}   打开新增表单，preFill 为动态预填参数（主从主键）
 *   {cmd:'editForm', row}           打开编辑表单，回填行数据
 * 对外事件：
 *   emitEvent('detail.saved', {action:'add'|'update', row})
 *   emitEvent('detail.cancel', {})
 */
(function (global) {
    function createDetailBlock(opts) {
        var msgCenter = opts.msgCenter;
        var blockId = opts.blockId;
        var cfg = (opts.setting && opts.setting.config) || null;
        var initModel = (opts.setting && opts.setting.default) || {};
        var saveAddUrl = opts.saveAddUrl || '';
        var saveEditUrl = opts.saveEditUrl || saveAddUrl;
        var tableName = opts.tableName || '';
        var el = typeof opts.mountTo === 'string' ? document.querySelector(opts.mountTo) : opts.mountTo;
        if (!el) throw new Error('[DetailBlock] 挂载容器不存在: ' + opts.mountTo);

        var app = global.Vue.createApp({
            data: function () {
                return { cfg: cfg, form: JSON.parse(JSON.stringify(initModel)), formError: '', saving: false };
            },
            template: '<div class="db">'
                + '<div class="db-err" v-if="formError">⚠ {{ formError }}</div>'
                + '<div class="db-body"><dyn-dynamic-com v-if="cfg" :jsonconfig="cfg" :parentmodelinfo="form"></dyn-dynamic-com></div>'
                + '<div class="db-btns">'
                +   '<el-button size="small" @click="cancel">取消</el-button>'
                +   '<el-button size="small" type="primary" :loading="saving" @click="save">保存</el-button>'
                + '</div></div>',
            methods: {
                // 命令入口：新增（preFill 动态预填参数 合并进 model 骨架）
                newForm: function (preFill) {
                    currentEdit = false;
                    this.form = Object.assign({}, JSON.parse(JSON.stringify(initModel)), preFill || {});
                    this.formError = '';
                },
                // 命令入口：编辑（回填行数据）
                editForm: function (row) {
                    currentEdit = true;
                    this.form = Object.assign({}, JSON.parse(JSON.stringify(initModel)), row || {});
                    this.formError = '';
                },
                cancel: function () { msgCenter.emitEvent('detail.cancel', {}); },
                save: function () {
                    var self = this;
                    var url = currentEdit ? saveEditUrl : saveAddUrl;
                    this.saving = true;
                    // business dyndata：insert/update 接口 body 为 {table, data}，table 在 body 中（url 不带 ?table=）
                    return fetch(url, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ table: tableName, data: this.form })
                    }).then(function (r) { return r.json(); }).then(function (res) {
                        self.saving = false;
                        if (res.code !== 0) { global.dyn.showMessage(res.msg || '保存失败', 'error'); return; }
                        global.dyn.showMessage('保存成功', 'success');
                        msgCenter.emitEvent('detail.saved', { action: currentEdit ? 'update' : 'add', row: self.form });
                    }).catch(function () { self.saving = false; global.dyn.showMessage('保存失败', 'error'); });
                }
            }
        });
        global.DynCom.setupApp(app);

        var currentEdit = false;
        var proxy = null;
        return {
            blockId: blockId,
            ready: global.DynCom.ensureRegistered(app).then(function () { proxy = app.mount(el); }),
            handleCommand: function (cmd) {
                if (!proxy) return Promise.resolve();
                if (cmd.cmd === 'newForm') { proxy.newForm(cmd.preFill || {}); return Promise.resolve(); }
                if (cmd.cmd === 'editForm') { proxy.editForm(cmd.row); return Promise.resolve(); }
                return Promise.resolve();
            },
            destroy: function () { try { app && app.unmount(); } catch (e) { } }
        };
    }
    global.createDetailBlock = createDetailBlock;
})(window);
