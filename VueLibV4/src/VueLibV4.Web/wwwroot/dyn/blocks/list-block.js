/**
 * list-block.js — List 积木（列表区）
 * ------------------------------------------------------------
 * 独立 Vue 应用（自有 createApp），渲染 el-table 列表 + 分页 + 操作列，
 * 负责：按条件请求 loadUrl 加载数据；渲染行；发布行操作事件。
 * 不关心谁订阅、不直接调用其他积木。
 *
 * 入参 opts：
 *   msgCenter, blockId, setting:{config:列定义, default}
 *   loadUrl    : 列表查询接口（EffectiveParams.ListUrl）
 *   tableName  : 业务表名
 *   mountTo    : 挂载容器
 *
 * 对外事件：
 *   emitEvent('list.add', {})                    点击 +新增
 *   emitEvent('list.edit', { row })              点击行 编辑
 *   emitEvent('list.addChild', { row })          点击行 +子(新增子记录)
 * 命令处理 handleCommand：
 *   {cmd:'loadData', filter}   加载数据（可携带筛选条件）
 *   {cmd:'reload'}             刷新列表
 */
(function (global) {
    function createListBlock(opts) {
        var msgCenter = opts.msgCenter;
        var blockId = opts.blockId;
        var cfg = (opts.setting && opts.setting.config) || [];
        var columns = (Array.isArray(cfg) ? cfg : (cfg && cfg.columns)) || [];
        var loadUrl = opts.loadUrl || '';
        var tableName = opts.tableName || '';
        var el = typeof opts.mountTo === 'string' ? document.querySelector(opts.mountTo) : opts.mountTo;
        if (!el) throw new Error('[ListBlock] 挂载容器不存在: ' + opts.mountTo);

        var app = global.Vue.createApp({
            data: function () {
                return { columns: columns, rows: [], total: 0, loading: false, pageIndex: 1, pageSize: 20, filter: {} };
            },
            template: '<div class="lb">'
                + '<div class="lb-toolbar"><el-button size="small" type="primary" @click="emitAdd">+ 新增</el-button></div>'
                + '<el-table :data="rows" size="small" v-loading="loading" border stripe>'
                +   '<el-table-column v-for="col in columns" :key="col.field" :prop="col.field" :label="col.label" :width="col.width" :min-width="col.minWidth || 90">'
                +     '<template #default="scope">'
                +       '<el-tag v-if="col.cellType===\'tag\'" :type="scope.row[col.field] ? \'success\' : \'info\'">{{ scope.row[col.field] ? \'启用\' : \'停用\' }}</el-tag>'
                +       '<span v-else>{{ scope.row[col.field] }}</span>'
                +     '</template>'
                +   '</el-table-column>'
                +   '<el-table-column label="操作" width="220" fixed="right"><template #default="scope">'
                +     '<el-button size="small" type="primary" link @click="emitEdit(scope.row)">编辑</el-button>'
                +     '<el-button size="small" type="success" link @click="emitAddChild(scope.row)">+子</el-button>'
                +     '<el-button size="small" type="danger" link @click="del(scope.row)">删除</el-button>'
                +   '</template></el-table-column>'
                + '</el-table>'
                + '<el-pagination background size="small" layout="total, prev, pager, next, sizes" :total="total" :page-size="pageSize" :current-page="pageIndex" :page-sizes="[10,20,50]" @current-change="onPage" @size-change="onSize"></el-pagination>'
                + '</div>',
            methods: {
                emitAdd: function () { msgCenter.emitEvent('list.add', {}); },
                emitEdit: function (row) { msgCenter.emitEvent('list.edit', { row: row }); },
                emitAddChild: function (row) { msgCenter.emitEvent('list.addChild', { row: row }); },
                del: function (row) {
                    var self = this;
                    global.dyn.confirmAsync('确定删除这条记录吗？').then(function (ok) {
                        if (!ok) return;
                        // business dyndata：delete 接口 body 为 {table, keys}
                        var url = self.deleteUrl || '/api/business/dyndata/delete';
                        fetch(url, {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ table: tableName, keys: { Id: row.Id } })
                        }).then(function (r) { return r.json(); }).then(function (res) {
                            if (res.code !== 0) { global.dyn.showMessage(res.msg || '删除失败', 'error'); return; }
                            global.dyn.showMessage('删除成功', 'success');
                            self.load();
                        });
                    });
                },
                // 命令入口：加载数据（可更新筛选条件）
                loadData: function (payload) {
                    if (payload && payload.filter) this.filter = payload.filter;
                    this.pageIndex = 1;
                    this.load();
                },
                load: function () {
                    var self = this; this.loading = true;
                    return fetch(loadUrl, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ table: tableName, page: this.pageIndex, size: this.pageSize, sort: { field: 'Id', order: 'desc' }, filter: this.filter })
                    }).then(function (r) { return r.json(); }).then(function (res) {
                        self.loading = false;
                        if (res.code !== 0 || !res.data) { global.dyn.showMessage(res.msg || '查询失败', 'error'); return; }
                        self.rows = res.data.rows || [];
                        self.total = res.data.total || 0;
                    }).catch(function () { self.loading = false; global.dyn.showMessage('查询失败', 'error'); });
                },
                onPage: function (p) { this.pageIndex = p; this.load(); },
                onSize: function (s) { this.pageSize = s; this.pageIndex = 1; this.load(); }
            },
            mounted: function () { this.load(); }
        });
        global.DynCom.setupApp(app);

        var proxy = null;
        return {
            blockId: blockId,
            ready: global.DynCom.ensureRegistered(app).then(function () { proxy = app.mount(el); }),
            handleCommand: function (cmd) {
                if (!proxy) return Promise.resolve();
                if (cmd.cmd === 'loadData') { proxy.loadData(cmd); return Promise.resolve(); }
                if (cmd.cmd === 'reload') { proxy.load(); return Promise.resolve(); }
                return Promise.resolve();
            },
            destroy: function () { try { app && app.unmount(); } catch (e) { } }
        };
    }
    global.createListBlock = createListBlock;
})(window);
