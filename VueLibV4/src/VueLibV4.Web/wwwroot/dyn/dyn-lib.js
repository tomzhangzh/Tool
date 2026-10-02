/* dyn-lib.js V4 资源加载控制器 */
/* 初始化动作统一走 dyn-init 管道（dyn-init="CreateApp|Toast('就绪')"），由挂载链扫描执行 */
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
 * @property {boolean} cacheBust 模块URL强制追加时间戳绕缓存（不经_Layout、无内容哈希清单的独立静态页/开发页使用，如 test.html）
 */

/** @type {DynLibConfig} */
const DEFAULT_CONFIG = {
  loadLodash:true,
  loadElementPlus:true,
  loadLayui:true,
  loadCodemirror:true,
  loadTailwind:true,
  loadAxios:true,
  disableEvalJs:false,
  cacheBust:false
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
DEFAULT_LIBS.push({url:"../lib/jquery/dist/jquery.min.js",name:"jquery"});
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
DEFAULT_LIBS.push({url:"../lib/vue-draggable-plus.umd.js",name:"vue-draggable-plus"});
if(DYN_LIB_CONFIG.loadCodemirror){
  DEFAULT_LIBS.push({url:"../lib/codemirror/codemirror.min.css",name:"codemirror-css"});
  DEFAULT_LIBS.push({url:"../lib/codemirror/codemirror.min.js",name:"codemirror"});
}
// axios UMD：默认false，页面可自行引入CDN；如需本地加载，把 axios.min.js 放入 ../lib/ 并设置 loadAxios:true
if(DYN_LIB_CONFIG.loadAxios) DEFAULT_LIBS.push({url:"../lib/axios.min.js",name:"axios"});

// 模块加载顺序：dyn-kernel 最先（显式内核/阶段装配）→ dyn-com（组件注册表）→ dyn-core（挂载内核，递归统一走 DynDynamicCom）→ 校验器 → 动作系统 → 设计器弹窗
// 注意：真正的初始化顺序由 DynKernel 按阶段（core→components→params→services→actions→blocks→layout→designer→debug）决定，与此数组顺序解耦
const DYN_MODULES = [
  "dyn-kernel.js",
  "dyn-load-com.js",
  "dyn-com.js",
  "dyn-core.js",
  "dyn-params.js",
  "dyn-call.js",
  "dyn-rpc.js",
  "dyn-validator.js",
  "dyn-designer-ops.js",
  "dyn-designer-sortable.js",
  "dyn-action.js",
  "dyn-designer-dialog.js",
  "dyn-template.js",
  "dyn-dsl.js",
  "dyn-debug.js"
];

const DynLib = {
  version:"4.4.4",
  base:BASE,
  _libs:{},
  _ready:false,
  _queue:[],
  // 版本号优先取服务端烘焙的内容哈希（window.DYN_ASSET_HASHES，_Layout 内联）：
  // 改任意 wwwroot/dyn/**/*.js → 哈希变化 → URL 变化 → 浏览器缓存自动失效，无需手工 bump。
  // 清单缺失（片段独立加载等极端场景）才回退 this.version 固定版本号。
  script(rel){
    // 独立静态页无哈希清单且开启 cacheBust：整次加载共用一个时间戳，既绕缓存又保持链内一致
    if(DYN_LIB_CONFIG.cacheBust){
      const bust = this._cacheBust || (this._cacheBust = Date.now());
      return BASE+rel+(rel.indexOf('?')>=0?'&':'?')+'t='+bust;
    }
    const hashes = global.DYN_ASSET_HASHES || {};
    const v = hashes[rel] || this.version;
    return BASE+rel+(rel.indexOf('?')>=0?'&':'?')+'v='+v;
  },
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
    // 就绪后自动扫描并执行页面 dyn-init 初始化动作（含ajax载入片段由mountCore触发）。
    // 无手动 mount 的独立页（如 dyn/test.html）完全依赖这次全局引导；
    // mountCore 有 __dynApp 幂等守卫，ready 回调内再手动 mount 不会重复挂载。
    try{
      if(global.dyn && typeof global.dyn.initActions === 'function'){
        global.dyn.initActions(document.body);
      }
    }catch(e){ console.error("[DynLib]initActions执行异常",e); }
    axios.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';
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
.then(()=>{
  // 全部脚本就绪 → 内核按阶段显式装配（boot 幂等）；无 kernel 的极端场景直接放行
  if(global.DynKernel && typeof global.DynKernel.boot==='function') return global.DynKernel.boot();
})
.then(()=>DynLib._fireReady())
.catch(err=>{
  console.error("[DynLib]依赖加载异常",err);
  DynLib._fireReady();
});

})(window);
