/* dyn-action-dom.js 事件委托与初始化扫描
 * 职责：document 捕获模式事件委托（管道属性 / dyn-click-<ActionName> 直连）、
 *       {actions:[...]} 批量执行、dyn-init 初始化管道扫描。
 * 依赖：dyn-action.js（K.dyn / K.CONST / K.parsePipe / K.parsePipeArg / K.runPipe /
 *       K.buildCtx / K.resolveAction / K.wrapCompositeAction / K.config）。
 * 约定：只允许向 K 追加属性，不得整体重新赋值 global.DynActionKit；本文件不注册 DynKernel 插件。
 */
(function(global){
'use strict';
var K = global.DynActionKit;

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
        if(K.config.disableEvalJs){ console.warn("[runJsonActions] evaljs已关闭，跳过script"); return; }
        const fn = new Function('ctx','return ('+item.script+')');
        fn({element:rootEl});
        return;
      }
      const act = K.resolveAction(item.action);
      if(!act){ console.warn("[runJsonActions]动作不存在:"+item.action); return; }
      const c = K.buildCtx(rootEl,'jsonAction',null,item.options||{},item.action);
      const wrappedFn = K.wrapCompositeAction(act);
      Promise.resolve(wrappedFn(c)).catch(e=>console.error("[runJsonActions]执行异常",e));
    }catch(err){ console.error("[runJsonActions]",err); }
  });
}

