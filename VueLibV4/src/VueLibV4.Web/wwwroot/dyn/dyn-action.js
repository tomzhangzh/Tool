/* dyn-action.js V4 动作系统；依赖 dyn-core.js，提供事件委托、动作链、bus事件总线、dyn-actions后端执行 */
/* 仅短语法：data-dyn-{event}-{action}="{}"；组合动作 $before/$onSuccess/$onFail/$after；postback 使用 axios UMD */
(function(global){
'use strict';
if(!global.dyn){
  console.error("[DynAction] 请先加载 dyn-core.js");
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
  ATTR_INIT_PREFIX: 'data-dyn-init-',
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
      const subCtx = dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
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
      // 失败执行 $onFail
      const failSteps = normActionSteps(o.$onFail);
      for(const step of failSteps){
        const fn = resolveAction(step.action);
        if(!fn) continue;
        const subCtx = dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
        await fn(subCtx);
      }
    }
    if(success){
      // 成功执行 $onSuccess
      const succSteps = normActionSteps(o.$onSuccess);
      for(const step of succSteps){
        const fn = resolveAction(step.action);
        if(!fn) continue;
        const subCtx = dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
        await fn(subCtx);
      }
    }
    // 后置动作链，无论成功失败
    const afterSteps = normActionSteps(o.$after);
    for(const step of afterSteps){
      const fn = resolveAction(step.action);
      if(!fn) continue;
      const subCtx = dyn.buildCtx(ctx.element,ctx.event,ctx.$event,step.options||{},step.action);
      await fn(subCtx);
    }
    return res;
  };
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
  if(c==='{'||c==='['){
    try{ return JSON.parse(t); }catch(e){
      console.warn("[parseActionOptions JSON解析失败 raw="+raw,e);
      return {};
    }
  }
  return {};
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
    if(!fn){ dyn.showMessage(CONST.MSG_ACTION_NOT_FOUND.replace('{name}',s.action),'warning'); continue; }
    const c = buildCtx(el,eventName||'pipe',$event,s.options||{},s.action);
    c.$step = i; c.$result = last;
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
 * <button data-dyn-click-toast data-dyn-action-ref="act1">按钮</button>
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
    $callStack:[],
    $abort:false
  };
}

// ---------------------- 内置动作定义 ----------------------
defineAction('createapp',async ctx=>{
  const o = ctx.options||{};
  const el = ctx.element;
  const url = o.url||el.getAttribute('data-dyn-url');
  if(url) el.setAttribute('data-dyn-url',url);
  el.setAttribute('data-dyn-mode','createApp');
  await dyn.mount(el);
});

defineAction('postback',async function(ctx){
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage(CONST.MSG_MISS_URL.replace('{action}','postback'),'error'); return; }
  if(o.confirm){
    const ok = await dyn.confirmAsync(o.confirm===true?'确定执行该操作吗？':o.confirm);
    if(!ok) return false;
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
    if(!window.axios) throw new Error("Axios未加载");
    const ajax = await window.axios({
      method:'POST',
      url:fullUrl,
      headers:{'Content-Type':'application/json'},
      data:body
    });
    const res = ajax.data;
    const failed = res.success===false||res.Success===false||(typeof res.code==='number'&&res.code!==0);
    if(failed){ dyn.showMessage(res.msg||res.Message||'操作失败','error'); throw new Error(res.msg||res.Message||'操作失败'); }
    //识别dyn-actions
    if(res&&Array.isArray(res[CONST.DATA_DYN_ACTIONS])){
      runJsonActions({actions:res[CONST.DATA_DYN_ACTIONS]},ctx.element);
    }
    if(o.reload) await dyn.reload(o.reload,{params:o.params});
    if(o.close) await dyn.close(ctx.element);
    if(o.message) dyn.showMessage(o.message,'success');
    return res;
  }catch(err){ dyn.showMessage('请求异常:'+err.message,'error'); throw err; }
});
defineAction('postdata',ctx=>_actions.postback(ctx));

defineAction('reload',ctx=>dyn.reload(ctx.options.selector||ctx.element,ctx.options));

/**
 * grid 表格动作（M4 三屏模板）：按 gridId 刷新注册到 window.DynGrids 的 DynTable
 *   dyn-click="grid('grid_student')"
 *   或配置式：{"gridId":"grid_student","page":1,"reset":true}  reset=true 先清空筛选区字段
 */
