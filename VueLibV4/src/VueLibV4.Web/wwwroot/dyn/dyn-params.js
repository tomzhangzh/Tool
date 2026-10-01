/* dyn-params.js — DynCore 统一参数上下文
 * ------------------------------------------------------------
 * 一套取值语法，多来源合并，就近优先。
 *
 * 层级（数字越小优先级越高）：
 *   L0 显式入参   调用点 one-shot（fork(local) / resolveValue explicit）
 *   L1 本地静态   容器 data-dyn-params / <script tag="dynparams"> / data-blk-config 适配
 *   L2 本地 Model el.__dynModel（业务数据层，按 key 命中才取）
 *   L3 上层继承   父 ParamContext（沿片段注入/嵌套App链，含共享实体层）
 *   L4 URL query window.location.search（全局兜底）
 *
 * 跨容器流动的"业务实体参数"（如 tab1 保存回来的 courseId）走共享实体层：
 *   ctx.commit(key, value) —— 任意 fork 都引用同一个 shared 响应式对象，
 *   兄弟 tab 因此可见；本地 L1/L0 仍可就近覆盖。
 *
 * 取值：dyn.params(el).get('courseId') / P.require('courseId') / {{$params.courseId}}
 * 透传：P.fork(holderEl, {rowId:27})
 * 发布：P.commit('courseId', newId)
 */
