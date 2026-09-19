/* dyn-designer-ops.js 设计器树操作：选中/拖拽/移动/复制/删除/路径/overlay
 * 所有函数直接操作共享 scope（reactive），4 个分区 app 共用。 */
(function (global) {
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
    arr.splice(i, 1);
    arr.splice(j, 0, cfg);
  }

  function duplicateSelected(scope) {
    const cfg = scope.selected;
    if (!cfg) return;
    const hit = findParent(scope.pageJson, cfg);
    if (!hit) return;
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

  global.DynDesignerOps = {
    uidOf, metaOf, isContainer, checkCanDrop, findNode, findParent, getPathList,
    defaultCfg, select, updateOverlay,
    onDragStartMeta, onDragStartCfg, onDragOver, onDragLeave, onDrop, dropToRoot, clearDrag,
    moveSelected, duplicateSelected, removeSelected, buildTreeData
  };
})(window);
