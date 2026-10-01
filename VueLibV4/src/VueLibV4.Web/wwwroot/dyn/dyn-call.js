/* dyn-call.js —— 平台统一 JSON 接口调用封装
 * 依赖：window.axios（dyn-lib 已加载本地 axios UMD）
 *
 * 设计目的：
 *   - 统一走标准信封 { code, msg, data }（业务成功判定 code===0）
 *   - 自动解包：成功 resolve(res.data)；失败统一 toast + reject，调用方无需重复判 code
 *   - 可选 loading 遮罩（绑定到调用方所属元素，多实例/多 scope 互不干扰）
 *   - silent：静默模式（失败不 toast），供命令链/批量/后台刷新场景复用
 *
 * 供模板 / 积木(Block) / 动作助手(defineAction) 统一调用，替代各处手写 axios/fetch。
 * 用法示例：
 *   DynCall.post('/api/dyndata/search', {table,page,size,filter,project}, {scopeEl, silent})
 *     .then(data=>{ ... }).catch(err=>{ ... });            // data 已是信封解包后的 data
 */
(function (global) {
  'use strict';

  var axios = global.axios;
  if (!axios) { console.warn('[DynCall] axios 未就绪，请先由 dyn-lib 加载'); }

  /* ---------- 轻量 loading 遮罩（按元素隔离） ---------- */
  var maskCache = {}; // el -> count

  function showMask(el) {
    if (!el || el.__dynMask) return;
    var m = document.createElement('div');
    m.className = 'dyncall-mask';
    m.innerHTML = '<div class="dyncall-spinner"></div>';
    // 确保定位上下文
    var pos = global.getComputedStyle(el).position;
    if (pos === 'static') el.style.position = 'relative';
    el.appendChild(m);
    el.__dynMask = m;
  }
  function hideMask(el) {
    if (!el || !el.__dynMask) return;
    var m = el.__dynMask; el.__dynMask = null;
    if (m && m.parentNode) m.parentNode.removeChild(m);
  }

  /* ---------- 核心请求 ---------- */
  function request(method, url, data, opts) {
    opts = opts || {};
    var scopeEl = opts.scopeEl || null; // 遮罩挂载元素（通常是当前 Block 的容器元素）
    var mask = opts.mask !== false;     // 是否显示 loading 遮罩（默认开，需配合 scopeEl）
    var silent = !!opts.silent;         // 静默：失败不 toast
    var showMaskNow = scopeEl && mask;
    if (showMaskNow) showMask(scopeEl);

    var p;
    if (method === 'GET') {
      p = axios.get(url, { params: data || {} });
    } else {
      p = axios.post(url, data || {}, { headers: { 'Content-Type': 'application/json' } });
    }
    return p.then(function (res) {
      return res.data; // 信封 { code, msg, data }
    }).then(function (envelope) {
      // 统一信封解包：成功返回 data；失败 toast 并 reject
      if (envelope && envelope.code === 0) return envelope.data;
      var msg = (envelope && envelope.msg) || '请求失败';
      if (!silent && global.dyn && typeof dyn.showMessage === 'function') {
        dyn.showMessage(msg, 'error');
      }
      var err = new Error(msg);
      err.envelope = envelope;
      throw err;
    }).finally(function () {
      if (showMaskNow) hideMask(scopeEl);
    });
  }

  var DynCall = {
    /** POST JSON 接口（信封契约） */
    post: function (url, data, opts) { return request('POST', url, data, opts); },
    /** GET 接口（query 参数，信封契约） */
    get: function (url, params, opts) { return request('GET', url, params, opts); },
    /** 删除（走 POST 信封，契约同 dyndata delete） */
    del: function (url, data, opts) { return request('POST', url, data, opts); },
    /** 直接发起，不强制信封（可传 raw=true 拿原始响应），保留扩展位 */
    _raw: function (method, url, data, opts) {
      var m = (method || 'POST').toUpperCase();
      return (m === 'GET' ? axios.get(url, { params: data }) : axios.post(url, data)).then(function (r) { return r.data; });
    }
  };
  global.DynCall = DynCall;
})(window);