(function (global) {
  'use strict';
  var Vue = global.Vue;
  if (!Vue) { console.error('[DynParams] 需要 Vue3 UMD'); return; }

  var ATTR_PARAMS = 'data-dyn-params';
  var ATTR_SHARED_PARAMS = 'data-dyn-shared-params';
  var ATTR_CTX = 'data-dyn-ctx';
  var ATTR_BLK_CONFIG = 'data-blk-config';
  var ATTR_WATCH = 'data-dyn-watch-params';
  var ATTR_REQUIRE = 'data-dyn-require-params';
  var SCRIPT_TAG_PARAMS = 'dynparams';
  var SCRIPT_TYPE_JSON = 'application/json';

  var _seq = 0;
  /** ctxId 注册表：游离容器（弹窗 holder/独立窗口）按 data-dyn-ctx 接回调用方层链 */
  var _registry = Object.create(null);
  var _rootCtx = null;

  // ---------------- 小工具 ----------------

  function parseJson(str, dft) {
    if (!str || !String(str).trim()) return dft;
    try { return JSON.parse(str); } catch (e) { console.warn('[DynParams] JSON 解析异常', e); return dft; }
  }

  function getPath(obj, path) {
    if (obj == null || !path) return undefined;
    return String(path).replace(/\[(\w+)\]/g, '.$1').split('.').filter(Boolean)
      .reduce(function (o, k) { return o == null ? undefined : o[k]; }, obj);
  }

  function isPlain(v) { return v !== null && typeof v === 'object' && !Array.isArray(v); }

  /** URL query 层：按 location.search 缓存解析结果 */
  var _urlCache = { search: null, map: {} };
  function urlQuery() {
    var s = global.location ? global.location.search : '';
    if (_urlCache.search !== s) {
      _urlCache.search = s;
      _urlCache.map = {};
      try {
        new URLSearchParams(s).forEach(function (v, k) { _urlCache.map[k] = v; });
      } catch (e) { _urlCache.map = {}; }
    }
    return _urlCache.map;
  }

  /** 直接子级 script[tag="dynparams"]（服务端片段下发参数层的官方通道） */
  function readParamsScript(el) {
    if (!el || !el.querySelector) return {};
    var node = el.querySelector(':scope > script[type="' + SCRIPT_TYPE_JSON + '"][tag="' + SCRIPT_TAG_PARAMS + '"]');
    return node ? (parseJson(node.textContent, {}) || {}) : {};
  }

  /** Block 适配：data-blk-config 整体作为 L1 静态层（老页面零改动即并入统一参数链） */
  function readBlkConfig(el) {
    if (!el || !el.getAttribute || !el.hasAttribute(ATTR_BLK_CONFIG)) return {};
    var cfg = parseJson(el.getAttribute(ATTR_BLK_CONFIG), null);
    return isPlain(cfg) ? cfg : {};
  }

  function readStaticLayer(el) {
    return Object.assign({},
      parseJson(el.getAttribute && el.getAttribute(ATTR_PARAMS), {}) || {},
      readParamsScript(el),
      readBlkConfig(el)
    );
  }

  // ---------------- 上下文 ----------------

  function hasDecl(el) {
    return el && el.getAttribute &&
      (el.hasAttribute(ATTR_PARAMS) || el.hasAttribute(ATTR_SHARED_PARAMS));
  }

  /**
   * 找最近的已就绪上下文；途中遇到带 data-dyn-params 声明但尚未建 ctx 的祖先，
   * 自顶向下惰性补建（模板把参数声明在普通 div 上、Block 先于编排脚本挂载时靠这条）。
   */
  function findHangingContext(el) {
    if (el && el.__dynParams) return el.__dynParams;
    var cur = el && el.parentNode;
    var declar = null;
    while (cur && cur.nodeType === 1) {
      if (cur.__dynParams) return cur.__dynParams;
      if (declar === null && hasDecl(cur)) declar = cur;
      var id = cur.getAttribute && cur.getAttribute(ATTR_CTX);
      if (id && _registry[id]) return _registry[id];
      cur = cur.parentNode;
    }
    if (declar) {
      // 递归先把更高层声明祖先补建好（递归沿 parentNode 上行，不经过 declar 自身，必然终止）
      var parent = findHangingContext(declar);
      return createContext(declar, { parent: parent || null });
    }
    return null;
  }

  function makeView(ctx) {
    // 模板里 {{$params.xxx}} 的响应式视图：
    // get 陷阱内读取各 reactive 层的同名字段，Vue effect 自动建立依赖；
    // shared/local/model 任一层变化，跟随型表达式自动更新。
    function touch(c, key) {
      try { void c.runtime[key]; void c.local[key]; void c.shared[key]; } catch (e) {}
      var m = c.el && c.el.__dynModel;
      try { if (m && isPlain(m)) void m[key]; } catch (e) {}
      if (c.parent) touch(c.parent, key);
    }
    return new Proxy({}, {
      get: function (t, key) {
        if (typeof key !== 'string') return undefined;
        touch(ctx, key);
        return ctx.get(key);
      },
      has: function (t, key) { return typeof key === 'string' && ctx.get(key) !== undefined; }
    });
  }

  /**
   * @param {HTMLElement} el 绑定的挂载点
   * @param {object} opt {parent, local, shared}
   */
  function createContext(el, opt) {
    opt = opt || {};
    var local = Vue.reactive(readStaticLayer(el));
    var runtime = Vue.reactive(Object.assign({}, opt.local || {}));
    var shared = opt.shared ||
      (opt.parent && opt.parent.shared) ||
      Vue.reactive(parseJson(el.getAttribute && el.getAttribute(ATTR_SHARED_PARAMS), {}) || {});

    var ctx = {
      id: 'pc' + (++_seq),
      el: el,
      parent: opt.parent || null,
      /** L1 静态声明层（reactive） */
      local: local,
      /** L0 本上下文运行时覆盖（ctx.set，私有；影响本层及子孙） */
      runtime: runtime,
      /** 跨 fork 共享实体层（链式同一个引用，reactive；ctx.commit 写入） */
      shared: shared,
      destroyed: false
    };

    /**
     * 核心解析：返回 {value, layer}
     * @param {string} path 点路径
     * @param {*} [explicit] L0 one-shot 显式值（仅当 !== undefined 时生效）
     */
    ctx.resolveOne = function (path, explicit) {
      var keyHead = String(path).split('.')[0];
      var fromLayer;
      // L0 调用点显式入参
      if (explicit !== undefined) {
        var e0 = getPath(explicit, path);
        if (e0 !== undefined) return { value: e0, layer: 'L0:explicit' };
      }
      // 本层 runtime / static / shared / model
      var v = getPath(runtime, path);
      if (v !== undefined) return { value: v, layer: 'L0:runtime' };
      v = getPath(local, path);
      if (v !== undefined) return { value: v, layer: 'L1:static' };
      v = getPath(shared, path);
      if (v !== undefined) return { value: v, layer: 'shared' };
      var m = el && el.__dynModel;
      if (m && isPlain(m)) {
        v = getPath(m, path);
        if (v !== undefined) return { value: v, layer: 'L2:model' };
      }
      // L3 父链（父链内重复 shared 查找无副作用）
      if (ctx.parent) {
        var r = ctx.parent.resolveOne(path);
        if (r && r.value !== undefined) return r;
      }
      // L4 URL 兜底（只在链根查一次）
      if (!ctx.parent) {
        v = getPath(urlQuery(), path);
        if (v !== undefined) return { value: v, layer: 'L4:url' };
      }
      return { value: undefined, layer: null };
    };

    ctx.get = function (path, dft) {
      if (ctx.destroyed) console.warn('[DynParams] 上下文已销毁仍在取值', ctx.id, path);
      var r = ctx.resolveOne(path);
      return r.value === undefined ? dft : r.value;
    };

    /** 必需键：缺失时统一告警（带来源定位），返回是否全部就绪 */
    ctx.require = function (keys) {
      var missing = (keys || []).filter(function (k) { return ctx.get(k) === undefined; });
      if (missing.length) {
        console.warn('[DynParams] 缺少必需参数', missing, 'at', el);
      }
      return !missing.length;
    };

    /** 本层私有覆盖（影响本层及子孙，兄弟不可见） */
    ctx.set = function (key, value) { runtime[key] = value; return ctx; };

    /** 发布到共享实体层（链式各 fork 立即可见，跟随型模板自动更新） */
    ctx.commit = function (key, value, meta) {
      shared[key] = value;
      if (meta && isPlain(meta)) ctx._provenance[key] = meta;
      return ctx;
    };
    ctx._provenance = Vue.reactive({});

    /** 派生上下文：片段/tab/弹窗挂载点用；local 为该挂载点的一次性入参层 */
    ctx.fork = function (scopeEl, localParams) {
      if (!scopeEl) return ctx;
      if (scopeEl.__dynParams) return scopeEl.__dynParams;
      return createContext(scopeEl, { parent: ctx, local: localParams || {}, shared: shared });
    };

    /** 合并快照（JSON 安全值的普通对象，用于请求/调试） */
    ctx.all = function () {
      var out = {};
      var chain = [];
      var c = ctx;
      while (c) { chain.unshift(c); c = c.parent; }
      // 从根到叶逐层覆盖：url → model → shared → static → runtime
      Object.keys(urlQuery()).forEach(function (k) { out[k] = urlQuery()[k]; });
      chain.forEach(function (node) {
        var m = node.el && node.el.__dynModel;
        if (m && isPlain(m)) Object.keys(m).forEach(function (k) { out[k] = m[k]; });
        Object.keys(node.shared).forEach(function (k) { out[k] = node.shared[k]; });
        Object.keys(node.local).forEach(function (k) { out[k] = node.local[k]; });
        Object.keys(node.runtime).forEach(function (k) { out[k] = node.runtime[k]; });
      });
      return out;
    };

    /** 调试：每个 key 的值 + 获胜层 */
    ctx.inspect = function (keys) {
      keys = keys || (function () {
        var ks = {};
        var c = ctx;
        while (c) {
          Object.keys(urlQuery()).forEach(function (k) { ks[k] = 1; });
          Object.keys(c.shared).forEach(function (k) { ks[k] = 1; });
          Object.keys(c.local).forEach(function (k) { ks[k] = 1; });
          Object.keys(c.runtime).forEach(function (k) { ks[k] = 1; });
          c = c.parent;
        }
        return Object.keys(ks);
      })();
      var out = {};
      keys.forEach(function (k) {
        var r = ctx.resolveOne(k);
        out[k] = { value: r.value, layer: r.layer };
        if (ctx._provenance[k]) out[k].committedBy = ctx._provenance[k].by || null;
      });
      return out;
    };

    /**
     * 跟随型监听：共享/继承层的键变化时回调（快照型 Block 不用）。
     * @param {string[]} keys
     * @param {(key,newVal,oldVal)=>void} cb
     * @returns {Function} 注销
     */
    ctx.watch = function (keys, cb) {
      var stopped = false;
      var stops = (keys || []).map(function (k) {
        var last = ctx.get(k);
        return Vue.watch(function () {
          // 依赖收集：view 已 touch 全部层
          return ctx.view[k];
        }, function (nv) {
          if (stopped) return;
          var ov = last;
          last = nv;
          if (!sameValue(ov, nv)) { try { cb(k, nv, ov); } catch (e) { console.error('[DynParams.watch]', e); } }
        });
      });
      return function () { stopped = true; stops.forEach(function (s) { try { s(); } catch (e) {} }); };
    };

    /**
     * 值内占位统一解析（动作 options / URL 模板 / 请求体）：
     *   "$params.courseId"   整串 token → 保留原始类型
     *   "$model.form.Name"   整串 token
     *   "$result.data.Id"    链路上一步结果
     *   "{{$params.x}}"      行内插值（字符串化）
     */
    ctx.resolveValue = function (val, resultObj, explicit) {
      var self = this;
      if (typeof val === 'string') {
        var m, v;
        // 整串 token：缺失时【保留原串】——buildCtx 首次解析时 $result 尚未产生，
        // 链路赋值后会重解析；若这里置 undefined，后续永远无法恢复。
        if ((m = val.match(/^\$params\.([\w.[\]]+)$/))) {
          v = self.get(m[1]);
          return v === undefined ? val : v;
        }
        if ((m = val.match(/^\$model\.([\w.[\]]+)$/))) {
          v = getPath(self.el && self.el.__dynModel, m[1]);
          return v === undefined ? val : v;
        }
        if ((m = val.match(/^\$result((?:\.[\w$]+)+)?$/))) {
          if (resultObj == null) return val; // 上一步结果尚未产生
          v = m[1] ? getPath(resultObj, m[1].slice(1)) : resultObj;
          return v === undefined ? val : v;
        }
        return val.replace(/\{\{\s*(\$params|\$model|\$result)((?:\.[\w$]+)+)\s*\}\}/g, function (full, kind, p) {
          var x;
          if (kind === '$params') x = self.get(p.slice(1));
          else if (kind === '$model') x = getPath(self.el && self.el.__dynModel, p.slice(1));
          else { if (resultObj == null) return full; x = getPath(resultObj, p.slice(1)); }
          if (x === undefined) return full; // 缺失保留占位，等待重解析/暴露配置错误
          return x == null ? '' : String(x);
        });
      }
      if (Array.isArray(val)) return val.map(function (x) { return self.resolveValue(x, resultObj, explicit); });
      if (isPlain(val)) {
        var o = {};
        Object.keys(val).forEach(function (k) { o[k] = self.resolveValue(val[k], resultObj, explicit); });
        return o;
      }
      return val;
    };

    ctx.destroy = function () {
      ctx.destroyed = true;
      if (_registry[ctx.id]) delete _registry[ctx.id];
    };
    /** 游离容器交接用：注册 ctxId（holder 节点打 data-dyn-ctx） */
    ctx.registerId = function () { _registry[ctx.id] = ctx; return ctx.id; };

    ctx.view = makeView(ctx);
    if (el) el.__dynParams = ctx;
    return ctx;
  }

  function sameValue(a, b) {
    // 引用/原始值比较即可；引用类型变更应通过 commit/set 触发
    return a === b || (a == null && b == null);
  }

  /** 页面根上下文（无挂载点时的链根） */
  function rootContext() {
    if (!_rootCtx || _rootCtx.destroyed) {
      var rootEl = document.body || document.documentElement;
      _rootCtx = createContext(rootEl, {});
      // body 上的上下文不应污染语义：仅作为 L4 URL 层的载体
    }
    return _rootCtx;
  }

  var DynParams = {
    /**
     * 为挂载点建立/复用上下文。
     * @param {HTMLElement} el 新挂载点（App 根/片段容器）
     * @param {HTMLElement} [parentEl] 显式父挂载点（嵌套 App 掩码还原时 DOM 祖先链暂时断开）
     */
    ensure: function (el, parentEl) {
      if (!el) return rootContext();
      if (el.__dynParams) return el.__dynParams;
      var parent = findHangingContext(el)
        || (parentEl && parentEl.__dynParams)
        || (parentEl && findHangingContext(parentEl))
        || null;
      return createContext(el, { parent: parent });
    },
    /** 片段注入：把目标元素接入最近上下文（没有则挂页面根），返回该 fork */
    attach: function (el) {
      if (!el) return null;
      if (el.__dynParams) return el.__dynParams;
      var parent = findHangingContext(el) || rootContext();
      return createContext(el, { parent: parent });
    },
    /** 任意元素取其所属上下文（非挂载点向上找 App 根/祖先 fork；都没有返回页面根） */
    fromEl: function (el) {
      var c = findHangingContext(el);
      if (c) return c;
      var appEl = el && el.closest ? el.closest('[data-dyn-mode="createApp"]') : null;
      if (appEl && appEl.__dynParams) return appEl.__dynParams;
      return rootContext();
    },
    byId: function (id) { return _registry[id] || null; },
    root: rootContext,
    parseJson: parseJson,
    getPath: getPath
  };

  global.DynParams = DynParams;
})(window);
