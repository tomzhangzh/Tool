/* dyn-action-db.js 数据库动作助手
 * 职责：DB 动作行的注册/执行（script / api / chain / url 四类）与从后端拉取全部启用动作。
 * 依赖：dyn-action.js（K.dyn / K.defineAction / K.runPipe / K.applyTpl / K.meta / K.bus）。
 * 约定：只允许向 K 追加属性，不得整体重新赋值 global.DynActionKit；仅本文件不注册 DynKernel 插件。
 */
(function(global){
'use strict';
var K = global.DynActionKit;

/** @type {Record<string,object>} 数据库动作原始定义（Code → 行记录），供动作助手页面展示 */
K._dbActions = Object.create(null);

/**
 * 脚本类 DB 动作执行器。
 * 脚本内可用：ctx（页面上下文，含 options=本次参数）/ args（=ctx.options 快捷方式）
 *            action（动作定义行）/ api（showMessage/confirm/fetch 等工具）
 * 平台内置 reload 种子脚本即：ctx.reload();
 */
function runDbScript(row,args,ctx){
  const api = {
    showMessage:function(m,t){ return K.dyn.showMessage(m,t||'success'); },
    confirm:function(msg){ return K.dyn.confirmAsync ? K.dyn.confirmAsync(msg) : Promise.resolve(confirm(msg)); },
    toast:function(m,t){ return K.dyn.showMessage(m,t||'success'); },
    fetch:function(url,opt){ return fetch(url,opt).then(function(r){return r.json();}); },
    bus:K.bus
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
  const url = K.applyTpl(cfg.url,args)||'';
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
    return { action:s.action, options:K.applyTpl(s.options||{},args) };
  });
  return K.runPipe(steps,(ctx&&ctx.element)||document.body,'chain',null);
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
  K._dbActions[code]=norm;
  const key = K.defineAction(code,async function(ctx){
    const args = (ctx&&ctx.options)||{};
    const type = (norm.ActionType||'script').toLowerCase();
    if(type==='url'){
      const url = norm.Script||args.url||'';
      if(url) global.location.href = K.applyTpl(url,args);
      return true;
    }
    if(type==='api') return runDbApi(norm,args,ctx);
    if(type==='chain') return runDbChain(norm,args,ctx);
    return runDbScript(norm,args,ctx);
  });
  // 在元信息上保留原始字段（动作助手页面直接读 Id/Name/Code/ActionType/ParmsJson）
  if(K.meta[key]) Object.assign(K.meta[key],norm,{source:'db'});
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

K.runDbScript = runDbScript;
K.runDbApi = runDbApi;
K.runDbChain = runDbChain;
K.registerDbAction = registerDbAction;
K.loadDbActionHelpers = loadDbActionHelpers;
})(window);