defineAction('grid',async ctx=>{
  const o = ctx.options||{};
  const id = o.gridId||o.target||(typeof o.value==='string'&&o.value?o.value:null);
  if(!id){ dyn.showMessage('[grid]缺少 gridId（如 grid(\'grid_xxx\')）','warning'); return false; }
  const g = (window.DynGrids||global.DynGrids) && (window.DynGrids||global.DynGrids)[id];
  if(!g){ console.warn('[grid]未找到已注册表格：'+id+'（检查 DynTable comoptions.gridId）'); return false; }
  if(o.reset && typeof g.resetFilter==='function') g.resetFilter();
  return g.refresh(o.page||1);
});

defineAction('load',async ctx=>{
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage("[load]缺少url",'error'); return; }
  const html = await dyn.fetchPartial(url,o.params||{},o.method||'POST');
  await dyn.render(ctx.element,html);
});

/**
 * updateEl 局部刷新（原则2核心）：
 *   <button dyn-click="ActionHelper.Submit" data-dyn-target="#resultPanel" data-url="/Home/QueryBlock">查询</button>
 * 提交当前 scope model → Controller 返回 HTML 片段替换目标区块并重新 mount；
 * 若返回 ApiResult JSON，则执行其中 dyn-actions，data 为片段字符串时同样替换区块。
 */
defineAction('updateel',async ctx=>{
  const o = ctx.options||{};
  const targetSel = o.target||o.selector
    ||(ctx.element?ctx.element.getAttribute('data-dyn-target'):null);
  if(!targetSel){ dyn.showMessage('[updateEl]缺少 target 选择器（如 data-dyn-target="#resultPanel"）','warning'); return false; }
  const targetEl = dyn.resolve(targetSel);
  if(!targetEl){ dyn.showMessage('[updateEl]未找到目标元素：'+targetSel,'warning'); return false; }
  const url = o.url
    ||(ctx.element&&ctx.element.getAttribute('data-dyn-url'))
    ||(ctx.element&&ctx.element.getAttribute('data-url'))
    ||ctx.url;
  if(!url){ dyn.showMessage('[updateEl]缺少url参数','error'); return false; }
  if(o.confirm){
    const ok = await dyn.confirmAsync(o.confirm===true?'确定执行该操作吗？':o.confirm);
    if(!ok) return false;
  }
  const body = Object.assign({},dyn.deepClone(ctx.model||{}),o.params||{});
  const text = await dyn.fetchPartial(url,body,o.method||'POST','text');
  let parsed = null;
  try{ parsed = JSON.parse(text); }catch(e){ parsed = null; }
  if(parsed&&typeof parsed==='object'){
    // ApiResult 失败（code 非 0）：明确提示，不执行 dyn-actions、不关闭弹窗
    if(parsed.code!==undefined&&parsed.code!==null&&Number(parsed.code)!==0){
      dyn.showMessage(parsed.msg||'操作失败','error');
      return parsed;
    }
    if(Array.isArray(parsed[CONST.DATA_DYN_ACTIONS])){
      runJsonActions({actions:parsed[CONST.DATA_DYN_ACTIONS]},targetEl);
    }
    if(typeof parsed.data==='string'&&parsed.data.indexOf('<')>=0){
      return await dyn.render(targetEl,parsed.data);
    }
    return parsed;
  }
  return await dyn.render(targetEl,text);
});
// 语义别名：Submit=提交并刷新区块；ReloadTarget=刷新指定区块
defineAction('submit',ctx=>_actions.updateel(ctx));
defineAction('reloadtarget',ctx=>_actions.updateel(ctx));

/**
 * open 弹窗（原则4：统一 layui layer）
 *   mode:'iframe'（或 openwindow）→ layer type:2 直接打开 MVC 页面
 *   默认 mode:'fragment'          → 拉 HTML 片段注入 layer，自动 dyn.mount（data-dyn-init 动作生效）
 *   onopen/onclose 动作钩子；reloadSelf 关闭后刷新触发容器
 */
