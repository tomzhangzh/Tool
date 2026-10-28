﻿/* dyn-action-builtins.js 内置动作定义（33 个）
 * 职责：仅注册内置动作实现（createapp/postback/.../redirect）。
 * 依赖：dyn-action.js 必须先加载；本文件在加载期直接使用 global.DynActionKit（K）。
 * 约定：只允许向 K 追加动作（K.defineAction）与读取 K.*，不得整体重新赋值 global.DynActionKit。
 * 运行期取 dyn 一律用 K.dyn（加载期 K.dyn 为 null）。
 */
(function(global){
'use strict';
var K = global.DynActionKit;
K.defineAction('createapp',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const el = ctx.element;
  const url = o.url||el.getAttribute('data-dyn-url');
  if(url) el.setAttribute('data-dyn-url',url);
  el.setAttribute('data-dyn-mode','createApp');
  await dyn.mount(el);
});
K.defineAction('postback',async function(ctx){
  const dyn = K.dyn;
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage(K.CONST.MSG_MISS_URL.replace('{action}','postback'),'error'); return; }
  if(!(await K.confirmIfNeeded(o,'确定执行该操作吗？'))) return false;
  // ---- 提交格式：json（默认）| form ----
  // form：把 body 拍平成 MVC 模型绑定字段（Code / Customer.Name / Items[0].Id），
  //       日期转 yyyy-MM-dd HH:mm:ss，自动附加 __RequestVerificationToken；
  //       后端可直接用实体或方法参数绑定（无需 [FromBody]）。
  const ctRaw=(o.contentType||o.format||'').toString().toLowerCase();
  const useForm=ctRaw==='form'||ctRaw==='urlencoded'||ctRaw==='x-www-form-urlencoded'||ctRaw==='multipart';
  // useEventPayload 默认true；form 模式下必须提交裸 model，否则会拍平成 data.Code=... 导致 MVC 绑不到实体
  const usePayload = o.useEventPayload!==false && !useForm;
  let body;
  if(o.body && typeof o.body==='object'){
    // 统一契约通道：显式声明请求体（{{占位}} 已由 buildCtx→applyTpl 用 data-* 行参数替换）
    // 例：{"body":{"table":"X","keys":{"Id":"{{Id}}"}}}
    body = dyn.deepClone(o.body);
  }else if(usePayload){
    body = { event:o.event||'', data:Object.assign({},ctx.model||{}) };
  }else{
    body = dyn.deepClone(ctx.model||{});
  }
  // wrap：把默认扁平 body 包成 dyndata 统一契约（表单类提交无法逐字段列 body 模板时用）
  // 例：{"wrap":{"table":"DesktopShortcut","dataField":"data","gridId":"grid_x"}}
  // form 模式下不包装，字段必须平铺到顶层才能被 MVC 绑定
  if(!useForm && o.wrap && typeof o.wrap==='object' && o.wrap.table && !(o.body&&typeof o.body==='object')){
    body = K.wrapDyndata(body,o.wrap);
  }
  // params 仍可走 query（body 显式声明时通常无需 params→query）
  const queryParams = o.body ? null : o.params;
  const qs = queryParams?new URLSearchParams(queryParams).toString():'';
  const fullUrl = qs?(url+(url.indexOf('?')>=0?'&':'?')+qs):url;
  let reqData=body;
  let reqHeaders={'Content-Type':'application/json'};
  if(useForm){
    let formEl=null;
    if(o.fromForm) formEl=(typeof o.fromForm==='string'?dyn.resolve(o.fromForm):ctx.element)||ctx.element;
    const f=dyn.serializeFormData(body,{rootKey:o.rootKey,antiForgery:o.antiForgery,formEl:formEl});
    if(f.hasFile){
      // 含文件：走 multipart/form-data，boundary 由浏览器自动生成
      const fd=new FormData();
      f.entries.forEach(function(pair){
        const v=pair[1];
        // Blob 判定用鸭子类型，避免跨 realm 的 instanceof 失效
        const isBlob=!!v&&typeof v==='object'&&typeof v.size==='number'&&typeof v.slice==='function';
        if(isBlob) fd.append(pair[0],v,v.name||'file');
        else fd.append(pair[0],String(v));
      });
      reqData=fd;
      reqHeaders={};
    }else{
      reqData=f.body;
      reqHeaders={'Content-Type':'application/x-www-form-urlencoded; charset=UTF-8'};
    }
  }
  try{
    if(!window.axios) throw new Error("Axios未加载");
    const ajax = await window.axios({
      method:'POST',
      url:fullUrl,
      headers:reqHeaders,
      data:reqData
    });
    const res = ajax.data;
    const failed = res.success===false||res.Success===false||(typeof res.code==='number'&&res.code!==0);
    if(failed){ dyn.showMessage(res.msg||res.Message||'操作失败','error'); throw new Error(res.msg||res.Message||'操作失败'); }
    //识别dyn-actions
    if(res&&Array.isArray(res[K.CONST.DATA_DYN_ACTIONS])){
      K.runJsonActions({actions:res[K.CONST.DATA_DYN_ACTIONS]},ctx.element);
    }
    if(o.reload) await dyn.reload(o.reload,{params:o.params});
    if(o.close) await dyn.close(ctx.element);
    if(o.message) dyn.showMessage(o.message,'success');
    return res;
  }catch(err){ dyn.showMessage('请求异常:'+err.message,'error'); throw err; }
});
K.defineAction('postdata',ctx=>K.actions.postback(ctx));
/** reload 刷新（双路分发）：目标（或其最近 [data-blk-role] 祖先）是 BlockApp 且句柄注册了 reload
 *  → handle.send('reload')；否则回退 dyn.reload 片段刷新。 */
