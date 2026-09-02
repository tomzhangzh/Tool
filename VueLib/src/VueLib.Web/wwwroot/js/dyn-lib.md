# dyn-lib.js API 文档

## 概述

`dyn-lib.js` 是 VueLib 低代码平台的运行时库，提供：
- **属性驱动的事件委托**：`dyn-{event}-{action}` 属性自动绑定事件
- **动作注册表**：`registerAction` 注册动作，注册即自动进入委托
- **统一上下文**：动作函数接收 `ctx` 对象，包含 element/model/options/params/url 等
- **初始化动作**：`dyn-init-{action}` 属性在页面初始化完毕后立即执行
- **reload/updateEl**：容器刷新，支持三层参数合并（固定参数 → Vue model → form 序列化）

## 属性约定

### 事件驱动属性

```html
dyn-{event}-{action}='{JSON options}'
```

- `{event}`：事件名（click/change/dblclick/error/focus/select/mouseover）
- `{action}`：动作名（postdata/reload/open/close/updateel 等）
- `{JSON options}`：动作参数（JSON 字符串）

示例：
```html
<button dyn-click-postdata='{"url":"/api/save","confirm":true}'>提交</button>
<input dyn-change-reload='{"selector":"#list"}' />
```

### 初始化动作属性

```html
dyn-init-{action}='{JSON options}'
```

- `{action}`：初始化动作名（load/badge 等）
- 页面初始化完毕后立即执行

示例：
```html
<div dyn-init-load='{"url":"/api/load","params":{"id":1}}'></div>
<span dyn-init-badge='{"text":"初始化完成"}'></span>
```

## 动作注册表

### registerAction(name, fn, meta)

注册一个运行时动作，注册后自动进入委托选择器。

**参数**：
- `name`：动作名（字符串）
- `fn`：动作函数，接收 `ctx` 对象
- `meta`：可选元数据 `{events: ['click', 'change']}`（指定可用事件）

**示例**：
```js
dyn.registerAction('toast', function(ctx) {
    dyn.showMessage(ctx.options.text || 'toast', 'success');
}, { events: ['click', 'change'] });
```

### registerInitAction(name, fn)

注册一个初始化动作，页面初始化完毕后立即执行。

**参数**：
- `name`：动作名（字符串）
- `fn`：动作函数，接收 `ctx` 对象

**示例**：
```js
dyn.registerInitAction('badge', function(ctx) {
    ctx.element.textContent = ctx.options.text || '已初始化';
});
```

## 统一上下文 ctx

动作函数接收的 `ctx` 对象：

```js
{
    element: el,           // 触发元素
    el: el,                // 同 element
    event: eventName,      // 事件名
    $event: $event,       // 原生事件对象
    targetInfo: $event,   // 同 $event
    action: actionName,    // 动作名
    options: options,      // 属性 JSON options
    params: options.params, // 合并后的参数
    model: model,          // 最近 dyn-init 祖先的 model
    vm: vm,                // Vue 组件实例（如果有）
    url: url               // 请求 URL
}
```

## 内置动作

### postdata / postback

POST 当前 model 到后端，返回 JSON 合并进 model（响应式刷新）。

**属性**：
```html
dyn-click-postdata='{"url":"/api/save","confirm":true,"message":"保存成功"}'
```

**参数**：
- `url`：请求 URL
- `confirm`：是否确认（true 或自定义提示文本）
- `message`：成功消息
- `params`：额外参数

### reload

刷新容器（data-url / data-dyn-url）。

**属性**：
```html
dyn-click-reload='{"selector":"#list"}'
dyn-change-reload='{"selector":"#list"}'
```

**参数**：
- `selector`：目标容器选择器

**参数三层合并**：
1. 固定参数（`__dynCfg.params`）
2. Vue model（容器内 dyn-init app 的 model）
3. form 序列化（容器内 input/select/textarea）

### updateel

从选择器元素开始，沿祖先链向上找最近的 data-url 容器并刷新。

**属性**：
```html
dyn-click-updateel='{"selector":"#list","url":"/api/reload","params":{"page":2}}'
```

**参数**：
- `selector`：起始元素选择器
- `url`：可选，覆盖容器的 data-url
- `params`：可选，额外参数

### open / close

打开/关闭窗口。