defineAction('open',async ctx=>{
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage("[open]缺少url",'error'); return; }
  const triggerEl = ctx.element;
  const layer = await dyn.getLayer();
  const onEnd = async ()=>{
    await _runEvents(normActionSteps(o.onclose),Object.assign({},ctx,{}));
    if(o.reloadSelf){
      const host = dyn.findAncestor(triggerEl,'[data-dyn-url]')||triggerEl;
      await dyn.reload(host);
    }
  };
  if(layer){
    if(o.mode==='iframe'||o.iframe===true){
      const idx = layer.open({
        type:2,
        title:o.title||'对话框',
        area:[o.width||'60%',o.height||'80%'],
        shadeClose:!!o.shadeClose,
        content:url,
        end:()=>{ onEnd().catch(e=>console.error('[open onEnd]',e)); }
      });
      return { index:idx };
    }
    const holder = document.createElement('div');
    holder.className='dyn-modal-host dyn-layer-fragment';
    holder.style.padding='12px';
    // layui layer type:1 的 DOM 内容必须【已存在于文档中】：内部对 content 执行
    // wrap/show/移动定位，游离元素会导致弹层与内容都不进 DOM（且无任何报错）。
    // 先挂到 body 并隐藏，layer.open 会把它移入内容区并 .show()。
    holder.style.display='none';
    document.body.appendChild(holder);
    // layui layer 还会直接调用 content.parents()/data()，必须传其内置 jQuery 包装对象；
    // 传原生元素会抛 "d.parents is not a function"
    const holderContent = (global.layui && global.layui.$) ? global.layui.$(holder) : holder;
    const idx = layer.open({
      type:1,
      title:o.title||'对话框',
      area:[o.width||'60%',o.height||'80%'],
      shadeClose:!!o.shadeClose,
      content:holderContent,
      success:async ()=>{
        try{
          holder.innerHTML = '<div style="padding:16px;color:#909399;">加载中…</div>';
          const html = await dyn.fetchPartial(url,o.params||{},o.method||'GET');
          holder.innerHTML = html;
          // 片段约定：内部以 [data-dyn-init-createapp] 块作为 Vue 挂载根，配置数据块
          // （#detailCfgData 等）是它的兄弟节点。必须挂在这个内部块上——配置数据留在
          // 挂载根之外；若直接挂外层 holder，Vue 挂载清空容器内容时 data() 里
          // document.getElementById('detailCfgData') 会读到 null。
          const appRoot = holder.querySelector('[data-dyn-init-createapp]')
                       || holder.querySelector('[data-dyn-mode="createApp"]');
          if(appRoot){
            appRoot.removeAttribute('data-dyn-init-createapp');
            appRoot.setAttribute('data-dyn-mode','createApp');
            await dyn.mount(appRoot);
          }else{
            await dyn.mount(holder);
          }
          await _runEvents(normActionSteps(o.onopen),Object.assign({},ctx,{holder}));
        }catch(e){
          holder.innerHTML = '<div style="padding:16px;color:#f56c6c;">加载失败：'+(e.message||e)+'</div>';
        }
      },
      end:()=>{ try{dyn.unmount(holder);}catch(e){} try{holder.remove();}catch(e){} onEnd().catch(e=>console.error('[open onEnd]',e)); }
    });
    return { index:idx, holder };
  }
  // layer 不可用时降级为浏览器新窗口
  global.open(url,'_blank');
  return null;
});

/** openWindow：以 iframe 方式打开完整 MVC 页面（V1 同名动作） */
defineAction('openwindow',ctx=>_actions.open(Object.assign({},ctx,{options:Object.assign({mode:'iframe'},ctx.options||{})})));

/** close/closeWindow：关闭最近 layer；在 iframe 子页内调用则关闭父窗 layer */
defineAction('close',async ctx=>{
  // iframe 子页面关闭父窗弹窗
  try{
    if(window.parent&&window.parent!==window&&window.parent.layui&&window.parent.layui.layer){
      const pidx = new URLSearchParams(window.location.search).get('layerIndex');
      if(pidx!=null) window.parent.layui.layer.close(pidx);
      else window.parent.layui.layer.closeAll();
      return;
    }
  }catch(e){}
  const layer = await dyn.getLayer();
  if(!layer) return;
  const start = ctx.element;
  const layero = start&&start.closest?start.closest('.layui-layer'):null;
  if(layero){
    const times = layero.getAttribute('times');
    if(times!=null&&times!==''){ layer.close(times); return; }
  }
  layer.closeAll();
});
defineAction('closewindow',ctx=>_actions.close(ctx));

