/* dyn‑load‑com.js DynLoadCom：后端动态加载自定义组件，原vueLoadCom重命名 */
(function(global){
'use strict';
const componentCache = new Map();
const loadingPromises = new Map();
const injectedStyles = new Set();
let API_BASE = '/api/component';

/**
 * @description 设置组件API基础地址
 * @param {{apiBase:string}} options
 * @demo DynLoadCom.config({apiBase:"/myapi/component"})
 */
function config(options){
  if(options&&options.apiBase) API_BASE = options.apiBase.replace(/\/$/,'');
}

/**
 * @description 拉取后端组件定义json
 * @param {string} componentName
 * @returns {Promise<any>}
 */
async function fetchComponentDefine(componentName){
  const url = API_BASE+'/define/'+encodeURIComponent(componentName);
  const resp = await fetch(url,{method:'GET',headers:{'Accept':'application/json'}});
  if(!resp.ok) throw new Error("加载组件["+componentName+"]HTTP "+resp.status);
  const res = await resp.json();
  if(!res.success||!res.data) throw new Error("加载组件["+componentName+"]失败:"+(res.message||"未知错误"));
  return res.data;
}

/**
 * @description 解析组件scriptContent，把export default转为可执行对象
 * @param {string} scriptContent
 * @param {string} componentName
 * @returns {object}
 */
function parseScriptContent(scriptContent,componentName){
  if(!scriptContent||!scriptContent.trim()) return {name:componentName};
  let code = scriptContent.trim();
  code = code.replace(/^\s*export\s+default\s*/,'return ');
  if(!/^\s*return\s+/.test(code) && code.startsWith('{')) code = 'return '+code;
  try{
    const factory = new Function(code);
    const opt = factory();
    if(!opt||typeof opt!=='object') throw new Error("script必须导出对象");
    if(!opt.name) opt.name = componentName;
    return opt;
  }catch(err){
    console.error("[DynLoadCom]解析组件["+componentName+"]script失败",err);
    throw err;
  }
}

/**
 * @description 动态注入组件样式到head，避免重复
 * @param {string} componentName
 * @param {string} styleContent
 */
function injectStyle(componentName,styleContent){
  if(!styleContent||!styleContent.trim()) return;
  if(injectedStyles.has(componentName)) return;
  const styleEl = document.createElement('style');
  styleEl.setAttribute('data-component',componentName);
  styleEl.textContent = styleContent;
  document.head.appendChild(styleEl);
  injectedStyles.add(componentName);
}

/**
 * @description 加载单个组件，缓存处理，防止并发重复请求
 * @param {string} componentName
 * @returns {Promise<object>}
 */
async function loadComponent(componentName){
  if(componentCache.has(componentName)) return componentCache.get(componentName);
  if(loadingPromises.has(componentName)) return loadingPromises.get(componentName);
  const promise = (async ()=>{
    const def = await fetchComponentDefine(componentName);
    const opt = parseScriptContent(def.scriptContent,componentName);
    opt.template = def.templateContent;
    injectStyle(componentName,def.styleContent);
    componentCache.set(componentName,opt);
    if(global.DynCom&&typeof global.DynCom.register==="function"){
      global.DynCom.register(componentName,opt,def.meta||null);
    }
    return opt;
  })().catch(err=>{
    loadingPromises.delete(componentName);
    throw err;
  });
  loadingPromises.set(componentName,promise);
  return promise;
}

/**
 * @description 创建Vue3异步组件 defineAsyncComponent
 * @param {string} componentName
 * @param {object} [asyncOptions]
 * @demo const MyCom = DynLoadCom("MyCustomCom")
 */
function DynLoadCom(componentName,asyncOptions){
  if(!global.Vue||!global.Vue.defineAsyncComponent){
    throw new Error("Vue3未加载，请先引入vue.global.prod.js");
  }
  const loader = ()=>loadComponent(componentName);
  const defaultOpt = {
    delay:200,
    timeout:10000,
    loadingComponent:{
      template:`<div style="padding:20px;text-align:center;color:#909399;"><span style="display:inline-block;width:20px;height:20px;border:2px solid #dcdfe6;border-top-color:#409eff;border-radius:50%;animation:spin 0.8s linear infinite;"></span><span style="margin-left:8px;">组件加载中...</span><style>@keyframes spin{to{transform:rotate(360deg);}}</style></div>`
    },
    errorComponent:{
      props:['error'],
      template:`<div style="padding:20px;text-align:center;color:#f56c6c;border:1px dashed #f56c6c;border-radius:4px;margin:8px;"><strong>组件加载失败</strong><br><span style="font-size:12px;">{{error&&error.message}}</span></div>`
    }
  };
  return global.Vue.defineAsyncComponent(Object.assign({},defaultOpt,asyncOptions||{},{loader}));
}

DynLoadCom.config = config;
/**
 * @description 预加载一批组件，后台静默加载
 * @param {string[]} names
 * @demo DynLoadCom.preload(["ComA","ComB"])
 */
DynLoadCom.preload = function(names){
  if(!Array.isArray(names)) return;
  names.forEach(name=>{
    if(!componentCache.has(name)){
      loadComponent(name).catch(e=>console.warn("[DynLoadCom]预加载组件["+name+"]失败",e.message));
    }
  });
};
/**
 * @description 清空组件缓存
 * @param {string} [name] 不传清空全部
 */
DynLoadCom.clearCache = function(name){
  if(name){
    componentCache.delete(name);
    injectedStyles.delete(name);
  }else{
    componentCache.clear();
    injectedStyles.clear();
  }
};
DynLoadCom.getCache = ()=>componentCache;

global.DynLoadCom = DynLoadCom;
global.vueLoadCom = DynLoadCom; //兼容旧别名
})(window);