**属性**：
```html
dyn-click-open='{"url":"/page","title":"标题"}'
dyn-click-close
```

## 内置初始化动作

### load

初始化完毕立即请求后端 HTML 填充本容器。

**属性**：
```html
dyn-init-load='{"url":"/api/load","params":{"id":1},"method":"POST"}'
```

**行为**：
1. 请求后端，返回 HTML
2. 填充到本容器
3. 调用 `init(div)` 重新扫描
4. 将 url 写入 `data-url` 属性
5. 将完整配置（url + params + method）写入 `__dynCfg`

## 工具函数

### setDynCfg(el, cfg)

更新元素的 `__dynCfg` 配置（url + params + method），同步更新 `data-url`。

**参数**：
- `el`：元素或选择器
- `cfg`：配置对象 `{url, params, method}`

**示例**：
```js
dyn.setDynCfg('#load-host', { url: '/api/new', params: { id: 2 } });
```

### serializeForm(root)

序列化容器内表单输入为参数对象。

**参数**：
- `root`：根元素

**返回**：参数对象 `{name: value}`

**示例**：
```js
var params = dyn.serializeForm('#load-host');
```

### fetchPartial(url, params, type, dataType)

发起 AJAX 请求。

**参数**：
- `url`：请求 URL
- `params`：参数对象
- `type`：请求方法（POST/GET）
- `dataType`：响应类型（html/json）

**返回**：Promise

## 事件委托

### 事件列表

`change / click / dblclick / error / focus / select / mouseover`

### 委托机制

每个事件一个 `document` capture 监听器，自动覆盖动态渲染出的所有元素。

### prevent 选项

默认阻止事件冒泡和默认行为。可通过 `prevent: false` 关闭：

```html
dyn-click-postdata='{"url":"/api/save","prevent":false}'
```

## 与 Vue 的集成

### dyn-init

Vue 初始化容器：
```html
<div dyn-init='{"count":0,"name":"test"}'>
    <div>{{ model.count }}</div>
    <button dyn-click-postdata='{"url":"/api/save"}'>+1</button>
</div>
```

### reload 与 Vue

- 如果目标容器是 `dyn-init` app 本身，reload 时后端返回 JSON（model 数据），Vue 自动重新渲染
- 如果目标容器是纯 HTML 容器，reload 时后端返回 HTML，直接 innerHTML

### ctx.vm

动作函数可通过 `ctx.vm` 访问 Vue 组件实例：
```js
dyn.registerAction('callMethod', function(ctx) {
    if (ctx.vm) {
        ctx.vm.$refs.child.method();
    }
});
```

## 错误处理

- 动作失败时 `console.error` 并 `showMessage` 提示用户
- 事件委托默认阻止冒泡，可通过 `prevent: false` 关闭

## 示例

### 完整示例

```html
<!DOCTYPE html>
<html>
<head>
    <script src="~/lib/jquery/dist/jquery.min.js"></script>
    <script src="~/js/vue.global.prod.js"></script>
    <script src="~/js/lodash.min.js"></script>
    <script src="~/js/element-plus.full.min.js"></script>
    <script src="~/js/element-plus-icons.min.js"></script>
    <script src="~/js/dyn-lib.js"></script>
</head>
<body>
    <div id="app" dyn-init='{"count":0,"name":"test"}'>
        <div>count: {{ model.count }}</div>
        <button dyn-click-postdata='{"url":"/api/save","confirm":true}'>+1</button>
        <button dyn-click-reload='{"selector":"#list"}'>刷新列表</button>
    </div>
    <div id="list" dyn-init-load='{"url":"/api/list"}'></div>
    <script>
        dyn.initAll();
    </script>
</body>
</html>
```

### 自定义动作

```js
dyn.registerAction('toast', function(ctx) {
    dyn.showMessage(ctx.options.text || 'toast', 'success');
}, { events: ['click', 'change'] });
```

```html
<button dyn-click-toast='{"text":"Hello"}'>显示消息</button>
```

### 自定义初始化动作

```js
dyn.registerInitAction('badge', function(ctx) {
    ctx.element.textContent = ctx.options.text || '已初始化';
});
```

```html
<span dyn-init-badge='{"text":"初始化完成"}'></span>
```

## 版本

- 版本：1.2.0
- 更新：2026-09-03
