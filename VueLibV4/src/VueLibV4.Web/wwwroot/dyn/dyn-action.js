/* dyn-action.js V4 动作系统内核（动作系统唯一注册 DynKernel 插件的文件）
 *
 * 职责：动作注册表 / 管道词法解析 / ctx 构建 / 组合动作包装 / bus 事件总线 /
 *      参数占位解析 / 公开 api 装配 / 启动引导。
 * 依赖：dyn-core.js（运行期由插件 setup 注入 global.dyn）。
 *
 * 模块间共享：本文件在模块作用域创建唯一命名空间 global.DynActionKit（下称 K），
 *   dyn-action-builtins.js / dyn-action-db.js / dyn-action-docs.js / dyn-action-dom.js
 *   只允许向 K 追加属性，禁止整体重新赋值 global.DynActionKit。
 *   加载期 K.dyn 为 null（dyn-core 在自身 setup 才设置 global.dyn），
 *   运行期一律通过 K.dyn 取用，不依赖 setup 局部变量。
 */
(function(global){
'use strict';

/**
 * @typedef DynActionCtx
 * @property {HTMLElement} element
 * @property {Event|null} $event
 * @property {string} action
 * @property {Record<string,any>} options
 * @property {Record<string,any>} params
 * @property {Record<string,any>} model
 * @property {number} $step
 * @property {any} $result
 * @property {Array<string>} $callStack
 * @property {boolean} $abort
 */

/**
 * @typedef ActionMeta
 * @property {string} name
 * @property {string} label
 * @property {string} doc
 * @property {string[]} events
 */

const CONST = {
  ATTR_ACTION_REF: 'data-dyn-action-ref',
  ATTR_ACTION_CFG: 'data-dyn-action-cfg',
  DATA_DYN_ACTIONS: 'dyn-actions',
  ACTION_EVENTS: ['click','dblclick','change','select'],
  // 字符串管道语法：dyn-click="ActionHelper.Submit|ActionHelper.Toast('保存成功')"
  PIPE_EVENT_ATTR: { click:'dyn-click', dblclick:'dyn-dblclick', change:'dyn-change', select:'dyn-select' },
  PIPE_INIT_ATTR: 'dyn-init',
  MSG_ACTION_NOT_FOUND: '动作[{name}]未注册，请检查配置',
  MSG_MISS_URL: '[{action}]缺少url参数',
  MSG_CHAIN_NEED_STEPS: 'chain动作需要steps数组',
  MSG_CHAIN_DEAD_LOOP: '检测到chain动作死循环，终止执行',
  DEBUG_GLOBAL_KEY: 'DYN_DEBUG'
};

/**
 * @description 动作系统唯一共享命名空间，供各 dyn-action-*.js 模块追加使用。
 * dyn：运行期由插件 setup 赋值；config：运行期由插件 setup 赋值为 global.DYN_LIB_CONFIG。
 */
var K = global.DynActionKit = {
  CONST: CONST,
  actions: {},
  /** @type {Record<string,ActionMeta>} */
  meta: {},
  /** 内置事件总线，替代旧dynCore.eventBus */
  bus: {
    _listeners:Object.create(null),
    /**
     * @param {string} name
     * @param {Function} handler
     * @returns {Function} off取消函数
     */
    on(name,handler){
      if(!K.bus._listeners[name]) K.bus._listeners[name]=[];
      K.bus._listeners[name].push(handler);
      return ()=>K.bus.off(name,handler);
    },
    off(name,handler){
      if(!K.bus._listeners[name]) return;
      const arr = K.bus._listeners[name];
      const idx = arr.indexOf(handler);
      if(idx>=0) arr.splice(idx,1);
    },
    emit(name,payload){
      const arr = K.bus._listeners[name]||[];
      arr.forEach(fn=>{ try{ fn(payload); }catch(e){ console.error("[bus emit error]",e); }});
    },
    clear(name){
      if(name) delete K.bus._listeners[name];
      else K.bus._listeners = Object.create(null);
    }
  },
  dyn: null,
  config: {},
  // 内核导出给其它动作模块的公共函数（函数声明提升，加载期即可用）
  defineAction: defineAction,
  applyTpl: applyTpl,
  resolveCtxTokens: resolveCtxTokens,
  resolveAction: resolveAction,
  getMeta: getMeta,
  normActionSteps: normActionSteps,
  wrapCompositeAction: wrapCompositeAction,
  parsePipeArg: parsePipeArg,
  parsePipe: parsePipe,
  runPipe: runPipe,
  resolveActionConfig: resolveActionConfig,
  buildCtx: buildCtx,
  firstTruthy: firstTruthy,
  firstDefined: firstDefined,
  confirmIfNeeded: confirmIfNeeded,
  wrapDyndata: wrapDyndata,
  _runEvents: _runEvents
};

/**
 * @description 注册动作（注册名统一小写，返回值即规范后的 key）
 * @param {string} name
 * @param {Function} fn
 * @returns {string} 规范后的动作名
 * @demo defineAction("toast",ctx=>{ dyn.showMessage(ctx.options.message); })
 */
function defineAction(name,fn){
  const key = String(name||'').toLowerCase();
  K.actions[key]=fn;
  K.meta[key]={
    name:key,
    label:fn._label||String(name||''),
    doc:fn._doc||'',
    events:fn._events?fn._events.slice():CONST.ACTION_EVENTS.slice()
  };
  return key;
}

/**
 * @description 模板占位符{{params.xxx}}递归替换，返回新对象，不修改原始入参
 * @param {any} val
 * @param {Record<string,any>} params
 * @returns {any}
 */
function applyTpl(val,params){
  if(typeof val === 'string'){
    let guard = 0;
    let s = val;
    while(guard++<5&&/\{\{/.test(s)){
      s = s.replace(/\{\{(\w[\w.-]*)\}\}/g,(_,k)=>params&&params[k]!==undefined?params[k]:_);
    }
    return s;
  }
  if(val&&typeof val==='object'){
    // 浅拷贝副本，不污染原始传入对象
    const copy = Array.isArray(val) ? [...val] : Object.assign({}, val);
    Object.keys(copy).forEach(k=>{
      copy[k]=applyTpl(copy[k], params);
    });
    return copy;
  }
  return val;
}

/**
 * 统一参数占位解析：动作 options 里的 "$params.x" / "$model.x" / "$result.x" 整串 token，
 * 以及行内 "{{$params.x}}" 插值，按触发元素所属的 ParamContext 就近解析。
 * 必须在 ctx.$result 赋值【之后】调用（链路上一步结果可能已更新）；幂等。
 * 注意：只替换作者显式写出的 token，绝不把上下文全量灌入请求体。
 */
function resolveCtxTokens(ctx){
  try{
    if(!ctx||!ctx.options) return;
    const pctx = typeof K.dyn.params==='function' ? K.dyn.params(ctx.element) : null;
    if(pctx && typeof pctx.resolveValue==='function'){
      ctx.options = pctx.resolveValue(ctx.options, ctx.$result);
      ctx.params = ctx.options.params || ctx.params || {};
      if(ctx.options.url) ctx.url = ctx.options.url;
    }
  }catch(e){ console.warn('[DynAction] 参数占位解析异常',e); }
}

/**
 * @description 大小写不敏感查找动作（先精确，再忽略大小写）
 * @param {string} name
 * @returns {Function|null}
 */
function resolveAction(name){
  if(!name) return null;
  if(K.actions[name]) return K.actions[name];
  const lower = name.toLowerCase();
  if(K.actions[lower]) return K.actions[lower];
  for(const k of Object.keys(K.actions)){
    if(k.toLowerCase()===lower) return K.actions[k];
  }
  return null;
}

/**
 * @description 大小写不敏感查找动作元信息（先精确，再忽略大小写）
 * @param {string} name
 * @returns {ActionMeta|null}
 */
function getMeta(name){
  if(!name) return null;
  if(K.meta[name]) return K.meta[name];
  const lower = String(name).toLowerCase();
  if(K.meta[lower]) return K.meta[lower];
  for(const k of Object.keys(K.meta)){
    if(k.toLowerCase()===lower) return K.meta[k];
  }
  return null;
}

/**
 * @description 标准化为动作数组：对象自动包装成数组
 * @param {any} v
 * @returns {Array}
 */
function normActionSteps(v){
  if(!v) return [];
  if(Array.isArray(v)) return v;
  return [v];
}

/**
 * @description 组合动作包装器：自动处理 $before / $onSuccess / $onFail / $after 四个元字段
 * $before  本体动作执行之前运行，任意一步返回false则中止本体动作
 * $onSuccess 本体动作成功后运行
 * $onFail    本体动作抛异常后运行
 * $after     无论成功失败，最后一定执行
 * @param {Function} originalFn 原始动作函数
 * @returns {Function} 包装后的动作函数
 */
function wrapCompositeAction(originalFn){
  return async function(ctx){
    const o = ctx.options;
    // 执行前置动作链
    const beforeSteps = normActionSteps(o.$before);
    for(const step of beforeSteps){
      const fn = resolveAction(step.action);
      if(!fn) continue;
      const subCtx = K.dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
      const ret = await fn(subCtx);
      if(ret===false){ ctx.$abort=true; return false; }
    }
    if(ctx.$abort) return false;
    let res;
    let success = true;
    try{
      res = await originalFn(ctx);
    }catch(err){
      success = false;
      console.error("[composite action error]",err);
      // 失败执行 $onFail（$result=err，步骤可用 "$result.message" 占位）
      const failSteps = normActionSteps(o.$onFail);
      for(const step of failSteps){
        const fn = resolveAction(step.action);
        if(!fn) continue;
        const subCtx = K.dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
        subCtx.$result = err; resolveCtxTokens(subCtx);
        await fn(subCtx);
      }
    }
    if(success){
      // 成功执行 $onSuccess（$result=动作返回值，如保存接口返回的 newId："$result.data.Id"）
      const succSteps = normActionSteps(o.$onSuccess);
      for(const step of succSteps){
        const fn = resolveAction(step.action);
        if(!fn) continue;
        const subCtx = K.dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
        subCtx.$result = res; resolveCtxTokens(subCtx);
        await fn(subCtx);
      }
    }
    // 后置动作链，无论成功失败（$result=成功返回值）
    const afterSteps = normActionSteps(o.$after);
    for(const step of afterSteps){
      const fn = resolveAction(step.action);
      if(!fn) continue;
      const subCtx = K.dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
      subCtx.$result = success?res:undefined; resolveCtxTokens(subCtx);
      await fn(subCtx);
    }
    return res;
  };
}

/**
 * @description 解析管道动作的单个括号参数：'保存成功' / "x" / {"a":1} / 123 / true
 * @param {string} raw
 * @returns {any}
 */
function parsePipeArg(raw){
  const t = (raw||'').trim();
  if(t==='') return '';
  if((t.charAt(0)==='{'||t.charAt(0)==='[') || /^(true|false|null|-?\d+(\.\d+)?)$/.test(t)){
    try{ return JSON.parse(t); }catch(e){ return t; }
  }
  // 单/双引号字符串：'保存成功' 或 "保存成功"
  if((t.charAt(0)==="'"&&t.charAt(t.length-1)==="'")||(t.charAt(0)==='"'&&t.charAt(t.length-1)==='"')){
    return t.slice(1,-1);
  }
  return t;
}

/**
 * @description 解析动作管道字符串："ActionHelper.Submit|ActionHelper.Toast('保存成功')|Reload"
 * @param {string} expr
 * @returns {Array<{action:string,options:object}>}
 */
function parsePipe(expr){
  if(!expr||typeof expr!=='string') return [];
  return expr.split('|').map(s=>s.trim()).filter(Boolean).map(tok=>{
    const m = tok.match(/^([A-Za-z_$][\w$.]*)\s*(\(([\s\S]*)\))?\s*$/);
    if(!m) return null;
    const action = m[1].replace(/^ActionHelper\./i,'');
    let options = {};
    if(m[3]!==undefined && m[3].trim()!==''){
      const arg = parsePipeArg(m[3]);
      options = (arg&&typeof arg==='object'&&!Array.isArray(arg)) ? arg : { value:arg };
    }
    return { action, options };
  }).filter(Boolean);
}

/**
 * @description 按序执行动作管道，某步返回 false 中止；返回最后一步结果
 * @param {Array<{action:string,options:object}>} steps
 * @param {HTMLElement} el
 * @param {string} eventName
 * @param {Event} $event
 * @returns {Promise<any>}
 */
async function runPipe(steps,el,eventName,$event){
  let last = null;
  for(let i=0;i<steps.length;i++){
    const s = steps[i];
    const fn = resolveAction(s.action);
    if(!fn){ K.dyn.showMessage(CONST.MSG_ACTION_NOT_FOUND.replace('{name}',s.action),'warning'); continue; }
    const c = buildCtx(el,eventName||'pipe',$event,s.options||{},s.action);
    c.$step = i; c.$result = last;
    resolveCtxTokens(c); // $result 已就绪，重解析 "$result.xxx" 占位
    const r = await wrapCompositeAction(fn)(c);
    if(r===false) break;
    last = r;
  }
  return last;
}

/**
 * @description 解析动作配置：支持DOM属性 + 隐藏配置块data-dyn-action-cfg（支持vue挂载__dynObj对象）
 * @param {HTMLElement} el 触发元素
 * @param {string} attrRaw 属性上原始json字符串
 * @returns {object} 合并后配置，属性优先级高于隐藏块
 * @demo
 * <div style="display:none" data-dyn-action-cfg id="act1"></div>
 * <button dyn-click="Toast" data-dyn-action-ref="act1">按钮</button>
 */
function resolveActionConfig(el, attrRaw){
  let baseCfg = {};
  const refId = el.getAttribute(CONST.ATTR_ACTION_REF);
  if(refId){
    const cfgDom = document.getElementById(refId);
    if(cfgDom && cfgDom.hasAttribute(CONST.ATTR_ACTION_CFG)){
      //优先读取vue挂载到dom对象属性__dynObj，避开attribute只能字符串限制
      if(cfgDom.__dynObj && typeof cfgDom.__dynObj === 'object'){
        baseCfg = cfgDom.__dynObj;
      }else{
        try{
          const text = (cfgDom.textContent||'').trim();
          if(text) baseCfg = JSON.parse(text);
        }catch(err){ console.warn('[resolveActionConfig]隐藏配置块JSON解析失败',err); }
      }
    }
  }
  //元素自身属性JSON，优先级更高覆盖隐藏块
  if(attrRaw && attrRaw.trim()){
    let attrCfg = {};
    try{ attrCfg = JSON.parse(attrRaw); }catch(e){ /*解析错误忽略*/ }
    baseCfg = Object.assign({},baseCfg,attrCfg);
  }
  return baseCfg;
}

/**
 * @description 构建动作执行上下文ctx，优先scope共享model
 * @param {HTMLElement} el
 * @param {string} eventName
 * @param {Event|null} $event
 * @param {object} options 【传入的是动作options子对象，不是顶层{action,options}】
 * @param {string} actionName
 * @returns {DynActionCtx}
 */
function buildCtx(el,eventName,$event,options,actionName){
  const params = {};
  if(el&&el.attributes){
    [].forEach.call(el.attributes,a=>{
      if(a.name.indexOf('data-')===0&&!a.name.startsWith('data-dyn-')){
        params[a.name.substring(5)] = a.value;
      }
    });
  }
  const optCopy = applyTpl(options,params);
  optCopy.params = Object.assign({},params,optCopy.params||{});
  // 记录 params 中来自元素 data-* 属性的键（如 data-v-app/data-trae-ref），
  // 供 setparams 等动作区分"用户显式声明"与"buildCtx 自动收集"，避免污染
  optCopy.__elParams = params;
  const sharedModel = K.dyn.getScopeModel(el);
  const localModel = K.dyn.getModel(K.dyn.closestDynInit(el));
  const finalModel = sharedModel ?? localModel;
  // vm 必须指向触发元素所在 createapp 容器（el 自身无 __dynApp），否则 ctx.vm 恒为 null，
  // 弹窗 onclose / postback $onSuccess 里的 evaljs（ctx.vm.onQuery()）会抛 "Cannot read properties of null"
  const app = (typeof K.dyn.getClosestApp === 'function' && K.dyn.getClosestApp(el)) || K.dyn.getApp(el);
  // 该 Vue 版本 app._instance 为 null，但 dyn-core 挂载后已把 proxy 写入 app.__dynProxy / el.__dynProxy
  const appVm = app && (app.__dynProxy || (app._instance && app._instance.proxy)) || null;
  const ctx = {
    element:el,el:el,
    event:eventName,$event:$event,targetInfo:$event,
    action:actionName,
    options: optCopy,
    params: optCopy.params,
    model:finalModel,
    vm:appVm,
    url:optCopy.url||(el?el.getAttribute('data-dyn-url'):'')||'',
    $step:0,$result:null,$chain:[],$chainAction:'',
    $callStack:[],
    $abort:false
  };
  // 统一参数占位解析（$params./$model.；$result 在链路赋值后由调用方再解析一次）
  resolveCtxTokens(ctx);
  return ctx;
}

/**
 * @description 按顺序返回第一个真值，等价于原来的 o.a||o.b||... 链
 * @param {object} o
 * @param {string[]} keys
 * @returns {any}
 */
function firstTruthy(o, keys){
  if(!o) return undefined;
  for(const k of keys){ if(o[k]) return o[k]; }
  return undefined;
}

/**
 * @description 按顺序返回第一个 != null 的值，等价于原来的 a ?? b ?? dft 链
 * @param {object} o
 * @param {string[]} keys
 * @param {any} dft
 * @returns {any}
 */
function firstDefined(o, keys, dft){
  if(o){
    for(const k of keys){ if(o[k]!==undefined&&o[k]!==null) return o[k]; }
  }
  return dft;
}

/**
 * @description 需要时弹确认框：o.confirm===true 用默认提示语，否则用 o.confirm 文本
 * @param {object} o 动作 options
 * @param {string} defaultMsg 默认提示语
 * @returns {Promise<boolean>} 未要求确认时恒为 true
 */
async function confirmIfNeeded(o, defaultMsg){
  if(!o||!o.confirm) return true;
  const ok = await K.dyn.confirmAsync(o.confirm===true?defaultMsg:o.confirm);
  return !!ok;
}

/**
 * @description 把扁平 body 包成 dyndata 统一契约 {table,gridId?,project?,dataField?}
 * @param {any} body
 * @param {object} wrap 必须含 table
 * @returns {object}
 */
function wrapDyndata(body, wrap){
  const wrapped = { table:wrap.table };
  if(wrap.gridId) wrapped.gridId = wrap.gridId;
  if(wrap.project) wrapped.project = wrap.project;
  wrapped[wrap.dataField||'data'] = body;
  return wrapped;
}

/**
 * @description 执行一组动作链，用于open窗口onopen/onclose钩子
 * @param {Array} steps
 * @param {DynActionCtx} baseCtx
 * @returns {Promise<any>}
 */
function _runEvents(steps,baseCtx){
  if(!Array.isArray(steps)||!steps.length) return Promise.resolve(null);
  const fakeCtx = Object.assign({},baseCtx,{options:{steps}});
  return K.actions.chain(fakeCtx);
}

// ---------------------- 插件壳（全动作系统唯一注册 DynKernel 的地方） ----------------------
var __plugin = {
  name:'actions', stage:'actions', requires:['dyn'],
  setup:function(ctx){
    if(!global.dyn){
      console.error("[DynAction] 请先加载 dyn-core.js");
      return;
    }
    K.dyn = global.dyn;
    K.config = global.DYN_LIB_CONFIG||{};

    const api = {
      /* 管道词法的权威实现：dyn-debug / lint 等所有消费方统一走这里，禁止另抄解析器 */
      parsePipe, parsePipeArg,
      /** 动作清单（内置 + 数据库），按分组/名称排序；字段含 label/doc/options/example/demo */
      actionList(){
        const order = ['数据与刷新','窗口与弹层','表单与状态','提示与交互','控制流','其他'];
        const g = n => { const i = order.indexOf(n || '其他'); return i < 0 ? order.length - 1 : i; };
        return Object.values(K.meta)
          .filter(m => m && m.name)
          .sort((a, b) => (g(a.group) - g(b.group)) || String(a.name).localeCompare(String(b.name)));
      },
      getMeta,
      resolveAction,
      buildCtx,
      initActions: K.initActions,
      _runEvents,
      runJsonActions: K.runJsonActions,
      loadDbActionHelpers: K.loadDbActionHelpers,
      defineAction,
      rebindActions: K.rebindActions,
      busOn:K.bus.on, busOff:K.bus.off, busEmit:K.bus.emit, busClear:K.bus.clear
    };

    if(typeof K.dyn.installActionApi === 'function'){
      K.dyn.installActionApi(api);
    }else{
      console.error("[DynAction] dyn.installActionApi不存在，请检查dyn-core.js");
    }

    /* 测试钩子：仅当宿主预置了 window.__DYN_TEST__（node 测试 harness）时导出内部纯函数；
       浏览器正常加载时该对象不存在，行为零变化。 */
    if(global.__DYN_TEST__){
      global.__DYN_TEST__.exports = {
        parsePipe, parsePipeArg, applyTpl, resolveCtxTokens,
        normActionSteps, runPipe, wrapCompositeAction, resolveAction,
        buildCtx, defineAction
      };
    }

    K.bindDelegation();

    global.DynAction = {
      run(code,args,ctx){
        const fn = resolveAction(code);
        if(!fn) return Promise.reject(new Error("动作不存在:"+code));
        const fakeCtx = Object.assign({},ctx||{},{options:args||{}});
        return Promise.resolve(fn(fakeCtx));
      },
      /** 从数据库加载并注册全部启用的动作助手（幂等，可重复调用刷新） */
      loadAll:K.loadDbActionHelpers,
      /** 动作清单（内置 + 数据库），含 label/doc/options/example/demo，供 Demo 与设计器下拉使用 */
      actionList:function(){ return api.actionList(); },
      /** 单个动作的元信息 */
      getMeta:function(name){ return api.getMeta(name); },
      /** 动态注册单个动作助手（与 /script 生成的 DynActionHelper.register 等价） */
      register:K.registerDbAction,
      /** 已注册的 DB 动作原始定义（Code → 行记录） */
      actions:K.actions,
      bus:K.bus
    };
    global.DynActionHelper = global.DynAction;

    // 启动后自动拉取数据库动作，使任意页面 dyn-click="ActionHelper.Xxx" 管道可直接使用
    K.loadDbActionHelpers().catch(function(e){ console.warn("[DynAction]加载数据库动作助手失败",e&&e.message); });
  }
};
if(global.DynKernel) global.DynKernel.register(__plugin);
else (global.__DYN_KERNEL_PENDING__=global.__DYN_KERNEL_PENDING__||[]).push(__plugin);
})(window);
