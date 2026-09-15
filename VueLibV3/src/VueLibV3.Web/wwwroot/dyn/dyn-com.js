/* ============================================================
 * VueLibV3  dyn-com.js  动态组件库（设计时/运行时同一套代码）
 * ------------------------------------------------------------
 * - 组件以 view（模板）实现，不写死 JS render
 * - 组件注册进 DynCom.registry；元数据来自数据库 ComponentMeta，
 *   代码中有定义时以代码为准（"如果存在定义不用在数据库中定义"）
 * - 内置组件：容器(DynGridContainer/DynCard/DynForm/DynWrapper/DynSlot/
 *   DynFragment/DynForEach/DynCombination/DynOpenWindow)、
 *   数据(DynTable/DynCrudPage/DynTree)、展示(DynText/DynEChart/
 *   DynCodeMirror/DynDesktop)
 * 所有静态变量集中在头部。
 * ============================================================ */
(function (global) {
  'use strict';

  var VueObj = global.Vue;
  var registry = {};          // 组件名 -> 组件定义
  var metaMap = {};           // 组件名 -> ComponentMeta
  var META_API = '/api/platform/componentmeta';

  /* ================= 注册 ================= */
  function register(name, component, meta) {
    registry[name] = component;
    if (meta) metaMap[name] = meta;
  }
  function get(name) { return registry[name]; }
  function meta(name) { return metaMap[name]; }

  /** 从后端加载组件元数据（供设计器组件库 + 默认值补全） */
  function loadMeta() {
    return fetch(META_API + '/all')
      .then(function (r) { return r.json(); })
      .then(function (res) {
        if (res.code === 0) {
          (res.data || []).forEach(function (m) {
            if (m && m.ComponentName) {
              try { m.PropsMeta = typeof m.PropsMeta === 'string' ? JSON.parse(m.PropsMeta) : (m.PropsMeta || []); } catch (e) { m.PropsMeta = []; }
              try { m.AllowDrop = typeof m.AllowDrop === 'string' ? JSON.parse(m.AllowDrop) : (m.AllowDrop || []); } catch (e) { m.AllowDrop = []; }
              try { m.CanDropInto = typeof m.CanDropInto === 'string' ? JSON.parse(m.CanDropInto) : (m.CanDropInto || []); } catch (e) { m.CanDropInto = []; }
              try { m.SlotsDefine = typeof m.SlotsDefine === 'string' ? JSON.parse(m.SlotsDefine) : (m.SlotsDefine || []); } catch (e) { m.SlotsDefine = []; }
              metaMap[m.ComponentName] = m;
            }
          });
        }
        return metaMap;
      })
      .catch(function (e) { console.error('[DynCom] 加载组件元数据失败', e); return metaMap; });
  }

  /** 应用初始化：注册 ElementPlus + 图标 + DynRender + 全部 Dyn 组件 */
  function setupApp(app) {
    if (global.ElementPlus) {
      var locale = (global.ElementPlus.locale && global.ElementPlus.locale.zhCn) || undefined;
      app.use(global.ElementPlus, locale ? { locale: locale } : undefined);
    }
    if (global.ElementPlusIconsVue) {
      Object.keys(global.ElementPlusIconsVue).forEach(function (k) {
        app.component(k, global.ElementPlusIconsVue[k]);
      });
    }
    if (global.DynCore && global.DynCore.DynRender) app.component('DynRender', global.DynCore.DynRender);
    Object.keys(registry).forEach(function (k) { app.component(k, registry[k]); });
  }

  /* ================================================================
   * 一、容器组件
   * ================================================================ */

  /** 栅格/弹性容器：comoptions 即 CSS 样式（grid-template-columns / gap 等），
   *  子组件通过 itemoptions.class 配置 col-span-2 等类 */
  register('DynGridContainer', {
    name: 'DynGridContainer',
    inheritAttrs: true,
    computed: {
      styleObj: function () {
        var s = {};
        var self = this;
        Object.keys(self.$attrs).forEach(function (k) {
          if (k === 'class' || k === 'style') return;
          // 只保留 CSS 属性：过滤 data-*/事件/draggable 等非样式键
          if (k.indexOf('data-') === 0 || k.indexOf('on') === 0 || k === 'draggable' || k === 'key' || k === 'ref') return;
          s[k] = self.$attrs[k];
        });
        return s;
      }
    },
    template: '<div :style="styleObj" :class="$attrs.class"><slot /></div>'
  });

  /** 卡片容器（带标题） */
  register('DynCard', {
    name: 'DynCard',
    inheritAttrs: true,
    props: { title: { type: String, default: '' }, shadow: { type: String, default: 'always' } },
    template: '<el-card :header="title" :shadow="shadow" :class="$attrs.class" :style="$attrs.style"><slot /></el-card>'
  });

  /** 表单容器：为子树提供 formModel 上下文 + 校验 */
  register('DynForm', {
    name: 'DynForm',
    inheritAttrs: true,
    props: {
      modelname: { type: String, default: 'formData' },
      labelWidth: { type: String, default: '100px' },
      labelPosition: { type: String, default: 'right' },
      inline: { type: Boolean, default: false },
      size: { type: String, default: '' }
    },
    setup: function () {
      var model = VueObj.reactive({});
      // 键名必须与 DynRender 注入的 formModel 一致，表单内的字段才能双向绑定
      VueObj.provide('formModel', model);
      return { model: model };
    },
    template: '<el-form :model="model" :label-width="labelWidth" :label-position="labelPosition" :inline="inline" :size="size || undefined" :class="$attrs.class" :style="$attrs.style"><slot /></el-form>'
  });

  /** 包装容器：tag 可配置 */
  register('DynWrapper', {
    name: 'DynWrapper',
    inheritAttrs: true,
    props: { tag: { type: String, default: 'div' }, title: { type: String, default: '' } },
    template: '<component :is="tag || \'div\'" :class="$attrs.class" :style="$attrs.style"><slot /></component>'
  });

  /** 插槽占位：设计中标识插槽区域，运行时渲染子组件 */
  register('DynSlot', {
    name: 'DynSlot',
    inheritAttrs: true,
    props: { slotName: { type: String, default: 'default' }, title: { type: String, default: '插槽区域' } },
    template: '<div class="dyn-slot" :class="$attrs.class" :style="$attrs.style"><div class="dyn-slot-title" v-if="title">{{ title }} ({{ slotName }})</div><slot /></div>'
  });

  /** 片段容器：不产生任何包装 DOM */
  register('DynFragment', {
    name: 'DynFragment',
    inheritAttrs: true,
    template: '<slot />'
  });

  /** 循环容器：对 dataSource 每项渲染一次子组件，注入 $item/$index */
  register('LoopItemRender', {
    name: 'LoopItemRender',
    props: { item: { default: null }, index: { type: Number, default: 0 }, vnodes: { type: Array, default: function () { return []; } } },
    setup: function (props) {
      VueObj.provide('dynLoopItem', props.item);
      VueObj.provide('dynLoopIndex', props.index);
      return function () { return props.vnodes; };
    }
  });

  register('DynForEach', {
    name: 'DynForEach',
    inheritAttrs: true,
    props: { dataSource: { type: Array, default: function () { return []; } }, itemName: { type: String, default: 'item' } },
    setup: function (props, context) {
      return function () {
        var data = props.dataSource || [];
        var vnodes = (context.slots.default && context.slots.default()) || [];
        return data.map(function (item, i) {
          return VueObj.h('LoopItemRender', { key: 'loop-' + i, item: item, index: i, vnodes: vnodes });
        });
      };
    },
    template: '<slot />'
  });

  /** 组合组件：开放属性 + 开放容器 + 开放插槽 */
  register('DynCombination', {
    name: 'DynCombination',
    inheritAttrs: true,
    props: { title: { type: String, default: '组合组件' }, layout: { type: String, default: 'block' } },
    template:
      '<section class="dyn-combination" :class="$attrs.class" :style="$attrs.style">' +
      '<div v-if="title" class="dyn-combination-title">{{ title }}</div>' +
      '<div class="dyn-combination-body" :class="\'layout-\' + layout"><slot /></div>' +
      '</section>'
  });

  /** 窗口组件：通过 $api.openWindow(name) 或事件 dyn-open-window 打开 */
  register('DynOpenWindow', {
    name: 'DynOpenWindow',
    inheritAttrs: true,
    props: { name: { type: String, default: '' }, title: { type: String, default: '窗口' }, width: { type: String, default: '60%' }, height: { type: String, default: '' }, modal: { type: Boolean, default: true } },
    data: function () { return { visible: false }; },
    mounted: function () {
      var self = this;
      this._onOpen = function (e) { if (!self.name || e.detail === self.name) self.visible = true; };
      window.addEventListener('dyn-open-window', this._onOpen);
    },
    beforeUnmount: function () { window.removeEventListener('dyn-open-window', this._onOpen); },
    template:
      '<el-dialog v-model="visible" :title="title" :width="width" :modal="modal" :class="$attrs.class" :style="$attrs.style">' +
      '<div :style="height ? {height: height} : {}" class="dyn-window-body"><slot /></div>' +
      '<template #footer><el-button type="primary" @click="visible = false">关闭</el-button></template>' +
      '</el-dialog>'
  });

  /* ================================================================
   * 二、数据组件（程序运行的基础）
   * ================================================================ */

  /** 动态表格：直接按表名读取业务库数据（免模型） */
  register('DynTable', {
    name: 'DynTable',
    inheritAttrs: true,
    props: {
      table: { type: String, default: '' },
      project: { type: String, default: 'Business-School' },
      columns: { type: Array, default: function () { return []; } },
      pageSize: { type: [Number, String], default: 20 },
      pagination: { type: Boolean, default: true }
    },
    data: function () {
      return { rows: [], total: 0, page: 1, size: Number(this.pageSize) || 20, loading: false, filter: {} };
    },
    methods: {
      load: function () {
        var self = this;
        this.loading = true;
        var url = '/api/business/dyndata/page?table=' + encodeURIComponent(this.table) +
          '&project=' + encodeURIComponent(this.project) +
          '&page=' + this.page + '&size=' + this.size;
        if (Object.keys(this.filter).length) url += '&filter=' + encodeURIComponent(JSON.stringify(this.filter));
        fetch(url).then(function (r) { return r.json(); }).then(function (res) {
          self.loading = false;
          if (res.code === 0) { var d = res.data || {}; self.rows = d.rows || d.Rows || []; self.total = d.total || d.Total || 0; }
          else { console.error(res.msg); }
        }).catch(function () { self.loading = false; });
      },
      onSize: function () { this.page = 1; this.load(); },
      refresh: function () { this.load(); }
    },
    mounted: function () { this.load(); },
    template:
      '<div :class="$attrs.class" :style="$attrs.style">' +
      '<el-table :data="rows" border stripe v-loading="loading" size="small">' +
      '<el-table-column v-for="c in columns" :key="c.prop" :prop="c.prop" :label="c.label" :width="c.width" />' +
      '</el-table>' +
      '<el-pagination v-if="pagination && total > 0" style="margin-top:8px;justify-content:flex-end" layout="total, sizes, prev, pager, next" :total="total" :page-sizes="[10,20,50,100]" v-model:current-page="page" v-model:page-size="size" @current-change="load" @size-change="onSize" />' +
      '</div>'
  });

  /** 增删改查页：搜索 + 工具栏 + 表格 + 分页 + 弹窗表单，全程免模型 */
  register('DynCrudPage', {
    name: 'DynCrudPage',
    inheritAttrs: true,
    props: {
      title: { type: String, default: '数据管理' },
      table: { type: String, default: '' },
      project: { type: String, default: 'Business-School' },
      pageSize: { type: [Number, String], default: 20 },
      columns: { type: Array, default: function () { return []; } },
      searchFields: { type: Array, default: function () { return []; } },
      formFields: { type: Array, default: function () { return []; } }
    },
    data: function () {
      return {
        rows: [], total: 0, page: 1, size: Number(this.pageSize) || 20, loading: false,
        search: {}, form: {}, dialog: { visible: false, title: '新增', isEdit: false },
        selection: [], rules: {}
      };
    },
    methods: {
      api: function () { return '/api/business/dyndata'; },
      buildFilter: function () {
        var f = {};
        var self = this;
        (this.searchFields || []).forEach(function (sf) {
          var v = self.search[sf.prop];
          if (v === '' || v === undefined || v === null) return;
          var op = sf.op || 'eq';
          if (op === 'like') f[sf.prop] = { op: 'like', value: v };
          else f[sf.prop] = v;
        });
        return f;
      },
      load: function () {
        var self = this;
        this.loading = true;
        var url = this.api() + '/page?table=' + encodeURIComponent(this.table) +
          '&project=' + encodeURIComponent(this.project) +
          '&page=' + this.page + '&size=' + this.size;
        var f = this.buildFilter();
        if (Object.keys(f).length) url += '&filter=' + encodeURIComponent(JSON.stringify(f));
        fetch(url).then(function (r) { return r.json(); }).then(function (res) {
          self.loading = false;
          if (res.code === 0) { var d = res.data || {}; self.rows = d.rows || d.Rows || []; self.total = d.total || d.Total || 0; }
          else global.ElementPlus && global.ElementPlus.ElMessage.error(res.msg);
        }).catch(function () { self.loading = false; });
      },
      reset: function () { this.search = {}; this.page = 1; this.load(); },
      refresh: function () { this.load(); },
      openAdd: function () {
        this.form = {};
        this.dialog = { visible: true, title: '新增' + (this.title || ''), isEdit: false };
      },
      openEdit: function (row) {
        this.form = Object.assign({}, row);
        this.dialog = { visible: true, title: '编辑' + (this.title || ''), isEdit: true };
      },
      save: function () {
        var self = this;
        fetch(this.api() + '/save', {
          method: 'POST', headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ table: this.table, project: this.project, data: this.form })
        }).then(function (r) { return r.json(); }).then(function (res) {
          if (res.code === 0) {
            global.ElementPlus && global.ElementPlus.ElMessage.success(res.msg || '保存成功');
            self.dialog.visible = false;
            self.load();
          } else {
            global.ElementPlus && global.ElementPlus.ElMessage.error(res.msg);
          }
        });
      },
      remove: function (row) {
        var self = this;
        var keys = { Id: row.Id };
        global.ElementPlus.ElMessageBox.confirm('确定删除该记录吗？', '提示', { type: 'warning' })
          .then(function () {
            return fetch(self.api() + '/delete', {
              method: 'POST', headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ table: self.table, project: self.project, keys: keys })
            }).then(function (r) { return r.json(); });
          })
          .then(function (res) {
            if (res.code === 0) { global.ElementPlus.ElMessage.success('删除成功'); self.load(); }
            else global.ElementPlus.ElMessage.error(res.msg);
          })
          .catch(function () { });
      },
      batchDelete: function () {
        var self = this;
        if (!this.selection.length) return;
        global.ElementPlus.ElMessageBox.confirm('确定删除选中的 ' + this.selection.length + ' 条记录吗？', '提示', { type: 'warning' })
          .then(function () {
            var tasks = self.selection.map(function (row) {
              return fetch(self.api() + '/delete', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ table: self.table, project: self.project, keys: { Id: row.Id } })
              }).then(function (r) { return r.json(); });
            });
            return Promise.all(tasks);
          })
          .then(function () { self.load(); })
          .catch(function () { });
      }
    },
    mounted: function () { this.load(); },
    computed: {
      searchCols: function () { return this.searchFields; }
    },
    template:
      '<div class="dyn-crud-page" :class="$attrs.class" :style="$attrs.style">' +
      '<el-card shadow="never">' +
      '<template #header><b>{{ title }}</b></template>' +

      /* 搜索区 */
      '<el-form inline :model="search" v-if="searchFields.length">' +
      '<el-form-item v-for="f in searchFields" :key="f.prop" :label="f.label">' +
      '<el-input v-if="f.component !== \'ElSelect\'" v-model="search[f.prop]" :placeholder="\'请输入\' + f.label" clearable style="width:160px" @keyup.enter="load" />' +
      '<el-select v-else v-model="search[f.prop]" :placeholder="\'请选择\' + f.label" clearable style="width:160px">' +
      '<el-option v-for="o in (f.options || [])" :key="o.value" :label="o.label" :value="o.value" />' +
      '</el-select>' +
      '</el-form-item>' +
      '<el-form-item><el-button type="primary" @click="page = 1; load()">查询</el-button><el-button @click="reset">重置</el-button></el-form-item>' +
      '</el-form>' +

      /* 工具栏 */
      '<div style="margin-bottom:8px">' +
      '<el-button type="primary" size="small" @click="openAdd">新增</el-button>' +
      '<el-button type="danger" size="small" :disabled="!selection.length" @click="batchDelete">批量删除</el-button>' +
      '<el-button size="small" @click="refresh">刷新</el-button>' +
      '</div>' +

      /* 表格 */
      '<el-table :data="rows" border stripe v-loading="loading" @selection-change="v => selection = v">' +
      '<el-table-column type="selection" width="45" />' +
      '<el-table-column v-for="c in columns" :key="c.prop" :prop="c.prop" :label="c.label" :width="c.width" />' +
      '<el-table-column label="操作" width="140" fixed="right">' +
      '<template #default="{ row }">' +
      '<el-button link type="primary" @click="openEdit(row)">编辑</el-button>' +
      '<el-button link type="danger" @click="remove(row)">删除</el-button>' +
      '</template>' +
      '</el-table-column>' +
      '</el-table>' +

      /* 分页 */
      '<el-pagination v-if="total > 0" style="margin-top:10px;justify-content:flex-end" background layout="total, sizes, prev, pager, next" :total="total" :page-sizes="[10,20,50,100]" v-model:current-page="page" v-model:page-size="size" @current-change="load" @size-change="page = 1; load()" />' +

      /* 弹窗表单 */
      '<el-dialog v-model="dialog.visible" :title="dialog.title" width="560px" destroy-on-close>' +
      '<el-form :model="form" label-width="90px">' +
      '<el-form-item v-for="f in formFields" :key="f.prop" :label="f.label" :required="!!f.required">' +
      '<el-input v-if="f.component === \'ElInput\' || !f.component" v-model="form[f.prop]" :placeholder="\'请输入\' + f.label" />' +
      '<el-input v-else-if="f.component === \'ElTextarea\'" v-model="form[f.prop]" type="textarea" :rows="3" />' +
      '<el-input-number v-else-if="f.component === \'ElInputNumber\'" v-model="form[f.prop]" v-bind="f.props || {}" style="width:100%" />' +
      '<el-select v-else-if="f.component === \'ElSelect\'" v-model="form[f.prop]" style="width:100%" clearable>' +
      '<el-option v-for="o in (f.options || [])" :key="o.value" :label="o.label" :value="o.value" />' +
      '</el-select>' +
      '<el-radio-group v-else-if="f.component === \'ElRadioGroup\'" v-model="form[f.prop]">' +
      '<el-radio v-for="o in (f.options || [])" :key="o.value" :value="o.value">{{ o.label }}</el-radio>' +
      '</el-radio-group>' +
      '<el-date-picker v-else-if="f.component === \'ElDatePicker\'" v-model="form[f.prop]" type="date" value-format="YYYY-MM-DD" style="width:100%" />' +
      '<el-switch v-else-if="f.component === \'ElSwitch\'" v-model="form[f.prop]" />' +
      '</el-form-item>' +
      '</el-form>' +
      '<template #footer>' +
      '<el-button @click="dialog.visible = false">取消</el-button>' +
      '<el-button type="primary" @click="save">保存</el-button>' +
      '</template>' +
      '</el-dialog>' +
      '</el-card>' +
      '</div>'
  });

  /** 动态树：从业务表按父子字段构建树 */
  register('DynTree', {
    name: 'DynTree',
    inheritAttrs: true,
    props: {
      table: { type: String, default: '' },
      project: { type: String, default: 'Business-School' },
      idField: { type: String, default: 'Id' },
      parentField: { type: String, default: 'ParentId' },
      labelField: { type: String, default: 'Name' },
      showCheckbox: { type: Boolean, default: false }
    },
    data: function () { return { treeData: [], loading: false }; },
    methods: {
      load: function () {
        var self = this;
        this.loading = true;
        fetch('/api/business/dyndata/page?table=' + encodeURIComponent(this.table) +
          '&project=' + encodeURIComponent(this.project) + '&page=1&size=1000')
          .then(function (r) { return r.json(); }).then(function (res) {
            self.loading = false;
            if (res.code === 0) self.treeData = self.buildTree((res.data && res.data.rows) || []);
          });
      },
      buildTree: function (rows) {
        var map = {};
        rows.forEach(function (r) { map[r[self.idField]] = Object.assign({}, r, { children: [] }); });
        var roots = [];
        rows.forEach(function (r) {
          var node = map[r[self.idField]];
          var pid = r[self.parentField];
          if (pid && map[pid]) map[pid].children.push(node);
          else roots.push(node);
        });
        var toTree = function (nodes) {
          return nodes.map(function (n) {
            return { label: n[self.labelField] || n[self.idField], value: n[self.idField], children: n.children && n.children.length ? toTree(n.children) : undefined };
          });
        };
        return toTree(roots);
      }
    },
    mounted: function () { this.load(); },
    template:
      '<div :class="$attrs.class" :style="$attrs.style" v-loading="loading">' +
      '<el-tree :data="treeData" :props="{label: \'label\', children: \'children\'}" :show-checkbox="showCheckbox" default-expand-all node-key="value" />' +
      '</div>'
  });

  /* ================================================================
   * 三、展示组件
   * ================================================================ */

  /** 单选组：options=[{label,value}] 渲染 ElRadioGroup */
  register('DynRadioGroup', {
    name: 'DynRadioGroup',
    inheritAttrs: true,
    props: {
      options: { type: Array, default: function () { return []; } },
      disabled: { type: Boolean, default: false },
      size: { type: String, default: '' }
    },
    template: '<el-radio-group v-bind="$attrs" :size="size || undefined"><el-radio v-for="o in options" :key="o.value" :value="o.value" :disabled="disabled">{{ o.label }}</el-radio></el-radio-group>'
  });

  /** 复选组：options=[{label,value}] 渲染 ElCheckboxGroup */
  register('DynCheckboxGroup', {
    name: 'DynCheckboxGroup',
    inheritAttrs: true,
    props: {
      options: { type: Array, default: function () { return []; } },
      disabled: { type: Boolean, default: false },
      size: { type: String, default: '' }
    },
    template: '<el-checkbox-group v-bind="$attrs" :size="size || undefined"><el-checkbox v-for="o in options" :key="o.value" :value="o.value" :disabled="disabled">{{ o.label }}</el-checkbox></el-checkbox-group>'
  });

  /** 下拉选择：options=[{label,value}] 渲染 ElSelect */
  register('DynSelect', {
    name: 'DynSelect',
    inheritAttrs: true,
    props: {
      options: { type: Array, default: function () { return []; } },
      placeholder: { type: String, default: '请选择' },
      clearable: { type: Boolean, default: true },
      disabled: { type: Boolean, default: false },
      size: { type: String, default: '' }
    },
    template: '<el-select v-bind="$attrs" :placeholder="placeholder" :clearable="clearable" :disabled="disabled" :size="size || undefined"><el-option v-for="o in options" :key="o.value" :label="o.label" :value="o.value" /></el-select>'
  });

  /** 文本组件 */
  register('DynText', {
    name: 'DynText',
    inheritAttrs: true,
    props: { text: { type: String, default: '' }, html: { type: Boolean, default: false } },
    template: '<span :class="$attrs.class" :style="$attrs.style"><span v-if="html" v-html="text"></span><span v-else>{{ text }}</span></span>'
  });

  /** 图表组件（ECharts） */
  register('DynEChart', {
    name: 'DynEChart',
    inheritAttrs: true,
    props: { option: { type: Object, default: function () { return {}; } }, height: { type: String, default: '300px' } },
    data: function () { return { chart: null }; },
    mounted: function () {
      if (!global.echarts) return;
      this.chart = global.echarts.init(this.$el);
      this.chart.setOption(this.option || {});
    },
    watch: {
      option: { deep: true, handler: function (v) { if (this.chart) this.chart.setOption(v || {}); } }
    },
    beforeUnmount: function () { if (this.chart) this.chart.dispose(); },
    template: '<div :class="$attrs.class" :style="Object.assign({}, $attrs.style, {height: height})"></div>'
  });

  /** CodeMirror JSON/代码编辑器 */
  register('DynCodeMirror', {
    name: 'DynCodeMirror',
    inheritAttrs: true,
    props: {
      modelValue: { default: '' },
      mode: { type: String, default: 'application/json' },
      height: { type: String, default: '300px' },
      readonly: { type: Boolean, default: false }
    },
    emits: ['update:modelValue'],
    data: function () { return { cm: null }; },
    computed: {
      textValue: function () {
        var v = this.modelValue;
        if (typeof v === 'string') return v;
        try { return JSON.stringify(v, null, 2); } catch (e) { return String(v || ''); }
      }
    },
    mounted: function () {
      if (!global.CodeMirror) return;
      var self = this;
      this.cm = global.CodeMirror(this.$refs.editor, {
        value: this.textValue,
        mode: this.mode || 'application/json',
        lineNumbers: true,
        lineWrapping: true,
        readOnly: !!this.readonly,
        extraKeys: { 'Ctrl-S': function () { } }
      });
      this.cm.on('change', function () {
        self.$emit('update:modelValue', self.cm.getValue());
      });
    },
    watch: {
      textValue: function (v) { if (this.cm && this.cm.getValue() !== v) this.cm.setValue(v); },
      mode: function (m) { if (this.cm) this.cm.setOption('mode', m); }
    },
    beforeUnmount: function () { if (this.cm) this.cm = null; },
    template: '<div :class="$attrs.class" :style="Object.assign({}, $attrs.style, {height: height})" class="dyn-codemirror"><textarea ref="editor" style="width:100%;height:100%"></textarea></div>'
  });

  /** 桌面组件：读取解决方案快捷方式，点击打开页面 */
  register('DynDesktop', {
    name: 'DynDesktop',
    inheritAttrs: true,
    props: { solutionId: { type: String, default: '' }, columns: { type: [Number, String], default: 6 } },
    data: function () { return { solution: null, shortcuts: [] }; },
    methods: {
      load: function () {
        var self = this;
        var solutionId = this.solutionId || 'ERP-LowCode';
        Promise.all([
          fetch('/api/platform/desktopsolution/get?id=' + encodeURIComponent(solutionId)).then(function (r) { return r.json(); }),
          fetch('/api/platform/desktopshortcut/all?solutionId=' + encodeURIComponent(solutionId)).then(function (r) { return r.json(); })
        ]).then(function (results) {
          if (results[0].code === 0) self.solution = results[0].data;
          if (results[1].code === 0) self.shortcuts = (results[1].data || []).filter(function (s) { return s.IsActive !== false; });
        });
      },
      open: function (sc) {
        if (sc.TargetType === 'url') { window.open(sc.Url, '_blank'); return; }
        if (sc.TargetType === 'action' && sc.ActionHelperId) { global.DynActionHelper.run(sc.ActionHelperId, {}, {}); return; }
        if (sc.Url) { window.location.href = sc.Url; }
      }
    },
    mounted: function () { this.load(); },
    template:
      '<div class="dyn-desktop" :class="$attrs.class" :style="$attrs.style">' +
      '<div class="dyn-desktop-title" v-if="solution">{{ solution.Name }}</div>' +
      '<div class="dyn-desktop-grid" :style="{gridTemplateColumns: \'repeat(\' + columns + \', 80px)\'}">' +
      '<div v-for="sc in shortcuts" :key="sc.Id" class="dyn-desktop-icon" @dblclick="open(sc)">' +
      '<div class="dyn-desktop-icon-box"><span class="dyn-desktop-emoji">{{ sc.Icon || "📁" }}</span></div>' +
      '<div class="dyn-desktop-icon-name">{{ sc.Name }}</div>' +
      '</div>' +
      '</div>' +
      '</div>'
  });

  /* ================= 导出 ================= */
  global.DynCom = {
    register: register,
    get: get,
    meta: meta,
    loadMeta: loadMeta,
    setupApp: setupApp,
    registry: registry,
    metaMap: metaMap
  };
})(window);
