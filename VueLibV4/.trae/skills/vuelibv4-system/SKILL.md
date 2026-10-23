---
name: vuelibv4-system
description: VueLibV4 动态页面框架开发指南。涵盖内核 dyn-core、组件系统 dyn-com、ActionHelper 动作管道、V2 Block 积木、布局壳、页面生成。用户讨论新增 Block、页面配置、动作管道、组件注册、布局壳时使用。
---

# VueLibV4 动态页面框架

本项目是 ASP.NET Core 8 + Vue 3 (runtime+compiler) + Element Plus 的低代码动态页面框架。核心思想：**页面 = 布局壳 + Block 积木 + 声明式动作管道**。

## 目录结构速查

```
src/VueLibV4.Web/
├── wwwroot/dyn/
│   ├── dyn-kernel.js        # 插件内核: core→components→params→services→actions
│   ├── dyn-core.js          # createApp/mount 核心 (template=el.innerHTML)
│   ├── dyn-com.js           # 组件注册表, 从 /api/component/list 加载
│   ├── dyn-action.js        # ActionHelper 动作管道 (dyn-click)
│   ├── dyn-eventbus.js      # execId 事件总线 (sticky/signal)
│   ├── dyn-params.js        # 参数上下文链 (L0 URL→L1 config→L2 fork→L3 父)
│   ├── dyn-call.js          # HTTP 封装 DynCall
│   ├── blocks/dyn-blocks-common.js  # 端口契约 CONTRACTS + createBlockHandle
│   └── layouts/dyn-layout-engine.js # 布局壳 wires 连线引擎
├── Views/DynBlocks/
│   ├── AppsV2/              # V2 Block 薄入口 (FilterApp/ListApp/DetailApp/TreeApp/TabApp/WidgetApp/ListViewApp)
│   ├── Partials/            # 共享布局壳 (_ListLayout/_ListViewLayout/_ListViewItemDefault)
│   └── Apps/                # 旧版 Block (TreeApp 用 createBlockHandle)
├── Views/DynLayouts/        # 5 个布局壳 (FilterListOpenWindow/ListMasterDetail/TreeMasterDetail/...)
└── Core/DynPageViewHelper.cs  # 布局壳公共辅助 (BlockAppPath/ApplyListSetting/ParseShellSpec)
```

## 核心约定

### 服务端渲染 HTML 中的 Vue 模板语法
- **禁止自闭合标签**：Razor 视图（.cshtml）输出的 HTML 由浏览器 HTML 解析器解析，不认 Vue 的自闭合语法。`<component :is="x" />`、`<Edit />`、`<Plus />` 等必须写成显式闭合：`<component :is="x"></component>`、`<Edit></Edit>`、`<Plus></Plus>`。否则 `/>` 会被当普通字符，导致后续 DOM 解析错乱、按钮/图标不显示。
- Vue 指令 `@click` 在 Razor 里要写 `@@click`（双 @ 转义）。

### Vue 挂载 (dyn-core.js mountCore)
- 根模板固定为 `el.innerHTML`，dynconfig 的 `template` 字段被忽略
- dynconfig 脚本：`<script type="text/plain" tag="dynconfig">`，执行后返回 `dynConfig` 对象
- 必须先建立参数上下文再执行 dynconfig（防止串表）
- 嵌套 App 先掩码到 `#dyn-holder` 再移除 script

### Block 入参双模解析
```csharp
IDictionary<string, object> M = Model is IDictionary<string, object> _md
    ? _md : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
object V2(string k) {
    if (M.TryGetValue(k, out var v0) && v0 != null) return v0;
    if (ViewData.TryGetValue(k, out var v1) && v1 != null) return v1;
    return null;
}
```
- `settingId` 由 PageController.LoadBlock 映射为 `{Role}SettingId`（如 list→ListSettingId）
- `TableName/KeyField/Project` 由布局壳 provide 提供，不在 Block 参数里

### 通信机制（两套并存）
1. **DynEventBus(execId)**：AppsV2 的 Filter/List/Detail/Tree 用此。事件：`query`(sticky) / `create`/`update`/`saved`/`cancel`/`select`(signal)
2. **BlockHandle(data-blk-role)**：WidgetApp 和 Apps/ 旧版用此。方法：`reg/send/on/emit/onReady/ready/destroy`