/** dyn-init 管道中是否包含 CreateApp 引导步（未被摘除时代表"待引导 App 根"） */
function dynInitHasCreateApp(node){
  if(!node || !node.hasAttribute || !node.hasAttribute(K.CONST.PIPE_INIT_ATTR)) return false;
  const expr = node.getAttribute(K.CONST.PIPE_INIT_ATTR)||'';
  return expr.split('|').some(function(tok){
    return /^\s*(ActionHelper\.)?createapp\s*(\(|$)/i.test(tok.trim());
  });
}

/**
 * @description 扫描dom执行初始化动作管道（唯一协议：dyn-init="CreateApp|Toast('就绪')"）
 * @param {HTMLElement} root
 */
function initActions(root){
  const dyn = K.dyn;
  root = dyn.resolve(root)||document.body;
  if(!root) return;
  const list = [];
  if(root.nodeType===1) list.push(root);
  if(root.querySelectorAll) list.push(...[].slice.call(root.querySelectorAll('*')));
  list.forEach(el=>{
    if(el.__dynInitDone) return;
    // 挂载掩码暂存区（#dyn-holder）里的节点属于外层 App 的递归挂载链，
    // 此时提前引导会把子 App 挂到游离节点；外层还原后会由其 mountCore 自行 initActions
    if(el.closest && el.closest('#dyn-holder')) return;
    // 最近的 createApp 祖先"尚未挂载完成"（mode 已打但无实例，或 dyn-init 管道还挂着 CreateApp 步）：
    // 本节点交给该祖先挂载完成后的递归 initActions，绝不能提前独立引导，否则会变成孤儿 App。
    // 祖先已挂载（__dynApp 存在）时则放行——那是 updateEl 注入进活 App 的片段，需要自举。
    if(el.closest){
      const anc = el.closest('[data-dyn-mode="createApp"],[dyn-init]');
      if(anc && anc!==el && !anc.__dynApp &&
         (anc.getAttribute('data-dyn-mode')==='createApp' || dynInitHasCreateApp(anc))) return;
    }
    // 初始化管道：dyn-init="CreateApp" / dyn-init="Toast('就绪')|Setvar({...})"
    if(el.hasAttribute&&el.hasAttribute(K.CONST.PIPE_INIT_ATTR)){
      const steps = K.parsePipe(el.getAttribute(K.CONST.PIPE_INIT_ATTR)||'');
      // 执行后移除init属性，防止Vue挂载重建DOM后重复执行
      el.removeAttribute(K.CONST.PIPE_INIT_ATTR);
      if(steps.length) K.runPipe(steps,el,'init',null).catch(e=>console.error("[init-pipe异常]",e));
    }
  });
}

let _delegationBound = false;
/** 已绑定的委托监听（事件名 + 处理函数），供 rebindActions 先解绑旧监听，避免 document 监听叠加 */
const _delegationHandlers = [];

/**
 * @description 重新绑定事件委托。管道委托不依赖动作注册表，常规情况下无需调用；
 *   保留 API 供未来按注册表过滤/外部需要时使用。每次 rebind 前先解绑旧监听，防止监听叠加。
 */
function rebindActions(){
  _delegationHandlers.forEach(function(h){ try{ document.removeEventListener(h.ev, h.fn, true); }catch(e){ /*忽略*/ } });
  _delegationHandlers.length = 0;
  _delegationBound = false;
  bindDelegation();
}

/**
 * 沿触发源向上查找"命名动作直连"属性：dyn-click-<ActionName>（如 dyn-click-DetailClose）。
 * CSS 选择器不支持属性名前缀匹配，这里手工遍历祖先链。
 * @returns {{el:HTMLElement,attr:string}|null}
 */
function findNamedActionAttr(start,prefix){
  let n = start;
  while(n && n.nodeType===1){
    const attrs = n.attributes;
    for(let i=0;i<attrs.length;i++){
      const nm = attrs[i].name;
      // 严格长于前缀，排除管道属性本体（dyn-click）与其 data- 形态
      if(nm.length>prefix.length && nm.slice(0,prefix.length)===prefix){
        return { el:n, attr:nm };
      }
    }
    if(n===document.documentElement) break;
    n = n.parentElement;
  }
  return null;
}

/**
 * @description document捕获模式事件委托（唯一协议：
 *   1) 管道属性 dyn-click="A|B('x')"；
 *   2) 命名动作直连属性 dyn-click-<ActionName>，属性值可空（无参）或 JSON 参数）。
 */
function bindDelegation(){
  const dyn = K.dyn;
  if(_delegationBound) return;
  _delegationBound = true;
  const doBind = ()=>{
    // dyn-click="ActionHelper.Submit|ActionHelper.Toast('保存成功')"
    Object.keys(K.CONST.PIPE_EVENT_ATTR).forEach(ev=>{
      const attrName = K.CONST.PIPE_EVENT_ATTR[ev];
      const namedPrefix = attrName + '-'; // dyn-click- / dyn-dblclick- / ...
      const handler = async e=>{
        let target = e.target&&e.target.closest?e.target.closest('['+attrName+']'):null;
        let steps = target ? K.parsePipe(target.getAttribute(attrName)||'') : [];
        // 管道未命中时，尝试命名动作直连：<button dyn-click-DetailClose>取消</button>
        if(!steps.length && e.target && e.target.closest){
          const hit = findNamedActionAttr(e.target, namedPrefix);
          if(hit){
            target = hit.el;
            const rawVal = target.getAttribute(hit.attr) || '';
            let options = {};
            if(rawVal.trim()){
              const arg = K.parsePipeArg(rawVal);
              options = (arg&&typeof arg==='object'&&!Array.isArray(arg)) ? arg : { value:arg };
            }
            steps = [{ action:hit.attr.slice(namedPrefix.length).replace(/^ActionHelper\./i,''), options:options }];
          }
        }
        if(!target || !steps.length) return;
        const prevent = !steps[0].options || steps[0].options.prevent!==false;
        if(prevent){ e.preventDefault(); e.stopPropagation(); }
        try{
          await K.runPipe(steps,target,ev,e);
        }catch(err){
          console.error("[DynAction]管道执行异常",steps,err);
          dyn.showMessage("操作失败："+err.message,'error');
        }
      };
      document.addEventListener(ev, handler, true);
      _delegationHandlers.push({ev:ev, fn:handler});
    });
  };
  if(document.readyState==='loading') document.addEventListener('DOMContentLoaded',doBind);
  else setTimeout(doBind,0);
}

K.runJsonActions = runJsonActions;
K.dynInitHasCreateApp = dynInitHasCreateApp;
K.initActions = initActions;
K.rebindActions = rebindActions;
K.findNamedActionAttr = findNamedActionAttr;
K.bindDelegation = bindDelegation;
})(window);
