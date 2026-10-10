/**
 * dyn-eventbus.js —— 基于 DOM 原生事件的 Template 实例级事件总线
 *
 * 设计原则：
 *  1. 每个 Template 根 DOM 元素拥有一个独立 execId（GUID），事件总线就是这个元素本身。
 *  2. 不手写发布订阅类，直接用 element.addEventListener / dispatchEvent(CustomEvent)。
 *  3. 全局只维护一张 Map：execId -> rootElement，供弹窗（脱离 DOM 树）按 execId 找根节点。
 *  4. 时序问题用「快照」解决，但仅限 sticky 事件（query）：emit 时把 payload 存在
 *     rootEl.__snap 上；晚注册的 on() 先读快照，有就立刻执行一次。signal 动作事件
 *     （create/update/saved/cancel）不存快照、不补发。
 *  5. rootEl 销毁时调 destroy(execId)，清快照 + 删 Map 记录；监听由浏览器随 DOM GC 回收。
 *
 * 用法：
 *   // Template 根元素初始化
 *   const execId = DynEventBus.registerRoot(rootEl);
 *
 *   // Filter 就绪后发 query（sticky：晚挂载的 List 注册时自动拿到最新条件）
 *   DynEventBus.emit(execId, 'query', { keyword: 'xxx' });
 *
 *   // List 监听 query
 *   const handler = (payload) => this.loadData(payload);
 *   DynEventBus.on(execId, 'query', handler);
 *
 *   // 动作类事件（create/update/saved/cancel）是 signal：只通知、不补发
 *   DynEventBus.emit(execId, 'update', { row });
 *
 *   // List 销毁时解绑
 *   DynEventBus.off(execId, 'query', handler);
 *
 *   // 打开弹窗 URL 带上 execId
 *   openModal(`/Page/Lookup?execId=${execId}`);
 *
 *   // 弹窗内按 url 参数里的 execId 向父页面发事件
 *   DynEventBus.emit(execId, 'rowSelected', row);
 *
 *   // Template 销毁
 *   DynEventBus.destroy(execId);
 */