### DynEventBus 事件语义
- `query` 是 sticky：emit 存快照，on 注册时补发最新值（Filter 首屏条件）
- `create/update/saved/cancel/select` 是 signal：只派发不补发
- Block 在 mounted 不得 emit 对外事件，只能用户交互触发

### 统一数据端点
`/api/dyndata/{search,save,insert,update,delete,get}`，`project` 坐标切库：空=业务库，`__platform__`=平台库

## Block 端口契约 (dyn-blocks-common.js CONTRACTS)
```js
{
  filter: { commands: ['submit'], events: ['changed'] },
  list:   { commands: ['loadData','reload'], events: ['add','edit','addChild'] },
  detail: { commands: ['newForm','editForm'], events: ['saved','cancel'] },
  tree:   { commands: ['reload'], events: ['nodeClick','addRoot','addChild'] }
}
```

## ActionHelper 内置动作
`dyn-click="Toast('成功')|Submit({...})"` 管道语法，内置：`open/close/reload/submit(updateel)/setVueModel/toast/notify/grid/load/confirm/postback/postdata`

- `reload` 双路分发：目标是 Block 且注册 reload → `handle.send('reload')`，否则回退 `dyn.reload()` 片段刷新
- `setVueModel` 跨 app：`target` 目标选择器 + `from/fromPath/fromMethod` 取值

## ParamConfigJson 格式（必须是完整组件 UI 树）
```json
{
  "component": "DynElContainer",
  "options": { "comoptions": {"direction":"vertical","size":"small"} },
  "childrenctrls": [
    { "component": "DynElSelect", "modelname": "settingId",
      "options": { "comoptions": {"sourceType":"ajax","url":"/api/platform/pagesetting/all?type=List","valueKey":"Id","labelKey":"Name"} } }
  ]
}
```
不是简单的 `[{field,type,label}]` 数组。

## 值绑定与属性关联（modelexpr / bindings）
- **`modelname`**（路径模式，可读写）：`parentmodelinfo[modelname]`
- **`modelexpr`**（表达式模式，只读）：`{{ model.price * model.qty }}`，作用域 `model`=表单对象, `$self`=当前值
- **`modelsetexpr`**（可选写回）：`model.total = $value`，`$value` 为新值
- **`bindings`**（对象：属性路径→表达式）：运行时求值合并到 options，优先级 静态 < bindings < evalcondition
  ```json
  { "bindings": { "labeloptions.show": "{{ model.type === 'A' }}", "comoptions.disabled": "{{ model.status === 'submitted' }}" } }
  ```
- 支持路径：`comoptions.*` / `labeloptions.*` / `shell.show|style|class`
- `shell.show` 控制整个组件显隐（v-if），`labeloptions.show` 控制标签显隐

## 新增 Block 步骤
1. DynBlock 表插入：Code, Name, ImplementsRole, ViewPath, ParamConfigJson, Commands, Events
2. AppsV2/ 创建薄入口：解析参数 → PartialAsync 到 Partials
3. Partials/ 创建布局：`dyn-init="createApp"` 容器 + dynconfig 脚本
4. dyn-blocks-common.js CONTRACTS 登记端口
5. DynPageViewHelper.BlockAppPath 加角色映射
6. PageController settingId 映射 switch 加 `{role} => {Role}SettingId`

## 关键文件链接
- ListApp 薄入口：`src/VueLibV4.Web/Views/DynBlocks/AppsV2/ListApp.cshtml`
- 列表共享布局：`src/VueLibV4.Web/Views/DynBlocks/Partials/_ListLayout.cshtml`
- Block 公共层：`src/VueLibV4.Web/wwwroot/dyn/blocks/dyn-blocks-common.js`
- 布局引擎：`src/VueLibV4.Web/wwwroot/dyn/layouts/dyn-layout-engine.js`
- 动作系统：`src/VueLibV4.Web/wwwroot/dyn/dyn-action.js`
- 挂载核心：`src/VueLibV4.Web/wwwroot/dyn/dyn-core.js`
- 布局辅助：`src/VueLibV4.Web/Core/DynPageViewHelper.cs`