K.defineAction('reload',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  let sel = K.firstTruthy(o,['selector','target']);
  if(sel==null&&o.value!==undefined) sel = o.value;
  let el = null;
  if(sel&&typeof sel==='string') el = dyn.resolve(sel);
  else if(sel&&sel.nodeType) el = sel;
  else el = ctx.element;
  if(el&&el.closest){
    const blk = (el.hasAttribute&&el.hasAttribute('data-blk-role')) ? el : el.closest('[data-blk-role]');
    const handle = blk&&blk.__dynBlock;
    if(handle&&typeof handle.send==='function'){
      try{ return await handle.send('reload'); }
      catch(e){ console.warn('[reload] block reload 失败，回退片段刷新',e); }
    }
  }
  return dyn.reload(el||ctx.element,o);
});
/** grid 表格动作（M4 三屏模板）：按 gridId 刷新注册到 window.DynGrids 的 DynTable
 *   dyn-click="grid('grid_student')" / 配置式：{"gridId":"grid_student","page":1,"reset":true} */
K.defineAction('grid',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  let id = K.firstTruthy(o,['gridId','target']);
  if(!id&&typeof o.value==='string'&&o.value) id = o.value;
  if(!id){ dyn.showMessage('[grid]缺少 gridId（如 grid(\'grid_xxx\')）','warning'); return false; }
  const g = (window.DynGrids||global.DynGrids) && (window.DynGrids||global.DynGrids)[id];
  if(!g){ console.warn('[grid]未找到已注册表格：'+id+'（检查 DynTable comoptions.gridId）'); return false; }
  if(o.reset && typeof g.resetFilter==='function') g.resetFilter();
  return g.refresh(o.page||1);
});
K.defineAction('load',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage("[load]缺少url",'error'); return; }
  const html = await dyn.fetchPartial(url,o.params||{},o.method||'POST');
  await dyn.html(ctx.element,html);
});
/** updateEl 局部刷新（原则2核心）：
 *  <button dyn-click="ActionHelper.Submit" data-dyn-target="#resultPanel" data-url="/Home/QueryBlock">查询</button>
 *  提交当前 scope model → Controller 返回 HTML 片段替换目标区块并重新 mount；
 *  若返回 ApiResult JSON，则执行其中 dyn-actions，data 为片段字符串时同样替换区块。 */
