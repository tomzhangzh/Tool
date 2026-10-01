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

let _uidSeq = 0;
const _appMap = typeof WeakMap!=='undefined'?new WeakMap():null;
/** scope共享作用域存储：DOM宿主元素 -> reactive对象 */
const _scopeStore = new WeakMap();
/** 不安全注入告警去重：元素 -> true */
const _injectWarned = typeof WeakSet!=='undefined'?new WeakSet():null;

/**
 * 官方注入点组件：updateEl/片段注入应落在 <dyn-inject-host> 内。
 * 只渲染一个稳定空容器，Vue 不会 diff 其命令式注入的子节点，
 * 外层 App 任何重渲染都不会抹掉注入内容（修复"往运行中 App 管辖 DOM 注入"的存活问题）。
 * @example <dyn-inject-host id="paneChild" name="childList"></dyn-inject-host>
 */
const DynInjectHost = {
  name:'DynInjectHost',
  props:{ name:{type:String,default:''}, tag:{type:String,default:'div'} },
  render(){
    return Vue.h(this.tag||'div',{class:'dyn-inject-host','data-dyn-inject':this.name||undefined});
  }
};

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
  // 注意：必须从父节点开始找。createapp 动作在调 mount 前已给自身打上 mode 标记，
  // 用 el.closest 会把自己误判为父 App，导致父子台账登记不上。
  let cur = el && el.parentNode;
  while(cur && cur.nodeType===1){
    if(cur.getAttribute && cur.getAttribute(CONST.ATTR_MODE)==='createApp') return cur;
    cur = cur.parentNode;
  }
  return null;
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
 * @description 读取【直接子级】script[type="application/json"][tag="dynmodel"]内嵌model。
 * 只认直接子节点：嵌套App自带的dynmodel不能污染外层App（多App/嵌套App场景）。
 * @param {HTMLElement} el
 * @returns {object|null}
 */
