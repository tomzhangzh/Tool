# dyn-lib.js API 文档

> 最后更新：2026-09-03（对齐当前 dyn-lib.js：actionHelper 约定式、属性驱动、零注册零标记）

## 概述

`dyn-lib.js` 是 VueLib 低代码平台的运行时库（Vue3 UMD + jQuery + lodash + Element Plus），提供：

- **属性驱动的事件委托**：`dyn-{event}-{action}` 属性自动绑定事件，事件由属性名决定（参考 common.js 的 `t-{event}-{fn}` 模式）
- **约定式动作注册**：`dyn.actionHelper.xxx = fn` 挂方法即动作，**无需 registerAction / registerInitAction / _init 标记**
- **统一上下文**：动作函数接收 `ctx` 对象（element / model / options / params / url / vm 等）
- **初始化动作**：`dyn-init-{action}`（或旧式 `dyn-{action}-init`）在页面/Vue 初始化或 innerHTML 更新（init/initAll）时自动执行
- **reload / updateEl**：容器局部刷新，三层参数合并（固定参数 `__dynCfg.params` → Vue model → form 序列化）
- **setVueModel**：设置 Vue model 值（支持 TargetEl / 数组下标 / lodash `_.set`）
- **祖先查找工具**：`findAncestor` / `closestDynInit` / `closestDataUrl`（替代各处 closest 重复）

## 动作注册（唯一方式：actionHelper）

```js
// 挂上即注册，无需任何标记：
window.dyn.actionHelper.hello = function (ctx) { ... };

// 可选元标记（在函数上挂属性）：
window.dyn.actionHelper.xxx._events = ['click'];   // 白名单：事件委托只认这些事件（不设 = 全部事件属性均可）
window.dyn.actionHelper.xxx._skip  = true;          // 跳过（辅助方法用下划线前缀或此标记隔离）
```

挂上后的自动能力：

1. **事件**：元素上写 `dyn-click-xxx` / `dyn-change-xxx` / `dyn-mouseover-xxx` … → 对应事件触发。事件由**属性名**决定，不是动作定义。
2. **初始化**：元素上写 `dyn-init-xxx`（或旧式 `dyn-xxx-init`）→ 页面/Vue 初始化、`init(el)`、`initAll()`、innerHTML 更新后的 `init` 时自动执行。

运行时新增动作后调用 `window.dyn.rebind()`（= `autoBindActions`）重新生成委托选择器。dyn-lib 加载时自动 `autoBindActions()` 一次。

> 内部：`autoBindActions` 把所有 actionHelper 方法放进 `_actions` 表（事件委托 + 初始化扫描共用一张表）。`defineAction(name, fn)` 是内部便捷定义。

## 属性约定

### 事件驱动属性

```html
dyn-{event}-{action}='{JSON options}'
```

- `{event}`：事件名（change/click/dblclick/error/focus/select/mouseover）
- `{action}`：动作名（postdata/reload/open/close/updateel/evaljs/setVueModel 或自定义）
- `{JSON options}`：动作参数（JSON 字符串）；非 `{` 开头的裸字符串按 `{selector: '...'}` 处理（兼容旧 reload 写法）

示例：
```html
<button dyn-click-postdata='{"url":"/api/save","confirm":true}'>提交</button>
<input dyn-change-reload='{"selector":"#list"}' />
<button dyn-mouseover-updateel='{"url":"/api/refresh"}'>滑过刷新（属性名决定事件）</button>
```

**没有对应动作函数时，委托静默忽略，不报错。**

### 初始化动作属性

```html
dyn-init-{action}='{JSON options}'      <!-- 推荐 -->
dyn-{action}-init='{JSON options}'      <!-- 兼容旧命名 -->
```

页面/Vue 初始化完毕立即执行。示例：
```html
<div dyn-init-load='{"url":"/api/load","params":{"id":1}}'></div>
<span dyn-init-badge='{"text":"初始化完成"}'>待初始化…</span>
<span dyn-init-setVueModel='{"modelName":"user.age","model":18,"settimeout":500}'></span>
```

