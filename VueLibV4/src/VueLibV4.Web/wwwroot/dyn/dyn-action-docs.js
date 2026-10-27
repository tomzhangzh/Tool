/* dyn-action-docs.js 内置动作文档元数据
 * 职责：ACTION_DOCS（label/group/doc/options/example/aliasOf/needEl/demo）定义，
 *      并在加载期合并进 K.meta（动作注册表由 dyn-action-builtins.js 同步注册完毕）。
 * 依赖：dyn-action.js（K.meta）；本文件不注册 DynKernel 插件。
 * 约定：只允许向 K 追加/读取属性；参数表统一为「主名 name + 别名 aliases[]」结构，
 *       文档键 setVueModel 等驼峰名合并时按小写查找。
 */
(function(global){
'use strict';
var K = global.DynActionKit;

/**
 * 每项字段：
 *   label    中文名
 *   group    分组（数据与刷新 / 窗口与弹层 / 表单与状态 / 提示与交互 / 控制流）
 *   doc      一句话说明
 *   options  参数表 [{name,aliases,type,desc}]（aliases 可省略）
 *   example  典型写法（管道 / DOM 属性）
 *   aliasOf  语义别名指向的动作名
 *   needEl   依赖触发元素上下文
 *   demo     可在 Demo 页直接试跑的 options（不含则只能复制示例）
 *   demoCtx  试跑时附带的最小 ctx（如 {model:{...}}）
 */
const ACTION_DOCS = {
  createapp: { label:'创建 Vue 应用', group:'数据与刷新',
    doc:'给元素打上 createApp 模式，按 url 拉取 JSON 配置并挂载 Vue 应用（页面/区块启动用）。',
    options:[{name:'url',type:'string',desc:'配置地址；缺省读元素 data-dyn-url'}],
    example:'dyn-init="CreateApp" 或 CreateApp({\'url\':\'/api/component/cfg\'})', needEl:true },

  postback: { label:'提交回传', group:'数据与刷新',
    doc:'POST 到 url，处理 ApiResult 与返回的 dyn-actions；支持确认、请求体模板、成功后刷新/关闭/提示。',
    options:[
      {name:'url',type:'string',desc:'提交地址'},
      {name:'confirm',type:'bool|string',desc:'提交前确认，字符串为提示语'},
      {name:'body',type:'object',desc:'显式请求体模板（支持 {{Id}} 占位）'},
      {name:'wrap',type:'object',desc:'把 model 包成 {table,data} 契约'},
      {name:'params',type:'object',desc:'query 参数（无 body 时）'},
      {name:'reload',type:'string',desc:'成功后刷新目标选择器'},
      {name:'close',type:'bool',desc:'成功后关闭所在弹窗'},
      {name:'message',type:'string',desc:'成功后提示语'}],
    example:'Postback({\'url\':\'/api/x/save\',\'confirm\':\'确定保存？\',\'close\':true,\'reload\':\'#grid\'})' },

  postdata: { label:'提交回传（别名）', group:'数据与刷新', aliasOf:'postback', doc:'与 postback 完全等价。' },

  reload: { label:'刷新区块', group:'数据与刷新',
    doc:'目标是 BlockApp 且句柄支持 reload → 走 handle.send(\'reload\')；否则按 data-dyn-url 做片段刷新。',
    options:[
      {name:'selector',aliases:['target'],type:'string',desc:'刷新目标，缺省为触发元素自身'},
      {name:'value',type:'string',desc:'reload(\'#id\') 单参写法'}],
    example:'Reload  或  Reload({\'selector\':\'#panel\'})' },

  grid: { label:'刷新表格', group:'数据与刷新',
    doc:'按 gridId 找到 window.DynGrids 中注册的 DynTable 并刷新（可先重置筛选）。',
    options:[
      {name:'gridId',type:'string',desc:'表格 id（comoptions.gridId）'},
      {name:'page',type:'number',desc:'刷新到第几页'},
      {name:'reset',type:'bool',desc:'先清空筛选区'}],
    example:'Grid({\'gridId\':\'grid_student\',\'reset\':true})' },

  load: { label:'加载片段', group:'数据与刷新',
    doc:'拉取 HTML 片段并替换到触发元素内部（随后自动 mount）。',
    options:[
      {name:'url',type:'string',desc:'片段地址'},
      {name:'params',type:'object',desc:'请求参数'},
      {name:'method',type:'string',desc:'默认 POST'}],
    example:'Load({\'url\':\'/Home/QueryBlock\'})' },

  updateel: { label:'局部刷新（提交）', group:'数据与刷新',
    doc:'提交当前 scope model 到 Controller：返回 HTML 即替换目标区块；返回 ApiResult 则执行其中 dyn-actions。',
    options:[
      {name:'target',aliases:['selector'],type:'string',desc:'目标选择器，缺省读 data-dyn-target'},
      {name:'url',type:'string',desc:'提交地址，缺省读 data-dyn-url'},
      {name:'confirm',type:'bool|string',desc:'提交前确认'},
      {name:'wrap',type:'object',desc:'model 包成 {table,data,gridId}'},
      {name:'params',type:'object',desc:'附加参数'}],
    example:'dyn-click="Submit" data-dyn-target="#panel" data-dyn-url="/x/save"' },

  submit: { label:'提交并刷新（别名）', group:'数据与刷新', aliasOf:'updateel', doc:'与 updateel 等价。' },

  reloadtarget: { label:'刷新目标区块（别名）', group:'数据与刷新', aliasOf:'updateel', doc:'与 updateel 等价。' },

  open: { label:'打开弹窗', group:'窗口与弹层',
    doc:'统一 layui layer 弹窗：默认拉片段注入并自动 mount；mode=iframe 直接打开 MVC 页面。',
    options:[
      {name:'url',type:'string',desc:'片段或页面地址'},
      {name:'mode',type:'string',desc:'fragment（默认）| iframe'},
      {name:'title',type:'string',desc:'标题'},
      {name:'width',aliases:['height'],type:'string',desc:'尺寸'},
      {name:'onopen',aliases:['onclose'],type:'array',desc:'打开/关闭钩子动作链'},
      {name:'reloadSelf',type:'bool',desc:'关闭后刷新触发容器'}],
    example:'Open({\'url\':\'/Detail/Edit?id=1\',\'title\':\'编辑\',\'width\':\'800px\'})', needEl:true },

  openwindow: { label:'打开 iframe 窗口（别名）', group:'窗口与弹层', aliasOf:'open', doc:'等价于 mode=iframe 的 open。' },

  close: { label:'关闭弹窗', group:'窗口与弹层',
    doc:'关闭触发元素所在 layer / iframe 弹窗；不在弹窗内时关闭全部。',
    example:'Close' },

  closewindow: { label:'关闭弹窗（别名）', group:'窗口与弹层', aliasOf:'close', doc:'与 close 等价。' },

  setwin: { label:'设置窗口标题尺寸', group:'窗口与弹层',
    doc:'仅对所在窗口生效：修改 layer 或 el-dialog 的 title/width/height/位置，不在窗口内静默返回。',
    options:[
      {name:'title',type:'string',desc:'标题'},
      {name:'width',aliases:['height'],type:'string',desc:'尺寸'},
      {name:'top',aliases:['left','right','bottom'],type:'string',desc:'位置'},
      {name:'css',type:'object',desc:'额外样式'}],
    example:'SetWin({\'title\':\'编辑\',\'width\':\'700px\'})', needEl:true },

  setwindow: { label:'iframe 消息投递', group:'窗口与弹层',
    doc:'向 layer iframe 子页 postMessage（{type:\'dyn-setwindow\',path,value,data}）；子页也可向父窗口回传。',
    options:[
      {name:'selector',type:'string',desc:'iframe 选择器，缺省 .layui-layer-iframe iframe'},
      {name:'path',type:'string',desc:'子页内数据路径'},
      {name:'value',aliases:['data'],type:'any',desc:'投递值'}],
    example:'SetWindow({\'path\':\'row\',\'value\':{\'Id\':1}})' },

  setVueModel: { label:'写 Vue 模型', group:'表单与状态',
    doc:'写触发元素 scope 的 model；支持 target/from/fromPath/fromMethod 做跨 App 编排（如把 filter 的查询条件推给 list）。',
    options:[
      {name:'path',aliases:['modelName'],type:'string',desc:'目标模型路径，如 filterModel'},
      {name:'value',type:'any',desc:'直接值（支持 {{params}} 占位）'},
      {name:'target',type:'string',desc:'目标元素选择器（跨 App）'},
      {name:'from',aliases:['fromPath','fromMethod'],type:'string',desc:'源元素/源取值路径/源方法'},
      {name:'clone',type:'bool',desc:'写入深拷贝（回填基线）'},
      {name:'settimeout',aliases:['delay'],type:'number',desc:'延迟毫秒'}],
    example:'SetVueModel({\'target\':\'#m\',\'path\':\'model.filter\',\'fromMethod\':\'buildSubmit\'})' },

  setvar: { label:'写当前模型变量', group:'表单与状态',
    doc:'写当前 scope model 的指定字段，值支持 {{params}} 占位与延迟。',
    options:[
      {name:'path',aliases:['modelName'],type:'string',desc:'字段路径，支持 a.b.c'},
      {name:'value',type:'any',desc:'写入值'},
      {name:'delay',type:'number',desc:'延迟毫秒'}],
    example:'SetVar({\'path\':\'count\',\'value\':1})',
    demo:{ path:'count', value: 1 }, demoCtx:{ model:{ count:0 } } },

  calc: { label:'安全运算', group:'表单与状态',
    doc:'不使用 eval：支持 a+=1 / a-=1 / a*=2 / a/=2 与 total = price * num 这类单步赋值运算。',
    options:[{name:'expr',type:'string',desc:'运算表达式'}],
    example:'Calc({\'expr\':\'total = price * num\'})',
    demo:{ expr:'total = price * num' }, demoCtx:{ model:{ price:10, num:3 } } },

  setattr: { label:'改元素属性', group:'表单与状态',
    doc:'按选择器或触发元素修改 attr/style/text/html。',
    options:[
      {name:'selector',type:'string',desc:'目标元素'},
      {name:'attr',type:'object',desc:'批量设置属性'},
      {name:'style',type:'object',desc:'批量设置样式'},
      {name:'text',aliases:['html'],type:'string',desc:'替换文本/HTML'}],
    example:'SetAttr({\'selector\':\'#tip\',\'text\':\'已加载\'})' },

  setdyncom: { label:'改组件配置', group:'表单与状态',
    doc:'运行时改某个 dyn 组件的 jsonconfig（响应式即时生效），可改单路径或整体 patch。',
    options:[
      {name:'selector',type:'string',desc:'组件元素'},
      {name:'path',type:'string',desc:'如 options.comoptions.disabled'},
      {name:'value',type:'any',desc:'新值'},
      {name:'patch',type:'object',desc:'整体合并对象'}],
    example:'SetDynCom({\'selector\':\'#com1\',\'path\':\'options.comoptions.disabled\',\'value\':true})' },

  setparams: { label:'写参数上下文', group:'表单与状态',
    doc:'写共享/局部参数上下文。scope=shared（默认）链式各 tab 立即可见；local 仅本上下文与子孙。',
    options:[
      {name:'scope',type:'string',desc:'shared（默认）| local'},
      {name:'params',type:'object',desc:'显式参数包'},
      {name:'key',aliases:['value'],type:'string|any',desc:'单键写法'}],
    example:'SetParams({\'courseId\':\'$result.data.Id\'}) 或 {"scope":"local","params":{"rowId":27}}' },

  toast: { label:'消息提示', group:'提示与交互',
    doc:'轻提示（layer.msg / ElMessage）。',
    options:[
      {name:'message',aliases:['msg','value'],type:'string',desc:'提示内容'},
      {name:'type',type:'string',desc:'success | error | warning'}],
    example:'Toast(\'保存成功\') 或 Toast({\'message\':\'失败\',\'type\':\'error\'})',
    demo:{ message:'来自内置动作 toast 的提示', type:'success' } },

  showmessage: { label:'消息提示（别名）', group:'提示与交互', aliasOf:'toast', doc:'与 toast 等价（V1 服务端动作名）。' },

  notify: { label:'通知提醒', group:'提示与交互',
    doc:'ElementPlus 右上角 ElNotification。',
    options:[
      {name:'title',type:'string',desc:'标题'},
      {name:'message',type:'string',desc:'内容'},
      {name:'type',type:'string',desc:'success | warning | info | error'},
      {name:'duration',type:'number',desc:'停留毫秒，默认 3000'}],
    example:'Notify({\'title\':\'提示\',\'message\':\'已保存\',\'type\':\'success\'})',
    demo:{ title:'内置动作 notify', message:'这是一条通知', type:'success', duration:2500 } },

  confirm: { label:'确认框', group:'提示与交互',
    doc:'弹确认框，返回 Promise<boolean>（可作为管道步骤，返回 false 会中止后续步骤）。',
    options:[{name:'message',aliases:['msg'],type:'string',desc:'确认文案'}],
    example:'Confirm(\'确定删除？\')|Postback(...)',
    demo:{ message:'这是 confirm 的确认框，点确定看返回结果' } },

  copy: { label:'复制文本', group:'提示与交互',
    doc:'复制给定文本（无 text 时取元素 value/textContent），失败降级 execCommand。',
    options:[{name:'text',type:'string',desc:'要复制的文本'}],
    example:'Copy({\'text\':\'hello\'})',
    demo:{ text:'VueLibV4 action demo' } },

  download: { label:'下载文件', group:'提示与交互',
    doc:'创建临时 a 标签触发下载。',
    options:[
      {name:'url',type:'string',desc:'文件地址'},
      {name:'filename',aliases:['name'],type:'string',desc:'保存文件名'}],
    example:'Download({\'url\':\'/export/x.xlsx\',\'filename\':\'导出.xlsx\'})' },

  delay: { label:'延迟', group:'控制流',
    doc:'等待若干毫秒，常用于管道中串联动画/节流。',
    options:[{name:'ms',aliases:['delay'],type:'number',desc:'毫秒，默认 300'}],
    example:'Delay(500)|Toast(\'稍后提示\')',
    demo:{ ms:500 } },

  chain: { label:'动作链', group:'控制流',
    doc:'顺序执行多个子动作，某步返回 false 即中止；自动防死循环。',
    options:[{name:'steps',type:'array',desc:'[{action,options}]'}],
    example:'Chain({\'steps\':[{\'action\':\'toast\',\'options\':{\'message\':\'第一步\'}}]})',
    demo:{ steps:[{action:'toast',options:{message:'动作链第 1 步',type:'info'}},{action:'toast',options:{message:'动作链第 2 步 ✅',type:'success'}}] } },

  switch: { label:'条件分支', group:'控制流',
    doc:'按表达式/值匹配 cases，命中则执行其 action；无命中走 default。',
    options:[
      {name:'expr',type:'string',desc:'表达式，如 model.type / params.x'},
      {name:'value',type:'any',desc:'直接比对值'},
      {name:'cases',type:'array',desc:'[{value,action,options}]'},
      {name:'default',type:'object',desc:'兜底分支'}],
    example:'Switch({\'expr\':\'type\',\'cases\':[{\'value\':\'a\',\'action\':\'toast\',\'options\':{\'message\':\'A\'}}]})',
    demo:{ expr:'type', cases:[{value:'a',action:'toast',options:{message:'命中分支 a'}}], default:{action:'toast',options:{message:'走 default'}} },
    demoCtx:{ model:{ type:'a' } } },

  triggerevent: { label:'触发事件', group:'控制流',
    doc:'向内部事件总线（或 window 自定义事件）广播消息，供其他组件订阅。',
    options:[
      {name:'name',aliases:['event'],type:'string',desc:'事件名'},
      {name:'payload',aliases:['params'],type:'object',desc:'负载'},
      {name:'type',type:'string',desc:'window → 用 CustomEvent 广播'}],
    example:'TriggerEvent({\'name\':\'refresh\',\'payload\':{\'Id\':1}})' },

  evaljs: { label:'执行 JS 表达式', group:'控制流',
    doc:'执行一段 JS 表达式（可用 ctx / $bus）。可按宿主 dyn 配置 disableEvalJs 全局关闭。',
    options:[{name:'js',aliases:['code'],type:'string',desc:'表达式，如 1+2'}],
    example:'EvalJs({\'js\':\'ctx.model.count+1\'})' },

  redirect: { label:'页面跳转', group:'控制流',
    doc:'整页跳转到指定地址（会离开当前页面）。',
    options:[{name:'url',aliases:['href'],type:'string',desc:'目标地址'}],
    example:'Redirect({\'url\':\'/Platform/Shell/Desktop\'})' }
};

// 文档合并进动作元信息：actionList() 输出的就是"实现 + 说明"的完整清单
Object.keys(ACTION_DOCS).forEach(function(name){
  // 动作注册表键统一小写，文档键允许驼峰（如 setVueModel），按小写查找
  const m = K.meta[String(name).toLowerCase()];
  if(!m) return;
  const d = ACTION_DOCS[name];
  m.label = d.label || m.label;
  m.group = d.group || '其他';
  m.doc = d.doc || '';
  m.options = d.options || [];
  m.example = d.example || '';
  m.aliasOf = d.aliasOf || null;
  m.needEl = !!d.needEl;
  m.demo = d.demo || null;
  m.demoCtx = d.demoCtx || null;
  m.source = 'builtin';
});

K.ACTION_DOCS = ACTION_DOCS;
})(window);
