/* dyn-designer-ops.js 设计器树操作：选中/拖拽/移动/复制/删除/路径/overlay
 * 所有函数直接操作共享 scope（reactive），4 个分区 app 共用。 */
(function (global) {
  'use strict';
var __plugin = {
  name:'designer-ops', stage:'designer', requires:['Vue'],
  setup:function(ctx){
  'use strict';
  const Vue = global.Vue;

  let _uidSeq = 0;
  function uidOf(cfg) {
    if (!cfg) return '';
    if (!cfg.__uid) cfg.__uid = 'dyn-' + (++_uidSeq);
    return cfg.__uid;
  }

  function metaOf(name) {
    return (global.DynCom && global.DynCom.meta(name)) || {};
  }

  function isContainer(name) {
    const m = metaOf(name);
    // 容器：AllowDrop 含 "*" 或组件名在容器白名单；粗判：有 childrenctrls 默认值或组件名含 Container/Form/Card/Tabs/Row/Collapse
    if (m && m.CanDropInto) return true;
    if (!name) return false;
    return /Container|Form|Card|Tabs|Row|Col|Collapse|Table/.test(name);
  }

  function checkCanDrop(targetName, dragName) {
    if (!targetName || !dragName) return false;
    const m = metaOf(targetName);
    if (m && m.AllowDrop && Array.isArray(m.AllowDrop)) {
      if (m.AllowDrop.indexOf('*') >= 0) return true;
      return m.AllowDrop.indexOf(dragName) >= 0;
    }
    return isContainer(targetName);
  }

  function findNode(root, uid) {
    if (!root) return null;
    if (root.__uid === uid) return root;
    const kids = root.childrenctrls || [];
    for (let i = 0; i < kids.length; i++) {
      const r = findNode(kids[i], uid);
      if (r) return r;
    }
    const slots = root.slots || {};
    for (const k in slots) {
      for (let i = 0; i < slots[k].length; i++) {
        const r = findNode(slots[k][i], uid);
        if (r) return r;
      }
    }
    return null;
  }

  function findParent(root, cfg) {
    if (!root || !cfg) return null;
    const kids = root.childrenctrls || [];
    for (let i = 0; i < kids.length; i++) {
      if (kids[i] === cfg) return { parent: root, slot: 'childrenctrls', index: i };
      const r = findParent(kids[i], cfg);
      if (r) return r;
    }
    const slots = root.slots || {};
    for (const k in slots) {
      const arr = slots[k] || [];
      for (let i = 0; i < arr.length; i++) {
        if (arr[i] === cfg) return { parent: root, slot: k, index: i };
        const r = findParent(arr[i], cfg);
        if (r) return r;
      }
    }
    return null;
  }

  function getPathList(root, cfg) {
    const arr = [];
    if (!root || !cfg) return arr;
    function find(node, chain) {
      if (node === cfg) { chain.push(node); return true; }
      const kids = node.childrenctrls || [];
      for (let i = 0; i < kids.length; i++) {
        if (find(kids[i], chain)) { chain.unshift(node); return true; }
      }
      return false;
    }
    const chain = [];
    find(root, chain);
    return chain;
  }

  function isDescendantOf(cfg, ancestor) {
    if (!cfg || !ancestor) return false;
    function walk(n) {
      if (n === cfg) return true;
      const kids = n.childrenctrls || [];
      for (let i = 0; i < kids.length; i++) if (walk(kids[i])) return true;
      return false;
    }
    return walk(ancestor);
  }

  function defaultCfg(componentName) {
    const m = metaOf(componentName);
    let cfg = null;
    try {
      if (m && m.DefaultConfigJson) cfg = JSON.parse(m.DefaultConfigJson);
    } catch (e) { cfg = null; }
    if (!cfg) {
      cfg = { component: componentName, modelname: '', options: { comoptions: {}, labeloptions: { label: '', show: true }, itemoptions: {} }, childrenctrls: [] };
    }
    uidOf(cfg);
    return cfg;
  }

  /* ============ 主操作（直接改 scope） ============ */
  function select(scope, cfg) {
    scope.selected = cfg;
    scope.current = cfg;
    scope.selectedUid = cfg ? uidOf(cfg) : '';
    scope.pathList = getPathList(scope.pageJson, cfg);
    if (cfg) {
      // 确保 options 下的全小写契约分组存在，右侧面板 v-model 才能正确绑定
      if (!cfg.options) cfg.options = {};
      if (!cfg.options.comoptions) cfg.options.comoptions = {};
      if (!cfg.options.labeloptions) cfg.options.labeloptions = { label: '', required: false, show: true };
      if (!cfg.options.itemoptions) cfg.options.itemoptions = { style: {}, class: '' };
      const m = metaOf(cfg.component);
      scope.overlay.label = m.Label || cfg.component;
    }
    setTimeout(() => updateOverlay(scope), 50);
    setTimeout(() => updateOverlay(scope), 200);
  }

  function updateOverlay(scope) {
    if (!scope.selectedUid) { scope.overlay.uid = ''; return; }
    const span = document.querySelector('[data-dyn-uid="' + scope.selectedUid + '"]');
    const canvas = document.querySelector('[data-designer-canvas]');
    if (!span || !canvas) return;
    // display:contents span has no box; use its first rendered child element (or the span itself)
    let el = span;
    if (getComputedStyle(span).display === 'contents') {
      const real = span.firstElementChild;
      if (real) el = real;
    }
    const er = el.getBoundingClientRect();
    const cr = canvas.getBoundingClientRect();
    // Account for canvas zoom transform
    const scale = parseFloat(getComputedStyle(canvas).getPropertyValue('--dyn-zoom')) || 1;
    scope.overlay.uid = scope.selectedUid;
    scope.overlay.top = (er.top - cr.top) / scale;
    scope.overlay.left = (er.left - cr.left) / scale;
    scope.overlay.width = er.width / scale;
    scope.overlay.height = er.height / scale;
  }

  function onDragStartMeta(scope, meta, e) {
    scope.dragging = meta.ComponentName;
    scope.draggingCfg = null;
    e.dataTransfer.setData('text/plain', meta.ComponentName);
    e.dataTransfer.effectAllowed = 'copy';
  }

  function onDragStartCfg(scope, cfg, e) {
    scope.dragging = cfg.component;
    scope.draggingCfg = cfg;
    e.dataTransfer.setData('text/plain', cfg.component);
    e.dataTransfer.effectAllowed = 'move';
  }

  function onDragOver(scope, cfg, e) {
    if (!scope.dragging) return;
    e.preventDefault();
    e.stopPropagation();
    let el = document.querySelector('[data-dyn-uid="' + uidOf(cfg) + '"]');
    const canvas = document.querySelector('[data-designer-canvas]');
    if (!el || !canvas) return;
    document.querySelectorAll('.drag-target-active').forEach(n => n.classList.remove('drag-target-active'));
    if (getComputedStyle(el).display === 'contents') { var real = el.firstElementChild; if (real) el = real; }
    const er = el.getBoundingClientRect();
    const cr = canvas.getBoundingClientRect();
    const scale = parseFloat(canvas.style.getPropertyValue('--dyn-zoom')) || 1;
    const canDrop = checkCanDrop(cfg.component, scope.dragging);
    const isContainerComp = isContainer(cfg.component);
    const ratio = (e.clientY - er.top) / Math.max(er.height, 1);
    scope.dropIndicator.show = true;
    scope.dropIndicator.insertInto = false;
    scope.dropIndicator.insertBefore = false;
    scope.dropIndicator.insertAfter = false;
    if (canDrop && isContainerComp && ratio >= 0.25 && ratio <= 0.75) {
      scope.dropIndicator.insertInto = true;
      scope.dropIndicator.top = (er.top - cr.top) / scale;
      scope.dropIndicator.left = (er.left - cr.left) / scale;
      scope.dropIndicator.width = er.width / scale;
      scope.dropIndicator.height = er.height / scale;
      el.classList.add('drag-target-active');
    } else if (ratio < 0.5) {
      scope.dropIndicator.insertBefore = true;
      scope.dropIndicator.top = (er.top - cr.top) / scale - 2;
      scope.dropIndicator.left = (er.left - cr.left) / scale;
      scope.dropIndicator.width = er.width / scale;
      scope.dropIndicator.height = 4;
    } else {
      scope.dropIndicator.insertAfter = true;
      scope.dropIndicator.top = (er.bottom - cr.top) / scale - 2;
      scope.dropIndicator.left = (er.left - cr.left) / scale;
      scope.dropIndicator.width = er.width / scale;
      scope.dropIndicator.height = 4;
    }
  }

  function onDragLeave(scope, cfg, e) {
    const el = document.querySelector('[data-dyn-uid="' + uidOf(cfg) + '"]');
    if (el) el.classList.remove('drag-target-active');
  }

  function clearDrag(scope) {
    scope.dropIndicator.show = false;
    document.querySelectorAll('.drag-target-active').forEach(n => n.classList.remove('drag-target-active'));
  }

  function onDrop(scope, cfg, e) {
    if (!scope.dragging) return;
    e.preventDefault(); e.stopPropagation();
    const wasInsertInto = !!scope.dropIndicator.insertInto;
    clearDrag(scope);
    if (scope.draggingCfg && (scope.draggingCfg === scope.pageJson || cfg === scope.draggingCfg || isDescendantOf(cfg, scope.draggingCfg))) {
      if (global.ElementPlus && ElementPlus.ElMessage) ElementPlus.ElMessage.warning('不能将容器移动到自身或其内部');
      scope.dragging = ''; scope.draggingCfg = null; return;
    }
    saveSnapshot(scope);
    let movingNode = null;
    if (scope.draggingCfg) {
      const hit = findParent(scope.pageJson, scope.draggingCfg);
      if (hit) {
        const oldArr = hit.parent.childrenctrls;
        const oi = oldArr.indexOf(scope.draggingCfg);
        if (oi >= 0) oldArr.splice(oi, 1);
      }
      movingNode = scope.draggingCfg;
    } else {
      movingNode = defaultCfg(scope.dragging);
    }
    if (wasInsertInto && checkCanDrop(cfg.component, scope.dragging)) {
      cfg.childrenctrls = cfg.childrenctrls || [];
      cfg.childrenctrls.push(movingNode);
    } else {
      const phit = findParent(scope.pageJson, cfg);
      if (phit && phit.parent) {
        const arr = phit.parent.childrenctrls;
        const ti = arr.indexOf(cfg);
        if (scope.dropIndicator.insertAfter) {
          if (ti >= 0) arr.splice(ti + 1, 0, movingNode);
          else arr.push(movingNode);
        } else {
          if (ti >= 0) arr.splice(ti, 0, movingNode);
          else arr.unshift(movingNode);
        }
      } else {
        // dropping on root itself
        scope.pageJson.childrenctrls = scope.pageJson.childrenctrls || [];
        scope.pageJson.childrenctrls.push(movingNode);
      }
    }
    select(scope, movingNode);
    scope.dragging = ''; scope.draggingCfg = null;
  }

  function dropToRoot(scope, e) {
    if (!scope.dragging) return;
    e.preventDefault();
    clearDrag(scope);
    if (scope.draggingCfg) {
      if (scope.draggingCfg === scope.pageJson) { scope.dragging = ''; scope.draggingCfg = null; return; }
      saveSnapshot(scope);
      const hit = findParent(scope.pageJson, scope.draggingCfg);
      if (hit) {
        const oldArr = hit.parent.childrenctrls;
        const oi = oldArr.indexOf(scope.draggingCfg);
        if (oi >= 0) oldArr.splice(oi, 1);
      }
      scope.pageJson.childrenctrls = scope.pageJson.childrenctrls || [];
      scope.pageJson.childrenctrls.push(scope.draggingCfg);
      select(scope, scope.draggingCfg);
    } else {
      scope.pageJson.childrenctrls = scope.pageJson.childrenctrls || [];
      const child = defaultCfg(scope.dragging);
      scope.pageJson.childrenctrls.push(child);
      select(scope, child);
    }
    scope.dragging = ''; scope.draggingCfg = null;
  }

  function moveSelected(scope, dir) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
    const arr = hit.parent.childrenctrls;
    const i = arr.indexOf(cfg);
    const j = i + dir;
    if (j < 0 || j >= arr.length) return;
    saveSnapshot(scope);
    arr.splice(i, 1);
    arr.splice(j, 0, cfg);
  }

  function duplicateSelected(scope) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
    saveSnapshot(scope);
    const copy = JSON.parse(JSON.stringify(cfg));
    delete copy.__uid;
    uidOf(copy);
    const i = hit.parent.childrenctrls.indexOf(cfg);
    hit.parent.childrenctrls.splice(i + 1, 0, copy);
    select(scope, copy);
  }

  function removeSelected(scope) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
    saveSnapshot(scope);
    const arr = hit.parent.childrenctrls;
    const i = arr.indexOf(cfg);
    if (i >= 0) arr.splice(i, 1);
    select(scope, null);
  }

  function buildTreeData(root) {
    if (!root) return [];
    function walk(node) {
      const m = metaOf(node.component);
      const kids = (node.childrenctrls || []).map(walk);
      if (node.slots) {
        for (const k in node.slots) {
          (node.slots[k] || []).forEach(c => kids.push({ uid: 'slot-' + uidOf(c), label: '#' + k, icon: '📥', cfg: null, children: [] }));
        }
      }
      return { uid: uidOf(node), label: (m && m.Label) || node.component, icon: (m && m.Icon) || '🧩', cfg: node, children: kids };
    }
    return [walk(root)];
  }

  // 清理 JSON 中的空值：删除 {}、[]、null、""、undefined 的字段
  function cleanJson(obj) {
    if (obj === null || obj === undefined) return undefined;
    if (Array.isArray(obj)) {
      const arr = obj.map(cleanJson).filter(v => v !== undefined);
      return arr.length ? arr : undefined;
    }
    if (typeof obj === 'object') {
      const result = {};
      for (const key in obj) {
        // 跳过内部字段
        if (key === '__uid' || key === '__dynId') continue;
        const v = cleanJson(obj[key]);
        if (v !== undefined) result[key] = v;
      }
      // 跳过空对象
      const keys = Object.keys(result);
      return keys.length ? result : undefined;
    }
    // 字符串空值跳过
    if (typeof obj === 'string' && obj === '') return undefined;
    return obj;
  }

  function getCleanJson(scope) {
    return cleanJson(scope.pageJson);
  }

  /* ============ 撤销 / 重做（快照栈方案） ============
   * 只在「真正修改页面数据」的操作前调用 saveSnapshot(scope)。
   * previewActive（Tab 预览切换等设计态临时状态）绝不入栈，与「不落库」约束一致。
   */
  const MAX_HISTORY = 20;
  function saveSnapshot(scope) {
    if (!scope) return;
    try {
      const snap = JSON.parse(JSON.stringify(scope.pageJson));
      if (JSON.stringify(snap) === JSON.stringify(scope.undoStack[scope.undoStack.length - 1])) return;
      scope.undoStack.push(snap);
      if (scope.undoStack.length > MAX_HISTORY) scope.undoStack.shift();
      scope.redoStack = [];
    } catch (e) {}
  }
  function applySnapshot(scope, snap) {
    scope.pageJson = snap;             // 纯对象赋给 reactive model 属性 → Vue 自动代理
    scope.selected = null; scope.current = null;
    scope.selectedUid = ''; scope.overlay.uid = '';
    scope.previewActive = { uid: '', index: 0 };
    scope.pathList = [];
    // 顶层引用已替换，但容器模板 setup 快照 jc 不响应 → 自增版本号强制整树重建
    scope.canvasVersion = (scope.canvasVersion || 0) + 1;
    setTimeout(function () { updateOverlay(scope); }, 60);
  }
  function undoAction(scope) {
    if (!scope || !scope.undoStack || !scope.undoStack.length) return false;
    scope.redoStack.push(JSON.parse(JSON.stringify(scope.pageJson)));
    applySnapshot(scope, scope.undoStack.pop());
    return true;
  }
  function redoAction(scope) {
    if (!scope || !scope.redoStack || !scope.redoStack.length) return false;
    scope.undoStack.push(JSON.parse(JSON.stringify(scope.pageJson)));
    applySnapshot(scope, scope.redoStack.pop());
    return true;
  }

  /* ============ 设计器扩展：子项管理（designerMeta）+ 组件专属操作（designerOperates） ============
   * 仅设计器使用，运行态 dyn-core 不下发这两个字段。
   * designerMeta.childrenSchemaPath：子项数组在组件 JSON 里的路径（如 childrenctrls / comoptions.columns）
   * previewActive：画布级预览状态（{uid,index}），只存共享 scope，不写 pageJson → 不落库，
   *                满足「设计态切换 Tab 只预览、不改动 PageSetting 原始 JSON」。
   */
  function parseJsonStr(s) {
    if (!s) return null;
    if (typeof s === 'object') return s;
    try { return JSON.parse(s); } catch (e) { return null; }
  }
  function designerMetaOf(name) {
    const m = metaOf(name);
    return m ? parseJsonStr(m.DesignerMeta || m.designerMeta) : null;
  }
  function designerOperatesOf(name) {
    const m = metaOf(name);
    return m ? parseJsonStr(m.DesignerOperates || m.designerOperates) : null;
  }
  function getPath(obj, path) {
    if (!obj || !path) return undefined;
    if (global._ && global._.get) return global._.get(obj, path);
    return path.split('.').reduce((o, k) => (o == null ? undefined : o[k]), obj);
  }
  function childListOf(scope) {
    const cfg = scope.selected;
    const dm = cfg ? designerMetaOf(cfg.component) : null;
    if (!dm || !dm.childrenSchemaPath) return null;
    const arr = getPath(cfg, dm.childrenSchemaPath);
    return Array.isArray(arr) ? arr : null;
  }
  function activeIndex(scope) {
    const pa = scope.previewActive;
    const arr = childListOf(scope);
    if (!arr || !arr.length) return 0;
    if (pa && pa.uid === scope.selectedUid && pa.index >= 0 && pa.index < arr.length) return pa.index;
    return 0;
  }
  function defaultChildFor(cfg) {
    if (cfg.component === 'DynElTabs') {
      const child = defaultCfg('DynElContainer');
      if (!child.options) child.options = {};
      if (!child.options.labeloptions) child.options.labeloptions = {};
      child.options.labeloptions.label = '新标签页';
      child.options.labeloptions.show = true;
      child.options.itemoptions = { style: { padding: '12px' }, class: '' };
      return child;
    }
    if (cfg.component === 'DynTable') {
      const cols = getPath(cfg, 'options.comoptions.columns') || [];
      return { prop: 'field' + (cols.length + 1), label: '新列', width: 120, sortable: false };
    }
    if (cfg.component === 'DynElCollapse') {
      return { component: 'DynElCollapseItem', options: { comoptions: { label: '折叠项' }, labeloptions: { show: false } }, childrenctrls: [] };
    }
    if (cfg.component === 'DynElSteps') {
      return { component: 'DynElStep', options: { comoptions: { title: '新步骤', description: '', status: '', icon: '' } }, childrenctrls: [] };
    }
    return { label: '新子项' };
  }
  function addChildItem(scope) {
    const cfg = scope.selected;
    const arr = childListOf(scope);
    if (!cfg || !arr) return;
    saveSnapshot(scope);
    arr.push(defaultChildFor(cfg));
    scope.previewActive = { uid: scope.selectedUid, index: arr.length - 1 };
    select(scope, cfg);
  }
  function removeActiveChild(scope) {
    const cfg = scope.selected;
    const arr = childListOf(scope);
    if (!cfg || !arr || !arr.length) return;
    saveSnapshot(scope);
    const idx = activeIndex(scope);
    arr.splice(idx, 1);
    scope.previewActive = { uid: scope.selectedUid, index: Math.max(0, Math.min(idx, arr.length - 1)) };
    setTimeout(() => updateOverlay(scope), 50);
  }
  function stepActive(scope, dir) {
    const arr = childListOf(scope);
    if (!arr || !arr.length) return;
    const n = (activeIndex(scope) + dir + arr.length) % arr.length;
    scope.previewActive = { uid: scope.selectedUid, index: n };
  }
  function moveActiveChild(scope, dir) {
    const cfg = scope.selected;
    const arr = childListOf(scope);
    if (!cfg || !arr || !arr.length) return;
    saveSnapshot(scope);
    const cur = activeIndex(scope);
    const j = cur + dir;
    if (j < 0 || j >= arr.length) return;
    const item = arr.splice(cur, 1)[0];
    arr.splice(j, 0, item);
    scope.previewActive = { uid: scope.selectedUid, index: j };
    select(scope, cfg);
  }

  // 内置命令注册表：designerOperates 里 command 类型 handlerKey 在此查找
  const commandRegistry = {
    'tab.switchPreview': (scope) => stepActive(scope, 1),
    'tab.prevPreview': (scope) => stepActive(scope, -1),
    'tab.addTab': (scope) => addChildItem(scope),
    'tab.removeActiveTab': (scope) => removeActiveChild(scope),
    'tab.moveTab': (scope, dir) => moveActiveChild(scope, dir),
    'table.addColumn': (scope) => addChildItem(scope),
    'table.removeColumn': (scope) => removeActiveChild(scope),
    'table.moveColumn': (scope, dir) => moveActiveChild(scope, dir)
  };
  function execOperate(scope, op) {
    if (!op) return;
    if (op.type === 'command') {
      const h = commandRegistry[op.handlerKey];
      if (h) { h(scope, op.arg); return; }
      if (global.ElementPlus && global.ElementPlus.ElMessage) global.ElementPlus.ElMessage.warning('未注册命令：' + op.handlerKey);
      return;
    }
    if (op.type === 'view') {
      // 自定义视图：把组件节点 + 编辑副本写入 scope.viewState，Canvas 内嵌视图组件渲染
      if (op.viewName === 'TableColumnConfigView') {
        const cols = getPath(scope.selected, 'options.comoptions.columns') || [];
        scope.viewState = {
          open: true,
          name: op.viewName,
          node: scope.selected,
          columns: JSON.parse(JSON.stringify(cols))
        };
        return;
      }
      if (op.viewName === 'TabConfigView') {
        const kids = getPath(scope.selected, 'childrenctrls') || [];
        scope.viewState = {
          open: true,
          name: op.viewName,
          node: scope.selected,
          items: JSON.parse(JSON.stringify(kids)).map(function (it, i) {
            if (!it.options) it.options = {};
            if (!it.options.labeloptions) it.options.labeloptions = { label: 'Tab ' + (i + 1), show: true };
            return it;
          })
        };
        return;
      }
      if (op.viewName === 'CollapseConfigView') {
        const kids = getPath(scope.selected, 'childrenctrls') || [];
        scope.viewState = {
          open: true,
          name: op.viewName,
          node: scope.selected,
          items: JSON.parse(JSON.stringify(kids)).map(function (it, i) {
            if (!it.options) it.options = {};
            if (!it.options.comoptions) it.options.comoptions = {};
            // 标题统一收敛到 options.comoptions.label（CollapseItem 渲染 jc.label || comoptions.label）
            if (!it.options.comoptions.label) it.options.comoptions.label = it.label || ('折叠项 ' + (i + 1));
            if (it.label) delete it.label;
            return it;
          })
        };
        return;
      }
      if (op.viewName === 'StepsConfigView') {
        const kids = getPath(scope.selected, 'childrenctrls') || [];
        scope.viewState = {
          open: true,
          name: op.viewName,
          node: scope.selected,
          items: JSON.parse(JSON.stringify(kids)).map(function (it, i) {
            if (!it.options) it.options = {};
            if (!it.options.comoptions) it.options.comoptions = {};
            if (!it.options.comoptions.title) it.options.comoptions.title = '步骤' + (i + 1);
            if (!('description' in it.options.comoptions)) it.options.comoptions.description = '';
            if (!('status' in it.options.comoptions)) it.options.comoptions.status = '';
            if (!('icon' in it.options.comoptions)) it.options.comoptions.icon = '';
            return it;
          })
        };
        return;
      }
      if (global.ElementPlus && global.ElementPlus.ElMessage) global.ElementPlus.ElMessage.info('自定义视图「' + (op.label || op.viewName) + '」开发中');
      return;
    }
  }

  global.DynDesignerOps = {
    uidOf, metaOf, isContainer, checkCanDrop, findNode, findParent, getPathList,
    defaultCfg, select, updateOverlay,
    onDragStartMeta, onDragStartCfg, onDragOver, onDragLeave, onDrop, dropToRoot, clearDrag,
    moveSelected, duplicateSelected, removeSelected, buildTreeData,
    cleanJson, getCleanJson,
    parseJsonStr, designerMetaOf, designerOperatesOf, getPath, childListOf, activeIndex,
    addChildItem, removeActiveChild, stepActive, moveActiveChild,
    commandRegistry, execOperate,
    saveSnapshot, undoAction, redoAction
  };
  }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);