## 统一上下文 ctx

动作函数接收的 `ctx` 对象：

```js
{
    element: el,            // 触发元素
    el: el,                 // 同 element
    event: eventName,       // 事件名（'click'/'change'/'init'...）
    $event: $event,         // 原生事件对象（init 时为 null）
    targetInfo: $event,     // 同 $event
    action: actionName,     // 动作名
    options: options,       // 属性 JSON options
    params: options.params, // 合并后的参数
    model: model,           // 最近 dyn-init 祖先的 model
    vm: vm,                 // Vue 组件实例（如果有）
    url: url                // 最近 data-dyn-url 祖先的 url
}
```

## 内置动作

### postback / postdata / confirm-postdata

POST 当前 model 到后端，返回 JSON 合并进 model（响应式刷新）。

```html
dyn-click-postback='{"url":"/api/save","confirm":true,"message":"保存成功"}'
dyn-click-postdata='{"url":"/api/save"}'
dyn-click-confirm-postdata='{"url":"/api/save","confirm":"确定删除？"}'
```
参数：`url`、`confirm`（true 或自定义文本）、`message`、`params`。

### reload

刷新容器（读目标元素的 `data-url` / `data-dyn-url`）。参数：`selector`（目标容器选择器）。

```html
dyn-click-reload='{"selector":"#list"}'
dyn-change-reload='{"selector":"#list"}'
```

### updateel

从选择器元素开始，沿祖先链向上找最近的 `data-url` 容器并刷新。参数：`selector`（起始元素）、`url`（可选覆盖）、`params`（可选额外参数）。

```html
dyn-click-updateel='{"selector":"#load-host","url":"/api/reload","params":{"page":2}}'
```

**reload / updateEl 参数三层合并**：固定参数（`__dynCfg.params`）→ Vue model（容器内 dyn-init app 的 model）→ form 序列化（容器内 input/select/textarea）。

### open / close

```html
dyn-click-open='{"url":"/page","title":"标题"}'   <!-- 打开窗口 -->
dyn-click-close                                    <!-- 关闭当前窗口 -->
```

### evaljs

执行 JavaScript 代码（裸 JS 字符串；对象形式兼容 `code.code || code.selector`）。事件 + 初始化双用。

```html
<button dyn-click-evaljs='ElementPlus.ElMessageBox.alert("xxx")'>alert</button>
<span dyn-init-evaljs='alert("Hello")'></span>
```

### setVueModel

设置 Vue model 值（支持 `dyn-init/click/change-setVueModel`）。

```html
dyn-click-setVueModel='{"modelName":"user.name","model":"张三","settimeout":100}'
dyn-change-setVueModel='{"modelName":"user.age","model":30}'
dyn-init-setVueModel='{"modelName":"page.title","model":"首页"}'
dyn-click-setVueModel='{"TargetEl":"#other-container","modelName":"items[0].name","model":"x"}'
```

参数：
- `modelName`：model 路径，支持点路径与数组下标（`user.name`、`items[0].name`）
- `model`：要设置的值（JSON 字符串自动解析为对象/数组）；change 事件未显式给 model 时取元素 `value`
- `TargetEl`：可选，指定目标 Vue 容器（选择器或 DOM 元素）；缺省用当前元素向上找最近 VueApp
- `settimeout` / `delay`：延迟毫秒

实现要点：model 定位链 `getVueModel → getModel → getProxy`（Vue 3.5.41 用 `el.__dynProxy`，即 `app.mount(el)` 返回值；旧版回退 `app._instance.proxy`）；写入用 lodash `_.set`（`setPathVal`）。

### load（初始化动作）

```html
dyn-init-load='{"url":"/api/load","params":{"id":1},"method":"POST"}'
```