K.defineAction('updateel',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const targetSel = K.firstTruthy(o,['target','selector'])
    ||(ctx.element?ctx.element.getAttribute('data-dyn-target'):null);
  if(!targetSel){ dyn.showMessage('[updateEl]缺少 target 选择器（如 data-dyn-target="#resultPanel"）','warning'); return false; }
  const targetEl = dyn.resolve(targetSel);
  if(!targetEl){ dyn.showMessage('[updateEl]未找到目标元素：'+targetSel,'warning'); return false; }
  const url = o.url
    ||(ctx.element&&ctx.element.getAttribute('data-dyn-url'))
    ||(ctx.element&&ctx.element.getAttribute('data-url'))
    ||ctx.url;
  if(!url){ dyn.showMessage('[updateEl]缺少url参数','error'); return false; }
  if(!(await K.confirmIfNeeded(o,'确定执行该操作吗？'))) return false;
  let body = Object.assign({},dyn.deepClone(ctx.model||{}),o.params||{});
  // wrap：表单 model 包成 dyndata 统一契约 {table,data,gridId?}（旧管理页保存按钮零业务 JS 的统一写法）
  if(o.wrap && typeof o.wrap==='object' && o.wrap.table){
    body = K.wrapDyndata(body,o.wrap);
  }
  // 片段参数链：目标 pane 从【触发方】上下文 fork（o.params 是 L0 入参，不自动灌入请求之外的地方）
  const forkCtx = typeof dyn.params==='function' ? dyn.params(ctx.element) : null;
  const bootInfo = { parentCtx:forkCtx, local:o.params||{} };
  const text = await dyn.fetchPartial(url,body,o.method||'POST','text');
  let parsed = null;
  try{ parsed = JSON.parse(text); }catch(e){ parsed = null; }
  if(parsed&&typeof parsed==='object'){
    // ApiResult 失败（code 非 0）：明确提示，不执行 dyn-actions、不关闭弹窗
    if(parsed.code!==undefined&&parsed.code!==null&&Number(parsed.code)!==0){
      dyn.showMessage(parsed.msg||'操作失败','error');
      return parsed;
    }
    if(Array.isArray(parsed[K.CONST.DATA_DYN_ACTIONS])){
      K.runJsonActions({actions:parsed[K.CONST.DATA_DYN_ACTIONS]},targetEl);
    }
    if(typeof parsed.data==='string'&&parsed.data.indexOf('<')>=0){
      return await dyn.html(targetEl,parsed.data,true,bootInfo);
    }
    return parsed;
  }
  return await dyn.html(targetEl,text,true,bootInfo);
});
// 语义别名：Submit=提交并刷新区块；ReloadTarget=刷新指定区块
K.defineAction('submit',ctx=>K.actions.updateel(ctx));
K.defineAction('reloadtarget',ctx=>K.actions.updateel(ctx));
/** open 弹窗（原则4：统一 layui layer）
 *   mode:'iframe'（或 openwindow）→ layer type:2 直接打开 MVC 页面
 *   默认 mode:'fragment'          → 拉 HTML 片段注入 layer，自动 dyn.mount（dyn-init 动作生效）
 *   onopen/onclose 动作钩子；reloadSelf 关闭后刷新触发容器 */
