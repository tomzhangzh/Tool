/* dyn-designer-history.js 设计器撤销/重做与脏状态
 * 契约：
 *   init(scope, vm)      首次挂载时接线：深度 watch pageJson，600ms 防抖合并连续属性编辑
 *   snapshot(scope)      结构操作前记录「操作前状态」（替换旧 saveSnapshot 语义）
 *   undo/redo(scope)     先提交待合并编辑，再弹栈
 *   markSaved(scope)     保存成功后记录已存基线并清 dirty
 *   reset(scope)         载入页面后清空历史并把当前状态视为已存基线
 *   baseline(scope)      程序化替换 pageJson（JSON 模式）后重设基线，保留可撤销性
 * 覆盖导出：DynDesignerOps.saveSnapshot / undoAction / redoAction，旧调用点无需改名。
 */
(function (global) {
  'use strict';
  var __plugin = {
    name: 'designer-history', stage: 'designer', requires: ['DynDesignerOps'],
    setup: function () {
      'use strict';
      const Ops = global.DynDesignerOps;
      const MAX_HISTORY = 20;
      let _timer = 0;

      function str(v) {
        try { return JSON.stringify(v == null ? {} : v); } catch (e) { return ''; }
      }
      function clone(v) {
        try { return JSON.parse(JSON.stringify(v == null ? {} : v)); } catch (e) { return {}; }
      }
      function dirtyOf(scope) {
        return !!scope.__savedJson && str(scope.pageJson) !== scope.__savedJson;
      }

      function commit(scope) {
        if (!scope) return false;
        if (_timer) { clearTimeout(_timer); _timer = 0; }
        const cur = str(scope.pageJson);
        if (cur === scope.__undoBaseline) return false;
        let prev = null;
        try { prev = JSON.parse(scope.__undoBaseline || '{}'); } catch (e) { prev = null; }
        if (prev) {
          scope.undoStack.push(prev);
          if (scope.undoStack.length > MAX_HISTORY) scope.undoStack.shift();
          scope.redoStack = [];
        }
        scope.__undoBaseline = cur;
        scope.dirty = dirtyOf(scope);
        return true;
      }

      function schedule(scope) {
        if (_timer) clearTimeout(_timer);
        _timer = setTimeout(function () { _timer = 0; commit(scope); }, 600);
      }

      function init(scope, vm) {
        if (!scope || !vm || scope.__undoWired) return;
        scope.__undoWired = true;
        if (!Array.isArray(scope.undoStack)) scope.undoStack = [];
        if (!Array.isArray(scope.redoStack)) scope.redoStack = [];
        scope.__undoBaseline = str(scope.pageJson);
        if (!scope.__savedJson) scope.__savedJson = scope.__undoBaseline;
        scope.dirty = false;
        vm.$watch(function () { return scope.pageJson; }, function () { schedule(scope); }, { deep: true });
      }

      function snapshot(scope) {
        if (!scope) return;
        commit(scope);
        const snap = clone(scope.pageJson);
        const top = scope.undoStack[scope.undoStack.length - 1];
        if (top && str(top) === str(snap)) return;
        scope.undoStack.push(snap);
        if (scope.undoStack.length > MAX_HISTORY) scope.undoStack.shift();
        scope.redoStack = [];
        scope.dirty = dirtyOf(scope);
      }

      function apply(scope, snap) {
        scope.pageJson = snap;
        scope.selected = null; scope.current = null;
        scope.selectedUid = ''; scope.overlay.uid = '';
        scope.previewActive = { uid: '', index: 0 };
        scope.pathList = [];
        // 顶层引用被替换，容器模板的 setup 快照不响应 → 自增版本号强制整树重建
        scope.canvasVersion = (scope.canvasVersion || 0) + 1;
        scope.__undoBaseline = str(scope.pageJson);
        scope.dirty = dirtyOf(scope);
        setTimeout(function () { if (Ops && Ops.updateOverlay) Ops.updateOverlay(scope); }, 60);
      }

      function undo(scope) {
        if (!scope || !scope.undoStack || !scope.undoStack.length) return false;
        commit(scope);
        if (!scope.undoStack.length) return false;
        scope.redoStack.push(clone(scope.pageJson));
        apply(scope, scope.undoStack.pop());
        return true;
      }

      function redo(scope) {
        if (!scope || !scope.redoStack || !scope.redoStack.length) return false;
        commit(scope);
        if (!scope.redoStack.length) return false;
        scope.undoStack.push(clone(scope.pageJson));
        apply(scope, scope.redoStack.pop());
        return true;
      }

      function markSaved(scope) {
        if (!scope) return;
        commit(scope);
        scope.__savedJson = str(scope.pageJson);
        scope.dirty = false;
      }

      function reset(scope) {
        if (!scope) return;
        if (_timer) { clearTimeout(_timer); _timer = 0; }
        scope.undoStack = [];
        scope.redoStack = [];
        scope.__undoBaseline = str(scope.pageJson);
        scope.__savedJson = scope.__undoBaseline;
        scope.dirty = false;
      }

      function baseline(scope) {
        if (!scope) return;
        scope.__undoBaseline = str(scope.pageJson);
        scope.dirty = dirtyOf(scope);
      }

      const api = {
        init: init, commit: commit, snapshot: snapshot, undo: undo, redo: redo,
        markSaved: markSaved, reset: reset, baseline: baseline
      };
      global.DynDesignerHistory = api;
      // 兼容旧调用点：Ops.saveSnapshot / undoAction / redoAction
      Ops.saveSnapshot = snapshot;
      Ops.undoAction = undo;
      Ops.redoAction = redo;
    }
  };
  if (global.DynKernel) global.DynKernel.register(__plugin);
  else (global.__DYN_KERNEL_PENDING__ = global.__DYN_KERNEL_PENDING__ || []).push(__plugin);
})(window);