行为：
1. 请求后端，返回 HTML
2. 填充到本容器 `innerHTML`
3. 调用 `init(el)` 重新扫描（内部 dyn-init 元素会继续执行）
4. 将 url 写入 `data-url` 属性
5. 将完整配置（url + params + method）写入 `__dynCfg`（供 reload/updateEl 读取）

## 扩展动作集（openwindow / setwindow / setdyncom / toast / copy / download / setattr）

> 全部挂在 `dyn.actionHelper` 上（挂方法即动作、属性驱动、零注册），与内置动作同样被
> `dyn-{event}-{action}` 属性触发、被 `dyn-init-{action}` 初始化扫描。每个动作带 `_label` / `_doc`
> 自描述元数据；`dyn.actionList()` 可枚举全部已注册动作的 `{name,label,doc,events}`（便于工具面板/自动生成文档）。

### openwindow —— 打开窗体（自动探测 UI 库）

```
dyn-click-openwindow='{"url":"/x","title":"标题","type":"auto|modal|layer|newtab|window","width":800,"height":600,"method":"GET|POST","params":{}}'
```

- `type=auto`（默认）：有 `layui.layer` 用 layer；有 `ElementPlus` 用模态；否则轻量窗口
- `type=modal`：ElementPlus `el-dialog` 模态（复用内置 `open()`），**`method` 透传**（后端 action 是 `[HttpPost]` 时传 `"method":"POST"`）
- `type=layer`：LayUI `layer.open`（type=2 iframe 弹层），需要页面已加载 `layui.js`
- `type=window`：内置**轻量独立窗口** `.dyn-window`（标题栏可拖动 + 最大化/最小化/关闭按钮 + iframe 内容），无桌面系统也能用
- `type=newtab`：`window.open` 新标签页

### setwindow —— 设置所在窗体的标题/尺寸/全屏/关闭

```
dyn-click-setwindow='{"title":"新标题","width":1000,"height":700,"fullscreen":true,"close":false}'
```

- 窗体宿主查找：先从点击元素向上找（`.dyn-window` → `.dyn-modal-host` → `.layui-layer` → 桌面 `.window`）；
  元素不在窗口内时**回退取页面最上层（最后创建的）窗口宿主**，便于窗口外的按钮控制"当前活动窗口"

### setdyncom —— 设置目标 DynCom 组件配置

```
dyn-click-setdyncom='{"configjson":{...},"modeljson":{...},"selector":"#com","mode":"merge|replace"}'
```

- `configjson` / `modeljson` 可为对象或 JSON 字符串（字符串以 `{` / `[` 开头自动 parse）
- `selector` 定位目标（缺省当前元素）
- 行为：① 更新 `data-config` / `data-model` 属性（声明式，供宿主读取）；② 更新元素上 `__dyncom` 存储；
  ③ 若元素是 Vue 组件实例（`__vueParentComponent`）则更新 props/setupState 并 `update()`；
  ④ 派发 `dyn:comchange` 自定义事件（bubbles），宿主可监听后重渲染
- `mode=replace` 整体替换；缺省 `merge` 合并

### toast —— 统一提示

```
dyn-click-toast='{"text":"内容","type":"success|error|warning","layui":true}'
```

ElementPlus / NutUI / layui 自动探测；传 `"layui":true` 强制用 `layer.msg`

### copy —— 复制文本

```
dyn-click-copy='{"text":"要复制的内容"}'
```

不带 `text` 时取元素 `value` / `textContent`（剪贴板 API 优先，失败回退 `execCommand('copy')`）

### download —— 下载文件

```
dyn-click-download='{"url":"/files/x.pdf","filename":"x.pdf"}'
```

`url` 缺省取元素 `href` 或 `data-url`

### setattr —— 设置元素属性/样式/文本/HTML

```
dyn-click-setattr='{"selector":"#x","attr":{"title":"新标题"},"style":{"color":"red"},"text":"新文本","html":"<b>x</b>"}'
```

## 扩展动作的写法（让 actionHelper 极易扩展）

