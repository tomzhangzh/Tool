/* dyn-kernel.js V4 显式内核：阶段化装配 + 插件契约 + 依赖校验
 *
 * 解决的问题：此前各模块靠 <script> 顺序 + 全局鸭子类型（global.DynX && fn）
 * 自行初始化，依赖缺失只在运行时散落告警。现在插件通过 DynKernel.register
 * 显式声明 name/stage/requires/setup，加载期只登记，由内核在全部脚本就绪后
 * 按固定阶段顺序 boot，顺序不再依赖 <script> 标签排列。
 *
 * 阶段（顺序即依赖方向）：
 *   core → components → params → services → actions
 *        → blocks → layout → designer → debug
 *
 * 兼容性：
 *  - boot 完成后再 register 的插件（cshtml 按需引入的 blocks/layouts 等）
 *    立即热装配；
 *  - 若本文件意外晚于某模块加载，该模块可把插件描述推入
 *    window.__DYN_KERNEL_PENDING__，本文件加载后自动消费。
 */
(function (global) {
'use strict';
if (global.DynKernel) return;

var STAGES = ['core', 'components', 'params', 'services', 'actions', 'blocks', 'layout', 'designer', 'debug'];

/** @type {Array<{name:string,stage:string,requires:string[],setup:Function,status:string}>} */
var plugins = [];
/** 插件显式提供的能力表：provideName → api */
var provided = Object.create(null);
var booted = false;
var bootPromise = null;

function isProvided(name) {
  // 插件 provide 的能力优先；未显式 provide 的回退全局（Vue/axios/DynCall 等）
  return Object.prototype.hasOwnProperty.call(provided, name) || !!global[name];
}

function missingDeps(p) {
  return (p.requires || []).filter(function (n) { return !isProvided(n); });
}

function bootOne(p) {
  var missing = missingDeps(p);
  if (missing.length) {
    p.status = 'skipped';
    console.error('[DynKernel] 插件[' + p.name + ']缺少依赖 [' + missing.join(', ') +
      ']，已跳过（阶段 ' + p.stage + '）。请检查模块加载与契约声明。');
    return;
  }
  try {
    var ctx = {
      stage: p.stage,
      provide: function (name, api) { provided[name] = api; },
      use: function (name) {
        if (Object.prototype.hasOwnProperty.call(provided, name)) return provided[name];
        return global[name] != null ? global[name] : null;
      }
    };
    p.setup(ctx);
    p.status = 'done';
  } catch (e) {
    p.status = 'failed';
    console.error('[DynKernel] 插件[' + p.name + '](阶段 ' + p.stage + ') 启动失败：', e);
  }
}

/**
 * 登记/热装配一个插件。
 * @param {object} p
 * @param {string} p.name 唯一插件名
 * @param {string} p.stage 所属阶段（见 STAGES）
 * @param {string[]} [p.requires] 强依赖（provide 名或全局名，如 dyn/Vue/DynCall）
 * @param {Function} p.setup 启动函数，参数 ctx 带 provide/use
 */
function register(p) {
  if (!p || !p.name || typeof p.setup !== 'function') {
    console.warn('[DynKernel] register 参数非法（需要 {name,stage,setup}）：', p);
    return;
  }
  if (STAGES.indexOf(p.stage) < 0) {
    console.warn('[DynKernel] 插件[' + p.name + ']声明了未知阶段 "' + p.stage + '"，将被忽略');
    return;
  }
  if (plugins.some(function (x) { return x.name === p.name; })) {
    console.warn('[DynKernel] 插件[' + p.name + ']重复注册，已忽略');
    return;
  }
  p.requires = p.requires || [];
  p.status = booted ? 'registered-hot' : 'registered';
  plugins.push(p);
  // boot 后才加载（ajax 片段 / cshtml 按需 <script>）→ 立即热装配
  if (booted) bootOne(p);
}

/** 按阶段顺序启动全部已登记插件；幂等，返回状态清单 */
function boot() {
  if (bootPromise) return bootPromise;
  bootPromise = Promise.resolve().then(function () {
    STAGES.forEach(function (stage) {
      plugins.filter(function (p) {
        return p.stage === stage && p.status === 'registered';
      }).forEach(bootOne);
    });
    booted = true;
    return plugins.map(function (p) { return { name: p.name, stage: p.stage, status: p.status }; });
  });
  return bootPromise;
}

global.DynKernel = {
  register: register,
  boot: boot,
  provide: function (name, api) { provided[name] = api; },
  use: function (name) {
    if (Object.prototype.hasOwnProperty.call(provided, name)) return provided[name];
    return global[name] != null ? global[name] : null;
  },
  isBooted: function () { return booted; },
  stages: STAGES.slice(),
  status: function () {
    return plugins.map(function (p) { return { name: p.name, stage: p.stage, status: p.status }; });
  }
};

// 消费先于本文件登记的插件（异常加载顺序兜底）
var pending = global.__DYN_KERNEL_PENDING__;
if (Array.isArray(pending)) {
  pending.forEach(function (p) { try { register(p); } catch (e) { console.warn('[DynKernel] pending 注册异常', e); } });
  global.__DYN_KERNEL_PENDING__ = [];
}
})(window);
