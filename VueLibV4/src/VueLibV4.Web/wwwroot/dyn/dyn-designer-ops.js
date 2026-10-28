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
    return (global.DynCom && global.DynCom.meta(name)) || null;
  }

  // 元数据字段兼容 PascalCase（define 接口）/ camelCase（list 接口）两种下发形态
  function metaFlag(m, pascal, camel) {
    return !!(m && (m[pascal] || m[camel]));
  }
  // AllowDrop / CanDropInto 在库里是 JSON 数组原文（也可能已是数组）；非法值按空名单处理
  function metaNameList(m, pascal, camel) {
    if (!m) return [];
    let raw = m[pascal];
    if (raw === undefined || raw === null) raw = m[camel];
    if (Array.isArray(raw)) return raw;
    if (typeof raw === 'string' && raw.trim()) {
      try {
        const v = JSON.parse(raw);
        return Array.isArray(v) ? v : [];
      } catch (e) { return []; }
    }
    return [];
  }

  // 唯一权威：ComponentMeta.IsContainer（视图层以使用 _ContainerLayout 的组件为准），不再按组件名猜
  function isContainer(name) {
    return metaFlag(metaOf(name), 'IsContainer', 'isContainer');
  }

  function checkCanDrop(targetName, dragName) {
    if (!targetName || !dragName) return false;
    const m = metaOf(targetName);
    if (!metaFlag(m, 'IsContainer', 'isContainer')) return false;
    if (metaFlag(m, 'AcceptAll', 'acceptAll')) return true;
    const allow = metaNameList(m, 'AllowDrop', 'allowDrop');
    if (allow.indexOf('*') >= 0 || allow.indexOf(dragName) >= 0) return true;
    // 容器未声明白名单时，参考子组件的 CanDropInto（子→父方向声明，如 DynElInput 声明可进 DynElFormItem）
    if (!allow.length) {
      const into = metaNameList(metaOf(dragName), 'CanDropInto', 'canDropInto');
      return into.indexOf('*') >= 0 || into.indexOf(targetName) >= 0;
    }
    return false;
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

  // 命中位置对应的真实数组：childrenctrls 或 slots[slot]
  function arrayOfHit(hit) {
    if (!hit || !hit.parent) return null;
    if (hit.slot === 'childrenctrls') return hit.parent.childrenctrls || null;
    const slots = hit.parent.slots || {};
    return slots[hit.slot] || null;
  }

  function getPathList(root, cfg) {
    if (!root || !cfg) return [];
    function find(node, chain) {
      if (node === cfg) { chain.push(node); return true; }
      const kids = node.childrenctrls || [];
      for (let i = 0; i < kids.length; i++) {
        if (find(kids[i], chain)) { chain.unshift(node); return true; }
      }
      const slots = node.slots || {};
      for (const k in slots) {
        const arr = slots[k] || [];
        for (let i = 0; i < arr.length; i++) {
          if (find(arr[i], chain)) { chain.unshift(node); return true; }
        }
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
      const slots = n.slots || {};
      for (const k in slots) {
        const arr = slots[k] || [];
        for (let i = 0; i < arr.length; i++) if (walk(arr[i])) return true;
      }
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
      scope.overlay.label = (m && (m.Label || m.label)) || cfg.component;
    }
    scheduleOverlay(scope);
  }

  // 统一的选中框重定位：Vue DOM 更新完成后测一次；后续异步尺寸变化由画布 ResizeObserver 兜底，
  // 不再用 50/200ms 魔法延时（图片加载、字体回流超过 200ms 时旧方案会错位）。
  function scheduleOverlay(scope) {
    const Vue = global.Vue;
    if (Vue && Vue.nextTick) Vue.nextTick(() => updateOverlay(scope));
    else setTimeout(() => updateOverlay(scope), 0);
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

  function moveSelected(scope, dir) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
    const arr = arrayOfHit(hit);
    if (!arr) return;
    const i = arr.indexOf(cfg);
    const j = i + dir;
    if (i < 0 || j < 0 || j >= arr.length) return;
    snap(scope);
    arr.splice(i, 1);
    arr.splice(j, 0, cfg);
  }

  // 递归重新生成整棵子树 __uid：复制组件/粘贴组合时避免与原节点 uid 冲突
  // （findNode / data-dyn-uid / overlay 定位都依赖 uid 唯一）
  function regenerateUids(cfg) {
    if (!cfg || typeof cfg !== 'object') return cfg;
    delete cfg.__uid;
    uidOf(cfg);
    (cfg.childrenctrls || []).forEach(regenerateUids);
    const slots = cfg.slots || {};
    for (const k in slots) (slots[k] || []).forEach(regenerateUids);
    return cfg;
  }

  function duplicateSelected(scope) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
    const arr = arrayOfHit(hit);
    if (!arr) return;
    snap(scope);
    const copy = regenerateUids(JSON.parse(JSON.stringify(cfg)));
    const i = arr.indexOf(cfg);
    arr.splice(i < 0 ? arr.length : i + 1, 0, copy);
    select(scope, copy);
  }

  function removeSelected(scope) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
    snap(scope);
    const arr = arrayOfHit(hit);
    if (!arr) return;
    const i = arr.indexOf(cfg);
    if (i >= 0) arr.splice(i, 1);
    // slots 被删空时移除该 slot 键，避免运行时留下空壳
    if (hit.slot !== 'childrenctrls' && arr.length === 0 && hit.parent.slots) delete hit.parent.slots[hit.slot];
    select(scope, null);
  }

  function buildTreeData(root) {
    if (!root) return [];
    function walk(node) {
      const m = metaOf(node.component);
      const kids = (node.childrenctrls || []).map(walk);
      const typeLabel = (m && m.Label) || node.component || '';
      const userLabel = (((node.options || {}).labeloptions || {}).label) || '';
      const mn = node.modelname || '';
      if (node.slots) {
        for (const k in node.slots) {
          const arr = node.slots[k] || [];
          if (!arr.length) continue;
          // 插槽分组节点（cfg:null 不可拖拽/选中），子节点正常递归为可选中的真实节点
          kids.push({
            uid: 'slot-' + uidOf(node) + '-' + k,
            label: '#' + k, icon: '📥', cfg: null, search: '#' + k,
            children: arr.map(walk)
          });
        }
      }
      // 展示：组件类型（modelname）；检索覆盖 类型/组件名/modelname/label
      return {
        uid: uidOf(node),
        label: typeLabel + (mn ? '（' + mn + '）' : ''),
        icon: (m && m.Icon) || '🧩',
        type: typeLabel,
        modelname: mn,
        userLabel: userLabel,
        search: [typeLabel, node.component, mn, userLabel].filter(Boolean).join(' '),
        cfg: node,
        children: kids
      };
    }
    return [walk(root)];
  }

  /* ============ 组件树拖拽（el-tree draggable 回写 pageJson） ============
   * 树上 cfg 节点可能位于 childrenctrls 或 slots[slot]；插槽分组节点 cfg:null 不参与拖拽。
   * dropType：'before' | 'after' | 'inner'（el-tree 原生取值）
   */
  function treeAllowDrop(scope, dragCfg, dropCfg, dropType) {
    if (!dragCfg || !dropCfg) return false;
    if (dragCfg === scope.pageJson || dropCfg === dragCfg) return false;
    // 禁止拖入自身或自己的后代节点
    if (isDescendantOf(dropCfg, dragCfg)) return false;
    if (dropType === 'inner') {
      return isContainer(dropCfg.component) && checkCanDrop(dropCfg.component, dragCfg.component);
    }
    // before/after：与目标节点同级，校验目标父容器的接收白名单
    if (dropCfg === scope.pageJson) return checkCanDrop(scope.pageJson.component, dragCfg.component);
    const hit = findParent(scope.pageJson, dropCfg);
    if (!hit) return false;
    return checkCanDrop(hit.parent.component, dragCfg.component);
  }

  function treeReorder(scope, dragCfg, dropCfg, dropType) {
    if (!treeAllowDrop(scope, dragCfg, dropCfg, dropType)) {
      if (global.ElementPlus && ElementPlus.ElMessage) ElementPlus.ElMessage.warning('目标位置不接受该组件');
      return false;
    }
    snap(scope);
    // 先从原位置摘出（childrenctrls / slots 均适用）
    const from = findParent(scope.pageJson, dragCfg);
    if (from) {
      const fromArr = arrayOfHit(from);
      if (fromArr) {
        const fi = fromArr.indexOf(dragCfg);
        if (fi >= 0) fromArr.splice(fi, 1);
        if (from.slot !== 'childrenctrls' && fromArr.length === 0 && from.parent.slots) delete from.parent.slots[from.slot];
      }
    }

    if (dropType === 'inner') {
      dropCfg.childrenctrls = dropCfg.childrenctrls || [];
      dropCfg.childrenctrls.push(dragCfg);
    } else if (dropCfg === scope.pageJson) {
      const arr = scope.pageJson.childrenctrls = scope.pageJson.childrenctrls || [];
      if (dropType === 'before') arr.unshift(dragCfg); else arr.push(dragCfg);
    } else {
      const hit = findParent(scope.pageJson, dropCfg);
      if (hit) {
        const arr = hit.slot === 'childrenctrls' ? hit.parent.childrenctrls : (hit.parent.slots || {})[hit.slot];
        if (arr) {
          const idx = arr.indexOf(dropCfg);
          arr.splice(dropType === 'before' ? Math.max(0, idx) : idx + 1, 0, dragCfg);
        }
      }
    }
    select(scope, dragCfg);
    return true;
  }

  /* ============ 撤销 / 重做 ============
   * 快照实现已下沉到 dyn-designer-history.js（本插件只负责结构操作前调用）。
   * previewActive（Tab 预览切换等设计态临时状态）绝不入栈，与「不落库」约束一致。
   */
  function snap(scope) {
    if (global.DynDesignerHistory && global.DynDesignerHistory.snapshot) {
      global.DynDesignerHistory.snapshot(scope);
    }
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
      // 树化表格：新增子项 = 标准 DynTableColumn 节点（推入 childrenctrls）
      const cols = (cfg.childrenctrls || []).filter(n => n && n.component === 'DynTableColumn');
      const child = defaultCfg('DynTableColumn');
      const cco = child.options && child.options.comoptions;
      if (cco) { cco.field = 'field' + (cols.length + 1); cco.label = '新列'; }
      return child;
    }
    if (cfg.component === 'DynElCollapse') {
      return { component: 'DynElCollapseItem', options: { comoptions: { label: '折叠项' }, labeloptions: { show: false } }, childrenctrls: [] };
    }
    if (cfg.component === 'DynElSteps') {
      return { component: 'DynElStep', options: { comoptions: { title: '新步骤', description: '', status: '', icon: '' } }, childrenctrls: [] };
    }
    // 通用兜底：容器 AllowDrop 白名单的第一个可接收组件（如 DynTableColumn→DynElTag、DynTableActions→DynElButton）
    const dm = metaOf(cfg.component);
    if (dm && Array.isArray(dm.AllowDrop)) {
      const first = dm.AllowDrop.filter(function (x) { return x && x !== '*'; })[0];
      if (first) return defaultCfg(first);
    }
    return { label: '新子项' };
  }
  function addChildItem(scope) {
    const cfg = scope.selected;
    const arr = childListOf(scope);
    if (!cfg || !arr) return;
    snap(scope);
    arr.push(defaultChildFor(cfg));
    scope.previewActive = { uid: scope.selectedUid, index: arr.length - 1 };
    select(scope, cfg);
  }
  function removeActiveChild(scope) {
    const cfg = scope.selected;
    const arr = childListOf(scope);
    if (!cfg || !arr || !arr.length) return;
    snap(scope);
    const idx = activeIndex(scope);
    arr.splice(idx, 1);
    scope.previewActive = { uid: scope.selectedUid, index: Math.max(0, Math.min(idx, arr.length - 1)) };
    scheduleOverlay(scope);
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
    snap(scope);
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
    uidOf, metaOf, isContainer, checkCanDrop, findNode, findParent, arrayOfHit, getPathList, isDescendantOf,
    defaultCfg, select, updateOverlay,
    moveSelected, duplicateSelected, removeSelected, buildTreeData,
    regenerateUids, treeAllowDrop, treeReorder,
    parseJsonStr, designerMetaOf, designerOperatesOf, getPath, childListOf, activeIndex,
    addChildItem, removeActiveChild, stepActive, moveActiveChild,
    commandRegistry, execOperate
  };
  }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);