1. 直接挂方法即可（事件与初始化均自动识别，无需 registerAction / registerInitAction / _init 标记）：

```js
dyn.actionHelper.xxx = function (ctx) { /* 用 ctx.options / ctx.element / ctx.model / ctx.url ... */ };
```

2. 可选自描述元数据（会被 `dyn.actionList()` 枚举）：

```js
var fn = function (ctx) { /* ... */ };
fn._label = '显示名';        // 可选
fn._doc   = '中文说明';      // 可选
fn._events = ['click'];      // 可选事件白名单；不设 = 全部 ACTION_EVENTS
dyn.actionHelper.xxx = fn;
```

3. 运行时新增动作后调用 `dyn.rebind()` 重新生成委托选择器；或直接在 dyn-lib.js 内置动作区
   用 `defineAction(name, fn)` + `_label/_doc/_events` 定义（构建时自动绑定，无需 rebind）。

## 事件委托机制

- 事件列表：`change / click / dblclick / error / focus / select / mouseover`（`ACTION_EVENTS`）
- 每个事件一个 **document capture** 监听器，`e.target.closest(选择器)` 匹配，自动覆盖动态渲染出的所有元素
- **触发与否由元素上的属性名决定**（`dyn-{event}-{action}`），动作本身不限事件
- `_events` 白名单只影响某动作的选择器生成；不设 = 全部事件属性均可
- **prevent 选项**：默认阻止冒泡和默认行为，`prevent: false` 关闭

### 关键坑：属性名大小写

- HTML 属性名会被浏览器**强制小写化**（`dyn-click-setVueModel` → DOM `dyn-click-setvuemodel`）
- 部分环境 `querySelectorAll`/`hasAttribute` 对属性名**大小写敏感**
- 因此：`selectorFor` 用 `name.toLowerCase()` 生成选择器；`resolveAction(name)` 做大小写不敏感匹配（动作可驼峰注册，如 `setVueModel`，DOM 属性用小写）

## 工具函数

### findAncestor(el, selector) / closestDynInit(el) / closestDataUrl(el)

祖先查找统一入口（原生 closest 优先、jQuery 兜底）：
- `findAncestor`：沿祖先链找匹配选择器的元素
- `closestDynInit`：找最近的 `[dyn-init]` 祖先
- `closestDataUrl`：找最近的 `[data-url],[data-dyn-url]` 祖先

### setDynCfg(el, cfg)

更新元素的 `__dynCfg` 配置（url + params + method），同步更新 `data-url`。

### serializeForm(root)

序列化容器内表单输入为参数对象。

### fetchPartial(url, params, type, dataType)

发起 AJAX 请求，返回 Promise。

## 与 Vue 的集成

### dyn-init（Vue 容器）

```html
<div dyn-init='{"count":0,"name":"test"}'>
    <div>{{ model.count }}</div>
    <button dyn-click-postdata='{"url":"/api/save"}'>+1</button>
</div>
```

### reload 与 Vue

- 目标容器是 `dyn-init` app → reload 时后端返回 JSON（model 数据），Vue 自动重新渲染
- 目标容器是纯 HTML 容器 → reload 时后端返回 HTML，直接 innerHTML

### ctx.vm / getVueModel

动作函数可通过 `ctx.vm` 访问 Vue 组件实例；`dyn.setVueModel(ctx, modelName, value, delay)` 可直接调用。

## 错误处理

- 动作失败时 `console.error` 并 `showMessage` 提示用户
- 事件委托默认阻止冒泡，可通过 `prevent: false` 关闭
- 初始化动作失败 `console.error`（不阻塞后续）

## 版本

- 版本：2.0.0（actionHelper 约定式重构）
- 更新：2026-09-03
- 变更要点：移除 `registerAction`/`registerInitAction`/`_init` 标记，统一 `actionHelper` 挂方法即动作；事件与初始化均由属性名驱动；内置动作全部改为 actionHelper 挂载
