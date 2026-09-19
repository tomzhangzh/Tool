/* dyn-core.js V4 底层工具库，DOM/Vue实例管理，多App嵌套，scope共享作用域 */
/* 网络层改用 axios UMD 全局（window.axios），去除 jQuery 依赖 */
(function(global){
'use strict';
const Vue = global.Vue;
if(!Vue){ console.error("[DynCore] 请先引入Vue3 UMD"); return; }

/**
 * @typedef DynScopeModel
 * @property {boolean} __dynScopeRoot 标记scope根对象，禁止整体替换
 */

const CONST = {
  HOLDER_ID:'dyn-holder',
  ATTR_MODE:'data-dyn-mode',
  ATTR_URL:'data-dyn-url',
  ATTR_USE_SCOPE:'data-dyn-use-scope',
  ATTR_SHARED_SCOPE:'data-dyn-shared-scope',
  ATTR_SCOPE_JSON:'data-dyn-scope-json',
  SCRIPT_TAG_DYNMODEL:'dynmodel',
  SCRIPT_TYPE_JSON:'application/json'
};

const _uidSeq = 0;
const _appMap = typeof WeakMap!=='undefined'?new WeakMap():null;
/** scope共享作用域存储：DOM宿主元素 -> reactive对象 */
const _scopeStore = new WeakMap();

/**
 * @description 获取DOM元素，支持选择器/DOM对象
 * @param {string|HTMLElement|null} target
 * @returns {HTMLElement|null}
 * @demo dyn.resolve("#app")
 */
function resolve(target){
  if(!target) return null;
  if(typeof target === 'string') return document.querySelector(target);
  if(target.nodeType === 1) return target;
  return null;
}

/**
 * @description 向上查找祖先选择器
 * @param {HTMLElement} el
 * @param {string} selector
 * @returns {HTMLElement|null}
 */
function findAncestor(el,selector){
  el = resolve(el);
  if(!el) return null;
  if(el.closest) return el.closest(selector)||null;
  let cur = el.parentNode;
  while(cur&&cur.nodeType===1){
    if(cur.matches(selector)) return cur;
    cur = cur.parentNode;
  }
  return null;
}

/**
 * @description 向上查找最近 dyn-mode=createApp容器
 * @param {HTMLElement} el
 * @returns {HTMLElement|null}
 */
function closestDynInit(el){
  return findAncestor(el,'['+CONST.ATTR_MODE+'="createApp"]');
}

/**
 * @description 判断元素是否为createApp容器
 * @param {HTMLElement} el
 * @returns {boolean}
 */
function isCreateApp(el){
  return !!el && el.hasAttribute && el.hasAttribute(CONST.ATTR_MODE) && el.getAttribute(CONST.ATTR_MODE)==='createApp';
}

/**
 * @description 获取/创建共享scope响应式model，增加根对象保护，禁止整体覆盖引用
 * @param {HTMLElement} el
 * @returns {DynScopeModel|null}
 * @demo const scope = dyn.getScopeModel(el);
 */
function getScopeModel(el){
  const hostEl = findAncestor(el,'['+CONST.ATTR_SHARED_SCOPE+']');
  if(!hostEl) return null;
  if(_scopeStore.has(hostEl)) return _scopeStore.get(hostEl);

  let jsonStr = hostEl.getAttribute(CONST.ATTR_SCOPE_JSON)||'{}';
  let initData = {};
  try{ initData = JSON.parse(jsonStr); }catch(e){ console.warn("[DynCore] scope json解析异常",e); }
  const reactiveObj = Vue.reactive(initData);
  // 标记根对象，业务禁止直接替换整个对象，防止多App共享断裂
  Object.defineProperty(reactiveObj,'__dynScopeRoot',{writable:false,value:true});
  _scopeStore.set(hostEl,reactiveObj);
  return reactiveObj;
}

/**
 * @description 深拷贝JSON可序列化对象
 * @param {any} o
 * @returns {any}
 */
function deepClone(o){
  try{ return JSON.parse(JSON.stringify(o)); }catch(e){ return {}; }
}

/**
 * @description 解析元素上data-dyn-mode-model属性或者内嵌script标签model
 * @param {HTMLElement} el
 * @returns {object|null}
 */
function parseModel(el){
  const attr = el.getAttribute('data-dyn-mode-model');
  if(attr&&attr.trim()){
    const s = attr.trim();
    if(s.charAt(0)==='#'){
      const node = document.querySelector(s);
      if(node) try{ return JSON.parse(node.textContent); }catch(e){}
      return {};
    }
    try{ return JSON.parse(s); }catch(e){ console.error("[DynCore] model解析错误",attr); return {}; }
  }
  return null;
}

/**
 * @description 读取script[type="application/json"][tag="dynmodel"]内嵌model
 * @param {HTMLElement} el
 * @returns {object|null}
 */
function readModelScript(el){
  if(!el||!el.querySelector) return null;
  const s = el.querySelector('script[type="'+CONST.SCRIPT_TYPE_JSON+'"][tag="'+CONST.SCRIPT_TAG_DYNMODEL+'"]');
  if(s) try{ return JSON.parse(s.textContent); }catch(e){ console.error("[DynCore] dynmodel脚本解析失败",e); }
  return null;
}

/**
 * @description ajax获取html片段，使用全局axios UMD（window.axios）
 * @param {string} url
 * @param {object} params
 * @param {string} [type]
 * @param {string} [dataType] json|text|html
 * @returns {Promise<any>}
 */
async function fetchPartial(url,params,type,dataType){
  type = type||'POST';
  dataType = dataType||'html';
  if(!global.axios){
    console.error("[DynCore] 需要引入axios UMD，例如：<script src='https://cdn.jsdelivr.net/npm/axios@1.13.2/dist/axios.min.js'></script>");
    throw new Error("Axios未加载");
  }
  const opt = {
    url,
    method:type,
    timeout:30000,
    responseType: dataType==='json' ? 'json' : 'text'
  };
  if(type==='GET'){
    opt.params = params||{};
  }else{
    opt.data = params||{};
  }
  const resp = await global.axios(opt);
  return resp.data;
}

/**
 * @description 消息提示
 * @param {string} msg
 * @param {string} [type] success|error
 */
function showMessage(msg,type){
  if(!msg) return;
  try{
    if(global.ElementPlus&&global.ElementPlus.ElMessage){
      return type==='error'?global.ElementPlus.ElMessage.error(msg):global.ElementPlus.ElMessage.success(msg);
    }
  }catch(e){}
  console[(type==='error'?'error':'log')]('[DynCore]',msg);
}

/**
 * @description 确认弹窗
 * @param {string} msg
 * @returns {Promise<boolean>}
 */
function confirmAsync(msg){
  if(global.ElementPlus&&global.ElementPlus.ElMessageBox){
    return global.ElementPlus.ElMessageBox.confirm(msg,'提示',{type:'warning',confirmButtonText:'确定',cancelButtonText:'取消'})
    .then(()=>true).catch(()=>false);
  }
  return Promise.resolve(!!global.confirm(msg));
}

function storeApp(el,app){
  if(_appMap)_appMap.set(el,app);
  el.__dynApp = app;
}
function getClosestApp(el){
  const host = closestDynInit(el);
  if(!host) return null;
  const app = getAppByEl(host);
  // host dom存在，但是app仍然可能为null（dom标记已打上，mount还没完成）
  return app;
}
function getAppByEl(el){
  if(!el) return null;
  if(_appMap&&_appMap.has(el)) return _appMap.get(el);
  return el.__dynApp||null;
}

function removeApp(el){
  if(!el) return;
  if(_appMap)_appMap.delete(el);
  el.__dynApp = null;
}

function holderEl(){
  let h = document.getElementById(CONST.HOLDER_ID);
  if(!h){ h = document.createElement('div'); h.id=CONST.HOLDER_ID; h.style.display='none'; document.body.appendChild(h); }
  return h;
}

/**
 * @description 掩码嵌套dyn-init-createApp节点，mount时递归处理子App
 * @param {HTMLElement} el
 * @param {Array} out 输出子节点列表
 */
function maskNested(el,out){
  if(!el.querySelectorAll) return;
  const nested = [].slice.call(el.querySelectorAll('['+CONST.ATTR_MODE+'="createApp"]'));
  nested.forEach(child=>{
    if(child.__dynApp||child.__dynMounting) return;
    const uid = child.getAttribute('data-dyn-uid')||('dyn'+(++_uidSeq));
    child.setAttribute('data-dyn-uid',uid);
    const host = document.createElement('dyn-host');
    host.setAttribute('data-dyn-uid',uid);
    child.parentNode.insertBefore(host,child);
    holderEl().appendChild(child);
    out.push({child,host,uid});
  });
}

/**
 * @description 运行时依赖检查，只告警，不加载库
 * @returns {boolean}
 */
function checkRuntimeDeps(){
  const cfg = global.DYN_LIB_CONFIG||{};
  const warns = [];
  if(cfg.loadLodash&&!window._) warns.push("lodash未加载");
  if(cfg.loadElementPlus&&!window.ElementPlus) warns.push("ElementPlus未加载");
  if(cfg.loadLayui&&!window.layui) warns.push("layui未加载");
  if(!global.axios) warns.push("axios未加载");
  warns.forEach(m=>console.warn("[DynCore]依赖警告："+m));
  return warns.length===0;
}

/**
 * @description 内部mount核心逻辑
 * @param {HTMLElement} el
 * @returns {Promise<any>}
 */
function mountCore(el){
  checkRuntimeDeps();
  let cfgScript = null;
  const cfgDom = el.querySelector('script[tag="dynconfig"]');
  if(cfgDom){
    try{
      cfgScript = new Function('element','dyn',cfgDom.textContent+"\n; return typeof dynConfig!=='undefined'?dynConfig:null;")(el,global.dyn);
    }catch(e){ console.error("[DynCore] dynconfig执行异常",e); }
  }
  const srcModel = readModelScript(el)||parseModel(el)||{};
  // 优先绑定共享scope
  const bindScopeId = el.getAttribute(CONST.ATTR_USE_SCOPE);
  let useSharedModel = null;
  if(bindScopeId) useSharedModel = getScopeModel(el);
  const reactiveModel = useSharedModel ?? Vue.reactive(srcModel);
  // 移除容器内script标签，避免Vue模板解析干扰（原生DOM，无jQuery）
  [].slice.call(el.querySelectorAll('script')).forEach(s=>s.remove());
  const nested = [];
  maskNested(el,nested);

  const component = {
    template:el.innerHTML,
    data(){ return {}; },
    setup(){
      const exposed = { model:reactiveModel,element:el,dyn:global.dyn };
      if(cfgScript&&typeof cfgScript.setup==='function'){
        const extra = cfgScript.setup({model:reactiveModel,element:el})||{};
        Object.keys(extra).forEach(k=>{ if(k!=='model') exposed[k]=extra[k]; });
      }
      return exposed;
    }
  };
  if(cfgScript){
    ['data','computed','methods','watch','created','beforeMount','mounted','updated','beforeUnmount','unmounted'].forEach(k=>{
      if(cfgScript[k]) component[k]=cfgScript[k];
    });
    Object.keys(cfgScript).forEach(k=>{ if(!(k in component)&&k!=='setup'&&k!=='template') component[k]=cfgScript[k]; });
  }

  const app = Vue.createApp(component);
  ///////////////////////
  // 插件注册
  global.DynCom.setupApp(app);
  global.DynCom.ensureRegistered(app);
  /////////////////////
  // 全局属性注册
  app.config.globalProperties.$dyn = global.dyn;
  el.__dynApp = app;
  el.__dynModel = reactiveModel;
  app.__dynModel = reactiveModel;
  el.__dynLoaded = true;
  storeApp(el,app);
  try{ el.__dynProxy = app.mount(el)||null; }catch(e){ el.__dynProxy=null; throw e; }
  app.__dynProxy = el.__dynProxy;
  app.model = el.__dynProxy?el.__dynProxy.model:Vue.reactive(srcModel);

  nested.forEach(item=>{
    if(item.host&&item.host.parentNode) item.host.appendChild(item.child);
    mount(item.child);
  });
  // mount完成后（含ajax载入片段），自动扫描执行容器内 data-dyn-init-* 初始化动作
  if(global.dyn&&typeof dyn.initActions==='function'){
    dyn.initActions(el);
  }
  return app;
}

/**
 * @description 挂载dyn-mode=createApp容器，自动处理url远程加载
 * @param {string|HTMLElement} el
 * @returns {Promise<any>}
 * @demo dyn.mount("#container")
 */
function mount(el){
  return new Promise(promiseResolve=>{
    // 这里调用的是dyn的dom解析工具函数，不再和promise回调冲突
    el = dyn.resolve(el);
    if(!el) return promiseResolve(null);
    if(el.__dynApp) return promiseResolve(el.__dynApp);
    if(el.__dynMounting) return promiseResolve(null);

    el.__dynMounting = true;
    const url = el.getAttribute(CONST.ATTR_URL);
    const force = el.getAttribute('data-dyn-load') === 'true';
    const empty = el.childElementCount === 0;
    const needLoad = url && !el.__dynLoaded && (empty || force);

    if(needLoad){
      const m = parseModel(el)||{};
      fetchPartial(url,m,'POST').then(html=>{
        el.innerHTML = html;
        el.__dynMounting = false;
        try{
          const app = mountCore(el);
          promiseResolve(app);
        }catch(e){
          console.error("[DynCore] mount失败",e);
          promiseResolve(null);
        }
      }).catch(err=>{
        el.__dynMounting = false;
        el.innerHTML = '<div class="dyn-loading">加载失败：'+((err&&err.message)||err)+'</div>';
        promiseResolve(null);
      });
    }else{
      el.__dynMounting = false;
      try{
        const app = mountCore(el);
        promiseResolve(app);
      }catch(e){
        console.error("[DynCore] mount失败",e);
        promiseResolve(null);
      }
    }
  });
}


/**
 * @description 卸载App，递归卸载子嵌套App，清理内存
 * @param {string|HTMLElement} el
 */
function unmount(el){
  el = resolve(el);
  if(!el) return;
  if(el.querySelectorAll){
    [].slice.call(el.querySelectorAll('['+CONST.ATTR_MODE+'="createApp"]')).forEach(n=>unmount(n));
  }
  if(el.__dynApp){
    try{ el.__dynApp.unmount(); }catch(e){}
    el.__dynApp = null; el.__dynModel=null;
    removeApp(el);
  }
}

/**
 * @description 替换容器html，自动unmount旧实例，重新mount
 * @param {string|HTMLElement} el
 * @param {string} html
 * @returns {Promise<any>}
 */
function render(el,html){
  el = resolve(el);
  if(!el) return Promise.resolve(null);
  unmount(el);
  el.innerHTML = html||'';
  return mount(el);
}

function getProxy(el){
  el = resolve(el);
  if(!el) return null;
  if(el.__dynProxy) return el.__dynProxy;
  const app = getAppByEl(el);
  return app&&app._instance?app._instance.proxy:null;
}

function getModel(el){
  if(el&&!el.nodeType){
    if(el.__dynProxy&&el.__dynProxy.model) return el.__dynProxy.model;
    if(el.__dynModel) return el.__dynModel;
  }
  const p = getProxy(el);
  return p?p.model:null;
}

/**
 * @description 表单序列化，收集name表单字段（原生DOM，无jQuery）
 * @param {HTMLElement} root
 * @returns {object|null}
 */
function serializeForm(root){
  if(!root||!root.querySelectorAll) return null;
  const inputs = [].slice.call(root.querySelectorAll('input,select,textarea'));
  const o = {};
  inputs.forEach(input=>{
    const n = input.name||'';
    if(!n||/^dyn-|^data-|^v-/.test(n)) return;
    const t = input.type;
    if(t==='radio'){
      if(input.checked) o[n] = input.value;
      return;
    }
    if(t==='checkbox'){
      if(input.checked) o[n] = input.value;
      return;
    }
    o[n] = input.value;
  });
  return Object.keys(o).length>0 ? o : null;
}

/**
 * @description 收集参数：model + 表单 + data-*属性
 * @param {HTMLElement} targetEl
 * @param {object} extra
 * @returns {object}
 */
function collectParams(targetEl,extra){
  const params = {};
  const cfg = targetEl.__dynCfg||{};
  Object.assign(params,cfg.params||{});
  const inner = isCreateApp(targetEl)?targetEl:(targetEl.querySelector('['+CONST.ATTR_MODE+'="createApp"]')||null);
  const app = inner?getAppByEl(inner):null;
  if(app&&app._instance) Object.assign(params,deepClone(app._instance.proxy.model));
  else if(inner) Object.assign(params,parseModel(inner)||{});
  const fp = serializeForm(inner||targetEl);
  if(fp) Object.assign(params,fp);
  if(extra) Object.assign(params,extra);
  return params;
}

/**
 * @description 刷新dyn-init容器，识别后端返回dyn-actions字段自动执行动作
 * @param {HTMLElement|string} target
 * @param {object} opts
 * @returns {Promise<any>}
 */
async function reload(target,opts){
  opts = opts||{};
  if(typeof target === 'function'){ try{ return Promise.resolve(target()); }catch(e){ return Promise.resolve(null); } }
  if(typeof target === 'string'&&typeof window[target]==='function'){ try{ return Promise.resolve(window[target]()); }catch(e){ return Promise.resolve(null); } }
  const el = resolve(target);
  if(!el){
    if(typeof window.__dynRouteReload === 'function') return Promise.resolve(window.__dynRouteReload());
    return Promise.resolve(null);
  }
  let targetEl = null;
  if(isCreateApp(el)||el.hasAttribute(CONST.ATTR_URL)) targetEl = el;
  else targetEl = findAncestor(el,'['+CONST.ATTR_URL+']');
  if(!targetEl) return Promise.resolve(null);
  const cfg = targetEl.__dynCfg||{};
  const url = opts.url||cfg.url||targetEl.getAttribute(CONST.ATTR_URL);
  if(!url){ if(opts.url===undefined&&!cfg.url) return Promise.resolve(null); console.warn("[DynCore] reload缺少url"); return Promise.resolve(null); }
  const params = collectParams(targetEl,opts.params);
  return fetchPartial(url,params,opts.method||cfg.method||'POST','text').then(text=>{
    let data=null;
    try{ data=JSON.parse(text); }catch(e){}
    // 后端返回dyn-actions自动执行动作
    if(data && typeof data === 'object' && Array.isArray(data['dyn-actions']) && global.dyn && global.dyn.runJsonActions){
      global.dyn.runJsonActions({ actions: data['dyn-actions'] }, targetEl);
    }
    if(data&&typeof data==='object'){
      const app = getAppByEl(targetEl);
      if(app&&app._instance) Object.assign(app._instance.proxy.model,data);
      return mount(targetEl);
    }else{
      unmount(targetEl);
      targetEl.innerHTML = text;
      return mount(targetEl);
    }
  }).catch(err=>{ showMessage('刷新失败：'+((err&&err.message)||err),'error'); return null; });
}

function setDynCfg(el,cfg){
  el = resolve(el);
  if(!el) return;
  el.__dynCfg = Object.assign({},el.__dynCfg||{},cfg);
  if(cfg.url) el.setAttribute(CONST.ATTR_URL,cfg.url);
}

function getVueModel(el,targetEl){
  let src = null;
  if(targetEl) src = typeof targetEl==='string'?document.querySelector(targetEl):targetEl;
  if(!src) src = el;
  if(!src) return null;
  let host = null;
  if(isCreateApp(src)) host = src;
  else if(src.querySelector) host = src.querySelector('['+CONST.ATTR_MODE+'="createApp"]');
  if(!host) host = closestDynInit(src)||src;
  return getModel(host);
}

function getByPath(obj,path){
  if(!obj||!path) return undefined;
  return String(path).split('.').reduce((o,k)=>o==null?undefined:o[k],obj);
}

/**
 * @description 设置对象路径值，lodash _.set 优先，原生降级实现
 * @param {object} obj
 * @param {string} path
 * @param {any} value
 */
function setPathVal(obj,path,value){
  if(global._&&typeof global._.set==='function'){ global._.set(obj,path,value); return; }
  const keys = String(path).split('.');
  const last = keys.pop();
  let cur = obj;
  for(let i=0;i<keys.length;i++){
    const k = keys[i];
    if(!cur[k]||typeof cur[k]!=='object') cur[k]={};
    cur = cur[k];
  }
  cur[last] = value;
}

const dyn = {
  VERSION:"4.0.0",
  CFG:CONST,
  showMessage,confirmAsync,
  resolve,findAncestor,closestDynInit,
  mount,unmount,render,
  getApp:getAppByEl,getClosestApp,getProxy,getModel,getScopeModel,
  fetchPartial,serializeForm,collectParams,
  reload,setDynCfg,getVueModel,
  getByPath,setPathVal,deepClone
};
// ========= 新增下面这一行 =========
dyn.installActionApi = function(obj){
  Object.keys(obj).forEach(k=>{
    if(k !== 'i18n') dyn[k] = obj[k];
  });
};
global.dynCore = dyn;
global.dyn = dyn;
})(window);
