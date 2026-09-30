/* dyn-rpc.js —— 能力桥前端：JS 直接调用 C# 服务 / 执行 C# 代码
 * 依赖：dyn-call.js（信封解包）、axios
 *
 * 两种用法：
 *
 * 1) 远程服务代理（像本地对象一样调用后端 DI 服务的公共方法）
 *    const svc = dyn.service('DesktopShortcutService');
 *    const rows = await svc.ListBySolution(null);          // POST /api/rpc/invoke/...
 *    const one  = await svc.GetById(1);
 *
 * 2) eval 自由代码（DynamicExpresso 执行，内置变量 Crud/Biz/Platform + 全部 DI 服务 + 实体门面）
 *    // 实体门面（变量名=实体类名）：条件是字符串 lambda，在数据库侧执行，支持排序/取数/选列
 *    await dyn.eval(`DesktopShortcut.Where("x => x.IsActive == true")
 *                      .OrderBy("x => x.SortNo").SelectFields("Name").ToList()`);
 *    // 内存 LINQ（先取到内存再过滤，适合小数据量）
 *    await dyn.eval(`DesktopShortcutService.List().Where(x => x.Name != "").ToList()`);
 *    await dyn.eval(`Crud.Tables(Biz)`);                    // 免模型：列出业务库所有表
 *
 * 带参数（值与代码分离，防止注入；字符串 lambda 内外都能直接引用）：
 *    await dyn.eval(`DesktopShortcut.Where("x => x.IsActive == active && x.SortNo >= sortNo").Count()`,
 *                   { active: true, sortNo: 1 });
 *
 * 可选 opts：{ silent:true 不弹错误提示, scopeEl, mask:false }（同 DynCall）
 */
(function (global) {
  'use strict';

  var RPC_BASE = '/api/rpc';

  function post(url, data, opts) {
    if (global.DynCall && typeof global.DynCall.post === 'function') {
      return global.DynCall.post(url, data, opts || {});
    }
    // 兜底：未加载 dyn-call 时直接走 axios，手动解包
    return global.axios.post(url, data || {}).then(function (r) {
      var env = r.data;
      if (env && env.code === 0) return env.data;
      throw new Error((env && env.msg) || 'RPC 请求失败');
    });
  }

  /**
   * @description 获取后端 DI 服务的远程代理；任意方法调用自动转为 invoke 请求
   * @param {string} serviceName 服务实现类名或业务接口名（不区分大小写）
   * @returns {Proxy} 方法名即远程方法，调用返回 Promise（resolve 已解包的 data）
   */
  function service(serviceName) {
    if (!serviceName) throw new Error('dyn.service 需要服务名，如 dyn.service("DesktopShortcutService")');
    return new Proxy({}, {
      get: function (target, prop) {
        // 防止被 Promise 机制误判为 thenable
        if (prop === 'then' || typeof prop !== 'string') return undefined;
        return function () {
          var args = Array.prototype.slice.call(arguments);
          return post(RPC_BASE + '/invoke/' +
            encodeURIComponent(serviceName) + '/' + encodeURIComponent(prop), args);
        };
      }
    });
  }

  /**
   * @description 执行一段 C# 代码并返回结果 JSON
   * @param {string} code C# 表达式/语句（可用全部 DI 服务名及 Crud/Biz/Platform）
   * @param {object} [args] 参数对象，代码中以变量名直接引用（值走 JSON，不拼接代码）
   * @param {object} [opts] { timeoutSec:30, silent, scopeEl, mask }
   * @returns {Promise<any>}
   */
  function evalCode(code, args, opts) {
    opts = opts || {};
    var payload = { code: code, args: args || {} };
    if (opts.timeoutSec) payload.timeoutSec = opts.timeoutSec;
    var callOpts = {};
    ['silent', 'scopeEl', 'mask'].forEach(function (k) {
      if (opts[k] !== undefined) callOpts[k] = opts[k];
    });
    return post(RPC_BASE + '/eval', payload, callOpts);
  }

  /** @description 获取后端 RPC 服务与方法目录（服务浏览面板/智能提示用） */
  function services(opts) {
    if (global.DynCall && typeof global.DynCall.get === 'function') {
      return global.DynCall.get(RPC_BASE + '/services', {}, opts || {});
    }
    return global.axios.get(RPC_BASE + '/services').then(function (r) { return r.data.data; });
  }

  global.dyn.service = service;
  global.dyn.eval = evalCode;
  global.dyn.rpcServices = services;
})(window);