defineAction('toast',ctx=>{
  const o = ctx.options||{};
  const msg = o.message||o.msg||(o.value!==undefined?String(o.value):'');
  if(!msg) return;
  dyn.showMessage(msg,o.type||'success');
});
// V1 服务端动作名 showmessage 与 toast 等价
defineAction('showmessage',ctx=>_actions.toast(ctx));

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

/**
 * @description calc 安全表达式运算动作，不使用eval/new Function，仅支持简单赋值运算
 * 支持语法：
 *   count +=1 / count -=1 / count *=2 / count /=2
 *   total = price * num / sum = a + b / diff = a - b / avg = total / count
 * 变量名仅限model内部字段，支持点路径 user.score += 5
 * @param {DynActionCtx} ctx
 * @returns {Promise<number|null>}
 */
defineAction('calc',async ctx=>{
  const o = ctx.options||{};
  const expr = o.expr || '';
  if(!expr){
    dyn.showMessage('calc：expr表达式不能为空','warning');
    return null;
  }
  const model = ctx.model;
  if(!model){
    dyn.showMessage('calc：未获取到model(scope)','warning');
    return null;
  }
  const src = expr.replace(/\s+/g,' ').trim();
  // 自增自减支持空格和步长：count +=1 / count += 1 / count += 5
  const assignSelfRe = /^([\w.]+)\s*(\+=|-=|\*=|\/=)\s*([0-9.]+)?$/;
  const assignBinRe = /^([\w.]+)\s*=\s*([\w.]+)\s*(\+|-|\*|\/)\s*([0-9.]+|[\w.]+)$/;
  let match;
  let targetPath;
  let resultValue;
  if((match = src.match(assignSelfRe))){
    // 自增自减：count +=1 / count += 5
    targetPath = match[1];
    const op = match[2];
    const step = match[3]===undefined ? 1 : Number(match[3]);
    const cur = dyn.getByPath(model, targetPath) ?? 0;
    let num = Number(cur);
    switch(op){
      case '+=': num = num + step; break;
      case '-=': num = num - step; break;
      case '*=': num = num * step; break;
      case '/=': num = step===0?0 : num / step; break;
    }
    resultValue = num;
  }else if((match = src.match(assignBinRe))){
    // 二元运算：total = price * num
    targetPath = match[1];
    const leftPath = match[2];
    const operator = match[3];
    const rightRaw = match[4];
    const getVal = (p)=>{
      if(/^[0-9.]+$/.test(p)) return Number(p);
      return dyn.getByPath(model,p)??0;
    };
    const lv = getVal(leftPath);
    const rv = getVal(rightRaw);
    switch(operator){
      case '+': resultValue = lv + rv; break;
      case '-': resultValue = lv - rv; break;
      case '*': resultValue = lv * rv; break;
      case '/': resultValue = rv===0?0 : lv / rv; break;
      default: dyn.showMessage(`calc不支持运算符${operator}`,'warning'); return null;
    }
  }else{
    dyn.showMessage(`calc表达式语法不支持：${expr}\n支持示例：count +=1、total=price*num`,'warning');
    return null;
  }
  dyn.setPathVal(model, targetPath, resultValue);
  return resultValue;
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

/**
 * setDynCom：运行时修改某个 dyn 组件的 jsonconfig（响应式，视图立即刷新）
 *   options: { selector:'#com1', path:'options.comoptions.disabled', value:true }
 *   或 patch 整体合并：{ selector:'#com1', patch:{...} }
 */
defineAction('setdyncom',ctx=>{
  const o = ctx.options||{};
  const targetEl = o.selector?document.querySelector(o.selector):ctx.element;
  if(!targetEl){ dyn.showMessage('setdyncom未找到元素','warning'); return; }
  let inst = targetEl.__vueParentComponent||null;
  while(inst){
    const props = inst.props;
    if(props&&props.jsonconfig&&typeof props.jsonconfig==='object'){
      if(o.path) dyn.setPathVal(props.jsonconfig,o.path,o.value);
      else if(o.patch&&typeof o.patch==='object') Object.assign(props.jsonconfig,o.patch);
      return props.jsonconfig;
    }
    inst = inst.parent;
  }
  dyn.showMessage('setdyncom：目标不是 dyn 组件（缺少 jsonconfig）','warning');
});

/**
 * setWindow：向 layer iframe 弹窗内的页面投递数据（postMessage）；
 * 也支持在 iframe 子页面内向父窗口回传。子页面可监听 window 的 "message" 事件消费 {type:'dyn-setwindow'}
 */
defineAction('setwindow',ctx=>{
  const o = ctx.options||{};
  const payload = { type:'dyn-setwindow', path:o.path, value:o.value, data:o.data };
  const iframe = o.selector?document.querySelector(o.selector):document.querySelector('.layui-layer-iframe iframe');
  if(iframe&&iframe.contentWindow){ iframe.contentWindow.postMessage(payload,'*'); return true; }
  if(window.parent&&window.parent!==window){ window.parent.postMessage(payload,'*'); return true; }
  dyn.showMessage('setwindow：未找到可投递的窗口','warning');
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
  const actions = Array.isArray(res)?res:(res&&Array.isArray(res.actions)?res.actions:[]);
  if(!actions.length) return;
  actions.forEach(item=>{
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
      const wrappedFn = wrapCompositeAction(act);
      Promise.resolve(wrappedFn(c)).catch(e=>console.error("[runJsonActions]执行异常",e));
    }catch(err){ console.error("[runJsonActions]",err); }
  });
}

/**
 * @description 扫描dom执行 data-dyn-init-* 初始化动作
 * @param {HTMLElement} root
 */
function initActions(root){
  root = dyn.resolve(root)||document.body;
  if(!root) return;
  const list = [];
  if(root.nodeType===1) list.push(root);
  if(root.querySelectorAll) list.push(...[].slice.call(root.querySelectorAll('*')));
  list.forEach(el=>{
    if(el.__dynInitDone) return;
    const attrs = el.attributes?[].slice.call(el.attributes):[];
    attrs.forEach(a=>{
      if(a.name.indexOf(CONST.ATTR_INIT_PREFIX)===0){
        const actNameRaw = a.name.substring(CONST.ATTR_INIT_PREFIX.length);
        const optRaw = a.value;
        const options = parseActionOptions(optRaw);
        const fn = resolveAction(actNameRaw);
        if(!fn) return;
        // 执行后移除init属性，防止Vue挂载重建DOM后重复执行
        el.removeAttribute(a.name);
        const c = buildCtx(el, 'init', null, options, actNameRaw);
        const wrappedFn = wrapCompositeAction(fn);
        wrappedFn(c).catch(e=>console.error("[init-action异常]",e));
      }
    });
    // 字符串管道初始化：dyn-init="ActionHelper.A|ActionHelper.B"
    if(el.hasAttribute&&el.hasAttribute(CONST.PIPE_INIT_ATTR)){
      const steps = parsePipe(el.getAttribute(CONST.PIPE_INIT_ATTR)||'');
      el.removeAttribute(CONST.PIPE_INIT_ATTR);
      if(steps.length) runPipe(steps,el,'init',null).catch(e=>console.error("[init-pipe异常]",e));
    }
  });
}

/** @type {Record<string,object>} 数据库动作原始定义（Code → 行记录），供动作助手页面展示 */
const _dbActions = Object.create(null);

/**
 * 脚本类 DB 动作执行器。
 * 脚本内可用：ctx（页面上下文，含 options=本次参数）/ args（=ctx.options 快捷方式）
 *            action（动作定义行）/ api（showMessage/confirm/fetch 等工具）
 * 平台内置 reload 种子脚本即：ctx.reload();
 */
function runDbScript(row,args,ctx){
  const api = {
    showMessage:function(m,t){ return dyn.showMessage(m,t||'success'); },
    confirm:function(msg){ return dyn.confirmAsync ? dyn.confirmAsync(msg) : Promise.resolve(confirm(msg)); },
    toast:function(m,t){ return dyn.showMessage(m,t||'success'); },
    fetch:function(url,opt){ return fetch(url,opt).then(function(r){return r.json();}); },
    bus:_bus
  };
  // eslint-disable-next-line no-new-func
  const fn = new Function('ctx','args','action','api','return (function(){\n'+(row.Script||'')+'\n})();');
  return fn(ctx||{},args||{},row,api);
}

/** api 类 DB 动作：Script 为 URL 或 {"url","method","body"} JSON，返回 JSON 响应 */
async function runDbApi(row,args){
  let cfg = { url:row.Script||'', method:null, body:null };
  try{
    const parsed = JSON.parse(row.Script||'');
    if(parsed&&typeof parsed==='object') cfg = Object.assign(cfg,parsed);
  }catch(e){ /* 非 JSON 即纯 URL */ }
  const url = applyTpl(cfg.url,args)||'';
  if(!url) throw new Error('api 动作缺少 url');
  const opt = { method:(cfg.method||(cfg.body||args&&args.body?'POST':'GET')).toUpperCase(), headers:{} };
  const body = cfg.body!==undefined&&cfg.body!==null ? cfg.body : (args&&args.body);
  if(body!==undefined&&body!==null){ opt.headers['Content-Type']='application/json'; opt.body=typeof body==='string'?body:JSON.stringify(body); }
  const resp = await fetch(url,opt);
  const json = await resp.json().catch(()=>null);
  if(json&&(json.code!==undefined&&json.code!==0)) throw new Error(json.msg||'接口返回失败');
  return json;
}

/** chain 类 DB 动作：Script 为 [{action,options}] 或 {"steps":[...]} JSON，顺序执行管道 */
async function runDbChain(row,args,ctx){
  let steps = [];
  try{
    const parsed = JSON.parse(row.Script||'[]');
    steps = Array.isArray(parsed)?parsed:(Array.isArray(parsed.steps)?parsed.steps:[]);
  }catch(e){ throw new Error('chain 动作 Script 不是合法 JSON'); }
  // 步骤 options 支持 {{args.xxx}} 占位
  steps = steps.map(function(s){
    return { action:s.action, options:applyTpl(s.options||{},args) };
  });
  return runPipe(steps,(ctx&&ctx.element)||document.body,'chain',null);
}

/**
 * 注册一个数据库动作助手（同时登记原始行到 _dbActions，供页面展示/管理）。
 * 兼容两种入参：DB 行（Code/ActionType/Script）与 /script 生成器风格（code/actionType）。
 */
function registerDbAction(row){
  if(!row) return;
  const code = row.Code||row.code;
  if(!code) return;
  const norm = {
    Id:row.Id||row.id||0,
    Code:code,
    Name:row.Name||row.name||code,
    ActionType:row.ActionType||row.actionType||'script',
    Script:row.Script!==undefined?row.Script:(row.script||''),
    ParamsJson:row.ParamsJson||row.paramsJson||null
  };
  _dbActions[code]=norm;
  defineAction(code,async function(ctx){
    const args = (ctx&&ctx.options)||{};
    const type = (norm.ActionType||'script').toLowerCase();
    if(type==='url'){
      const url = norm.Script||args.url||'';
      if(url) global.location.href = applyTpl(url,args);
      return true;
    }
    if(type==='api') return runDbApi(norm,args,ctx);
    if(type==='chain') return runDbChain(norm,args,ctx);
    return runDbScript(norm,args,ctx);
  });
  // 在元信息上保留原始字段（动作助手页面直接读 Id/Name/Code/ActionType/ParmsJson）
  if(_actionMeta[code]) Object.assign(_actionMeta[code],norm);
}

/**
 * @description 从 /api/platform/dynactionhelper/all 拉取全部启用动作并动态注册
 * @returns {Promise<Array>} 动作行数组
 */
async function loadDbActionHelpers(){
  const resp = await fetch('/api/platform/dynactionhelper/all',{cache:'no-store'});
  const res = await resp.json().catch(()=>null);
  const rows = res&&res.code===0&&Array.isArray(res.data)?res.data:[];
  rows.forEach(registerDbAction);
  return rows;
}

let _delegationBound = false;
/**
 * @description 根据动作注册表生成事件委托选择器（data-dyn-{event}-{actionName}）
 * @returns {string}
 */
function generateSelector(){
  const parts = [];
  CONST.ACTION_EVENTS.forEach(ev=>{
    Object.keys(_actions).forEach(actName=>{
      parts.push('[data-dyn-'+ev+'-'+actName.toLowerCase()+']');
    });
  });
  return parts.join(',');
}

/**
 * @description 重新绑定事件委托；新增自定义动作后调用 dyn.rebindActions() 刷新选择器
 */
function rebindActions(){
  _delegationBound = false;
  bindDelegation();
}

/**
 * @description document捕获模式事件委托，处理 data-dyn-{event}-{action} 短语法事件
 */
function bindDelegation(){
  if(_delegationBound) return;
  _delegationBound = true;
  const doBind = ()=>{
    // 1) 短语法：data-dyn-{event}-{actionName}
    CONST.ACTION_EVENTS.forEach(ev=>{
      const prefix = 'data-dyn-'+ev+'-';
      document.addEventListener(ev,async e=>{
        const sel = generateSelector();
        if(!sel) return;
        const target = e.target&&e.target.closest?e.target.closest(sel):null;
        if(!target) return;
        // 从元素属性中解析出动作名：data-dyn-click-{actionName}
        let hitAttr = null;
        let actNameRaw = null;
        [].slice.call(target.attributes).forEach(a=>{
          if(!hitAttr&&a.name.indexOf(prefix)===0){
            hitAttr = a;
            actNameRaw = a.name.substring(prefix.length);
          }
        });
        if(!hitAttr||!actNameRaw) return;
        const optRaw = hitAttr.value;
        // 裸选择器值（如 data-dyn-click-updateel="#panel"）直接作为 target
        const options = parseActionOptions(optRaw);
        if(actNameRaw==='updateel'&&optRaw&&optRaw.trim()&&optRaw.trim().charAt(0)!=='{'){
          options.target = optRaw.trim();
        }
        const fn = resolveAction(actNameRaw);
        if(!fn){ dyn.showMessage(CONST.MSG_ACTION_NOT_FOUND.replace('{name}',actNameRaw),'warning'); return; }
        const ctx = buildCtx(target, ev, e, options, actNameRaw);
        const prevent = options.prevent!==false;
        if(prevent){ e.preventDefault(); e.stopPropagation(); }
        // 组合动作包装：$before/$onSuccess/$onFail/$after
        const wrappedFn = wrapCompositeAction(fn);
        try{
          await wrappedFn(ctx);
        }catch(err){
          console.error("[DynAction]动作执行异常",actNameRaw,err);
          dyn.showMessage("操作失败："+err.message,'error');
        }
      },true);
    });
    // 2) 字符串管道语法：dyn-click="ActionHelper.Submit|ActionHelper.Toast('保存成功')"
    Object.keys(CONST.PIPE_EVENT_ATTR).forEach(ev=>{
      const attrName = CONST.PIPE_EVENT_ATTR[ev];
      document.addEventListener(ev,async e=>{
        const target = e.target&&e.target.closest?e.target.closest('['+attrName+']'):null;
        if(!target) return;
        const steps = parsePipe(target.getAttribute(attrName)||'');
        if(!steps.length) return;
        const prevent = !steps[0].options || steps[0].options.prevent!==false;
        if(prevent){ e.preventDefault(); e.stopPropagation(); }
        try{
          await runPipe(steps,target,ev,e);
        }catch(err){
          console.error("[DynAction]管道执行异常",steps,err);
          dyn.showMessage("操作失败："+err.message,'error');
        }
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
  rebindActions,
  busOn:_bus.on, busOff:_bus.off, busEmit:_bus.emit, busClear:_bus.clear
};

if(typeof dyn.installActionApi === 'function'){
  dyn.installActionApi(api);
}else{
  console.error("[DynAction] dyn.installActionApi不存在，请检查dyn-core.js");
}
bindDelegation();

global.DynAction = {
  run(code,args,ctx){
    const fn = resolveAction(code);
    if(!fn) return Promise.reject(new Error("动作不存在:"+code));
    const fakeCtx = Object.assign({},ctx||{},{options:args||{}});
    return Promise.resolve(fn(fakeCtx));
  },
  /** 从数据库加载并注册全部启用的动作助手（幂等，可重复调用刷新） */
  loadAll:loadDbActionHelpers,
  /** 动态注册单个动作助手（与 /script 生成的 DynActionHelper.register 等价） */
  register:registerDbAction,
  /** 已注册的 DB 动作原始定义（Code → 行记录） */
  actions:_dbActions,
  bus:_bus
};
global.DynActionHelper = global.DynAction;

// 启动后自动拉取数据库动作，使任意页面 dyn-click="ActionHelper.Xxx" 管道可直接使用
loadDbActionHelpers().catch(function(e){ console.warn("[DynAction]加载数据库动作助手失败",e&&e.message); });
})(window);
