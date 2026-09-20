/* dyn-com.js V4 组件注册表，组合组件解析；路径工具统一使用 dyn-core 的 setPathVal（lodash 驱动） */
(function(global){
'use strict';
const Vue = global.Vue;
if(!Vue){ console.error("[DynCom] 未加载Vue"); return; }

const compositeComponents = {};
const _registry = {};
const _metaMap = {};
/** 组件清单加载去重（模块级，不能声明在 ensureRegistered 函数内部——否则缓存永远失效） */
let _regPromise = null;

/**
 * @description 组合组件属性展开，把外部props/slots合并进组件内部cfg树
 * @param {object} tree cfg配置树
 * @param {object} config compositeComponents的配置
 * @param {object} externalProps 外部传入属性
 * @param {object} externalSlots 外部传入插槽
 * @returns {object}
 */
function applyCompositeProps(tree,config,externalProps,externalSlots){
  if(!tree||typeof tree!=='object') return tree;
  if(Array.isArray(tree)) return tree.map(t=>applyCompositeProps(t,config,externalProps,externalSlots));
  const node = Object.assign({},tree);
  const openProps = (config&&(config.exposedProps||config.openProps))||[];
  openProps.forEach(op=>{
    if(!op||!op.key) return;
    const val = externalProps?externalProps[op.key]:undefined;
    if(val!==undefined){
      const tp = op.target||op.key;
      setPath(node,tp,val);
    }
  });
  if(config&&config.openContainers&&config.openContainers.length){
    config.openContainers.forEach(oc=>{
      if(!oc||!oc.key) return;
      const hasSlot = !!(externalSlots&&oc.key in externalSlots);
      if(!hasSlot) return;
      const kids = externalSlots[oc.key]||[];
      setPath(node,oc.key,kids.map(k=>Object.assign({__unlocked:true},k)));
    });
  }
  if(node.childrenctrls&&node.childrenctrls.length){
    node.childrenctrls = node.childrenctrls.map(c=>applyCompositeProps(c,config,externalProps,externalSlots));
  }
  return node;
}

/**
 * @description 设置对象路径值，统一走 dyn-core（lodash _.set，支持点路径/数组下标）
 */
function setPath(obj,path,value){
  const core = global.dyn||global.dynCore;
  if(core&&typeof core.setPathVal==='function'){
    return core.setPathVal(obj,path,value);
  }
  if(global._ && global._.set){
    global._.set(obj,path,value);
    return;
  }
  console.warn("[DynCom] setPath缺少实现，请确保dyn-core.js已加载");
}

async function ensureRegistered(app){
  if(!_regPromise){
    _regPromise = fetch('/api/component/list')
    .then(r=>r.json())
    .then(res=>{
      if(res&&res.data&&Array.isArray(res.data)){
        return registerComponents(app,res.data);
      }
      return {count:0,composites:0,registry:{}};
    }).catch(e=>{
      console.error("[DynCom]加载组件清单失败",e);
      return {count:0,composites:0,registry:{}};
    });
  }
  const ret = await _regPromise;
  // 多 createApp 场景（设计器四分区各自一个 app、并发 mount）：清单只拉一次，
  // 但注册必须对每个调用方 app 各做一次——否则后挂载 app 里 dyn-* 组件会退化成未注册的原生自定义元素。
  if(app){
    const existing = app._context && app._context.components ? app._context.components : {};
    Object.keys(_registry).forEach(k=>{
      if(k==='Button') return;
      if(!existing[k]) app.component(k,_registry[k]);
    });
    if(_registry['DynDynamicCom'] && !existing['n-dynamic-com']){
      app.component('n-dynamic-com',_registry['DynDynamicCom']);
    }
  }
  return ret;
}

async function registerComponents(app,metas){
  let count=0,composites=0;
  if(!metas||!metas.length) return {count,composites,registry:_registry};
  metas.forEach(m=>{
    const name = m.ComponentName||m.componentName;
    if(!name) return;
    if(m.isComposite&&m.compositeConfigJson){
      try{ compositeComponents[name]=JSON.parse(m.compositeConfigJson); composites++; }
      catch(e){ console.error("[DynCom]解析组合组件失败",name,e); }
    }
    if(name&&m.loadUrl&&global.DynLoadCom){
      const comp = global.DynLoadCom(name);
      if(name!=='Button') app.component(name,comp);
      if(name==='DynDynamicCom'){ app.component('n-dynamic-com',comp); }
      _registry[name]=comp; count++;
    }
  });
  return {count,composites,registry:_registry};
}

function setupApp(app){
  if(global.ElementPlus){
    const locale = (global.ElementPlus.locale&&global.ElementPlus.locale.zhCn)||undefined;
    app.use(global.ElementPlus, locale?{locale}:undefined);
  }
  if(global.ElementPlusIconsVue){
    Object.keys(global.ElementPlusIconsVue).forEach(k=>app.component(k,global.ElementPlusIconsVue[k]));
  }
  // 递归渲染统一使用 DynDynamicCom（组件清单中已注册），不再有独立的 DynRender h() 内核
  Object.keys(_registry).forEach(k=>{ if(k!=='Button') app.component(k,_registry[k]); });
}

global.DynCom = {
  compositeComponents,
  applyCompositeProps,
  register(name,comp,meta){
    _registry[name]=comp; if(meta)_metaMap[name]=meta;
  },
  get(name){ return _registry[name]; },
  meta(name){ return _metaMap[name]; },
  loadMeta(){
    return fetch('/api/platform/componentmeta/all').then(r=>r.json()).then(res=>{
      if(res.code===0){
        (res.data||[]).forEach(m=>{
          if(!m||!m.ComponentName) return;
          try{m.PropsMeta = typeof m.PropsMeta==='string'?JSON.parse(m.PropsMeta):(m.PropsMeta||[]);}catch(e){m.PropsMeta=[];}
          try{m.AllowDrop = typeof m.AllowDrop==='string'?JSON.parse(m.AllowDrop):(m.AllowDrop||[]);}catch(e){m.AllowDrop=[];}
          try{m.SlotsDefine = typeof m.SlotsDefine==='string'?JSON.parse(m.SlotsDefine):(m.SlotsDefine||[]);}catch(e){m.SlotsDefine=[];}
          _metaMap[m.ComponentName]=m;
        });
      }
      return _metaMap;
    });
  },
  setupApp,
  ensureRegistered,
  registerComponents,
  registry:_registry,
  metaMap:_metaMap
};
})(window);
