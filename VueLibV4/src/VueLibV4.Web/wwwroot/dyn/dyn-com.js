/* dyn-com.js V4 组件注册表，组合组件解析；DynRender已剥离至dyn-render.js */
(function(global){
'use strict';
const Vue = global.Vue;
if(!Vue){ console.error("[DynCom] 未加载Vue"); return; }

const compositeComponents = {};
const _registry = {};
const _metaMap = {};

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
 * @description 设置对象路径值，依赖dyn-render导出的setPath
 */
function setPath(obj,path,value){
  if(global.DynRender && typeof global.DynRender.setPath === 'function'){
    return global.DynRender.setPath(obj,path,value);
  }
  if(global._ && global._.set){
    global._.set(obj,path,value);
    return;
  }
  console.warn("[DynCom] setPath缺少实现，请确保dyn-render.js已加载");
}

async function ensureRegistered(app){
  let _regPromise = null;
  if(_regPromise) return _regPromise;
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
  return _regPromise;
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
  // if(global.DynRender){
  //   app.component('DynRender', global.DynRender);
  // }
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
