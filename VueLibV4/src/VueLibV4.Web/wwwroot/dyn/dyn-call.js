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
var __plugin = {
  name:'call', stage:'services', requires:[],
  setup:function(ctx){
  'use strict';

  var axios = global.axios;
  if (!axios) { console.warn('[DynCall] axios 未就绪，请先由 dyn-lib 加载'); }

  /* ---------- 可选：为所有 axios 请求自动注入 X-Api-Key（默认关闭） ----------
   * 场景：部署到服务器后，浏览器直连且本机回环放行（AllowLocal）不够用时。
   * 用法：把 enabled 置 true、key 填成后端 Dyn:Auth:ApiKey 的值。
   * 注意：key 会暴露在前端 JS 里，只适合内网/可信环境；公网请改用登录态（cookie/JWT）。 */
  var API_KEY_AUTH = { enabled: false, key: '' };
  (function setupApiKeyAuth() {
    if (!global.axios || !API_KEY_AUTH.enabled || !API_KEY_AUTH.key) return;
    global.axios.interceptors.request.use(function (cfg) {
      cfg.headers = cfg.headers || {};
      cfg.headers['X-Api-Key'] = API_KEY_AUTH.key;
      return cfg;
    });
  })();

  /* ---------- 401 统一处理：登录过期/未登录时清权限缓存并跳登录页 ----------
   * 登录页自身不再跳转，避免循环；所有业务请求共用 axios 实例，一处拦截全局生效。 */
  (function setupAuthRedirect() {
    if (!global.axios) return;
    global.axios.interceptors.response.use(null, function (err) {
      if (err && err.response && err.response.status === 401) {
        try {
          if (global.DynPermission && typeof global.DynPermission.clearCache === 'function') {
            global.DynPermission.clearCache();
          } else {
            sessionStorage.removeItem('dyn_permission_keys');
          }
        } catch (e) { /* ignore */ }
        var path = location.pathname + location.search;
        var onLogin = location.pathname.indexOf('/Platform/Page/Login') === 0;
        if (!onLogin) {
          location.href = '/Platform/Page/Login?returnUrl=' + encodeURIComponent(path);
        }
      }
      return Promise.reject(err);
    });
  })();

  /* ---------- 轻量 loading 遮罩（按元素隔离 + 引用计数） ----------
   * 同一元素可能并发多个请求：show 一次就把 count+1，hide 一次 count-1，
   * 归零才真正移除遮罩——避免先完成的请求把还在飞的另一个请求的 loading 提前关掉。 */
  function showMask(el) {
    if (!el) return;
    el.__dynMaskCount = (el.__dynMaskCount || 0) + 1;
    if (el.__dynMask) return; // 遮罩已在，不重复建
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
    el.__dynMaskCount = (el.__dynMaskCount || 0) - 1;
    if (el.__dynMaskCount > 0) return; // 还有别的请求在用遮罩
    el.__dynMaskCount = 0;
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
  ctx.provide('call', DynCall);
  }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);
