#!/usr/bin/env node
/*
 * Dyn 引擎纯函数回归网（零依赖，Node 内置 vm 沙箱）
 *
 * 用法：
 *   node tests/dyn/run.js                  # 正常跑全部用例
 *   DYN_TEST_SELF_CHECK=1 node tests/dyn/run.js   # 故意注入 1 条失败用例，验证 harness 本身有效
 *
 * 原理：用 vm 加载【真实】的 wwwroot/dyn/dyn-core.js + dyn-action.js
 * （不复制实现），配合极简的 Vue/DOM/fetch 桩，让闭包内部的纯函数
 * 通过 dyn-action.js 里的 __DYN_TEST__ 测试钩子导出后直接断言。
 */
'use strict';
const fs = require('fs');
const path = require('path');
const vm = require('vm');

// ---------- 极简桩 ----------
function makeEl(attrs) {
  // 模拟 NamedNodeMap：元素上 data-* 属性的迭代形态（{name,value} 数组）
  const attrList = Object.keys(attrs || {}).map(k => ({ name: k, value: attrs[k] }));
  const map = Object.assign({}, attrs || {});
  return {
    nodeType: 1,
    attributes: attrList,
    parentNode: null,
    childElementCount: 0,
    style: {},
    classList: { contains: () => false },
    hasAttribute: n => Object.prototype.hasOwnProperty.call(map, n),
    getAttribute: n => (Object.prototype.hasOwnProperty.call(map, n) ? map[n] : null),
    setAttribute: (n, v) => { map[n] = String(v); attrList.push({ name: n, value: String(v) }); },
    removeAttribute: n => { delete map[n]; },
    closest: () => null,
    matches: () => false,
    querySelectorAll: () => []
  };
}

function makeSandbox() {
  const sandbox = {};
  // JS 内建由 vm 上下文自带（Promise/WeakMap/JSON/RegExp…）；Node 专有全局需手动给
  sandbox.window = sandbox;
  sandbox.console = console;
  sandbox.setTimeout = (fn, ms) => setTimeout(fn, ms);
  sandbox.clearTimeout = t => clearTimeout(t);
  sandbox.URLSearchParams = URLSearchParams;
  sandbox.fetch = async () => ({ json: async () => ({ code: 0, data: [] }) });
  sandbox.document = {
    readyState: 'complete',
    body: makeEl({}),
    addEventListener: () => {},
    removeEventListener: () => {},
    createElement: () => makeEl({}),
    getElementById: () => null,
    querySelector: () => null,
    querySelectorAll: () => []
  };
  sandbox.Vue = {
    version: '3.0.0',
    reactive: o => o,
    h: () => null,
    provide: () => {},
    createApp: () => ({
      config: { compilerOptions: {}, globalProperties: {} },
      component: function () { return this; },
      mount: () => null,
      unmount: () => {}
    })
  };
  sandbox.__DYN_TEST__ = {};
  vm.createContext(sandbox);
  return sandbox;
}

function loadEngine() {
  const dynDir = path.join(__dirname, '..', '..', 'src', 'VueLibV4.Web', 'wwwroot', 'dyn');
  const sb = makeSandbox();
  // 顺序模拟 dyn-lib 的加载链；初始化顺序由 DynKernel 按阶段决定
  vm.runInContext(fs.readFileSync(path.join(dynDir, 'dyn-kernel.js'), 'utf8'), sb, { filename: 'dyn-kernel.js' });
  vm.runInContext(fs.readFileSync(path.join(dynDir, 'dyn-core.js'), 'utf8'), sb, { filename: 'dyn-core.js' });
  vm.runInContext(fs.readFileSync(path.join(dynDir, 'dyn-action.js'), 'utf8'), sb, { filename: 'dyn-action.js' });
  return sb;
}

async function bootEngine(sb) {
  await vm.runInContext('DynKernel.boot()', sb);
  return { dyn: sb.dyn, I: sb.__DYN_TEST__.exports, kernel: sb.DynKernel };
}

// ---------- 迷你测试框架 ----------
let total = 0, passed = 0;
const failures = [];
const groups = [];
let current = null;