(function (global) {
  'use strict';

  /** execId -> rootElement */
  var roots = new Map();

  /**
   * sticky 事件（状态/条件类）：emit 存快照、on 注册时补发最新值。
   *   query —— filter → list/tree 的查询条件，晚挂载也要拿到首屏条件。
   * 其余事件（create/update/saved/cancel 等动作通知）为 signal：
   *   不存快照、不补发，避免「打开新实例被上一次的动作快照误触发」
   *   （旧实现里 saved 快照会让弹窗一开就被上次的 saved 关掉）。
   * 需要覆盖时：emit/on 第四/三参传 { sticky: true|false }。
   */
  var STICKY_EVENTS = { query: true };

  function isSticky(eventName, opts) {
    if (opts && typeof opts.sticky === 'boolean') return opts.sticky;
    return !!STICKY_EVENTS[eventName];
  }

  function guid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
      var r = Math.random() * 16 | 0;
      return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }

  /**
   * 在 Template 根 DOM 上注册实例，返回 execId。
   * 会自动给 rootEl 加 data-exec-id 属性，并在元素上挂快照存储位。
   * @param {HTMLElement} rootEl
   * @returns {string} execId
   */
  function registerRoot(rootEl) {
    if (!rootEl) throw new Error('DynEventBus.registerRoot: rootEl 不能为空');
    var execId = rootEl.dataset.execId || guid();
    rootEl.dataset.execId = execId;
    // 快照命名空间：rootEl.__snap = { eventName: lastPayload }
    rootEl.__snap = rootEl.__snap || {};
    roots.set(execId, rootEl);
    return execId;
  }

  /**
   * 按 execId 取根 DOM（弹窗跨 DOM 场景用）。找不到返回 null。
   * @param {string} execId
   * @returns {HTMLElement|null}
   */
  function getRoot(execId) {
    return execId ? roots.get(execId) || null : null;
  }

  /**
   * 发事件：sticky 事件先存快照再派发；signal 事件只派发、不留快照。
   * @param {string} execId
   * @param {string} eventName
   * @param {*} payload
   * @param {{sticky?:boolean}} [opts] 覆盖该事件默认的 sticky/signal 归类
   */
  function emit(execId, eventName, payload, opts) {
    var rootEl = roots.get(execId);
    if (!rootEl) {
      rootEl = document.getElementById(execId);
      if (rootEl) {
        rootEl.__snap = rootEl.__snap || {};
        roots.set(execId, rootEl);
      } else {
        console.warn('[DynEventBus] emit 找不到 rootEl, execId=', execId);
        return;
      }
    }
    rootEl.__snap = rootEl.__snap || {};
    if (isSticky(eventName, opts)) {
      // 存最新快照（覆盖旧值，只保留一份）
      rootEl.__snap[eventName] = payload;
    } else {
      // signal 事件不允许历史快照误伤后注册的实例
      delete rootEl.__snap[eventName];
    }
    // 派发原生 DOM 事件，bubbles=false 防止窜到外层
    rootEl.dispatchEvent(new CustomEvent('dyn:' + eventName, {
      detail: payload,
      bubbles: false,
      cancelable: false
    }));
  }

  /**
   * 监听事件：sticky 事件注册时先读快照补发一次，再 addEventListener；
   * signal 事件只监听后续实时派发，不补发（语义见 STICKY_EVENTS）。
   * @param {string} execId
   * @param {string} eventName
   * @param {function(*):void} handler 收到 payload
   * @param {{sticky?:boolean}} [opts] 覆盖该事件默认的 sticky/signal 归类
   * @returns {function|null} 解绑函数（也可手动调 off）
   */
  function on(execId, eventName, handler, opts) {
    var rootEl = roots.get(execId);
    if (!rootEl) {
      // 兜底：DOM 已存在但 registerRoot 还没调，自动注册
      rootEl = document.getElementById(execId);
      if (rootEl) {
        rootEl.__snap = rootEl.__snap || {};
        roots.set(execId, rootEl);
      } else {
        console.warn('[DynEventBus] on 找不到 rootEl, execId=', execId);
        return null;
      }
    }
    // sticky 事件：注册监听前先读快照，晚挂载的组件自动拿到最新 payload
    if (isSticky(eventName, opts)) {
      var snap = rootEl.__snap[eventName];
      if (snap !== undefined && snap !== null) {
        try { handler(snap); } catch (e) { console.error('[DynEventBus] 快照补发异常:', e); }
      }
    }
    // 包一层把 CustomEvent.detail 解出来给业务 handler
    var wrapped = function (e) { handler(e.detail); };
    rootEl.addEventListener('dyn:' + eventName, wrapped);
    // 在 handler 上挂引用，便于 off 时按 handler 找到 wrapped
    if (!handler.__dynWrapped) handler.__dynWrapped = {};
    handler.__dynWrapped[eventName + '@' + execId] = wrapped;
    return function () { off(execId, eventName, handler); };
  }

  /**
   * 解绑事件。
   * @param {string} execId
   * @param {string} eventName
   * @param {function(*):void} handler 注册时传入的同一个函数引用
   */
  function off(execId, eventName, handler) {
    var rootEl = roots.get(execId);
    if (!rootEl || !handler) return;
    var key = eventName + '@' + execId;
    var wrapped = handler.__dynWrapped && handler.__dynWrapped[key];
    if (wrapped) {
      rootEl.removeEventListener('dyn:' + eventName, wrapped);
      delete handler.__dynWrapped[key];
    }
  }

  /**
   * 销毁实例：清快照、删 Map。DOM 上的 listener 随节点移除由浏览器回收。
   * @param {string} execId
   */
  function destroy(execId) {
    var rootEl = roots.get(execId);
    if (rootEl) {
      rootEl.__snap = {};
      delete rootEl.dataset.execId;
    }
    roots.delete(execId);
  }

  global.DynEventBus = {
    registerRoot: registerRoot,
    getRoot: getRoot,
    emit: emit,
    on: on,
    off: off,
    destroy: destroy
  };
})(window);