function readModelScript(el){
  if(!el||!el.querySelector) return null;
  const s = el.querySelector(':scope > script[type="'+CONST.SCRIPT_TYPE_JSON+'"][tag="'+CONST.SCRIPT_TAG_DYNMODEL+'"]');
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
    responseType: dataType==='json' ? 'json' : 'text',
    // 显式带 AJAX 标识头：使 _ViewStart/_Layout 的 X-Requested-With 判断走 _AjaxLayout，
    // 返回局部片段而非完整页面（不依赖 axios.defaults 全局配置是否已生效）
    headers:{ 'X-Requested-With':'XMLHttpRequest' }
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
 * @description 获取 layui layer 对象（原则4：弹窗统一使用 layui layer）；不可用时返回 null
 * @returns {Promise<object|null>}
 */
function getLayer(){
  return new Promise(resolve=>{
    try{
      if(global.layui){
        if(global.layui.layer) return resolve(global.layui.layer);
        if(typeof global.layui.use==='function'){
          global.layui.use(['layer'],()=>resolve(global.layui.layer||null));
          return;
        }
      }
    }catch(e){}
    resolve(null);
  });
}

/**
 * @description 消息提示（优先 layui layer.msg，降级 ElementPlus ElMessage）
 * @param {string} msg
 * @param {string} [type] success|error|warning
 */
function showMessage(msg,type){
  if(!msg) return;
  getLayer().then(layer=>{
    if(layer){
      const icon = type==='error'?2:(type==='warning'?0:1);
      layer.msg(String(msg),{icon:icon,time:2200});
      return;
    }
    try{
      if(global.ElementPlus&&global.ElementPlus.ElMessage){
        if(type==='error') return global.ElementPlus.ElMessage.error(msg);
        if(type==='warning') return global.ElementPlus.ElMessage.warning(msg);
        return global.ElementPlus.ElMessage.success(msg);
      }
    }catch(e){}
    console[(type==='error'?'error':'log')]('[DynCore]',msg);
  });
}

/**
 * @description 确认弹窗（优先 layui layer.confirm，降级 ElementPlus/native）
 * @param {string} msg
 * @returns {Promise<boolean>}
 */
function confirmAsync(msg){
  return getLayer().then(layer=>new Promise(resolve=>{
    if(layer){
      layer.confirm(String(msg||'确定执行？'),{icon:3,title:'提示'},function(idx){
        layer.close(idx); resolve(true);
      },function(){ resolve(false); });
      return;
    }
    if(global.ElementPlus&&global.ElementPlus.ElMessageBox){
      global.ElementPlus.ElMessageBox.confirm(msg,'提示',{type:'warning',confirmButtonText:'确定',cancelButtonText:'取消'})
      .then(()=>resolve(true)).catch(()=>resolve(false));
      return;
    }
    resolve(!!global.confirm(msg));
  }));
}

/**
 * App 台账登记：记录 el->app 及父子关系。
 * unmount 按台账级联，不依赖 DOM 位置（嵌套节点挂载期间会临时脱离文档到 #dyn-holder）。
 */
function registerApp(el,app,parentEl){
  if(!el) return;
  if(_appMap)_appMap.set(el,app);
  el.__dynApp = app;
  if(parentEl && parentEl!==el){
    if(!parentEl.__dynChildApps) parentEl.__dynChildApps = new Set();
    parentEl.__dynChildApps.add(el);
    el.__dynParentAppEl = parentEl;
  }
}
function storeApp(el,app){
  registerApp(el,app,el&&el.__dynParentAppEl||null);
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
  // 从父台账摘除
  const p = el.__dynParentAppEl;
  if(p && p.__dynChildApps) p.__dynChildApps.delete(el);
  el.__dynParentAppEl = null;
  el.__dynChildApps = null;
}

function holderEl(){
  let h = document.getElementById(CONST.HOLDER_ID);
  if(!h){ h = document.createElement('div'); h.id=CONST.HOLDER_ID; h.style.display='none'; document.body.appendChild(h); }
  return h;
}

/**
 * @description 收集【直接】嵌套的 dyn-mode=createApp 节点。
 * 只取最近嵌套祖先就是 el 的节点，不深入已命中的嵌套子树——
 * 深层节点交给子 App 自己的 mountCore 递归掩码，避免 dyn-host 被重复包裹。
 * @param {HTMLElement} el
 * @returns {Array<HTMLElement>}
 */
function collectNestedApps(el){
  const out = [];
  if(!el.querySelectorAll) return out;
  // 同时识别两种根标记：
  //  data-dyn-mode="createApp"（已引导/远程片段）
  //  data-dyn-init-createapp（initActions 尚未扫到、mode 还没打上的待引导节点）
  const all = [].slice.call(el.querySelectorAll(
    '['+CONST.ATTR_MODE+'="createApp"],[data-dyn-init-createapp]'
  ));
  all.forEach(node=>{
    if(node.__dynApp||node.__dynMounting) return;
    let p = node.parentNode, nearest = null;
    while(p && p!==el){
      if(p.nodeType===1 && p.getAttribute &&
         (p.getAttribute(CONST.ATTR_MODE)==='createApp' || p.hasAttribute('data-dyn-init-createapp'))){ nearest=p; break; }
      p = p.parentNode;
    }
    if(!nearest) out.push(node);
  });
  return out;
}

/**
 * 绑定 data-dyn-watch-params：参数变化时在挂载点派发 dyn-param-changed（bubbles），
 * detail:{key,value,oldValue}。注销句柄挂 el.__dynParamStop，unmount 时自动失效（ctx 销毁/watch stop）。
 */
function bindParamWatchers(el,paramCtx){
  const attr = el.getAttribute('data-dyn-watch-params');
  if(!attr||!paramCtx) return;
  const keys = attr.split(',').map(s=>s.trim()).filter(Boolean);
  if(!keys.length) return;
  el.__dynParamStop = paramCtx.watch(keys,(key,value,oldValue)=>{
    try{
      el.dispatchEvent(new CustomEvent('dyn-param-changed',{bubbles:true,detail:{key,value,oldValue}}));
    }catch(e){}
  });
}

/**
 * @description 掩码直接嵌套的dyn-init-createApp节点，mount时递归处理子App
 * @param {HTMLElement} el
 * @param {Array} out 输出子节点列表 {child,host,uid}
 */
function maskNested(el,out){
  collectNestedApps(el).forEach(child=>{
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
 * @description mountCore 失败回滚：销毁半成品 app，把尚未挂载的掩码节点从 holder 还原回占位点
 * @param {HTMLElement} el
 * @param {Array} nested maskNested 产出
 * @param {object|null} app
 */
function rollbackMasked(el,nested,app){
  if(app){ try{ app.unmount(); }catch(e){} }
  (nested||[]).forEach(item=>{
    try{
      if(item.child && !item.child.__dynApp){
        if(item.host && item.host.parentNode) item.host.parentNode.insertBefore(item.child,item.host);
        if(item.host && item.host.parentNode) item.host.parentNode.removeChild(item.host);
        item.child.removeAttribute('data-dyn-uid');
      }
    }catch(e){}
  });
  removeApp(el);
  el.__dynModel = null;
  el.__dynProxy = null;
  el.__dynLoaded = false;
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
 * @description 读取 App 根元素的直接子级扩展脚本 script[tag="dynconfig-ext"]（页面扩展视图注入）。
 *   必须在 mountCore 删除容器内 script 之前调用；每个扩展脚本约定声明 dynConfigExt 对象。
 * @param {HTMLElement} el
 * @returns {object[]}
 */
function readExtConfigs(el){
  const list = [];
  const doms = el.querySelectorAll(':scope > script[tag="dynconfig-ext"]');
  [].forEach.call(doms,function(d){
    try{
      const cfg = new Function('element','dyn',d.textContent+"\n; return typeof dynConfigExt!=='undefined'?dynConfigExt:null;")(el,global.dyn);
      if(cfg&&typeof cfg==='object') list.push(cfg);
    }catch(e){ console.error("[DynCore] dynconfig-ext执行异常",e); }
  });
  return list;
}

/**
 * @description 把页面扩展配置合并进 Vue 组件选项（就地修改 component）。
 *   - methods/computed/watch/components/directives：键合并，扩展同名覆盖（允许深度定制）
 *   - data：基础与扩展各自求值后浅合并，扩展键覆盖
 *   - 生命周期（created/mounted/...）：按 基础→扩展 顺序串联；扩展钩子异常不阻断
 *   - 其他顶层属性：扩展直接覆盖；setup/template 不允许扩展
 * @param {object} component
 * @param {object[]} extCfgs
 */
function mergeExtOptions(component,extCfgs){
  if(!extCfgs||!extCfgs.length) return;
  const LIFECYCLE = ['beforeCreate','created','beforeMount','mounted','beforeUpdate','updated','beforeUnmount','unmounted'];
  const MERGE_MAP = ['methods','computed','watch','components','directives','filters'];
  extCfgs.forEach(function(ext){
    if(!ext||typeof ext!=='object') return;
    if(typeof ext.data==='function'){
      const baseData = typeof component.data==='function'?component.data:function(){ return {}; };
      const extData = ext.data;
      component.data = function(){
        const a = baseData.call(this)||{};
        const b = extData.call(this)||{};
        return Object.assign(a,b); // 扩展 data 键覆盖同名
      };
    }
    MERGE_MAP.forEach(function(k){
      if(ext[k]&&typeof ext[k]==='object'){
        component[k] = Object.assign(component[k]||{},ext[k]);
      }
    });
    LIFECYCLE.forEach(function(k){
      if(typeof ext[k]!=='function') return;
      const baseFn = typeof component[k]==='function'?component[k]:null;
      const extFn = ext[k];
      component[k] = function(){
        let ret;
        if(baseFn) ret = baseFn.apply(this,arguments);
        try{ extFn.apply(this,arguments); }
        catch(e){ console.error("[DynCore] dynconfig-ext "+k+" 异常",e); }
        return ret;
      };
    });
    Object.keys(ext).forEach(function(k){
      if(k==='data'||k==='setup'||k==='template') return;
      if(MERGE_MAP.indexOf(k)>=0||LIFECYCLE.indexOf(k)>=0) return;
      component[k] = ext[k];
    });
  });
}

/**
 * @description 内部mount核心逻辑
 * @param {HTMLElement} el
 * @param {HTMLElement|null} [parentEl] 父App宿主（台账用，嵌套掩码还原挂载时显式传入）
 * @returns {Promise<any>}
 */
async function mountCore(el,parentEl){
  checkRuntimeDeps();
  // 幂等守卫：任何链路重复进入都直接返回已挂载实例，杜绝重复 createApp/mount
  if(el.__dynApp) return el.__dynApp;
  // 统一参数上下文：必须【先于】dynconfig 脚本执行建立——脚本内 dyn.params(element)
  // （fromEl）取到的应是块自身 ctx（L1 含 data-blk-config），否则会捕获到中间片段容器
  // 的 ctx，其 local 只有 tableName 没有 table，table 将继续沿 L3 父链上溯到宿主，发生串表。
  // 同样必须在 maskNested/删 script 之前建立（L1 要读直接子级 dynparams 脚本）。
  // 嵌套 App 掩码期间 DOM 祖先链断开，父上下文用显式 parentEl 接回。
  const paramCtx = global.DynParams ? global.DynParams.ensure(el,parentEl) : null;
  let cfgScript = null;
  // dynconfig 同样只认直接子级，避免读到嵌套App的配置
  const cfgDom = el.querySelector(':scope > script[tag="dynconfig"]');
  if(cfgDom){
    try{
      cfgScript = new Function('element','dyn',cfgDom.textContent+"\n; return typeof dynConfig!=='undefined'?dynConfig:null;")(el,global.dyn);
    }catch(e){ console.error("[DynCore] dynconfig执行异常",e); }
  }
  // 页面扩展脚本（dynconfig-ext）：同样只认直接子级；须在下方移除容器 script 之前读取
  const extCfgs = readExtConfigs(el);
  const srcModel = readModelScript(el)||parseModel(el)||{};
  // 优先绑定共享scope
  const bindScopeId = el.getAttribute(CONST.ATTR_USE_SCOPE);
  let useSharedModel = null;
  if(bindScopeId) useSharedModel = getScopeModel(el);
  const reactiveModel = useSharedModel ?? Vue.reactive(srcModel);
  // 先掩码把直接嵌套App整体搬到 #dyn-holder，再移除script——
  // 否则嵌套App自带的 dynmodel 等script会被外层提前删掉，子App挂载时拿不到初始model
  const nested = [];
  maskNested(el,nested);
  // 移除容器内剩余（均为本App自身的）script标签，避免Vue模板解析干扰
  [].slice.call(el.querySelectorAll('script')).forEach(s=>s.remove());

  const component = {
    template:el.innerHTML,
    data(){ return {}; },
    setup(){
      // 直连模式：把页面模型 provide 给容器插槽里的裸标签组件（<dyn-el-input modelname="x"/> 可免写 :parentmodelinfo）
      Vue.provide('dynSlotParentModel', reactiveModel);
      // 统一参数上下文：App 内组件树 inject 同一个 ctx（无需 DOM 反查，跨 createApp 走 fork 链）
      if(paramCtx) Vue.provide('dynParams', paramCtx);
      const exposed = { model:reactiveModel,element:el,dyn:global.dyn };
      if(paramCtx) exposed.$params = paramCtx.view;
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
  // 页面扩展（DynWebPage.ExtViewPath 槽位脚本）：methods 覆盖 / data 浅合并 / 生命周期串联
  mergeExtOptions(component,extCfgs);

  const app = Vue.createApp(component);
  // 掩码占位符 dyn-host 是原生自定义元素，禁止 Vue 尝试解析为组件
  app.config.compilerOptions.isCustomElement = tag=>tag==='dyn-host';
  // 官方注入点：模板中可写 <dyn-inject-host> 作为 updateEl 的安全目标
  app.component('dyn-inject-host',DynInjectHost);
  // 模板 {{$params.xxx}} 响应式参数视图（setup 已暴露同名，这里兜底 optionAPI/全局）
  if(paramCtx) app.config.globalProperties.$params = paramCtx.view;
  let mounted = false;
  try{
    // 先登记台账（含挂载中状态），失败时 rollbackMasked 统一清理
    registerApp(el,app,parentEl||el.__dynParentAppEl||null);
    global.DynCom.setupApp(app);
    await global.DynCom.ensureRegistered(app);
    // 全局属性注册
    app.config.globalProperties.$dyn = global.dyn;
    el.__dynModel = reactiveModel;
    app.__dynModel = reactiveModel;
    el.__dynProxy = app.mount(el)||null;
    mounted = true;
    el.__dynLoaded = true;
    app.__dynProxy = el.__dynProxy;
    app.model = el.__dynProxy?el.__dynProxy.model:Vue.reactive(srcModel);
  }catch(e){
    el.__dynProxy = null;
    if(!mounted) rollbackMasked(el,nested,app);
    else removeApp(el);
    throw e;
  }

  // 外层挂载成功后，再依次还原并挂载直接子App：
  // 必须 for...of await——ensureRegistered 异步组件注册未完成就返回会与外层清空容器竞态，
  // 导致子App挂到游离节点（嵌套app孤儿）
  for(const item of nested){
    try{
      // app.mount 会按模板重建占位元素，不能再持有旧 host 引用，按 uid 定位渲染后的占位符
      const liveHost = el.querySelector
        ? el.querySelector('dyn-host[data-dyn-uid="'+item.uid+'"]')
        : null;
      const host = liveHost || (item.host && item.host.parentNode ? item.host : null);
      if(host){
        host.appendChild(item.child);
        item.child.__dynParentAppEl = el;
        if(el.__dynChildApps) el.__dynChildApps.add(item.child);
        await mount(item.child);
      }else{
        // 占位符丢失（模板里没渲染出来）：兜底还原到容器末尾，避免子App永久滞留 holder
        el.appendChild(item.child);
        item.child.removeAttribute('data-dyn-uid');
        item.child.__dynParentAppEl = el;
        await mount(item.child);
      }
    }catch(e){
      console.error('[DynCore] 嵌套App挂载失败',item.uid,e);
    }
  }
  // 跟随型参数：data-dyn-watch-params="a,b" —— 共享/继承层变化时派发 dyn-param-changed 事件，
  // Block/脚本既可在 dynconfig 里用 P.watch 编程订阅，也可在模板侧 addEventListener 声明式接线。
  if(paramCtx){
    bindParamWatchers(el,paramCtx);
    const reqAttr = el.getAttribute('data-dyn-require-params');
    if(reqAttr) paramCtx.require(reqAttr.split(',').map(s=>s.trim()).filter(Boolean));
  }
  // mount完成后（含ajax载入片段），自动扫描执行容器内 data-dyn-init-* 初始化动作
  if(global.dyn&&typeof dyn.initActions==='function'){
    dyn.initActions(el);
  }
  // 注入/挂载完成即扫描Block句柄，供 Tabs 模板等编排方 await mount 后直接取用
  if(global.DynBlocks && typeof global.DynBlocks.scan==='function'){
    try{ app.__dynBlocks = global.DynBlocks.scan(el); }catch(e){}
  }
  return app;
}

/**
 * @description 规范化组件配置树：补齐 options/childrenctrls/slots/validators/extendinfo 默认结构（原地修改并返回）
 * @param {object} cfg
 * @returns {object}
 */
function normalize(cfg){
  if(!cfg||typeof cfg!=='object') return cfg;
  if(!cfg.options||typeof cfg.options!=='object') cfg.options={};
  if(!cfg.options.comoptions||typeof cfg.options.comoptions!=='object') cfg.options.comoptions={};
  if(!Array.isArray(cfg.childrenctrls)) cfg.childrenctrls=[];
  if(!cfg.slots||typeof cfg.slots!=='object') cfg.slots={};
  if(!Array.isArray(cfg.validators)) cfg.validators=[];
  if(!cfg.extendinfo||typeof cfg.extendinfo!=='object') cfg.extendinfo={};
  cfg.childrenctrls.forEach(normalize);
  Object.keys(cfg.slots).forEach(k=>{
    if(Array.isArray(cfg.slots[k])) cfg.slots[k].forEach(normalize);
  });
  return cfg;
}

/**
 * @description 把组件配置树挂载到目标元素（统一走 DynDynamicCom，替代旧 DynRender h() 内核）
 * @param {object} cfg 组件配置树
 * @param {string|HTMLElement} target 目标元素
 * @param {object} [model] 初始数据模型
 * @returns {Promise<any>}
 * @demo DynCore.mount({component:'DynCrudPage',options:{...}}, '#page-root')
 */
async function mountConfig(cfg,target,model){
  target = resolve(target);
  if(!target) return null;
  unmount(target);
  normalize(cfg);
  // 目标位于 data-dyn-shared-scope 内时，自动挂载到同一份共享 scope model（三屏模板：筛选区/列表区共享数据）
  const scopeHost = target.closest&&target.closest('['+CONST.ATTR_SHARED_SCOPE+']');
  const reactiveModel = scopeHost ? (getScopeModel(target)||Vue.reactive(model||{})) : Vue.reactive(model||{});
  target.setAttribute(CONST.ATTR_MODE,'createApp');
  // 统一参数上下文（mountConfig 根：从最近祖先 fork；无祖先挂页面根，URL 层仍可用）
  const paramCtx = global.DynParams ? global.DynParams.ensure(target,null) : null;
  const component = {
    // 注意：data 键不能以 _ 开头——Vue3 不会把 _/$ 前缀属性代理到组件实例，模板将恒取到 undefined
    template:'<dyn-dynamic-com :jsonconfig="pageCfg" :parentmodelinfo="model"></dyn-dynamic-com>',
    data(){ return { pageCfg:cfg }; },
    setup(){
      // 直连模式：页面模型 provide 给插槽裸标签组件
      Vue.provide('dynSlotParentModel', reactiveModel);
      if(paramCtx) Vue.provide('dynParams', paramCtx);
      const exposed = { model:reactiveModel, element:target, dyn:global.dyn };
      if(paramCtx) exposed.$params = paramCtx.view;
      return exposed;
    }
  };
  const app = Vue.createApp(component);
  app.config.compilerOptions.isCustomElement = tag=>tag==='dyn-host';
  app.component('dyn-inject-host',DynInjectHost);
  if(paramCtx) app.config.globalProperties.$params = paramCtx.view;
  registerApp(target,app,null);
  global.DynCom.setupApp(app);
  await global.DynCom.ensureRegistered(app);
  target.__dynModel = reactiveModel;
  app.__dynModel = reactiveModel;
  target.__dynLoaded = true;
  try{ target.__dynProxy = app.mount(target)||null; }
  catch(e){
    target.__dynProxy=null;
    rollbackMasked(target,[],app);
    throw e;
  }
  if(paramCtx){
    bindParamWatchers(target,paramCtx);
    const reqAttr = target.getAttribute('data-dyn-require-params');
    if(reqAttr) paramCtx.require(reqAttr.split(',').map(s=>s.trim()).filter(Boolean));
  }
  if(global.dyn&&typeof dyn.initActions==='function') dyn.initActions(target);
  if(global.DynBlocks && typeof global.DynBlocks.scan==='function'){
    try{ app.__dynBlocks = global.DynBlocks.scan(target); }catch(e){}
  }
  return app;
}
/**
 * 注入目标安全性提醒：updateEl 往"运行中 App 管辖的普通元素"里注入时，
 * 外层重渲染可能清空命令式内容，建议改用 <dyn-inject-host>。每元素只告警一次。
 * @param {HTMLElement} el
 */
function warnIfUnsafeInject(el){
  try{
    if(el.hasAttribute && el.hasAttribute('data-dyn-inject')) return;
    if(el.classList && el.classList.contains('dyn-inject-host')) return;
    if(el.closest && el.closest('.dyn-inject-host')) return;
    const host = closestDynInit(el);
    if(host && host!==el && host.__dynApp){
      if(_injectWarned) _injectWarned.add(el);
      console.warn('[DynCore] 注入目标位于运行中 App 管辖的普通元素内，外层重渲染可能清空注入内容；建议模板中改用 <dyn-inject-host> 作为 updateEl 注入点。', el);
    }
  }catch(e){}
}

/**
 * 渲染html，执行所有层级内联script，
 * 跳过带 tag="xxx" 属性的script节点，
 * 脚本内可以直接访问变量 el（当前容器DOM）
 * @param {HTMLElement} el 目标容器
 * @param {string} htmlStr html字符串
 * @param {boolean} [boot=true] 是否立即扫描挂载 data-dyn-init-* 容器。
 *   updateEl/open 直接注入时为 true；render/reload 之后还要走 mountCore 的链路必须传 false，
 *   否则内嵌 App 被提前挂载，会与 mountCore 的掩码递归冲突产生孤儿 App。
 * @returns {object|undefined} boot=true 时返回 DynBlocks.scan 句柄表（若存在 DynBlocks）
 */
function html(el, htmlStr, boot) {
  el = resolve(el);
  if(!el) return undefined;
  if(boot === undefined) boot = true;
  if(boot){
    // 片段替换前先释放旧内容中的 App（重复 updateEl/open 防实例泄漏）。
    // boot=false 的链路调用方已自行 unmount（render/reload/mount）。
    unmount(el);
    if(_injectWarned && !_injectWarned.has(el)) warnIfUnsafeInject(el);
  }
  // 统一参数链接入：片段挂载点从最近上下文 fork（嵌套App/服务端片段/弹窗片段同一约定）。
  // 已存在上下文（如 mount 链路自建）则保留；boot=true 重复注入时旧 fork 已随 unmount 销毁，这里重建。
  // bootInfo.parentCtx 显式指定调用方上下文（updateel 目标 pane 可能与按钮不在同一子树）。
  if(global.DynParams && !el.__dynParams){
    try{
      const bi = arguments[3]||{};
      if(bi.parentCtx) bi.parentCtx.fork(el,bi.local||{});
      else global.DynParams.attach(el);
    }catch(e){}
  }
  el.innerHTML = "";
  const temp = document.createElement('div');
  temp.innerHTML = htmlStr;

  // 【关键】在移动DOM之前，先把所有script节点抓取保存
  const allScripts = Array.from(temp.querySelectorAll('script'));

  // 全部节点移入真实容器el
  while (temp.firstChild) {
    el.appendChild(temp.firstChild);
  }

  const scriptsToRun = [];
  allScripts.forEach(oldScript => {
    // 此时oldScript的引用已经跟着DOM移动到el内
    const realScript = oldScript;

    // 只要存在 tag 属性，直接跳过不执行（comconfig配置节点）
    if (realScript.hasAttribute('tag')) {
      return;
    }

    // 外部src脚本不处理，只处理内联脚本
    if (realScript.src) return;

    const code = realScript.textContent.trim();
    if (!code) return;

    realScript.remove(); // 删除原script标签，避免浏览器原生执行
    scriptsToRun.push(code);
  });

  // 执行脚本，注入局部变量 el
  scriptsToRun.forEach(code => {
    try {
      const fn = new Function('parentElement', code);
      fn(el);
    } catch (err) {
      console.error('脚本执行失败', err);
    }
  });
  if(!boot) return undefined;
  dyn.initActions(el);
  // 注入即接线：返回片段内全部 Block 句柄（谁注入，谁编排）
  if(global.DynBlocks && typeof global.DynBlocks.scan==='function'){
    try{ return global.DynBlocks.scan(el); }catch(e){}
  }
  return undefined;
}
/**
 * @description 挂载。两种签名：
 *   dyn.mount(el)                                  挂载 dyn-mode=createApp 容器（支持 data-dyn-url 远程片段）
 *   dyn.mount(cfg, targetEl, model)                把组件配置树通过 DynDynamicCom 挂到目标元素
 * @returns {Promise<any>}
 * @demo dyn.mount("#container")
 */
function mount(elOrCfg,targetEl,model){
  // 配置树挂载：DynCore.mount(cfg, element, model)
  if(elOrCfg&&typeof elOrCfg==='object'&&elOrCfg.nodeType!==1&&elOrCfg.component){
    return mountConfig(elOrCfg,targetEl,model);
  }
  let el = dyn.resolve(elOrCfg);
  if(!el) return Promise.resolve(null);
  if(el.__dynApp) return Promise.resolve(el.__dynApp);
  // 重入共享：挂载进行中的并发调用等待同一个 Promise，而不是被静默吞成 null
  if(el.__dynMounting) return el.__dynMounting;

  // 父App宿主：优先显式指定（嵌套还原挂载），否则取最近已挂载祖先（片段注入进活App场景）
  const parentEl = el.__dynParentAppEl || closestDynInit(el);
  let loaded = false;
  const task = (async()=>{
    try{
      const url = el.getAttribute(CONST.ATTR_URL);
      const force = el.getAttribute('data-dyn-load') === 'true';
      const empty = el.childElementCount === 0;
      const needLoad = !!(url && !el.__dynLoaded && (empty || force));
      if(needLoad){
        const m = parseModel(el)||{};
        const method = el.getAttribute('data-dyn-method')||'POST';
        const htmlText = await fetchPartial(url,m,method);
        // 只注入不引导：内嵌 App 统一由下面的 mountCore 递归链挂载，避免 render+mountCore 双挂载
        dyn.html(el,htmlText,false);
      }
      loaded = true;
      return await mountCore(el,parentEl);
    }catch(err){
      console.error('[DynCore] mount失败',err);
      if(!loaded){
        try{ el.innerHTML = '<div class="dyn-loading">加载失败：'+((err&&err.message)||err)+'</div>'; }catch(e){}
      }
      return null;
    }
  })();
  el.__dynMounting = task;
  task.then(()=>{ if(el.__dynMounting===task) el.__dynMounting=null; },
            ()=>{ if(el.__dynMounting===task) el.__dynMounting=null; });
  return task;
}


/**
 * @description 卸载App，递归卸载子嵌套App，清理内存。
 * 优先按 App 台账级联（子App可能已脱离DOM文档，如被Vue patch抹掉或还滞留在holder），
 * 再用 DOM 查询兜底旧式/游离属性节点。
 * @param {string|HTMLElement} el
 */
function unmount(el){
  el = resolve(el);
  if(!el) return;
  // 1) 台账级联（快照，子节点卸载时会从 Set 中摘除自己）
  if(el.__dynChildApps && el.__dynChildApps.size){
    Array.from(el.__dynChildApps).forEach(child=>unmount(child));
  }
  // 2) DOM 兜底：台账未登记的嵌套节点（旧版本挂载的、外部手动标记的）
  if(el.querySelectorAll){
    [].slice.call(el.querySelectorAll('['+CONST.ATTR_MODE+'="createApp"]')).forEach(n=>{
      if(n!==el && n.__dynApp) unmount(n);
    });
  }
  if(el.__dynApp){
    try{ el.__dynApp.unmount(); }catch(e){}
    el.__dynModel=null;
    el.__dynProxy=null;
    removeApp(el);
  }
  // 3) 参数上下文释放（App 根或片段 fork 挂载点都可能挂着）：注销 watch + 注销 ctxId
  if(global.DynParams && el.__dynParams){
    try{ if(typeof el.__dynParamStop==='function') el.__dynParamStop(); }catch(e){}
    try{ el.__dynParams.destroy(); }catch(e){}
    el.__dynParams = null;
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
  // boot=false：内嵌App交给随后 mount() 的 mountCore 递归链统一引导，避免提前挂载产生孤儿
  global.dyn.html(el,html||'',false);
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
      global.dyn.html(targetEl,text,false);
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
  showMessage,confirmAsync,getLayer,
  resolve,findAncestor,closestDynInit,
  mount,mountConfig,normalize,unmount,render,
  getApp:getAppByEl,getClosestApp,getProxy,getModel,getScopeModel,
  /** 统一参数上下文：dyn.params(el).get('x') / .commit / .fork / .watch / .inspect */
  params:function(el){ return global.DynParams?global.DynParams.fromEl(el):null; },
  attachParams:function(el,parentEl){ return global.DynParams?global.DynParams.ensure(el,parentEl):null; },
  fetchPartial,serializeForm,collectParams,
  reload,setDynCfg,getVueModel,
  getByPath,setPathVal,deepClone,html
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