function describe(name, fn) {
  current = { name, tests: [] };
  groups.push(current);
  fn();
  current = null;
}
function it(name, fn) {
  current.tests.push({ name, fn });
}

// 轻量 deep-equal（JSON 可序列化结构即可）
function deepEq(a, b) {
  return JSON.stringify(a) === JSON.stringify(b);
}
function eq(actual, expected, msg) {
  if (actual !== expected) {
    throw new Error(`${msg || 'eq'}: 期望 ${JSON.stringify(expected)}，实得 ${JSON.stringify(actual)}`);
  }
}
function jsonEq(actual, expected, msg) {
  if (!deepEq(actual, expected)) {
    throw new Error(`${msg || 'jsonEq'}: 期望 ${JSON.stringify(expected)}，实得 ${JSON.stringify(actual)}`);
  }
}
function ok(v, msg) {
  if (!v) throw new Error(msg || '期望真值');
}
function nullable(v, msg) {
  if (v !== null) throw new Error(`${msg || 'null'}: 实得 ${JSON.stringify(v)}`);
}

async function run() {
  if (process.env.DYN_TEST_SELF_CHECK === '1') {
    describe('【自检】harness 失败探测', () => {
      it('故意失败：1 应当等于 2', () => eq(1, 2, '自检断言'));
    });
  }

  const { dyn, I, kernel } = await bootEngine(loadEngine());

  // showMessage 间谍：捕获 calc/runPipe 的告警提示
  const msgs = [];
  dyn.showMessage = (m, t) => { msgs.push({ m: String(m), t: t || 'info' }); };
  const lastMsg = () => (msgs.length ? msgs[msgs.length - 1] : null);

  // ---------------- parsePipe ----------------
  describe('parsePipe 管道解析（本次动作协议统一的核心）', () => {
    it('null / 非字符串 / 空串 → []', () => {
      jsonEq(I.parsePipe(null), []);
      jsonEq(I.parsePipe(123), []);
      jsonEq(I.parsePipe(''), []);
      jsonEq(I.parsePipe('   '), []);
    });
    it('裸动作名 → 空 options', () => {
      jsonEq(I.parsePipe('submit'), [{ action: 'submit', options: {} }]);
    });
    it('自动 trim 两侧空白', () => {
      jsonEq(I.parsePipe('  submit  '), [{ action: 'submit', options: {} }]);
    });
    it('剥 ActionHelper. 前缀（大小写不敏感）', () => {
      eq(I.parsePipe('ActionHelper.Toast')[0].action, 'Toast');
      eq(I.parsePipe('actionhelper.toast')[0].action, 'toast');
      eq(I.parsePipe('ActionHelper.close')[0].action, 'close');
    });
    it('单/双引号字符串参数 → {value}', () => {
      jsonEq(I.parsePipe(`toast('保存成功')`), [{ action: 'toast', options: { value: '保存成功' } }]);
      jsonEq(I.parsePipe('toast("ok")'), [{ action: 'toast', options: { value: 'ok' } }]);
    });
    it('数字 / 布尔 / null 参数 → {value}', () => {
      eq(I.parsePipe('delay(300)')[0].options.value, 300);
      eq(I.parsePipe('x(-1.5)')[0].options.value, -1.5);
      eq(I.parsePipe('x(true)')[0].options.value, true);
      eq(I.parsePipe('x(false)')[0].options.value, false);
      eq(I.parsePipe('x(null)')[0].options.value, null);
    });
    it('JSON 对象参数 → 原对象（含 $before 等元字段）', () => {
      const steps = I.parsePipe(`calc({"expr":"count += 1"})`);
      jsonEq(steps, [{ action: 'calc', options: { expr: 'count += 1' } }]);
      const s2 = I.parsePipe(`postback({"$before":[{"action":"confirm"}],"url":"/x"})`);
      eq(s2[0].options.url, '/x');
      jsonEq(s2[0].options.$before, [{ action: 'confirm' }]);
    });
    it('数组参数 → {value:数组}（非对象一律包 value）', () => {
      const s = I.parsePipe('chain({"steps":[1,2]})');
      jsonEq(s[0].options, { steps: [1, 2] }); // 对象，不包
      const s2 = I.parsePipe('x([1,2,3])');
      jsonEq(s2[0].options, { value: [1, 2, 3] });
    });
    it('多步管道：顺序、数量、| 空白容错', () => {
      const s = I.parsePipe(' a |  b |c ');
      eq(s.length, 3);
      jsonEq(s.map(x => x.action), ['a', 'b', 'c']);
      jsonEq(s.map(x => x.options), [{}, {}, {}]);
    });
    it('空步（||）被过滤', () => {
      eq(I.parsePipe('a||b').length, 2);
      eq(I.parsePipe('a|').length, 1);
    });
    it('非法 token 被整体过滤', () => {
      jsonEq(I.parsePipe('!!bad'), []);
      eq(I.parsePipe('ok|123abc').length, 1); // 123abc 不是合法动作名
    });
    it('空括号 → 空 options', () => {
      jsonEq(I.parsePipe('createapp()'), [{ action: 'createapp', options: {} }]);
    });
    it('括号内 JSON 非法 → 原样字符串包进 value（不抛异常）', () => {
      const s = I.parsePipe('x({bad json)');
      jsonEq(s[0].options, { value: '{bad json' });
    });
  });

  // ---------------- parsePipeArg ----------------
  describe('parsePipeArg 单参数解析', () => {
    it('空白 → 空字符串', () => eq(I.parsePipeArg('   '), ''));
    it('裸串原样返回', () => eq(I.parsePipeArg('abc'), 'abc'));
    it('引号串去引号', () => {
      eq(I.parsePipeArg("'hi'"), 'hi');
      eq(I.parsePipeArg('"hi"'), 'hi');
    });
    it('JSON 数字/对象', () => {
      eq(I.parsePipeArg('42'), 42);
      jsonEq(I.parsePipeArg('{"a":1}'), { a: 1 });
    });
  });

  // ---------------- applyTpl ----------------
  describe('applyTpl 占位符替换', () => {
    it('命中键替换', () => eq(I.applyTpl('/x/{{Id}}', { Id: 7 }), '/x/7'));
    it('未命中键保留原样字面量', () => eq(I.applyTpl('{{x}}', { y: 1 }), '{{x}}'));
    it('params 为 null 不抛且保留原样', () => eq(I.applyTpl('{{x}}', null), '{{x}}'));
    it('数字等原始类型透传', () => {
      eq(I.applyTpl(5, {}), 5);
      eq(I.applyTpl(true, {}), true);
    });
    it('递归替换对象/数组，且不污染入参', () => {
      const input = { url: '/a/{{k}}', arr: ['{{k}}', 2], nested: { keep: 1 } };
      const out = I.applyTpl(input, { k: 'Z' });
      eq(out.url, '/a/Z');
      eq(out.arr[0], 'Z');
      eq(input.url, '/a/{{k}}'); // 原对象未被修改
    });
  });

  // ---------------- resolveAction / defineAction ----------------
  describe('动作注册表', () => {
    it('内置动作存在', () => {
      ok(typeof I.resolveAction('calc') === 'function');
      ok(typeof I.resolveAction('toast') === 'function');
    });
    it('大小写不敏感查找', () => {
      ok(I.resolveAction('CALC') === I.resolveAction('calc'));
      ok(I.resolveAction('CreateApp') === I.resolveAction('createapp'));
    });
    it('未知动作 → null', () => nullable(I.resolveAction('不存在的动作xyz')));
    it('defineAction 注册自定义动作可解析', () => {
      I.defineAction('ut_marker', async () => 'ok');
      eq(typeof I.resolveAction('UT_MARKER'), 'function');
    });
  });

  // ---------------- calc 动作 ----------------
  describe('calc 安全表达式运算', () => {
    it('自增自减 *= /= 与默认步长 1', async () => {
      const calc = I.resolveAction('calc');
      const run = (model, expr) => calc({ model, options: { expr } });
      const m1 = { count: 2 };
      eq(await run(m1, 'count += 1'), 3);
      eq(m1.count, 3);
      const m2 = { count: 10 };
      eq(await run(m2, 'count -= 4'), 6);
      const m3 = { n: 3 };
      eq(await run(m3, 'n *= 2'), 6);
      const m4 = { n: 8 };
      eq(await run(m4, 'n /= 4'), 2);
      const m5 = { n: 8 };
      eq(await run(m5, 'n += '), 9); // 省略步长默认 1
    });
    it('除以 0 安全归零', async () => {
      const m = { n: 9 };
      eq(await I.resolveAction('calc')({ model: m, options: { expr: 'n /= 0' } }), 0);
    });
    it('二元运算 total = price * num（缺失字段按 0）', async () => {
      const calc = I.resolveAction('calc');
      const m = { price: 100, num: 2 };
      eq(await calc({ model: m, options: { expr: 'total = price * num' } }), 200);
      eq(m.total, 200);
      const m2 = { price: 5 };
      eq(await calc({ model: m2, options: { expr: 'total = price * num' } }), 0);
      const m3 = { a: 10, b: 3 };
      eq(await calc({ model: m3, options: { expr: 'd = a - b' } }), 7);
      const m4 = { a: 10, b: 3 };
      eq(await calc({ model: m4, options: { expr: 'd = a + b' } }), 13);
    });
    it('支持点路径 user.score += 5', async () => {
      const m = { user: { score: 1 } };
      eq(await I.resolveAction('calc')({ model: m, options: { expr: 'user.score += 5' } }), 6);
      eq(m.user.score, 6);
    });
    it('非法表达式 / 缺 model → null 且告警', async () => {
      const calc = I.resolveAction('calc');
      nullable(await calc({ model: { x: 1 }, options: { expr: 'drop table' } }));
      ok(lastMsg() && /不支持/.test(lastMsg().m), '应当提示表达式不支持');
      nullable(await calc({ options: { expr: 'x += 1' } }));
      ok(lastMsg() && /model/.test(lastMsg().m), '应当提示缺 model');
    });
  });

  // ---------------- dyn-core 已导出的路径工具 ----------------
  describe('getByPath / setPathVal / deepClone', () => {
    it('getByPath 点路径与空值安全', () => {
      eq(dyn.getByPath({ a: { b: 2 } }, 'a.b'), 2);
      eq(dyn.getByPath(null, 'a.b'), undefined);
      eq(dyn.getByPath({ a: {} }, 'a.b.c'), undefined);
    });
    it('setPathVal 自动补中间对象', () => {
      const o = {};
      dyn.setPathVal(o, 'a.b.c', 9);
      eq(o.a.b.c, 9);
    });
    it('deepClone 结果与源脱钩', () => {
      const src = { a: [1, { b: 2 }] };
      const cp = dyn.deepClone(src);
      cp.a[1].b = 99;
      eq(src.a[1].b, 2);
    });
  });

  // ---------------- buildCtx ----------------
  describe('buildCtx 行参数收集与占位替换', () => {
    it('data-* 行参数收进 params 并替换 options 占位', () => {
      const el = makeEl({ 'data-id': 'X7', 'data-dyn-mode': 'createApp' });
      const ctx = I.buildCtx(el, 'click', null, { url: '/x/{{id}}' }, 'open');
      eq(ctx.params.id, 'X7');
      eq(ctx.options.url, '/x/X7');
      eq(ctx.action, 'open');
      ok(!('dyn-mode' in ctx.params), 'data-dyn-* 不应被收进行参数');
    });
  });

  // ---------------- runPipe ----------------
  describe('runPipe 管道执行语义', () => {
    it('顺序执行并返回最后一步结果，$result 承接上一步', async () => {
      const log = [];
      I.defineAction('ut_a', async ctx => { log.push('a:' + (ctx.options.v || '')); return 10; });
      I.defineAction('ut_b', async ctx => { log.push('b'); return ctx.$result + 5; });
      const r = await I.runPipe(
        I.parsePipe(`ut_a({"v":"x"})|ut_b()`), makeEl({}), 'click', null);
      eq(r, 15);
      jsonEq(log, ['a:x', 'b']);
    });
    it('某步返回 false 中止后续', async () => {
      let called = false;
      I.defineAction('ut_stop', async () => false);
      I.defineAction('ut_after', async () => { called = true; });
      await I.runPipe(I.parsePipe('ut_stop|ut_after'), makeEl({}), 'click', null);
      eq(called, false);
    });
    it('未注册动作：告警但不中断管道', async () => {
      msgs.length = 0;
      let reached = false;
      I.defineAction('ut_reach', async () => { reached = true; });
      await I.runPipe(I.parsePipe('nope_xyz|ut_reach'), makeEl({}), 'click', null);
      ok(lastMsg() && /未注册/.test(lastMsg().m), '应当提示未注册');
      eq(reached, true);
    });
  });

  // ---------------- wrapCompositeAction ----------------
  describe('wrapCompositeAction 组合动作 $before/$onSuccess/$onFail/$after', () => {
    it('$before 返回 false → 本体不执行，整体 false', async () => {
      let body = 0;
      I.defineAction('ut_body1', async () => { body++; return 'r'; });
      I.defineAction('ut_no', async () => false);
      const wrapped = I.wrapCompositeAction(I.resolveAction('ut_body1'));
      const r = await wrapped({ options: { $before: [{ action: 'ut_no' }] }, element: makeEl({}) });
      eq(r, false);
      eq(body, 0);
    });
    it('成功 → $onSuccess 收到返回值；$after 必定执行', async () => {
      const seen = [];
      I.defineAction('ut_body2', async () => 'RR');
      I.defineAction('ut_succ', async ctx => { seen.push('succ:' + ctx.$result); });
      I.defineAction('ut_after2', async () => { seen.push('after'); });
      const wrapped = I.wrapCompositeAction(I.resolveAction('ut_body2'));
      const r = await wrapped({
        options: { $onSuccess: [{ action: 'ut_succ' }], $after: [{ action: 'ut_after2' }] },
        element: makeEl({})
      });
      eq(r, 'RR');
      jsonEq(seen, ['succ:RR', 'after']);
    });
    it('本体抛异常 → $onFail 与 $after 执行，异常被吞为 composite 语义', async () => {
      const seen = [];
      I.defineAction('ut_boom', async () => { throw new Error('boom'); });
      I.defineAction('ut_fail', async () => { seen.push('fail'); });
      I.defineAction('ut_after3', async () => { seen.push('after'); });
      const wrapped = I.wrapCompositeAction(I.resolveAction('ut_boom'));
      const origErr = console.error;
      console.error = () => {}; // 该用例【预期】本体抛错，composite 会 console.error 记录，压噪
      try {
        await wrapped({
          options: { $onFail: [{ action: 'ut_fail' }], $after: [{ action: 'ut_after3' }] },
          element: makeEl({})
        });
      } finally {
        console.error = origErr;
      }
      jsonEq(seen, ['fail', 'after']);
    });
  });

  // ---------------- bus ----------------
  describe('bus 事件总线', () => {
    it('on/emit/off', () => {
      let got = null;
      const off = dyn.busOn('ut_ev', p => { got = p; });
      dyn.busEmit('ut_ev', 42);
      eq(got, 42);
      off();
      dyn.busEmit('ut_ev', 43);
      eq(got, 42);
    });
  });

  // ---------------- DynKernel 显式内核 ----------------
  describe('DynKernel 阶段装配与契约', () => {
    const byName = {};
    kernel.status().forEach(p => { byName[p.name] = p.status; });

    it('已注册插件全部 done（core → actions 阶段链）', () => {
      eq(byName['core'], 'done');
      eq(byName['actions'], 'done');
    });
    it('actions 的 requires[dyn] 由 core 阶段 provide 满足（全局顺序无关）', () => {
      eq(typeof dyn.parsePipe, 'function');
      ok(dyn.parsePipe === I.parsePipe, 'dyn.parsePipe 必须就是内核导出的权威实现（同一引用）');
      ok(typeof dyn.parsePipeArg === 'function');
    });
    it('boot 幂等 + isBooted', async () => {
      const p1 = kernel.boot();
      const p2 = kernel.boot();
      ok(p1 === p2, '重复 boot 必须复用同一 Promise');
      await p1;
      eq(kernel.isBooted(), true);
    });
    it('缺强依赖的插件被 skip（不再是运行时散落报错）', () => {
      const origErr = console.error;
      console.error = () => {};
      try {
        kernel.register({ name: 'ut_need_ghost', stage: 'services', requires: ['ghost_xyz'], setup: () => {} });
      } finally { console.error = origErr; }
      const st = kernel.status().find(p => p.name === 'ut_need_ghost');
      eq(st.status, 'skipped');
    });
    it('boot 后注册的插件立即热装配（blocks/layouts 按需加载场景）', () => {
      let hotRan = 0;
      kernel.register({
        name: 'ut_hot', stage: 'blocks', requires: [],
        setup: () => { hotRan++; }
      });
      eq(hotRan, 1, '热注册插件应当场执行 setup');
      eq(kernel.status().find(p => p.name === 'ut_hot').status, 'done');
    });
    it('重复注册 / 非法参数 / 未知阶段被忽略', () => {
      kernel.register({ name: 'core', stage: 'core', setup: () => {} }); // 重名
      kernel.register(null);
      kernel.register({ name: 'ut_badstage', stage: 'nope', setup: () => {} });
      const names = kernel.status().map(p => p.name);
      eq(names.filter(n => n === 'core').length, 1);
      ok(!names.includes('ut_badstage'));
    });
    it('setup 抛错被隔离标记 failed，不影响内核与其他插件', () => {
      const origErr = console.error;
      console.error = () => {};
      try {
        kernel.register({ name: 'ut_boom_plugin', stage: 'debug', setup: () => { throw new Error('x'); } });
      } finally { console.error = origErr; }
      eq(kernel.status().find(p => p.name === 'ut_boom_plugin').status, 'failed');
      eq(kernel.isBooted(), true);
    });
    it('pending 兜底：kernel 加载前登记的插件被接管，boot 时正常装配', async () => {
      const sb2 = makeSandbox();
      vm.runInContext('window.__DYN_KERNEL_PENDING__ = [{name:"pre",stage:"debug",requires:[],setup:function(){window.__preRan=true;}}];', sb2);
      vm.runInContext(fs.readFileSync(path.join(path.join(__dirname, '..', '..', 'src', 'VueLibV4.Web', 'wwwroot', 'dyn'), 'dyn-kernel.js'), 'utf8'), sb2, { filename: 'dyn-kernel.js' });
      eq(sb2.__preRan, undefined, 'kernel 加载只接管登记，不应立即执行');
      eq(sb2.DynKernel.status()[0].name, 'pre');
      await vm.runInContext('DynKernel.boot()', sb2);
      eq(sb2.__preRan, true, 'boot 后 pending 插件 setup 被执行');
      eq(sb2.DynKernel.status()[0].status, 'done');
    });
  });

  // ---------- 执行 ----------
  for (const g of groups) {
    console.log('\n' + g.name);
    for (const t of g.tests) {
      total++;
      try {
        await t.fn();
        passed++;
        console.log('  \x1b[32m✓\x1b[0m ' + t.name);
      } catch (e) {
        failures.push({ group: g.name, name: t.name, err: e });
        console.log('  \x1b[31m✗\x1b[0m ' + t.name + '\n      ' + e.message);
      }
    }
  }

  console.log('\n────────────────────────────────');
  console.log(`断言用例：${passed}/${total} 通过`);
  if (failures.length) {
    console.log(`\x1b[31m失败 ${failures.length} 条\x1b[0m`);
    process.exitCode = 1;
  } else if (process.env.DYN_TEST_SELF_CHECK === '1') {
    console.log('\x1b[31m自检异常：故意失败用例竟然通过了\x1b[0m');
    process.exitCode = 1;
  } else {
    console.log('\x1b[32m全部通过\x1b[0m');
  }
}

run().catch(e => {
  console.error('harness 自身异常：', e);
  process.exit(2);
});
