/* ============================================================
 * VueLibV3  dyn-actionhelper.js  动作助手
 * ------------------------------------------------------------
 * 核心原则：所有动作（含脚本）都必须存在于数据库 DynActionHelper，
 * 前端从 /api/platform/dynactionhelper/all 动态加载注册（动态注册），
 * 也可从 /api/platform/dynactionhelper/script 拉取"生成脚本"。
 * 动作类型：script(脚本) / url(链接) / api(接口) / chain(动作链)
 * 脚本上下文：$action(动作定义) $args(参数) $ctx(页面上下文) $api(API工具)
 * ============================================================ */
(function (global) {
  'use strict';

  var ACTIONS = {};          // code -> action 定义
  var API_BASE = '/api/platform/dynactionhelper';

  /* ---------- $api 工具集（静态） ---------- */
  var api = {
    msg: function (message, type) {
      var ElMessage = global.ElementPlus && global.ElementPlus.ElMessage;
      if (ElMessage) ElMessage({ message: message || '', type: type || 'success' });
      else window.alert(message);
    },
    alert: function (message) {
      var ElMessageBox = global.ElementPlus && global.ElementPlus.ElMessageBox;
      if (ElMessageBox) ElMessageBox.alert(message || '提示', '提示', { confirmButtonText: '确定' });
      else window.alert(message);
    },
    confirm: function (message, title) {
      var ElMessageBox = global.ElementPlus && global.ElementPlus.ElMessageBox;
      if (ElMessageBox) {
        return ElMessageBox.confirm(message || '确定执行吗？', title || '提示', { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' })
          .then(function () { return true; })
          .catch(function () { return false; });
      }
      return Promise.resolve(window.confirm(message || '确定？'));
    },
    openUrl: function (url, target) {
      window.open(url, target || '_blank');
    },
    openWindow: function (name) {
      window.dispatchEvent(new CustomEvent('dyn-open-window', { detail: name }));
    },
    refresh: function () { window.location.reload(); },
    http: function (url, method, body) {
      return fetch(url, {
        method: method || 'GET',
        headers: { 'Content-Type': 'application/json' },
        body: body ? JSON.stringify(body) : undefined
      }).then(function (r) { return r.json(); });
    }
  };

  /* ---------- 注册 ---------- */
  function register(action) {
    if (!action || !action.Code) return;
    ACTIONS[action.Code] = action;
  }

  function registerAll(list) {
    (list || []).forEach(register);
  }

  /** 从数据库动态加载所有动作 */
  function loadAll() {
    return fetch(API_BASE + '/all')
      .then(function (r) { return r.json(); })
      .then(function (res) {
        if (res.code === 0) { registerAll(res.data); }
        return ACTIONS;
      })
      .catch(function (e) {
        console.error('[DynActionHelper] 加载动作失败', e);
        return ACTIONS;
      });
  }

  function get(code) { return ACTIONS[code]; }

  /* ---------- 执行 ---------- */
  function run(code, args, ctx) {
    var def = ACTIONS[code];
    if (!def) {
      // 未注册时尝试直接拉取
      return fetch(API_BASE + '/get?id=' + encodeURIComponent(code))
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res.code === 0 && res.data) { register(res.data); return run(res.data.Code || code, args, ctx); }
          console.warn('[DynActionHelper] 动作不存在: ' + code);
          return Promise.resolve(null);
        });
    }
    try {
      switch (def.ActionType) {
        case 'url':
          return runUrl(def, args);
        case 'api':
          return runApi(def, args);
        case 'chain':
          return runChain(def, args, ctx);
        case 'script':
        default:
          return runScript(def, args, ctx);
      }
    } catch (e) {
      console.error('[DynActionHelper] 执行异常', def.Code, e);
      return Promise.reject(e);
    }
  }

  function runScript(def, args, ctx) {
    var fn = new Function('$action', '$args', '$ctx', '$api', def.Script || '');
    return Promise.resolve(fn(def, args || {}, ctx || {}, api));
  }

  function runUrl(def, args) {
    var url = fillTemplate(def.Script || '', args);
    var target = (args && args.target) || 'self';
    if (url) global.window.open(url, target === '_blank' ? '_blank' : '_self');
    return Promise.resolve(url);
  }

  function runApi(def, args) {
    var url = fillTemplate(def.Script || '', args);
    var method = (args && args.method) || 'GET';
    return api.http(url, method, args && args.body).then(function (res) { return res && res.data; });
  }

  /** 动作链：Script 为 JSON 数组 [{action, params, when}]，when 可用 prev 判断 */
  function runChain(def, args, ctx) {
    var steps = [];
    try { steps = JSON.parse(def.Script || '[]'); } catch (e) { steps = []; }
    var prev = null;
    return steps.reduce(function (p, step) {
      return p.then(function (last) {
        if (step.when) {
          if (step.when === 'result' && !last) return prev;   // 上一步成功才继续
          if (step.when === 'false' && last) return prev;
        }
        prev = last;
        return run(step.action, Object.assign({}, args || {}, step.params || {}, { __prev: last }), ctx);
      });
    }, Promise.resolve(null));
  }

  function fillTemplate(tpl, args) {
    return (tpl || '').replace(/\{(\w+)\}/g, function (_, key) {
      return args && args[key] !== undefined ? args[key] : '';
    });
  }

  global.DynActionHelper = {
    register: register,
    registerAll: registerAll,
    loadAll: loadAll,
    get: get,
    run: run,
    runChain: runChain,
    actions: ACTIONS,
    api: api
  };
})(window);
