/**
 * filter-block.js — Filter 积木（筛选区）
 * ------------------------------------------------------------
 * 独立 Vue 应用（自有 createApp），渲染动态筛选组件（dyn-dynamic-com），
 * 负责：收集筛选条件；发布事件；不关心谁订阅、不直接调用其他积木。
 *
 * 入参 opts：
 *   msgCenter : 模板注入的独立消息中心
 *   blockId   : 本积木在模板内的唯一标识（如 'filter'）
 *   setting   : { config: PageSetting.ConfigJson(组件树), default: DefaultJson(model骨架) }
 *   mountTo   : 挂载容器选择器 / DOM 元素
 *
 * 对外事件：
 *   emitEvent('filter.changed', { params: {字段:{op,value}} })  查询/重置后发布
 * 命令处理：
 *   MVP 阶段无命令（仅渲染 + 查询/重置）
 */
(function (global) {
    function createFilterBlock(opts) {
        var msgCenter = opts.msgCenter;
        var blockId = opts.blockId;
        var cfg = (opts.setting && opts.setting.config) || null;
        var initDefault = (opts.setting && opts.setting.default) || {};
        var el = typeof opts.mountTo === 'string' ? document.querySelector(opts.mountTo) : opts.mountTo;
        if (!el) throw new Error('[FilterBlock] 挂载容器不存在: ' + opts.mountTo);

        // model 用「字段值」结构（{字段: 值}），让 DynElInput 正常显示；
        // 每个字段的默认操作符（op）从 DefaultJson 提取（{字段:{op}} 或 {字段:值} 两种写法兼容）。
        var opMap = {};
        var initModel = {};
        Object.keys(initDefault).forEach(function (k) {
            var v = initDefault[k];
            if (v && typeof v === 'object' && v.op) { opMap[k] = v.op; initModel[k] = null; }
            else { opMap[k] = 'eq'; initModel[k] = (v === null || v === undefined) ? null : v; }
        });

        var app = global.Vue.createApp({
            data: function () {
                return { cfg: cfg, model: JSON.parse(JSON.stringify(initModel)) };
            },
            template: '<div class="fb">'
                + '<div class="fb-body"><dyn-dynamic-com v-if="cfg" :jsonconfig="cfg" :parentmodelinfo="model"></dyn-dynamic-com></div>'
                + '<div class="fb-btns">'
                +   '<el-button size="small" type="primary" @click="query">查询</el-button>'
                +   '<el-button size="small" @click="reset">重置</el-button>'
                + '</div></div>',
            methods: {
                // 组装 {字段:{op,value}} 提交结构；剔除空值字段
                buildSubmit: function () {
                    var self = this;
                    var f = {};
                    Object.keys(this.model).forEach(function (k) {
                        var v = self.model[k];
                        if (v !== null && v !== undefined && v !== '') {
                            f[k] = { op: opMap[k] || 'eq', value: v };
                        }
                    });
                    return f;
                },
                query: function () { msgCenter.emitEvent('filter.changed', { params: this.buildSubmit() }); },
                reset: function () {
                    this.model = JSON.parse(JSON.stringify(initModel));
                    msgCenter.emitEvent('filter.changed', { params: {} });
                }
            }
        });
        global.DynCom.setupApp(app);

        return {
            blockId: blockId,
            // 就绪 Promise（动态组件注册后挂载）
            ready: global.DynCom.ensureRegistered(app).then(function () { app.mount(el); }),
            handleCommand: function () { return Promise.resolve(); },
            destroy: function () { try { app && app.unmount(); } catch (e) { } }
        };
    }
    global.createFilterBlock = createFilterBlock;
})(window);