K.defineAction('open',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const url = o.url||ctx.url;
  if(!url){ dyn.showMessage("[open]缺少url",'error'); return; }
  const triggerEl = ctx.element;
  const layer = await dyn.getLayer();
  const onEnd = async ()=>{
    await K._runEvents(K.normActionSteps(o.onclose),Object.assign({},ctx,{}));
    if(o.reloadSelf){
      const host = dyn.findAncestor(triggerEl,'[data-dyn-url]')||triggerEl;
      await dyn.reload(host);
    }
  };
  if(layer){
    // 基础默认配置，外部o中的同名字段会覆盖这里
    const baseOpt = {
      title: '对话框',
      shadeClose: false,
      maxmin:true,
    };
    // 合并：用户options覆盖默认
    const layerOpt = Object.assign({}, baseOpt, o);
    // area 处理：优先使用o.area；没有则用 width/height 生成
    if(!layerOpt.area){
      layerOpt.area = [layerOpt.width || '60%', layerOpt.height || '80%'];
    }
    if(o.mode==='iframe'||o.iframe===true){
      // iframe 固定 type=2，强制覆盖，不可外部修改
      const idx = layer.open({
        ...layerOpt,
        type:2,
        content:url,
        end:()=>{ onEnd().catch(e=>console.error('[open onEnd]',e)); }
      });
      if(o.max){ try{ layer.full(idx); }catch(e){} } // 打开即最大化
      return { index:idx };
    }
    // fragment type:1
    const holder = document.createElement('div');
    holder.className='dyn-modal-host dyn-layer-fragment';
    holder.style.padding='0px';
    holder.style.display='none';
    document.body.appendChild(holder);
    // 弹窗片段继承调用方参数链：holder fork 到触发上下文（o.params 为 L0 入参层），
    // 片段内 Block/App 无需关心 URL query，按 dyn.params(el).get() 同一约定取值。
    if(typeof dyn.params==='function'){
      try{ dyn.params(triggerEl).fork(holder, o.params||{}); }catch(e){}
    }
    const holderContent = (global.layui && global.layui.$) ? global.layui.$(holder) : holder;
    const idx = layer.open({
      ...layerOpt,
      type:1, // fragment固定type=1，强制覆盖
      content:holderContent,
      success:async ()=>{
        try{
          holder.innerHTML = '<div style="padding:16px;color:#909399;">加载中…</div>';
          const html = await dyn.fetchPartial(url,o.params||{},o.method||'GET');
          K.dyn.html(holder,html);
          // 权限上下文透传：inheritResource=true 时，从触发源向上找 resourceKey，写到弹窗根节点
          if(o.inheritResource && typeof window.DynPermission==='object' && window.DynPermission.findResourceKey){
            const rk = window.DynPermission.findResourceKey(triggerEl);
            if(rk){
              holder.setAttribute('data-block-resource', rk);
              if(typeof window.DynPermission.scan==='function') window.DynPermission.scan(holder);
            }
          }
          await K._runEvents(K.normActionSteps(o.onopen),Object.assign({},ctx,{holder}));
        }catch(e){
          holder.innerHTML = '<div style="padding:16px;color:#f56c6c;">加载失败：'+(e.message||e)+'</div>';
        }
      },
      end:()=>{ 
        try{dyn.unmount(holder);}catch(e){} 
        try{holder.remove();}catch(e){} 
        onEnd().catch(e=>console.error('[open onEnd]',e)); 
      }
    });
    if(o.max){ try{ layer.full(idx); }catch(e){} } // 打开即最大化
    return { index:idx, holder };
  }
  // layer 不可用降级
  global.open(url,'_blank');
  return null;
});
/** openWindow：以 iframe 方式打开完整 MVC 页面（V1 同名动作） */
K.defineAction('openwindow',ctx=>K.actions.open(Object.assign({},ctx,{options:Object.assign({mode:'iframe'},ctx.options||{})})));
/** close/closeWindow：关闭最近 layer；在 iframe 子页内调用则关闭父窗 layer */
K.defineAction('close',async ctx=>{
  const dyn = K.dyn;
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
K.defineAction('closewindow',ctx=>K.actions.close(ctx));
/** setwin：设置当前 layer 窗口的标题/尺寸/位置。
 *  仅当触发元素位于窗口容器(.layui-layer)内才生效，不在窗口内则静默返回、不做任何事。
 *  用法（点击管道）：dyn-click="ActionHelper.setwin|{\"title\":\"编辑桌面快捷方式\",\"width\":\"700px\",\"height\":\"520px\"}"
 *  支持：title / width / height / top / left / right / bottom 直传；css 对象可再透传更多样式。
 *  （区别于 setwindow：那是 iframe 间 postMessage 投递，用于父子窗口通信。） */
K.defineAction('setwin',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const start = ctx.element;
  // 1) layui layer 窗口
  const layero = start&&start.closest?start.closest('.layui-layer'):null;
  if(layero){
    const layer = await dyn.getLayer();
    if(!layer) return { changed:false, reason:'no-layer' };
    const times = layero.getAttribute('times');
    if(times==null||times==='') return { changed:false, reason:'no-times' };
    // 标题
    if(o.title!==undefined&&o.title!==null&&o.title!==''){
      try{ layer.title(String(o.title),times); }catch(e){ console.error('[setwin title]',e); }
    }
    // 尺寸/位置：width/height/top/left/right/bottom 直传，css 对象可再合并
    const style = {};
    ['width','height','top','left','right','bottom'].forEach(k=>{
      if(o[k]!==undefined&&o[k]!==null&&o[k]!=='') style[k]=o[k];
    });
    if(o.css&&typeof o.css==='object') Object.assign(style,o.css);
    if(Object.keys(style).length){
      try{ layer.style(times,style); }catch(e){ console.error('[setwin style]',e); }
    }
    return { changed:true, kind:'layer', times:times };
  }
  // 2) Element Plus / Element UI el-dialog（append-to-body 后容器在 body 下）
  const dlgHost = start&&start.closest?start.closest('.el-dialog, .el-dialog__wrapper'):null;
  if(dlgHost){
    const dialog = dlgHost.classList.contains('el-dialog')?dlgHost:dlgHost.querySelector('.el-dialog');
    if(dialog){
      // 标题
      if(o.title!==undefined&&o.title!==null&&o.title!==''){
        const t = dialog.querySelector('.el-dialog__title');
        if(t) t.textContent = String(o.title);
      }
      if(o.width){
        dialog.style.width = o.width;
        // Element Plus 的 width 由 CSS 变量 --el-dialog-width 控制，须同步覆盖，否则 style.width 无效
        dialog.style.setProperty('--el-dialog-width', o.width);
      }
      if(o.height){
        // 固定高 + body 自适应滚动：纵向 flex 布局
        dialog.style.height = o.height;
        dialog.style.display = 'flex';
        dialog.style.flexDirection = 'column';
        const body = dialog.querySelector('.el-dialog__body');
        if(body){ body.style.flex = '1 1 auto'; body.style.minHeight = '0'; body.style.overflow = 'auto'; }
        const footer = dialog.querySelector('.el-dialog__footer');
        if(footer){ footer.style.flex = '0 0 auto'; }
      }
      if(o.top!=null) dialog.style.marginTop = o.top;
      if(o.left!=null) dialog.style.marginLeft = o.left;
      if(o.css&&typeof o.css==='object') Object.assign(dialog.style,o.css);
      return { changed:true, kind:'el-dialog' };
    }
  }
  // 不在任何窗口容器内：静默返回
  return { changed:false, reason:'not-in-window' };
});
K.defineAction('toast',ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  let msg = K.firstTruthy(o,['message','msg']);
  if(msg==null&&o.value!==undefined) msg = String(o.value);
  if(!msg) return;
  dyn.showMessage(msg,o.type||'success');
});
// V1 服务端动作名 showmessage 与 toast 等价
K.defineAction('showmessage',ctx=>K.actions.toast(ctx));
K.defineAction('notify',ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  if(!global.ElementPlus||!global.ElementPlus.ElNotification) return dyn.showMessage(o.message);
  global.ElementPlus.ElNotification({
    title:o.title||'',message:o.message||'',type:o.type||'success',duration:Number(o.duration||3000)
  });
});
/** setVueModel 写 Vue 模型（兼容旧用法 + 跨 app 编排）：
 *  旧用法：{modelName/path, value} → 写触发元素自身 scope model。
 *  跨 app：target 目标元素选择器（省略=触发方）；from 源元素选择器；fromPath 源 proxy 取值路径；
 *         fromMethod 源 proxy 无参方法（优先级高于 fromPath）。 */
