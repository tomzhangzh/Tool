/* dyn‑action.js V4 动作系统；依赖 dyn‑core.js，提供事件委托、动作链、bus事件总线、dyn‑actions后端执行 */
(function(global){
'use strict';
if(!global.dyn){
  console.error("[DynAction] 请先加载 dyn‑core.js");
  return;
}
const dyn = global.dyn;
const Vue = global.Vue;
const DYN_LIB_CONFIG = global.DYN_LIB_CONFIG||{};

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
 */

/**
 * @typedef ActionMeta
 * @property {string} name
 * @property {string} label
 * @property {string} doc
 * @property {string[]} events
 */

const CONST = {
  ATTR_ON_PREFIX: 'data-dyn-on-',
  ATTR_ACTION_REF: 'data-dyn-action-ref',
  ATTR_ACTION_CFG: 'data-dyn-action-cfg',
  ATTR_INIT: 'data-dyn-on-init',
  DATA_DYN_ACTIONS: 'dyn-actions',
  ACTION_EVENTS: ['click','dblclick','change','select'],
  MSG_ACTION_NOT_FOUND: '动作[{name}]未注册，请检查配置',
  MSG_MISS_URL: '[{action}]缺少url参数',
  MSG_CHAIN_NEED_STEPS: 'chain动作需要steps数组',
  MSG_CHAIN_DEAD_LOOP: '检测到chain动作死循环，终止执行',
  DEBUG_GLOBAL_KEY: 'DYN_DEBUG'
};

const _actions = {};
/** @type {Record<string,ActionMeta>} */
const _actionMeta = {};

/** 内置事件总线，替代旧dynCore.eventBus */
const _bus = {
  _listeners:Object.create(null),
  /**
   * @param {string} name
   * @param {Function} handler
   * @returns {Function} off取消函数
   */
  on(name,handler){
    if(!_bus._listeners[name]) _bus._listeners[name]=[];
    _bus._listeners[name].push(handler);
    return ()=>_bus.off(name,handler);
  },
  off(name,handler){
    if(!_bus._listeners[name]) return;
    const arr = _bus._listeners[name];
    const idx = arr.indexOf(handler);
    if(idx>=0) arr.splice(idx,1);
  },
  emit(name,payload){
    const arr = _bus._listeners[name]||[];
    arr.forEach(fn=>{ try{ fn(payload); }catch(e){ console.error("[bus emit error]",e); }});
  },
  clear(name){
    if(name) delete _bus._listeners[name];
    else _bus._listeners = Object.create(null);
  }
};

/**
 * @description 注册动作
 * @param {string} name
 * @param {Function} fn
 * @demo defineAction("toast",ctx=>{ dyn.showMessage(ctx.options.message); })
 */
function defineAction(name,fn){
  _actions[name]=fn;
  _actionMeta[name]={
    name,
    label:fn._label||name,
    doc:fn._doc||'',
    events:fn._events?fn._events.slice():CONST.ACTION_EVENTS.slice()
  };
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
 * @description 大小写不敏感查找动作
 * @param {string} name
 * @returns {Function|null}
 */
function resolveAction(name){
  if(_actions[name]) return _actions[name];
  const lower = name.toLowerCase();
  for(const k of Object.keys(_actions)){
    if(k.toLowerCase()===lower) return _actions[k];
  }
  return null;
}

/**
 * @description 解析原始配置字符串，容错JSON
 * @param {string} raw
 * @returns {object}
 */
function parseActionOptions(raw){
  if(!raw||!raw.trim()) return {};
  const t = raw.trim();
  const c = t.charAt(0);
  if(c==='{'||c==='[') try{ return JSON.parse(t); }catch(e){ return {selector:t}; }
  return {selector:t};
}

/**
 * @description 解析动作配置：支持DOM属性 + 隐藏配置块data‑dyn‑action‑cfg（支持vue挂载__dynObj对象）
 * @param {HTMLElement} el 触发元素
 * @param {string} attrRaw 属性上原始json字符串
 * @returns {object} 合并后配置，属性优先级高于隐藏块
 * @demo
 * <div style="display:none" data-dyn-action-cfg id="act1"></div>
 * <button data-dyn-on-click data-dyn-action-ref="act1">按钮</button>
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
  const sharedModel = dyn.getScopeModel(el);
  const localModel = dyn.getModel(dyn.closestDynInit(el));
  const finalModel = sharedModel ?? localModel;
  const app = dyn.getApp(el);
  return {
    element:el,el:el,
    event:eventName,$event:$event,targetInfo:$event,
    action:actionName,
    options: optCopy,
    params: optCopy.params,
    model:finalModel,
    vm:app&&app._instance?app._instance.proxy:null,
    url:optCopy.url||(el?el.getAttribute('data-dyn-url'):'')||'',
    $step:0,$result:null,$chain:[],$chainAction:'',
    $callStack:[]
  };
}

// ---------------------- 内置动作定义 ----------------------
defineAction('postback',async function(ctx){
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage(CONST.MSG_MISS_URL.replace('{action}','postback'),'error'); return; }
  if(o.confirm){
    const ok = await dyn.confirmAsync(o.confirm===true?'确定执行该操作吗？':o.confirm);
    if(!ok) return;
  }
  // useEventPayload 默认true
  const usePayload = o.useEventPayload!==false;
  let body;
  if(usePayload){
    body = { event:o.event||'', data:Object.assign({},ctx.model||{}) };
  }else{
    body = dyn.deepClone(ctx.model||{});
  }
  const qs = o.params?new URLSearchParams(o.params).toString():'';
  const fullUrl = qs?(url+(url.indexOf('?')>=0?'&':'?')+qs):url;
  try{
    const ajax = await fetch(fullUrl,{
      method:'POST',headers:{'Content-Type':'application/json'},
      body:JSON.stringify(body)
    });
    const res = await ajax.json();
    if(res.success===false||res.Success===false){ dyn.showMessage(res.Message||'操作失败','error'); return; }
    //识别dyn‑actions
    if(res&&Array.isArray(res[CONST.DATA_DYN_ACTIONS])){
      runJsonActions(res,ctx.element);
    }
    if(o.reload) await dyn.reload(o.reload,{params:o.params});
    if(o.close) await dyn.close(ctx.element);
    if(o.message) dyn.showMessage(o.message,'success');
    return res;
  }catch(err){ dyn.showMessage('请求异常:'+err.message,'error'); }
});
defineAction('postdata',ctx=>_actions.postback(ctx));

defineAction('reload',ctx=>dyn.reload(ctx.options.selector||ctx.element,ctx.options));

defineAction('load',async ctx=>{
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage("[load]缺少url",'error'); return; }
  const html = await dyn.fetchPartial(url,o.params||{},o.method||'POST');
  await dyn.render(ctx.element,html);
});

defineAction('open',async ctx=>{
  const o = ctx.options||{};
  if(!o.url){ dyn.showMessage("[open]缺少url",'error'); return; }
  const holder = document.createElement('div');
  holder.className='dyn‑modal‑host';
  holder.id='dyn‑modal‑'+Math.random().toString(36);
  document.body.appendChild(holder);
  const app = Vue.createApp({
    data(){ return { visible:true,title:o.title||'对话框',width:o.width||'60%',loading:true,html:'',err:''}; },
    template:`
<el-dialog v-model="visible" :title="title" :width="width" top="6vh" :close-on-click-modal="false" teleported="false" @closed="onClosed">
  <div v-if="loading">加载中...</div>
  <div v-else-if="err">{{err}}</div>
  <div v-else v-html="html"></div>
</el-dialog>`,
    methods:{
      async load(){
        try{
          this.html = await dyn.fetchPartial(o.url,o.params||{},o.method||'GET');
          this.loading = false;
          await dyn.mount(holder.querySelector('.el-dialog__body'));
          if(o.events&&o.events.onopen) await _runEvents(o.events.onopen,Object.assign({},ctx,{holder}));
        }catch(e){ this.err=e.message; this.loading=false; }
      },
      async onClosed(){
        if(o.events&&o.events.onclose) await _runEvents(o.events.onclose,Object.assign({},ctx,{holder}));
        dyn.unmount(holder); app.unmount(); holder.remove();
      }
    },
    mounted(){ this.load(); }
  });
  if(global.ElementPlus) app.use(global.ElementPlus);
  holder.__dynApp = app;
  app.mount(holder);
  return holder;
});

defineAction('close',ctx=>{
  const el = ctx.element;
  let host = null;
  let p = el;
  while(p){
    if(p.classList&&p.classList.contains('dyn‑modal‑host')){ host=p; break; }
    p = p.parentNode;
  }
  if(host&&host.__dynApp&&host.__dynApp._instance){
    host.__dynApp._instance.proxy.visible = false;
  }
});

defineAction('toast',ctx=>{
  const o = ctx.options||{};
  const msg = o.message||o.msg||'';
  if(!msg) return;
  dyn.showMessage(msg,o.type||'success');
});

defineAction('notify',ctx=>{
  const o = ctx.options||{};
  if(!global.ElementPlus||!global.ElementPlus.ElNotification) return dyn.showMessage(o.message);
  global.ElementPlus.ElNotification({
    title:o.title||'',message:o.message||'',type:o.type||'success',duration:Number(o.duration||3000)
  });
});

defineAction('setVueModel',async ctx=>{
  const o = ctx.options||{};
  const modelName = o.modelName||o.path;
  if(!modelName){
    dyn.showMessage('setVueModel：path/modelName不能为空','warning');
    return null;
  }
  const model = ctx.model;
  if(!model){
    dyn.showMessage('setVueModel：未获取到model(scope)','warning');
    return null;
  }
  const delay = Number(o.settimeout||o.delay||0);
  const val = o.value;
  let realVal = val;
  if(typeof realVal === 'string') realVal = applyTpl(realVal, ctx.params);
  try{
    if(delay>0){
      await new Promise(resolve=>setTimeout(resolve,delay));
    }
    dyn.setPathVal(model,modelName,realVal);
    return realVal;
  }catch(err){
    dyn.showMessage(`setVueModel执行异常:${err.message}`,'error');
    return null;
  }
});

defineAction('setvar',async ctx=>{
  const o = ctx.options||{};
  const modelName = o.modelName||o.path;
  if(!modelName){
    dyn.showMessage('setvar：path/modelName不能为空','warning');
    return null;
  }
  const model = ctx.model;
  if(!model){
    dyn.showMessage('setvar：未获取到model(scope)','warning');
    return null;
  }
  const delay = Number(o.delay || 0);
  let v = o.value;
  if(typeof v === 'string'){
    v = applyTpl(v, ctx.params);
  }
  try{
    if(delay > 0){
      await new Promise(resolve=>setTimeout(resolve,delay));
    }
    dyn.setPathVal(model, modelName, v);
    return v;
  }catch(err){
    dyn.showMessage(`setvar执行异常:${err.message}`,'error');
    return null;
  }
});

defineAction('evaljs',async ctx=>{
  if(DYN_LIB_CONFIG.disableEvalJs){ dyn.showMessage("evaljs已全局关闭",'warning'); return; }
  const code = ctx.options.js||ctx.options.code||ctx.options;
  if(!code) return;
  try{
    const fn = new Function('ctx','$bus','return ('+code+')');
    return fn(ctx,_bus);
  }catch(e){ dyn.showMessage("evaljs执行异常:"+e.message,'error'); }
});

defineAction('setattr',ctx=>{
  const o = ctx.options||{};
  const el = o.selector?document.querySelector(o.selector):ctx.element;
  if(!el){ dyn.showMessage("setattr未找到元素",'warning'); return; }
  if(o.attr) Object.keys(o.attr).forEach(k=>el.setAttribute(k,o.attr[k]));
  if(o.style) Object.keys(o.style).forEach(k=>el.style[k]=o.style[k]);
  if(o.text!==undefined) el.textContent = o.text;
  if(o.html!==undefined) el.innerHTML = o.html;
});

defineAction('copy',async ctx=>{
  const o = ctx.options||{};
  let text = o.text;
  if(text===undefined) text = ('value' in ctx.element)?ctx.element.value:ctx.element.textContent;
  text = String(text||'');
  try{
    await navigator.clipboard.writeText(text);
    dyn.showMessage("已复制："+text.slice(0,20)+(text.length>20?'…':''),'success');
  }catch(e){
    const ta = document.createElement('textarea');
    ta.value=text; ta.style.position='fixed'; ta.style.opacity='0';
    document.body.appendChild(ta); ta.select(); document.execCommand('copy');
    document.body.removeChild(ta);
    dyn.showMessage("已复制（降级）",'success');
  }
});

defineAction('download',ctx=>{
  const o = ctx.options||{};
  const url = o.url||(ctx.element?(ctx.element.href||ctx.element.getAttribute('data-url')):'');
  if(!url){ dyn.showMessage("download缺少url",'warning'); return; }
  const a = document.createElement('a');
  a.href=url; a.download=o.filename||o.name||'';
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
});

defineAction('confirm',async ctx=>{
  const o = ctx.options||{};
  return await dyn.confirmAsync(o.message||o.msg||'确定执行？');
});

defineAction('delay',async ctx=>{
  const ms = Number(ctx.options.ms??ctx.options.delay??300);
  return new Promise(resolve=>setTimeout(resolve,ms));
});

defineAction('chain',async ctx=>{
  let steps = ctx.options.steps;
  if(Array.isArray(ctx.options)) steps = ctx.options;
  if(!Array.isArray(steps)){ dyn.showMessage(CONST.MSG_CHAIN_NEED_STEPS,'warning'); return; }
  //防死循环检测
  if(ctx.$callStack.includes(ctx.action+JSON.stringify(steps))){
    dyn.showMessage(CONST.MSG_CHAIN_DEAD_LOOP,'error');
    return null;
  }
  ctx.$callStack.push(ctx.action+JSON.stringify(steps));
  let last;
  const run = async i=>{
    if(i>=steps.length) return last;
    const s = steps[i];
    if(!s) return run(i+1);
    const actName = typeof s==='string'?s:(s.action||'');
    const opt = typeof s==='object'&&s.options?s.options:(typeof s==='string'?{}:s);
    if(!actName){ dyn.showMessage("chain步骤缺少action",'warning'); return run(i+1); }
    const fn = resolveAction(actName);
    if(!fn){ dyn.showMessage(CONST.MSG_ACTION_NOT_FOUND.replace('{name}',actName),'error'); return run(i+1); }
    const subCtx = buildCtx(ctx.element,'chain',ctx.$event,opt,actName);
    subCtx.$step = i; subCtx.$result = last; subCtx.$chain = steps;
    subCtx.$callStack = [...ctx.$callStack];
    const r = await fn(subCtx);
    if(r===false) return last;
    last = r;
    return run(i+1);
  };
  return await run(0);
});

defineAction('switch',async ctx=>{
  const o = ctx.options||{};
  let value;
  const expr = o.expr||o.expression||'';
  if(expr.startsWith('model.')) value = dyn.getByPath(ctx.model,expr.substring(6));
  else if(expr.startsWith('params.')) value = ctx.params[expr.substring(7)];
  else if(expr) value = dyn.getByPath(ctx.model,expr);
  else value = o.value;
  let hit = null;
  const cases = o.cases||[];
  for(const c of cases){
    if(String(c.value)===String(value)){ hit = c; break; }
  }
  if(!hit&&o.default) hit = o.default;
  if(!hit||typeof hit!=='object') return null;
  if(hit.chain){
    const subCtx = buildCtx(ctx.element,'switch',ctx.$event,Array.isArray(hit.chain)?{steps:hit.chain}:hit.chain,'chain');
    return await _actions.chain(subCtx);
  }
  if(!hit.action) return null;
  const fn = resolveAction(hit.action);
  if(!fn){ dyn.showMessage("switch动作不存在:"+hit.action,'error'); return null; }
  const subCtx = buildCtx(ctx.element,'switch',ctx.$event,hit.options||{},hit.action);
  return await fn(subCtx);
});

defineAction('triggerevent',ctx=>{
  const o = ctx.options||{};
  const name = o.name||o.event;
  if(!name) return;
  const payload = o.payload||o.params||{};
  if(o.type==='window'||o.window){
    window.dispatchEvent(new CustomEvent(name,{detail:payload}));
  }else{
    _bus.emit(name,payload);
  }
});

defineAction('redirect',ctx=>{
  const o = ctx.options||{};
  const url = o.url||o.href;
  if(url) window.location.href = url;
});

/**
 * @description 执行一组动作链，用于open窗口onopen/onclose钩子
 * @param {Array} steps
 * @param {DynActionCtx} baseCtx
 * @returns {Promise<any>}
 */
function _runEvents(steps,baseCtx){
  if(!Array.isArray(steps)||!steps.length) return Promise.resolve(null);
  const fakeCtx = Object.assign({},baseCtx,{options:{steps}});
  return _actions.chain(fakeCtx);
}

/**
 * @description 后端返回actions数组批量执行动作
 * @param {{actions:Array}} res
 * @param {HTMLElement} rootEl
 */
function runJsonActions(res,rootEl){
  if(!res||!Array.isArray(res.actions)||!res.actions.length) return;
  res.actions.forEach(item=>{
    if(!item||typeof item!=='object') return;
    try{
      if(item.script){
        if(DYN_LIB_CONFIG.disableEvalJs){ console.warn("[runJsonActions] evaljs已关闭，跳过script"); return; }
        const fn = new Function('ctx','return ('+item.script+')');
        fn({element:rootEl});
        return;
      }
      const act = resolveAction(item.action);
      if(!act){ console.warn("[runJsonActions]动作不存在:"+item.action); return; }
      const c = buildCtx(rootEl,'jsonAction',null,item.options||{},item.action);
      Promise.resolve(act(c)).catch(e=>console.error("[runJsonActions]执行异常",e));
    }catch(err){ console.error("[runJsonActions]",err); }
  });
}

/**
 * @description 扫描dom执行data‑dyn‑on‑init初始化动作
 * @param {HTMLElement} root
 */
function initActions(root){
  root = dyn.resolve(root)||document.body;
  if(!root) return;
  const scanAttrs = [CONST.ATTR_INIT];
  scanAttrs.forEach(attrName=>{
    let arr = [];
    if(root.hasAttribute&&root.hasAttribute(attrName)) arr.push(root);
    if(root.querySelectorAll) arr = arr.concat([].slice.call(root.querySelectorAll('['+attrName+']')));
    arr.forEach(el=>{
      if(el.__dynInitDone) return;
      el.__dynInitDone = true;
      const raw = el.getAttribute(attrName);
      const opt = resolveActionConfig(el,raw);
      const actName = opt.action;
      if(!actName) return;
      const fn = resolveAction(actName);
      if(!fn) return;
      //修复：传入opt.options，不是顶层opt对象
      const c = buildCtx(el, 'init', null, opt.options || {}, actName);
      Promise.resolve(fn(c)).catch(e=>console.error("[init‑action异常]",e));
    });
  });
}

async function loadDbActionHelpers(){
  //可在此扩展从后端/api/platform/dynactionhelper/all加载数据库自定义动作
}

let _delegationBound = false;
/**
 * @description document捕获模式事件委托，处理data‑dyn‑on‑xxx事件
 */
function bindDelegation(){
  if(_delegationBound) return;
  _delegationBound = true;
  const doBind = ()=>{
    CONST.ACTION_EVENTS.forEach(ev=>{
      const sel = '['+CONST.ATTR_ON_PREFIX+ev+']';
      document.addEventListener(ev,e=>{
        const target = e.target&&e.target.closest?e.target.closest(sel):null;
        if(!target) return;
        const raw = target.getAttribute(CONST.ATTR_ON_PREFIX+ev);
        const opt = resolveActionConfig(target,raw);
        const actName = opt.action;
        if(!actName) return;
        const fn = resolveAction(actName);
        if(!fn){ dyn.showMessage(CONST.MSG_ACTION_NOT_FOUND.replace('{name}',actName),'warning'); return; }
        //修复：第四个参数传入 opt.options，不是顶层opt对象
        const ctx = buildCtx(target, ev, e, opt.options || {}, actName);
        const prevent = opt.prevent!==false;
        if(prevent){ e.preventDefault(); e.stopPropagation(); }
        Promise.resolve(fn(ctx)).catch(err=>{
          console.error("[DynAction]动作执行异常",actName,err);
          dyn.showMessage("操作失败："+err.message,'error');
        });
      },true);
    });
  };
  if(document.readyState==='loading') document.addEventListener('DOMContentLoaded',doBind);
  else setTimeout(doBind,0);
}

const api = {
  actionList(){ return Object.values(_actionMeta); },
  getMeta(name){ return _actionMeta[name]||null; },
  resolveAction,
  buildCtx,
  initActions,
  _runEvents,
  runJsonActions,
  loadDbActionHelpers,
  defineAction,
  busOn:_bus.on, busOff:_bus.off, busEmit:_bus.emit, busClear:_bus.clear
};

if(typeof dyn.installActionApi === 'function'){
  dyn.installActionApi(api);
}else{
  console.error("[DynAction] dyn.installActionApi不存在，请检查dyn‑core.js");
}
bindDelegation();

global.DynAction = {
  run(code,args,ctx){
    const fn = resolveAction(code);
    if(!fn) return Promise.reject(new Error("动作不存在:"+code));
    const fakeCtx = Object.assign({},ctx||{},{options:args||{}});
    return Promise.resolve(fn(fakeCtx));
  },
  bus:_bus
};
global.DynActionHelper = global.DynAction;
})(window);
