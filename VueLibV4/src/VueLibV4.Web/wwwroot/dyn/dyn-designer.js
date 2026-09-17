/* ============================================================
 * VueLibV4  dyn-designer.js  可视化设计器
 * ------------------------------------------------------------
 * 核心设计：
 *   - 画布不包装额外 div：选中框/手柄为"绝对定位悬浮层"，
 *     flex/grid 布局完全不受干扰（子组件 class=col-span-2 生效）
 *   - 组件拖拽准入规则：ComponentMeta.allowDrop / acceptAll
 *   - 属性面板由 ComponentMeta.PropsMeta 自动渲染
 *   - 导入 ElementPlus 模板 / 导出 Vue 模板（DynTemplate）
 * 依赖：dyn-lib / dyn-core / dyn-com / dyn-template（DynLib.ready 后调用 create）
 * ============================================================ */
(function (global) {
  'use strict';

  function create(mountEl) {
    var VueObj = global.Vue;
    var h = VueObj.h;

    /* ================= 状态 ================= */
    var state = VueObj.reactive({
      metaGroups: [],
      metaMap: {},
      actions: [],
      templates: [],
      pages: [],

      pageCfg: null,
      pageId: '',
      pageCode: '',
      pageName: '',

      selected: null,
      selectedUid: '',
      overlay: { uid: '', top: 0, left: 0, width: 0, height: 0, label: '' },
      dropIndicator: { show: false, top: 0, left: 0, width: 0, height: 0 },

      tab: 'props',
      leftTab: 'comp',
      leftCollapsed: false,
      rightCollapsed: false,
      eventRows: [],
      validatorRows: [],
      slotRows: [],

      dragging: '',
      draggingCfg: null,   // 画布内移动的组件配置（非空=移动已有组件，空=从组件库拖入新组件）
      dragOverTarget: null,

      jsonText: '',
      jsonError: '',
      importVisible: false, importText: '', importReport: [],
      exportVisible: false, exportText: '',
      pageDialogVisible: false, pageForm: { name: '', code: '', projectId: 'proj-school', templateId: '' },
      templateDialogVisible: false,
      jsonVisible: false,
      loading: false,

      /* ---- V1 对齐扩展 ---- */
      collapsedCats: {},        // 组件库分组折叠
      showModelDialog: false,   // Model 查看/编辑弹窗
      modelJsonText: '',        // 当前 model JSON 文本
      pathList: [],             // 当前选中组件路径（底部状态栏）
    });

    /* ================= 工具 ================= */
    var uidCounter = 0;
    var uidMap = new WeakMap();
    var uidToCfg = {};

    function uidOf(cfg) {
      if (!uidMap.has(cfg)) {
        uidCounter++;
        uidMap.set(cfg, 'n' + uidCounter);
        uidToCfg['n' + uidCounter] = cfg;
      }
      return uidMap.get(cfg);
    }

    function metaOf(name) { return state.metaMap[name] || null; }

    function isContainer(cfg) {
      var meta = metaOf(cfg && cfg.component);
      if (!meta) return false;
      if (meta.AcceptAll === '1') return true;
      var slots = meta.SlotsDefine || [];
      return slots.indexOf('default') >= 0;
    }

    function checkCanDrop(parentCfg, childName) {
      if (!parentCfg || !childName) return false;
      var pm = metaOf(parentCfg.component);
      if (!pm) return false;
      if (pm.AcceptAll === '1' || pm.AcceptAll === true || String(pm.AcceptAll).toLowerCase() === 'true') return true;
      var allow = pm.AllowDrop || [];
      return allow.indexOf(childName) >= 0;
    }

    function defaultCfg(componentName) {
      var meta = metaOf(componentName);
      var cfg = {
        component: componentName,
        modelname: '',
        options: {
          comoptions: {},
          comlisteners: {},
          labeloptions: { label: '', required: false, show: true },
          itemoptions: { style: {}, class: '' }
        },
        validators: [],
        childrenctrls: [],
        slots: {},
        extendinfo: {}
      };
      if (meta && meta.PropsMeta) {
        meta.PropsMeta.forEach(function (p) {
          if (p && p.name && p.default !== undefined) cfg.options.comoptions[p.name] = p.default;
        });
      }
      // V1 对齐：ElFormItem 拖入时自动用元数据 Label 填充标签（避免空标签）
      if (componentName === 'ElFormItem') {
        cfg.options.labeloptions.label = (meta && meta.Label) || '表单项';
      }
      return cfg;
    }

    function newEmptyPage() {
      return {
        component: 'DynWrapper',
        modelname: '',
        options: { comoptions: { tag: 'div' }, comlisteners: {}, labeloptions: { label: '', required: false, show: false }, itemoptions: { style: { padding: '16px' }, class: '' } },
        validators: [], childrenctrls: [], slots: {}, extendinfo: {}
      };
    }

    /* ================= 加载数据 ================= */
    function loadAll() {
      state.loading = true;
      Promise.all([
        fetch('/api/platform/componentmeta/grouped').then(function (r) { return r.json(); }),
        fetch('/api/platform/dynactionhelper/all').then(function (r) { return r.json(); }),
        fetch('/api/platform/dyntemplate/all').then(function (r) { return r.json(); }),
        fetch('/api/platform/dynwebpage/all').then(function (r) { return r.json(); })
      ]).then(function (results) {
        if (results[0].code === 0) {
          state.metaGroups = results[0].data || [];
          state.metaGroups.forEach(function (g) {
            (g.components || []).forEach(function (m) {
              try { m.PropsMeta = typeof m.PropsMeta === 'string' ? JSON.parse(m.PropsMeta) : (m.PropsMeta || []); } catch (e) { m.PropsMeta = []; }
              try { m.AllowDrop = typeof m.AllowDrop === 'string' ? JSON.parse(m.AllowDrop) : (m.AllowDrop || []); } catch (e) { m.AllowDrop = []; }
              try { m.SlotsDefine = typeof m.SlotsDefine === 'string' ? JSON.parse(m.SlotsDefine) : (m.SlotsDefine || []); } catch (e) { m.SlotsDefine = []; }
              state.metaMap[m.ComponentName] = m;
            });
          });
        }
        if (results[1].code === 0) state.actions = results[1].data || [];
        if (results[2].code === 0) state.templates = results[2].data || [];
        if (results[3].code === 0) state.pages = results[3].data || [];
        state.loading = false;
      }).catch(function () { state.loading = false; });
    }

    /* ================= 选中与悬浮层 ================= */
    function select(cfg) {
      state.selected = cfg;
      state.selectedUid = cfg ? uidOf(cfg) : '';
      pageCtx.selected = cfg; // 供 dyn-core 判断"再点一次选中父级"
      syncPanels(cfg);
      state.pathList = getPathList(cfg);
      VueObj.nextTick(updateOverlay);
    }

    /* 当前选中组件路径链（根 → 当前），供底部状态栏点击跳转 */
    function getPathList(cfg) {
      var arr = [];
      if (!cfg || !state.pageCfg) return arr;
      function find(node, chain) {
        if (node === cfg) { chain.push(node); return true; }
        for (var i = 0; i < (node.childrenctrls || []).length; i++) {
          if (find(node.childrenctrls[i], chain)) { chain.unshift(node); return true; }
        }
        for (var k in (node.slots || {})) {
          for (var j = 0; j < (node.slots[k] || []).length; j++) {
            if (find(node.slots[k][j], chain)) { chain.unshift(node); return true; }
          }
        }
        return false;
      }
      var chain = [];
      find(state.pageCfg, chain);
      return chain; // 根在前
    }

    function syncPanels(cfg) {
      if (!cfg) { state.eventRows = []; state.validatorRows = []; state.slotRows = []; state.jsonText = ''; state.jsonError = ''; return; }
      var comlisteners = cfg.options && cfg.options.comlisteners || {};
      state.eventRows = Object.keys(comlisteners).map(function (k) { return { event: k, action: comlisteners[k] }; });
      state.validatorRows = (cfg.validators || []).map(function (v) { return Object.assign({}, v); });
      var meta = metaOf(cfg.component);
      var slotsDef = (meta && meta.SlotsDefine) || [];
      state.slotRows = slotsDef.map(function (name) {
        var list = (cfg.slots && cfg.slots[name]) || (name === 'default' ? cfg.childrenctrls : []);
        return { name: name, count: list ? list.length : 0 };
      });
      state.jsonText = JSON.stringify(cfg, null, 2);
    }

    /* ---- display:contents 包裹层工具：uid 元素可能是 display:contents span（rect 为 0），
     * 实际定位/高亮应使用第一个有尺寸的子元素（组件根元素）---- */
    function dynNodeEl(uid) {
      var el = document.querySelector('[data-dyn-uid="' + uid + '"]');
      if (!el) return null;
      var r = el.getBoundingClientRect();
      if (r.width === 0 && r.height === 0) {
        for (var i = 0; i < el.children.length; i++) {
          var c = el.children[i];
          var cr = c.getBoundingClientRect();
          if (cr.width > 0 || cr.height > 0) return c;
        }
      }
      return el;
    }

    function updateOverlay() {
      if (!state.selectedUid) { state.overlay.uid = ''; return; }
      var el = dynNodeEl(state.selectedUid);
      var canvas = canvasEl();
      if (!el || !canvas) return;
      var er = el.getBoundingClientRect();
      var cr = canvas.getBoundingClientRect();
      state.overlay = {
        uid: state.selectedUid,
        top: er.top - cr.top,
        left: er.left - cr.left,
        width: er.width,
        height: er.height,
        label: (metaOf(state.selected.component) || {}).Label || state.selected.component
      };
    }

    function onHover(cfg) {
      if (!cfg) return;
      var uid = uidOf(cfg);
      var el = dynNodeEl(uid);
      document.querySelectorAll('.dyn-hover-active').forEach(function (n) { n.classList.remove('dyn-hover-active'); });
      if (el) el.classList.add('dyn-hover-active');
    }

    /* ================= 拖拽 ================= */
    function onDragStart(meta, e) {
      state.dragging = meta.ComponentName;
      state.draggingCfg = null; // 组件库拖入 → 新建
      e.dataTransfer.setData('text/plain', meta.ComponentName);
      e.dataTransfer.effectAllowed = 'copy';
    }

    /** 画布内组件移动：dragstart 由 DynRender 在设计模式触发 */
    function onDragStartCfg(cfg, e) {
      state.dragging = cfg.component;
      state.draggingCfg = cfg; // 移动已有组件
      e.dataTransfer.setData('text/plain', cfg.component);
      e.dataTransfer.effectAllowed = 'move';
    }

    function onDragOver(targetCfg, e) {
      if (!state.dragging) return;
      e.preventDefault();
      e.stopPropagation();
      state.dragOverTarget = targetCfg;
      if (!targetCfg) { state.dropIndicator.show = false; return; }
      var ok = checkCanDrop(targetCfg, state.dragging);
      var el = dynNodeEl(uidOf(targetCfg));
      var canvas = canvasEl();
      // 清除旧的容器高亮
      document.querySelectorAll('.drag-target-active').forEach(function (n) { n.classList.remove('drag-target-active'); });
      if (ok && el && canvas) {
        var er = el.getBoundingClientRect();
        var cr = canvas.getBoundingClientRect();
        state.dropIndicator = { show: true, top: er.top - cr.top, left: er.left - cr.left, width: er.width, height: er.height };
        el.classList.add('drag-target-active'); // 容器高亮占位
      } else if (el && canvas) {
        // 非容器：显示插入线（拖到组件上 = 插入到其前方）
        var er2 = el.getBoundingClientRect();
        var cr2 = canvas.getBoundingClientRect();
        state.dropIndicator = { show: true, insert: true, top: er2.top - cr2.top - 2, left: er2.left - cr2.left, width: er2.width, height: 4 };
      } else {
        state.dropIndicator.show = false;
      }
    }

    function onDragLeave(targetCfg, e) {
      if (!targetCfg) return;
      var el = dynNodeEl(uidOf(targetCfg));
      if (el) el.classList.remove('drag-target-active');
    }

    function isDescendantOf(cfg, ancestor) {
      // cfg 是否在 ancestor 的子树内（含自身）
      function walk(node) {
        if (node === cfg) return true;
        for (var i = 0; i < (node.childrenctrls || []).length; i++) { if (walk(node.childrenctrls[i])) return true; }
        for (var k in (node.slots || {})) {
          for (var j = 0; j < (node.slots[k] || []).length; j++) { if (walk(node.slots[k][j])) return true; }
        }
        return false;
      }
      return ancestor ? walk(ancestor) : false;
    }

    function onDrop(targetCfg, e) {
      if (!state.dragging) return;
      e.preventDefault();
      e.stopPropagation();
      clearDragHighlights();
      // 画布内移动：禁止拖根容器 / 拖到自身或子孙（防止循环引用）
      if (state.draggingCfg) {
        if (state.draggingCfg === state.pageCfg || targetCfg === state.draggingCfg || isDescendantOf(targetCfg, state.draggingCfg)) {
          global.ElementPlus && global.ElementPlus.ElMessage.warning('不能将容器移动到自身或其内部');
          state.dragging = '';
          state.draggingCfg = null;
          return;
        }
      }
      if (targetCfg && checkCanDrop(targetCfg, state.dragging)) {
        if (state.draggingCfg) {
          // 画布内移动：从原位置移除 → 追加到目标容器末尾
          var hit = findParent(state.pageCfg, state.draggingCfg);
          if (hit) {
            var oldArr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots[hit.slot] || []);
            var oi = oldArr.indexOf(state.draggingCfg);
            if (oi >= 0) oldArr.splice(oi, 1);
          }
          targetCfg.childrenctrls = targetCfg.childrenctrls || [];
          targetCfg.childrenctrls.push(state.draggingCfg);
          select(state.draggingCfg);
        } else {
          var child = defaultCfg(state.dragging);
          targetCfg.childrenctrls = targetCfg.childrenctrls || [];
          targetCfg.childrenctrls.push(child);
          select(targetCfg.childrenctrls[targetCfg.childrenctrls.length - 1]);
        }
      } else {
        // 目标不是容器 → 作为兄弟插入到目标组件之前（画布内重排/组件库拖入到组件前）
        var phit = findParent(state.pageCfg, targetCfg);
        if (phit && phit.parent) {
          var arr = phit.slot === 'childrenctrls' ? phit.parent.childrenctrls : (phit.parent.slots[phit.slot] || []);
          if (state.draggingCfg) {
            var hit2 = findParent(state.pageCfg, state.draggingCfg);
            if (hit2) {
              var oldArr2 = hit2.slot === 'childrenctrls' ? hit2.parent.childrenctrls : (hit2.parent.slots[hit2.slot] || []);
              var oi2 = oldArr2.indexOf(state.draggingCfg);
              if (oi2 >= 0) oldArr2.splice(oi2, 1);
            }
            var ti2 = arr.indexOf(targetCfg);
            if (ti2 >= 0) arr.splice(ti2, 0, state.draggingCfg);
            select(state.draggingCfg);
          } else {
            var child2 = defaultCfg(state.dragging);
            var ti3 = arr.indexOf(targetCfg);
            if (ti3 >= 0) arr.splice(ti3, 0, child2);
            select(child2);
          }
        } else {
          global.ElementPlus && global.ElementPlus.ElMessage.warning('不允许将 ' + state.dragging + ' 放入 ' + (targetCfg && targetCfg.component));
        }
      }
      state.dragging = '';
      state.draggingCfg = null;
    }

    function clearDragHighlights() {
      state.dropIndicator.show = false;
      document.querySelectorAll('.drag-target-active').forEach(function (n) { n.classList.remove('drag-target-active'); });
    }

    function dropToRoot(e) {
      if (!state.dragging) return;
      e.preventDefault();
      clearDragHighlights();
      if (state.draggingCfg) {
        // 禁止移动根容器到自身
        if (state.draggingCfg === state.pageCfg) {
          global.ElementPlus && global.ElementPlus.ElMessage.warning('根容器不能移动');
          state.dragging = '';
          state.draggingCfg = null;
          return;
        }
        // 画布内移动 → 放到画布根
        var hit = findParent(state.pageCfg, state.draggingCfg);
        if (hit) {
          var oldArr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots[hit.slot] || []);
          var oi = oldArr.indexOf(state.draggingCfg);
          if (oi >= 0) oldArr.splice(oi, 1);
        }
        state.pageCfg.childrenctrls = state.pageCfg.childrenctrls || [];
        state.pageCfg.childrenctrls.push(state.draggingCfg);
        select(state.draggingCfg);
      } else if (state.pageCfg && checkCanDrop(state.pageCfg, state.dragging)) {
        state.pageCfg.childrenctrls = state.pageCfg.childrenctrls || [];
        var child = defaultCfg(state.dragging);
        state.pageCfg.childrenctrls.push(child);
        select(state.pageCfg.childrenctrls[state.pageCfg.childrenctrls.length - 1]);
      } else if (!state.pageCfg) {
        state.pageCfg = defaultCfg(state.dragging);
        select(state.pageCfg);
      } else {
      }
      state.dragging = '';
      state.draggingCfg = null;
    }

    /* ================= 节点操作 ================= */
    function findParent(root, target) {
      if (!root || !target) return null;
      var hit = { parent: null, slot: null };
      (function walk(node) {
        if (!node || node === target) return;
        var children = node.childrenctrls || [];
        for (var i = 0; i < children.length; i++) {
          if (children[i] === target) { hit.parent = node; hit.slot = 'childrenctrls'; return; }
          walk(children[i]);
          if (hit.parent) return;
        }
        var slots = node.slots || {};
        Object.keys(slots).forEach(function (k) {
          if (hit.parent) return;
          var arr = slots[k] || [];
          for (var j = 0; j < arr.length; j++) {
            if (arr[j] === target) { hit.parent = node; hit.slot = k; return; }
            walk(arr[j]);
            if (hit.parent) return;
          }
        });
      })(root);
      return hit.parent ? hit : null;
    }

    function removeSelected() {
      var sel = state.selected;
      if (!sel) return;
      if (sel === state.pageCfg) { state.pageCfg = newEmptyPage(); select(null); return; }
      var hit = findParent(state.pageCfg, sel);
      if (hit) {
        var arr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots[hit.slot] || []);
        var idx = arr.indexOf(sel);
        if (idx >= 0) arr.splice(idx, 1);
      }
      select(hit ? hit.parent : null);
    }

    function duplicateSelected() {
      var sel = state.selected;
      if (!sel) return;
      var hit = findParent(state.pageCfg, sel);
      if (hit) {
        var copy = JSON.parse(JSON.stringify(sel));
        var arr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots[hit.slot] || []);
        arr.splice(arr.indexOf(sel) + 1, 0, copy);
        select(copy);
      }
    }

    function moveSelected(dir) {
      var sel = state.selected;
      if (!sel) return;
      var hit = findParent(state.pageCfg, sel);
      if (!hit) return;
      var arr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots[hit.slot] || []);
      var idx = arr.indexOf(sel);
      var to = idx + dir;
      if (to < 0 || to >= arr.length) return;
      arr.splice(idx, 1);
      arr.splice(to, 0, sel);
      VueObj.nextTick(updateOverlay);
    }

    /* ================= 保存/加载 ================= */
    function savePage() {
      if (!state.pageCfg) { global.ElementPlus.ElMessage.warning('页面为空'); return; }
      var cleaned = global.DynCore.clean(state.pageCfg);
      var body = {
        Id: state.pageId || undefined,
        Name: state.pageName || state.pageCode || '未命名页面',
        Code: state.pageCode || ('page-' + Date.now()),
        ProjectId: 'proj-platform',
        TemplateId: state.templateId || undefined,
        PageJson: JSON.stringify(cleaned),
        ConfigJson: '{}',
        Url: state.pageCode ? '/Platform/Page/WebPageRender?code=' + state.pageCode : ''
      };
      fetch('/api/platform/dynwebpage/save', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body)
      }).then(function (r) { return r.json(); }).then(function (res) {
        if (res.code === 0) {
          global.ElementPlus.ElMessage.success('保存成功');
          if (!state.pageId && res.data && res.data !== true) state.pageId = res.data;
          loadAll();
        } else global.ElementPlus.ElMessage.error(res.msg);
      });
    }

    function openPage(row) {
      fetch('/api/platform/dynwebpage/render?id=' + encodeURIComponent(row.Id))
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res.code === 0 && res.data && res.data.config) {
            state.pageCfg = global.DynCore.normalize(res.data.config);
            state.pageId = res.data.id || row.Id;
            state.pageCode = res.data.code || row.Code;
            state.pageName = res.data.name || row.Name;
            select(null);
            global.ElementPlus.ElMessage.success('已打开页面：' + state.pageName);
          } else global.ElementPlus.ElMessage.error(res.msg || '加载失败');
        });
    }

    function loadTemplate(row) {
      fetch('/api/platform/dyntemplate/config?id=' + encodeURIComponent(row.Id))
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res.code === 0) {
            state.pageCfg = global.DynCore.normalize(res.data);
            state.pageId = '';
            state.pageCode = '';
            state.pageName = row.Name;
            state.templateId = row.Id;
            select(null);
            global.ElementPlus.ElMessage.success('已加载模板：' + row.Name);
          } else global.ElementPlus.ElMessage.error(res.msg);
        });
    }

    /* ================= 面板同步 ================= */
    function applyEvents() {
      if (!state.selected) return;
      var listeners = {};
      state.eventRows.forEach(function (r) {
        if (r.event && r.action) listeners[r.event] = r.action;
      });
      state.selected.options.comlisteners = listeners;
    }

    function applyValidators() {
      if (!state.selected) return;
      state.selected.validators = state.validatorRows.filter(function (v) { return v.type; });
    }

    function applyJson() {
      if (!state.selected) return;
      try {
        var obj = JSON.parse(state.jsonText);
        Object.keys(state.selected).forEach(function (k) { delete state.selected[k]; });
        Object.keys(obj).forEach(function (k) { state.selected[k] = obj[k]; });
        state.jsonError = '';
        syncPanels(state.selected);
        state.pathList = getPathList(state.selected);
        VueObj.nextTick(updateOverlay);
        global.ElementPlus.ElMessage.success('JSON 已应用');
      } catch (e) {
        state.jsonError = e.message;
        global.ElementPlus.ElMessage.error('JSON 格式错误: ' + e.message);
      }
    }

    /* ---- JSON 面板实时校验（输入过程不应用，仅提示；点"应用JSON"才回写） ---- */
    function onJsonInput(v) {
      state.jsonText = v;
      try { JSON.parse(v); state.jsonError = ''; }
      catch (e) { state.jsonError = e.message; }
    }

    /* ---- Model 查看/编辑弹窗 ---- */
    function openModelDialog() {
      state.modelJsonText = JSON.stringify(pageCtx.pageData || {}, null, 2);
      state.showModelDialog = true;
    }
    function applyModelJson() {
      try {
        var obj = JSON.parse(state.modelJsonText);
        Object.keys(pageCtx.pageData).forEach(function (k) { delete pageCtx.pageData[k]; });
        Object.keys(obj).forEach(function (k) { pageCtx.pageData[k] = obj[k]; });
        state.showModelDialog = false;
        global.ElementPlus.ElMessage.success('Model 已应用（页面数据）');
      } catch (e) {
        global.ElementPlus.ElMessage.error('JSON 格式错误: ' + e.message);
      }
    }
    function addModelField() {
      try {
        var obj = JSON.parse(state.modelJsonText || '{}');
        var k = 'field' + Object.keys(obj).length;
        obj[k] = '';
        state.modelJsonText = JSON.stringify(obj, null, 2);
      } catch (e) { /* ignore */ }
    }

    /* ================= 导入导出 ================= */
    function doImport() {
      try {
        var roots = global.DynTemplate.importTemplate(state.importText);
        state.importReport = roots.map(function (r) { return '✓ 导入组件: ' + r.component; });
        if (roots.length && state.pageCfg) {
          roots.forEach(function (r) {
            state.pageCfg.childrenctrls = state.pageCfg.childrenctrls || [];
            state.pageCfg.childrenctrls.push(r);
          });
          select(null);
        } else if (roots.length) {
          state.pageCfg = roots[0];
          select(null);
        }
        global.ElementPlus.ElMessage.success('导入成功 ' + roots.length + ' 个组件');
      } catch (e) {
        global.ElementPlus.ElMessage.error('导入失败: ' + e.message);
        state.importReport.push('✗ ' + e.message);
      }
    }

    function doExport() {
      if (!state.pageCfg) return;
      state.exportText = global.DynTemplate.exportTemplate(state.pageCfg);
      state.exportVisible = true;
    }

    /* ================= 属性面板辅助 ================= */
    function selectedMeta() { return state.selected ? metaOf(state.selected.component) : null; }

    function styleText() {
      if (!state.selected) return '';
      return JSON.stringify((state.selected.options.itemoptions && state.selected.options.itemoptions.style) || {}, null, 2);
    }
    function applyStyleText(v) {
      if (!state.selected) return;
      try { state.selected.options.itemoptions.style = JSON.parse(v || '{}'); } catch (e) { /* 编辑中忽略 */ }
    }
    function classText() { return state.selected ? ((state.selected.options.itemoptions || {}).class || '') : ''; }

    /* ================= 结构树 ================= */
    function structTree() {
      if (!state.pageCfg) return [];
      function walk(cfg) {
        var meta = metaOf(cfg.component) || {};
        var label = meta.Label || cfg.component;
        var children = (cfg.childrenctrls || []).map(walk);
        var slots = cfg.slots || {};
        Object.keys(slots).forEach(function (k) {
          children.push({ uid: 'slot-' + uidOf(cfg) + '-' + k, label: '#' + k, icon: '📥', isSlot: true, cfg: null, children: (slots[k] || []).map(walk) });
        });
        return { uid: uidOf(cfg), label: label, icon: meta.Icon || '🧩', cfg: cfg, children: children };
      }
      return [walk(state.pageCfg)];
    }

    /* 组件树拖拽排序：拖拽树节点 → 移动配置树对应节点 */
    function onTreeNodeDrop(draggingNode, dropNode, dropType) {
      var dragCfg = draggingNode && draggingNode.data && draggingNode.data.cfg;
      var dropCfg = dropNode && dropNode.data && dropNode.data.cfg;
      if (!dragCfg || !dropCfg) return;
      if (dragCfg === dropCfg || isDescendantOf(dropCfg, dragCfg)) {
        global.ElementPlus && global.ElementPlus.ElMessage.warning('不能将容器移动到自身或其内部');
        return;
      }
      // 从原位置移除
      var hit = findParent(state.pageCfg, dragCfg);
      if (!hit) return;
      var oldArr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots[hit.slot] || []);
      var oi = oldArr.indexOf(dragCfg);
      if (oi >= 0) oldArr.splice(oi, 1);
      if (dropType === 'inner') {
        // 放入目标内部末尾
        dropCfg.childrenctrls = dropCfg.childrenctrls || [];
        dropCfg.childrenctrls.push(dragCfg);
      } else {
        // before/after：插入目标前/后
        var dhit = findParent(state.pageCfg, dropCfg);
        var arr = dhit ? (dhit.slot === 'childrenctrls' ? dhit.parent.childrenctrls : (dhit.parent.slots[dhit.slot] || [])) : null;
        if (arr) {
          var di = arr.indexOf(dropCfg);
          arr.splice(dropType === 'after' ? di + 1 : di, 0, dragCfg);
        } else {
          state.pageCfg.childrenctrls = state.pageCfg.childrenctrls || [];
          state.pageCfg.childrenctrls.push(dragCfg);
        }
      }
      select(dragCfg);
      global.ElementPlus && global.ElementPlus.ElMessage.success('已移动 ' + ((metaOf(dragCfg.component) || {}).Label || dragCfg.component));
    }

    /* ================= 画布元素 ================= */
    var _canvasEl = null;
    function canvasEl() {
      if (!_canvasEl) _canvasEl = document.querySelector('.designer-canvas');
      return _canvasEl;
    }

    /* ================= 父级查找（再点一次选中父容器） ================= */
    function parentOf(cfg) {
      function find(node, parent) {
        if (node === cfg) return parent;
        for (var i = 0; i < (node.childrenctrls || []).length; i++) {
          var r = find(node.childrenctrls[i], node);
          if (r) return r;
        }
        for (var k in (node.slots || {})) {
          for (var j = 0; j < (node.slots[k] || []).length; j++) {
            var r2 = find(node.slots[k][j], node);
            if (r2) return r2;
          }
        }
        return null;
      }
      return state.pageCfg ? find(state.pageCfg, null) : null;
    }

    /* ================= 内置 JSON 编辑器组件 =================
     * DynCodeMirror 在 DB/View 中的定义缺失（FormItem 契约 + 无模板），
     * 设计器内直接内置一个纯 CodeMirror 编辑器，供 JSON tab / Model / 页面 JSON 弹窗使用。
     */
    var DynJsonEditor = {
      name: 'DynJsonEditor',
      props: {
        modelValue: { type: String, default: '' },
        mode: { type: String, default: 'application/json' },
        height: { type: String, default: '300px' }
      },
      emits: ['update:modelValue'],
      data: function () { return { cm: null }; },
      mounted: function () {
        var self = this;
        if (typeof window.CodeMirror !== 'function') return;
        // 使用 CodeMirror(container, options) 容器方式：value 参数直接生效，
        // 不依赖 fromTextArea（其初始化时机不可靠，容易在隐藏容器中不渲染视图）
        var cmMode = self.mode;
        if (cmMode === 'application/json' && window.CodeMirror && !window.CodeMirror.modes.json) {
          cmMode = 'text/javascript';
        }
        self.cm = window.CodeMirror(this.$el, {
            mode: cmMode,
            lineNumbers: true,
            lineWrapping: true,
            viewportMargin: Infinity,
            value: self.modelValue || ''
          });
        this.$el.__dynCm = self.cm; // 供外部程序化读取/测试
        if (!self.cm) return;
        // CM 可能创建于隐藏容器（el-tabs 懒渲染、弹窗未打开）→ 视图不渲染。
        // 轮询等待容器可见后 refresh（display:none 时 ResizeObserver 不触发，轮询最可靠）
        self._poll = setInterval(function () {
          if (!self.cm) { clearInterval(self._poll); return; }
          if (self.$el.clientHeight > 0) {
            self.cm.refresh();
            var sc = self.cm.getScrollerElement();
            var sizer = sc && sc.querySelector('.CodeMirror-sizer');
            if (sizer && sizer.clientHeight > 20) clearInterval(self._poll);
          }
        }, 300);
        setTimeout(function () { if (self._poll) { clearInterval(self._poll); self._poll = null; } }, 6000);
        // 容器可能处于隐藏/尺寸 0 状态（el-tabs 懒渲染、弹窗未打开），
        // 定时兜底 refresh
        [100, 300, 800].forEach(function (t) {
          setTimeout(function () { if (self.cm) self.cm.refresh(); }, t);
        });
        self.cm.on('change', function () {
          self.$emit('update:modelValue', self.cm.getValue());
        });
      },
      beforeUnmount: function () {
        if (this._poll) { clearInterval(this._poll); this._poll = null; }
        if (this.cm) { this.cm = null; }
      },
      watch: {
        modelValue: function (v) {
          if (this.cm && this.cm.getValue() !== (v || '')) this.cm.setValue(v || '');
        }
      },
      template: '<div class="dyn-json-editor" :style="{height: height}"></div>'
    };

    /* ================= 根组件 ================= */
    var pageCtx = VueObj.reactive({
      designMode: true,
      pageData: VueObj.reactive({}),
      selected: null,
      uidOf: uidOf,
      parentOf: parentOf,
      isContainer: isContainer,
      onSelect: select,
      onHover: onHover,
      onDragStartCfg: onDragStartCfg,
      onDragOver: onDragOver,
      onDragLeave: onDragLeave,
      onDrop: onDrop,
      openWindow: function (name) { window.dispatchEvent(new CustomEvent('dyn-open-window', { detail: name })); }
    });

    var Root = {
      provide: function () { return { pageCtx: pageCtx }; },
      data: function () { return { st: state }; },
      computed: {
        selectedMeta: selectedMeta,
        overlayStyle: function () {
          var o = state.overlay;
          return { top: o.top + 'px', left: o.left + 'px', width: o.width + 'px', height: o.height + 'px' };
        },
        dropStyle: function () {
          var d = state.dropIndicator;
          if (!d.show) return { display: 'none' };
          if (d.insert) {
            // 插入线：细线指示"插入到组件前方"（兄弟插入）
            return { display: 'block', top: d.top + 'px', left: d.left + 'px', width: d.width + 'px', height: '4px', background: '#409eff', borderRadius: '2px', boxShadow: '0 0 4px rgba(64,158,255,.6)' };
          }
          return { display: 'block', top: d.top + 'px', left: d.left + 'px', width: d.width + 'px', height: d.height + 'px' };
        },
        structData: structTree,
        styleTextValue: styleText
      },
      methods: {
        uidOf: uidOf, metaOf: metaOf, isContainer: isContainer, checkCanDrop: checkCanDrop,
        select: select, updateOverlay: updateOverlay,
        overlayClick: function () {
          // 悬浮框盖在组件上拦截点击：点击它 = 再点一次选中组件 → 选中父容器
          if (!state.selected) return;
          var parent = parentOf(state.selected);
          select(parent || state.selected);
        },
        onDragStart: onDragStart, dropToRoot: dropToRoot,
        removeSelected: removeSelected, duplicateSelected: duplicateSelected, moveSelected: moveSelected,
        savePage: savePage, openPage: openPage, loadTemplate: loadTemplate,
        applyEvents: applyEvents, applyValidators: applyValidators, applyJson: applyJson,
        onJsonInput: onJsonInput,
        openModelDialog: openModelDialog, applyModelJson: applyModelJson, addModelField: addModelField,
        onTreeNodeDrop: onTreeNodeDrop,
        toggleCat: function (cat) { state.collapsedCats[cat] = !state.collapsedCats[cat]; },
        isCatCollapsed: function (cat) { return !!state.collapsedCats[cat]; },
        doImport: doImport, doExport: doExport,
        applyStyleText: applyStyleText, classText: classText,
        newPage: function () {
          state.pageCfg = newEmptyPage(); state.pageId = ''; state.pageCode = ''; state.pageName = ''; state.templateId = '';
          select(null);
        },
        preview: function () {
          if (!state.pageId && !state.pageCode) { global.ElementPlus.ElMessage.warning('请先保存页面'); return; }
          savePage();
          setTimeout(function () {
            var url = state.pageCode ? '/Platform/Page/WebPageRender?code=' + encodeURIComponent(state.pageCode) : '/Platform/Page/WebPageRender?id=' + state.pageId;
            window.open(url, '_blank');
          }, 300);
        },
        openPageDialog: function () { state.pageDialogVisible = true; },
        openTemplateDialog: function () { state.templateDialogVisible = true; },
        openImport: function () { state.importVisible = true; state.importText = ''; state.importReport = []; },
        openExport: doExport,
        openJson: function () { state.jsonVisible = true; state.jsonText = JSON.stringify(state.pageCfg || {}, null, 2); },
        applyPageJson: function () {
          try {
            state.pageCfg = global.DynCore.normalize(JSON.parse(state.jsonText));
            state.jsonVisible = false;
            select(null);
            global.ElementPlus.ElMessage.success('页面 JSON 已应用');
          } catch (e) { global.ElementPlus.ElMessage.error('JSON 格式错误'); }
        },
        copyText: function () {
          navigator.clipboard.writeText(state.exportText).then(function () {
            global.ElementPlus.ElMessage.success('已复制');
          });
        }
      },
      mounted: function () {
        var self = this;
        loadAll();
        state.pageCfg = newEmptyPage();
        window.addEventListener('resize', function () { self.updateOverlay(); });
        if (window.ResizeObserver) {
          var obs = new ResizeObserver(function () { self.updateOverlay(); });
          var canvas = document.querySelector('.designer-center');
          if (canvas) obs.observe(canvas);
        }
      },
      template: ''
    };

    /* ================= 模板 ================= */
    Root.template = [
      '<div class="designer-body">',
      /* ---- 顶部工具栏 ---- */
      '<div class="designer-topbar">',
      '<span class="designer-logo">VueLibV4 设计器</span>',
      '<el-button size="small" type="primary" @click="newPage">新建页面</el-button>',
      '<el-button size="small" @click="openTemplateDialog">从模板创建</el-button>',
      '<el-button size="small" @click="openPageDialog">打开页面</el-button>',
      '<el-button size="small" @click="savePage">保存</el-button>',
      '<el-button size="small" @click="preview" type="success">预览</el-button>',
      '<el-divider direction="vertical" />',
      '<el-button size="small" @click="openImport">导入模板</el-button>',
      '<el-button size="small" @click="openExport">导出模板</el-button>',
      '<el-button size="small" @click="openJson">页面JSON</el-button>',
      '<el-button size="small" type="warning" plain @click="openModelDialog">Model 查看</el-button>',
      '<span style="flex:1"></span>',
      '<el-tag size="small" v-if="st.pageName">{{ st.pageName }}</el-tag>',
      '</div>',

      '<div class="designer-layout">',
      /* ---- 左侧面板（组件库/模板/结构树 Tab + 折叠） ---- */
      '<div class="designer-left" :class="{collapsed: st.leftCollapsed}">',
      '<div class="designer-left-body" v-if="!st.leftCollapsed">',
      '<el-tabs v-model="st.leftTab" class="designer-left-tabs">',
      '<el-tab-pane label="🧩 组件库" name="comp">',
      '<div class="comp-scroll">',
      '<div v-for="g in st.metaGroups" :key="g.category">',
      '<div class="comp-category" @click="toggleCat(g.category)">{{ g.category }}<span class="cat-arrow" :class="{collapsed: isCatCollapsed(g.category)}">▼</span></div>',
      '<template v-if="!isCatCollapsed(g.category)">',
      '<div class="comp-item" v-for="c in g.components" :key="c.ComponentName" draggable="true" @dragstart="onDragStart(c, $event)">',
      '<span class="comp-emoji">{{ c.Icon || "🧩" }}</span>{{ c.Label }}',
      '</div>',
      '</template>',
      '</div>',
      '</div>',
      '</el-tab-pane>',
      '<el-tab-pane label="📄 模板" name="tpl">',
      '<div class="comp-item" v-for="t in st.templates" :key="t.Id" @click="loadTemplate(t)">📄 {{ t.Name }}</div>',
      '</el-tab-pane>',
      '<el-tab-pane label="🌳 结构树" name="tree">',
      '<div class="designer-tree">',
      '<el-tree :data="structData" :props="{label: \'label\', children: \'children\'}" default-expand-all node-key="uid" highlight-current :current-node-key="st.selectedUid" draggable @node-drop="onTreeNodeDrop" @node-click="(n) => n.cfg && select(n.cfg)">',
      '<template #default="{ data }"><span class="dyn-struct-node" :class="{active: st.selectedUid === data.uid}"><span class="tree-icon">{{ data.icon }}</span>{{ data.label }}</span></template>',
      '</el-tree>',
      '</div>',
      '</el-tab-pane>',
      '</el-tabs>',
      '</div>',
      '<div class="designer-panel-toggle left" @click="st.leftCollapsed = !st.leftCollapsed" :title="st.leftCollapsed ? \'展开左侧\' : \'收起左侧\'">{{ st.leftCollapsed ? "▶" : "◀" }}</div>',
      '</div>',

      /* ---- 中间画布 ---- */
      '<div class="designer-center" @dragover.prevent @drop.prevent="dropToRoot">',
      '<div ref="canvasRef" class="designer-canvas design-mode" @click.self="select(null)">',
      '<DynRender :cfg="st.pageCfg" />',
      '<div class="dyn-overlay-container">',
      '<div v-if="st.overlay.uid" class="dyn-overlay" :style="overlayStyle" @click.stop="overlayClick">',
      '<div class="dyn-overlay-tools">',
      '<button @click.stop="moveSelected(-1)" title="上移">↑</button>',
      '<button @click.stop="moveSelected(1)" title="下移">↓</button>',
      '<button @click.stop="duplicateSelected">复制</button>',
      '<button @click.stop="removeSelected" style="color:#f56c6c">删除</button>',
      '<span class="dyn-overlay-label">{{ st.overlay.label }}</span>',
      '</div>',
      '</div>',
      '</div>',
      '<div class="dyn-drop-indicator" :style="dropStyle"></div>',
      '</div>',
      '</div>',

      /* ---- 右侧属性面板 ---- */
      '<div class="designer-right" :class="{collapsed: st.rightCollapsed}">',
      '<div class="designer-right-body" v-if="!st.rightCollapsed">',
      '<div class="panel-title" v-if="st.selected">{{ st.overlay.label }} 属性</div>',
      '<div class="panel-title" v-else>属性面板（请选择组件）</div>',
      '<el-tabs v-model="st.tab" style="flex:1;display:flex;flex-direction:column;min-height:0">',
      '<el-tab-pane label="属性" name="props">',
      '<div class="prop-body" v-if="st.selected">',
      '<div class="prop-group">基础</div>',
      '<div class="prop-item"><label>组件</label><el-input :model-value="st.selected.component" readonly /></div>',
      '<div class="prop-item"><label>数据绑定 modelname</label><el-input v-model="st.selected.modelname" placeholder="如 formData.name" /></div>',
      '<div class="prop-item"><label>标签</label><el-input v-model="st.selected.options.labeloptions.label" placeholder="表单标签" /></div>',
      '<div class="prop-item"><label>必填</label><el-switch v-model="st.selected.options.labeloptions.required" /></div>',
      '<div class="prop-item"><label>显示标签</label><el-switch v-model="st.selected.options.labeloptions.show" /></div>',
      '<div class="prop-item"><label>标签宽度</label><el-input v-model="st.selected.options.labeloptions.labelWidth" placeholder="如 120px / 30% / 12em" /></div>',
      '<div class="prop-group" v-if="selectedMeta && selectedMeta.PropsMeta && selectedMeta.PropsMeta.length">组件属性（PropsMeta 自动渲染）</div>',
      '<div class="prop-item" v-for="p in (selectedMeta ? selectedMeta.PropsMeta : [])" :key="p.name">',
      '<label>{{ p.label || p.name }}</label>',
      '<el-input v-if="p.type === \'string\' || p.type === \'textarea\'" v-model="st.selected.options.comoptions[p.name]" :type="p.type === \'textarea\' ? \'textarea\' : \'text\'" :rows="2" />',
      '<el-input-number v-else-if="p.type === \'number\'" v-model="st.selected.options.comoptions[p.name]" style="width:100%" />',
      '<el-switch v-else-if="p.type === \'boolean\'" v-model="st.selected.options.comoptions[p.name]" />',
      '<el-select v-else-if="p.type === \'select\'" v-model="st.selected.options.comoptions[p.name]" clearable style="width:100%">',
      '<el-option v-for="o in (p.options || [])" :key="o.value" :label="o.label" :value="o.value" />',
      '</el-select>',
      '<el-color-picker v-else-if="p.type === \'color\'" v-model="st.selected.options.comoptions[p.name]" />',
      '<div v-else-if="p.type === \'options\'" style="width:100%">',
      '<div v-for="(o, oi) in (st.selected.options.comoptions[p.name] || [])" :key="oi" style="display:flex;gap:4px;margin-bottom:4px">',
      '<el-input v-model="o.label" placeholder="显示文本" size="small" style="flex:1" />',
      '<el-input v-model="o.value" placeholder="值" size="small" style="flex:1" />',
      '<el-button size="small" @click="st.selected.options.comoptions[p.name].splice(oi,1)">-</el-button>',
      '</div>',
      '<el-button size="small" type="primary" plain @click="st.selected.options.comoptions[p.name] = st.selected.options.comoptions[p.name] || []; st.selected.options.comoptions[p.name].push({label:\'\', value:\'\'})">添加选项</el-button>',
      '</div>',
      '<el-input v-else v-model="st.selected.options.comoptions[p.name]" type="textarea" :rows="3" />',
      '</div>',
      '</div>',
      '<el-empty v-else description="点击画布中的组件查看属性" />',
      '</el-tab-pane>',

      '<el-tab-pane label="样式" name="style">',
      '<div class="prop-body" v-if="st.selected">',
      '<div class="prop-group">容器/组件 CSS 类（grid col-span-2 等）</div>',
      '<div class="prop-item"><label>class</label><el-input :model-value="classText()" @update:model-value="v => st.selected.options.itemoptions.class = v" placeholder="如 col-span-2, mt-2" /></div>',
      '<div class="prop-group">行内样式（JSON）</div>',
      '<div class="prop-item"><label>style</label><el-input type="textarea" :rows="5" :model-value="styleTextValue" @update:model-value="applyStyleText" /></div>',
      '</div>',
      '<el-empty v-else description="请选择组件" />',
      '</el-tab-pane>',

      '<el-tab-pane label="事件" name="events">',
      '<div class="prop-body" v-if="st.selected">',
      '<div class="prop-item" v-for="(r, i) in st.eventRows" :key="i">',
      '<label>事件 / 动作助手</label>',
      '<div style="display:flex;gap:4px">',
      '<el-input v-model="r.event" placeholder="click" style="width:90px" />',
      '<el-select v-model="r.action" filterable placeholder="选择动作" style="flex:1">',
      '<el-option v-for="a in st.actions" :key="a.Code" :label="a.Name + \' (\' + a.Code + \')\'" :value="a.Code" />',
      '</el-select>',
      '<el-button @click="st.eventRows.splice(i,1)">-</el-button>',
      '</div>',
      '</div>',
      '<el-button size="small" @click="st.eventRows.push({event:\'click\',action:\'\'})">添加事件</el-button>',
      '<el-button size="small" type="primary" @click="applyEvents">应用</el-button>',
      '</div>',
      '<el-empty v-else description="请选择组件" />',
      '</el-tab-pane>',

      '<el-tab-pane label="校验" name="validators">',
      '<div class="prop-body" v-if="st.selected">',
      '<div class="prop-item" v-for="(v, i) in st.validatorRows" :key="i">',
      '<label>校验规则</label>',
      '<div style="display:flex;gap:4px;flex-wrap:wrap">',
      '<el-select v-model="v.type" style="width:110px">',
      '<el-option label="必填" value="required" /><el-option label="邮箱" value="email" /><el-option label="手机号" value="phone" /><el-option label="正则" value="pattern" /><el-option label="最小" value="min" /><el-option label="最大" value="max" /><el-option label="长度" value="length" /><el-option label="自定义脚本" value="custom" />',
      '</el-select>',
      '<el-input v-model="v.message" placeholder="错误提示" style="flex:1" />',
      '<el-button @click="st.validatorRows.splice(i,1)">-</el-button>',
      '</div>',
      '<el-input v-if="v.type === \'pattern\'" v-model="v.pattern" placeholder="正则表达式" style="margin-top:4px" />',
      '<el-input v-if="v.type === \'custom\'" v-model="v.script" placeholder="return v.length > 2;" style="margin-top:4px" />',
      '</div>',
      '<el-button size="small" @click="st.validatorRows.push({type:\'required\',message:\'\'})">添加校验</el-button>',
      '<el-button size="small" type="primary" @click="applyValidators">应用</el-button>',
      '</div>',
      '<el-empty v-else description="请选择组件" />',
      '</el-tab-pane>',

      '<el-tab-pane label="JSON" name="json">',
      '<div class="prop-body" v-if="st.selected">',
      '<DynJsonEditor :model-value="st.jsonText" mode="application/json" height="360px" @update:model-value="onJsonInput" />',
      '<div v-if="st.jsonError" class="json-error-tip">JSON 错误：{{ st.jsonError }}</div>',
      '<div style="margin-top:8px"><el-button size="small" type="primary" @click="applyJson">应用JSON</el-button></div>',
      '</div>',
      '<el-empty v-else description="请选择组件" />',
      '</el-tab-pane>',
      '</el-tabs>',
      '</div>',
      '<div class="designer-panel-toggle right" @click="st.rightCollapsed = !st.rightCollapsed" :title="st.rightCollapsed ? \'展开右侧\' : \'收起右侧\'">{{ st.rightCollapsed ? "◀" : "▶" }}</div>',
      '</div>',
      '</div>',

      /* ---- 底部状态栏：当前组件路径（可点击跳转选中） ---- */
      '<div class="designer-statusbar">',
      '<span class="path-label">组件路径：</span>',
      '<template v-if="st.pathList && st.pathList.length">',
      '<template v-for="(p, i) in st.pathList" :key="i">',
      '<span class="path-item" :class="{current: p === st.selected}" @click="select(p)">{{ (metaOf(p.component) || {}).Label || p.component }}</span>',
      '<span v-if="i < st.pathList.length - 1" class="path-sep">›</span>',
      '</template>',
      '</template>',
      '<span v-else style="color:#c0c4cc">未选中组件</span>',
      '</div>',

      /* ---- 打开页面弹窗 ---- */
      '<el-dialog v-model="st.pageDialogVisible" title="打开页面" width="560px">',
      '<el-table :data="st.pages" size="small" @row-dblclick="row => { openPage(row); st.pageDialogVisible = false; }">',
      '<el-table-column prop="Name" label="名称" /><el-table-column prop="Code" label="Code" /><el-table-column prop="CreateTime" label="创建时间" />',
      '<el-table-column label="操作" width="120"><template #default="{ row }"><el-button size="small" type="primary" link @click="openPage(row); st.pageDialogVisible = false">打开</el-button></template></el-table-column>',
      '</el-table>',
      '</el-dialog>',

      /* ---- 模板弹窗 ---- */
      '<el-dialog v-model="st.templateDialogVisible" title="从模板创建" width="560px">',
      '<el-table :data="st.templates" size="small" @row-dblclick="row => { loadTemplate(row); st.templateDialogVisible = false; }">',
      '<el-table-column prop="Name" label="模板" /><el-table-column prop="Category" label="分类" width="160" /><el-table-column prop="Description" label="说明" />',
      '<el-table-column label="操作" width="120"><template #default="{ row }"><el-button size="small" type="primary" link @click="loadTemplate(row); st.templateDialogVisible = false">使用</el-button></template></el-table-column>',
      '</el-table>',
      '</el-dialog>',

      /* ---- 导入模板弹窗 ---- */
      '<el-dialog v-model="st.importVisible" title="导入 ElementPlus 模板" width="640px">',
      '<el-input v-model="st.importText" type="textarea" :rows="10" placeholder="粘贴 Vue Template 片段，例如：&#10;&lt;ElInput v-model=&quot;formData.name&quot; placeholder=&quot;请输入姓名&quot; clearable&gt;&#10;  &lt;template #prefix&gt;&lt;ElIcon name=&quot;User&quot; /&gt;&lt;/template&gt;&#10;&lt;/ElInput&gt;" />',
      '<div style="margin-top:8px"><el-button type="primary" @click="doImport">解析并导入</el-button></div>',
      '<div v-for="r in st.importReport" :key="r" style="margin-top:4px;font-size:12px">{{ r }}</div>',
      '</el-dialog>',

      /* ---- 导出模板弹窗 ---- */
      '<el-dialog v-model="st.exportVisible" title="导出 Vue Template" width="640px">',
      '<pre style="background:#f5f7fa;padding:12px;border-radius:6px;max-height:400px;overflow:auto;font-size:12px">{{ st.exportText }}</pre>',
      '<div style="margin-top:8px"><el-button type="primary" @click="copyText">复制</el-button></div>',
      '</el-dialog>',

      /* ---- 页面JSON弹窗 ---- */
      '<el-dialog v-model="st.jsonVisible" title="页面 JSON" width="720px">',
      '<DynJsonEditor :model-value="st.jsonText" mode="application/json" height="420px" @update:model-value="v => st.jsonText = v" />',
      '<div style="margin-top:8px"><el-button type="primary" @click="applyPageJson">应用</el-button></div>',
      '</el-dialog>',

      /* ---- Model 查看/编辑弹窗 ---- */
      '<el-dialog v-model="st.showModelDialog" title="Model 查看 / 编辑（页面数据）" width="680px">',
      '<div style="display:flex;gap:8px;margin-bottom:8px;align-items:center">',
      '<el-button size="small" @click="addModelField">添加字段</el-button>',
      '<span style="font-size:12px;color:#909399">组件 modelname 绑定此对象字段（如 formData.name），编辑后点"应用 Model"写回页面数据</span>',
      '</div>',
      '<DynJsonEditor :model-value="st.modelJsonText" mode="application/json" height="360px" @update:model-value="v => st.modelJsonText = v" />',
      '<div style="margin-top:8px"><el-button type="primary" @click="applyModelJson">应用 Model</el-button></div>',
      '</el-dialog>',
      '</div>'
    ].join('');

    var app = VueObj.createApp(Root);
    // 内置 JSON 编辑器（不依赖 DB/View 定义）
    app.component('DynJsonEditor', DynJsonEditor);
    // 注册 DynRender（dyn-core 递归渲染器）——设计器画布 <DynRender :cfg="st.pageCfg" /> 依赖
    if (global.DynCore && global.DynCore.DynRender) {
        app.component('DynRender', global.DynCore.DynRender);
    }
    if (global.DynCom) {
        global.DynCom.setupApp(app);
        // 注册 DB/View 组件清单后再挂载（设计器画布与运行时渲染同一套组件）
        if (typeof global.DynCom.ensureRegistered === 'function') {
            return global.DynCom.ensureRegistered(app).then(function () {
                app.mount(mountEl);
                return app;
            });
        }
    }
    app.mount(mountEl);
    return app;
  }

  global.DynDesigner = { create: create };
})(window);