K.defineAction('setVueModel',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const modelName = K.firstTruthy(o,['modelName','path']);
  if(!modelName){
    dyn.showMessage('setVueModel：path/modelName不能为空','warning');
    return null;
  }
  const delay = Number(K.firstTruthy(o,['settimeout','delay'])||0);
  // —— 目标 proxy：target 指定跨 app 元素；省略维持旧行为（写触发方 scope model）——
  let targetProxy = ctx.model;
  if(o.target){
    const tel = dyn.resolve(o.target);
    if(!tel){ dyn.showMessage('setVueModel：未找到目标元素 '+o.target,'warning'); return null; }
    targetProxy = (typeof dyn.getProxy==='function'&&dyn.getProxy(tel))||null;
  }
  if(!targetProxy){
    dyn.showMessage('setVueModel：未获取到model(scope)','warning');
    return null;
  }
  // —— 源值：from/fromPath/fromMethod 跨 app 取；否则沿用旧行为 value（支持 {{params}} 占位）——
  let realVal;
  if(o.from!==undefined||o.fromPath||o.fromMethod){
    const srcEl = (o.from ? dyn.resolve(o.from) : null)||ctx.element;
    if(!srcEl){ dyn.showMessage('setVueModel：未找到源元素 '+(o.from||''),'warning'); return null; }
    const srcProxy = (typeof dyn.getProxy==='function'&&dyn.getProxy(srcEl))||null;
    if(!srcProxy){ dyn.showMessage('setVueModel：源元素上无Vue实例','warning'); return null; }
    if(o.fromMethod){
      const fn = dyn.getByPath(srcProxy,o.fromMethod);
      if(typeof fn!=='function'){ dyn.showMessage('setVueModel：源方法不存在 '+o.fromMethod,'warning'); return null; }
      realVal = fn.call(srcProxy);
    }else if(o.fromPath){
      realVal = dyn.getByPath(srcProxy,o.fromPath);
    }else{
      realVal = srcProxy;
    }
  }else{
    realVal = o.value;
    if(typeof realVal === 'string') realVal = K.applyTpl(realVal, ctx.params);
  }
  try{
    if(delay>0){
      await new Promise(resolve=>setTimeout(resolve,delay));
    }
    // clone:true 写入深拷贝（如"重置回基线"场景，避免后续编辑反向污染源模型）
    if(o.clone&&realVal&&typeof realVal==='object') realVal = dyn.deepClone(realVal);
    dyn.setPathVal(targetProxy,modelName,realVal);
    return realVal;
  }catch(err){
    dyn.showMessage(`setVueModel执行异常:${err.message}`,'error');
    return null;
  }
});
K.defineAction('setvar',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const modelName = K.firstTruthy(o,['modelName','path']);
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
    v = K.applyTpl(v, ctx.params);
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
/** calc 安全表达式运算动作，不使用eval/new Function，仅支持简单赋值运算：
 *  count +=1 / count -=1 / count *=2 / count /=2；total = price * num / diff = a - b。
 *  变量名仅限model内部字段，支持点路径 user.score += 5 */
K.defineAction('calc',async ctx=>{
  const dyn = K.dyn;
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
K.defineAction('evaljs',async ctx=>{
  const dyn = K.dyn;
  if(K.config.disableEvalJs){ dyn.showMessage("evaljs已全局关闭",'warning'); return; }
  const code = K.firstTruthy(ctx.options,['js','code'])||ctx.options;
  if(!code) return;
  try{
    const fn = new Function('ctx','$bus','return ('+code+')');
    return fn(ctx,K.bus);
  }catch(e){ dyn.showMessage("evaljs执行异常:"+e.message,'error'); }
});
/** setparams：统一参数上下文写值。
 *  scope:'shared'（默认）→ ctx.commit，链式各 fork/兄弟 tab 立即可见；scope:'local' → ctx.set，仅本上下文及子孙可见。
 *  保留键：scope/params/key/value；单键写法 {key:'courseId', value:27}。 */
K.defineAction('setparams',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const pctx = typeof dyn.params==='function' ? dyn.params(ctx.element) : null;
  if(!pctx){ console.warn('[setparams] 参数上下文不可用'); return false; }
  const RESERVED = ['scope','params','key','value','__elParams'];
  const elParams = o.__elParams || {};
  // o.params 中排除 buildCtx 自动收集的元素 data-* 键（data-v-app/data-trae-ref 等），
  // 剩下的才是用户在动作配置里显式声明的 params
  const explicitPairs = {};
  let hasExplicit = false;
  if(o.params && typeof o.params==='object'){
    Object.keys(o.params).forEach(k=>{
      if(!(k in elParams)){ explicitPairs[k]=o.params[k]; hasExplicit=true; }
    });
  }
  const pairs = Object.assign({},
    o.key ? {[o.key]:o.value} : null,
    hasExplicit ? explicitPairs : null
  );
  // 平铺形式：既没有 key，也没有显式 params 对象时，保留字以外的 options 键全部作为参数
  if(!o.key && !hasExplicit){
    Object.keys(o).forEach(k=>{ if(RESERVED.indexOf(k)<0) pairs[k]=o[k]; });
  }
  const scope = o.scope==='local' ? 'local' : 'shared';
  Object.keys(pairs).forEach(k=>{
    if(scope==='local') pctx.set(k,pairs[k]);
    else pctx.commit(k,pairs[k],{by:'setparams:'+ctx.action});
  });
  return pairs;
});
K.defineAction('setattr',ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const el = o.selector?document.querySelector(o.selector):ctx.element;
  if(!el){ dyn.showMessage("setattr未找到元素",'warning'); return; }
  if(o.attr) Object.keys(o.attr).forEach(k=>el.setAttribute(k,o.attr[k]));
  if(o.style) Object.keys(o.style).forEach(k=>el.style[k]=o.style[k]);
  if(o.text!==undefined) el.textContent = o.text;
  if(o.html!==undefined) el.innerHTML = o.html;
});
/** setDynCom：运行时修改某个 dyn 组件的 jsonconfig（响应式，视图立即刷新）
 *   options: { selector:'#com1', path:'options.comoptions.disabled', value:true } 或 patch 整体合并 */
K.defineAction('setdyncom',ctx=>{
  const dyn = K.dyn;
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
/** setWindow：向 layer iframe 弹窗内的页面投递数据（postMessage）；
 * 也支持在 iframe 子页面内向父窗口回传。子页面可监听 window 的 "message" 事件消费 {type:'dyn-setwindow'} */
K.defineAction('setwindow',ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const payload = { type:'dyn-setwindow', path:o.path, value:o.value, data:o.data };
  const iframe = o.selector?document.querySelector(o.selector):document.querySelector('.layui-layer-iframe iframe');
  if(iframe&&iframe.contentWindow){ iframe.contentWindow.postMessage(payload,'*'); return true; }
  if(window.parent&&window.parent!==window){ window.parent.postMessage(payload,'*'); return true; }
  dyn.showMessage('setwindow：未找到可投递的窗口','warning');
});
K.defineAction('copy',async ctx=>{
  const dyn = K.dyn;
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
K.defineAction('download',ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  const url = o.url||(ctx.element?(ctx.element.href||ctx.element.getAttribute('data-url')):'');
  if(!url){ dyn.showMessage("download缺少url",'warning'); return; }
  const a = document.createElement('a');
  a.href=url; a.download=K.firstTruthy(o,['filename','name'])||'';
  document.body.appendChild(a); a.click(); document.body.removeChild(a);
});
K.defineAction('confirm',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  return await dyn.confirmAsync(K.firstTruthy(o,['message','msg'])||'确定执行？');
});
K.defineAction('delay',async ctx=>{
  const ms = Number(K.firstDefined(ctx.options,['ms','delay'],300));
  return new Promise(resolve=>setTimeout(resolve,ms));
});
K.defineAction('chain',async ctx=>{
  const dyn = K.dyn;
  let steps = ctx.options.steps;
  if(Array.isArray(ctx.options)) steps = ctx.options;
  if(!Array.isArray(steps)){ dyn.showMessage(K.CONST.MSG_CHAIN_NEED_STEPS,'warning'); return; }
  //防死循环检测
  if(ctx.$callStack.includes(ctx.action+JSON.stringify(steps))){
    dyn.showMessage(K.CONST.MSG_CHAIN_DEAD_LOOP,'error');
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
    const fn = K.resolveAction(actName);
    if(!fn){ dyn.showMessage(K.CONST.MSG_ACTION_NOT_FOUND.replace('{name}',actName),'error'); return run(i+1); }
    const subCtx = K.buildCtx(ctx.element,'chain',ctx.$event,opt,actName);
    subCtx.$step = i; subCtx.$result = last; subCtx.$chain = steps;
    K.resolveCtxTokens(subCtx); // $result 已就绪，重解析 "$result.xxx" 占位
    subCtx.$callStack = [...ctx.$callStack];
    const r = await fn(subCtx);
    if(r===false) return last;
    last = r;
    return run(i+1);
  };
  return await run(0);
});
K.defineAction('switch',async ctx=>{
  const dyn = K.dyn;
  const o = ctx.options||{};
  let value;
  const expr = K.firstTruthy(o,['expr','expression'])||'';
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
    const subCtx = K.buildCtx(ctx.element,'switch',ctx.$event,Array.isArray(hit.chain)?{steps:hit.chain}:hit.chain,'chain');
    return await K.actions.chain(subCtx);
  }
  if(!hit.action) return null;
  const fn = K.resolveAction(hit.action);
  if(!fn){ dyn.showMessage("switch动作不存在:"+hit.action,'error'); return null; }
  const subCtx = K.buildCtx(ctx.element,'switch',ctx.$event,hit.options||{},hit.action);
  return await fn(subCtx);
});
K.defineAction('triggerevent',ctx=>{
  const o = ctx.options||{};
  const name = K.firstTruthy(o,['name','event']);
  if(!name) return;
  const payload = K.firstTruthy(o,['payload','params'])||{};
  if(o.type==='window'||o.window){
    window.dispatchEvent(new CustomEvent(name,{detail:payload}));
  }else{
    K.bus.emit(name,payload);
  }
});
K.defineAction('redirect',ctx=>{
  const o = ctx.options||{};
  const url = K.firstTruthy(o,['url','href']);
  if(url) window.location.href = url;
});
})(window);
