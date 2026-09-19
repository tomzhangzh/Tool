/* dyn-lib.js V4 资源加载控制器 */
/* 就绪后自动执行 dyn.initActions(document.body)，扫描 data-dyn-init-* 初始化动作 */
(function(global){
'use strict';

/**
 * @typedef DynLibConfig
 * @property {boolean} loadLodash
 * @property {boolean} loadElementPlus
 * @property {boolean} loadLayui
 * @property {boolean} loadCodemirror
 * @property {boolean} loadTailwind
 * @property {boolean} loadAxios 是否加载本地 ../lib/axios.min.js（默认false，页面可自行引入axios UMD CDN）
 * @property {boolean} disableEvalJs 全局关闭evaljs动作，防止任意脚本执行
 */

/** @type {DynLibConfig} */
const DEFAULT_CONFIG = {
  loadLodash:true,
  loadElementPlus:true,
  loadLayui:true,
  loadCodemirror:true,
  loadTailwind:true,
  loadAxios:true,
  disableEvalJs:false
};
global.DYN_LIB_CONFIG = Object.assign({}, DEFAULT_CONFIG, global.DYN_LIB_CONFIG||{});
const DYN_LIB_CONFIG = global.DYN_LIB_CONFIG;

/**
 * @description 获取当前脚本所在base路径
 * @returns {string}
 */
function getBase(){
  const scripts = document.getElementsByTagName('script');
  const src = scripts[scripts.length-1].src||'';
  return src.substring(0, src.lastIndexOf('/')+1);
}
const BASE = getBase();

const DEFAULT_LIBS = [];
DEFAULT_LIBS.push({url:"../lib/vue.global.prod.js",name:"vue"});
if(DYN_LIB_CONFIG.loadLodash) DEFAULT_LIBS.push({url:"../lib/lodash.min.js",name:"lodash"});
if(DYN_LIB_CONFIG.loadElementPlus){
  DEFAULT_LIBS.push({url:"../lib/element-plus/index.css",name:"element-plus-css"});
  DEFAULT_LIBS.push({url:"../lib/element-plus/index.full.min.js",name:"element-plus"});
  DEFAULT_LIBS.push({url:"../lib/element-plus/icons.min.js",name:"element-plus-icons"});
}
if(DYN_LIB_CONFIG.loadLayui){
  DEFAULT_LIBS.push({url:"../lib/layui/css/layui.css",name:"layui-css"});
  DEFAULT_LIBS.push({url:"../lib/layui/layui.js",name:"layui"});
}
if(DYN_LIB_CONFIG.loadTailwind) DEFAULT_LIBS.push({url:"../lib/tailwind.css",name:"tailwind"});
DEFAULT_LIBS.push({url:"../lib/sortable.min.js",name:"sortable"});
if(DYN_LIB_CONFIG.loadCodemirror){
  DEFAULT_LIBS.push({url:"../lib/codemirror/codemirror.min.css",name:"codemirror-css"});
  DEFAULT_LIBS.push({url:"../lib/codemirror/codemirror.min.js",name:"codemirror"});
}
// axios UMD：默认false，页面可自行引入CDN；如需本地加载，把 axios.min.js 放入 ../lib/ 并设置 loadAxios:true
if(DYN_LIB_CONFIG.loadAxios) DEFAULT_LIBS.push({url:"../lib/axios.min.js",name:"axios"});

// 模块加载顺序：dyn-render 优先 dyn-com
const DYN_MODULES = [
  "dyn-load-com.js",
  "dyn-render.js",
  "dyn-com.js",
  "dyn-core.js",
  "dyn-designer-ops.js",
  "dyn-designer-sortable.js",
  "dyn-action.js",
  "dyn-template.js"
];

const DynLib = {
  version:"4.0.0",
  base:BASE,
  _libs:{},
  _ready:false,
  _queue:[],
  script(rel){ return BASE+rel+(rel.indexOf('?')>=0?'&':'?')+'v='+this.version; },
  lib(rel){ return BASE+'../lib/'+rel; },
  use(name){ return this._libs[name]?global[name]:null; },
  /**
   * @description 动态加载js/css资源
   * @param {string} url
   * @returns {Promise<boolean>}
   * @demo DynLib.load("./test.js")
   */
  load(url){
    const self = this;
    if(this._libs[url]) return Promise.resolve(true);
    return new Promise((resolve,reject)=>{
      let el;
      if(/\.css($|\?)/.test(url)){
        el = document.createElement('link');
        el.rel = "stylesheet";
        el.href = url;
        el.onload = ()=>{ self._libs[url]=true; resolve(true); };
        el.onerror = ()=>reject(new Error("[DynLib]样式加载失败:"+url));
      }else{
        el = document.createElement('script');
        el.src = url;
        el.async = false;
        el.onload = ()=>{ self._libs[url]=true; resolve(true); };
        el.onerror = ()=>reject(new Error("[DynLib]脚本加载失败:"+url));
      }
      document.head.appendChild(el);
    });
  },
  /**
   * @description 资源就绪回调
   * @param {Function} cb
   * @demo DynLib.ready(()=>{ console.log("全部就绪"); })
   */
  ready(cb){
    if(this._ready){ cb(); return; }
    this._queue.push(cb);
  },
  _fireReady(){
    // 就绪后自动扫描并执行页面 data-dyn-init-* 初始化动作（含ajax载入片段由mountCore触发）
    try{
      if(global.dyn && typeof dyn.initActions === 'function'){
        dyn.initActions(document.body);
      }
    }catch(e){ console.error("[DynLib]initActions执行异常",e); }
    this._ready = true;
    const q = [...this._queue];
    this._queue.length = 0;
    q.forEach(cb=>{ try{ cb(); }catch(e){ console.error("[DynLib]ready回调异常",e); }});
  }
};
global.DynLib = DynLib;

let chain = DEFAULT_LIBS.map(item=>({url:DynLib.lib(item.url),name:item.name}))
.concat(DYN_MODULES.map(m=>({url:DynLib.script(m),name:m})));

chain.reduce((prev,item)=>prev.then(()=>DynLib.load(item.url)),Promise.resolve())
.then(()=>DynLib._fireReady())
.catch(err=>{
  console.error("[DynLib]依赖加载异常",err);
  DynLib._fireReady();
});

})(window);
