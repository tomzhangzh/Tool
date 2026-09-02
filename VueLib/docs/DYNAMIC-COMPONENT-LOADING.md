# ASP.NET Core MVC + Vue3 UMD 动态组件加载系统

> 本文档完整阐述 VueLib 的「组件动态加载」机制：组件定义存在哪里、后端如何暴露、前端如何异步加载注册、设计器/运行时如何消费，以及如何新增一个组件。

## 1. 设计目标

传统前端把组件编译进 bundle，新增/修改组件必须重新构建发布。VueLib 采用 **属性驱动的运行时动态加载**：

- 组件 = **数据**（数据库表或 Razor View），不是编译产物；
- 前端用 Vue3 的 `defineAsyncComponent` 在运行时拉取组件定义并注册；
- 改一个组件视图，刷新页面即生效，无需重新打包；
- 组件同时服务**设计器**（拖拽配置）与**预览运行时**（渲染）。

整体链路：

```text
                        ┌───────────────────────────────────────────┐
                        │              SQL Server                   │
                        │  ComponentDefinitions  ComponentMeta      │
                        └──────────────┬────────────────────────────┘
                                      │ SqlSugar 查询
                        ┌──────────────▼────────────────────────────┐
  设计器 / 预览 / 运行时 │  ComponentService（DB 优先 → Razor 回退）    │
  fetch ──────────────►│  ComponentController（/api/component/*）     │
                        └──────────────┬────────────────────────────┘
                                       │ JSON（template + script + style）
                        ┌──────────────▼────────────────────────────┐
                        │  nut-runtime.js：nutLoadCom                │
                        │  fetch → DOMParser → comConfig →           │
                        │  defineAsyncComponent → 缓存               │
                        └───────────────────────────────────────────┘
```

## 2. 组件定义的三层来源

### 2.1 数据库表 `ComponentDefinitions`（优先）

实体：`Models/ComponentDefinition.cs`。存储 Vue 组件的三段式定义：

| 列 | 说明 |
|---|---|
| `ComponentName` | 组件注册名（全局唯一，如 `DynNInput`） |
| `ComponentType` | `Common`=公共组件 / `Page`=页面组件（用于构建路由） |
| `RoutePath` | 页面组件路由（公共组件为 null） |
| `TemplateContent` | Vue template HTML |
| `ScriptContent` | Vue script JS（`export default {...}` 风格的 setup 对象） |
| `StyleContent` | 组件样式 CSS（可选） |
| `PropertyConfigJson` | 属性面板配置（Element Plus 动态表单） |
| `DefaultConfigJson` | 拖入画布时的默认节点配置 |
| `IsEnabled` | 是否启用 |

### 2.2 Razor View 回退

目录 `Views/Shared/Components/*.cshtml`。当数据库无该组件时，`RazorComponentRenderer` 将 Razor 渲染为同样的三段式 DTO。Razor 组件通过文件头注释声明元数据：

```cshtml
@* ComponentType: Common *@
@* RoutePath: /about *@
@* Description: 关于页面 *@
```

### 2.3 设计器元数据表 `ComponentMeta`（设计器专用）

实体：`Models/LowCodeModels.cs`。它不是组件实现，而是**设计器可拖拽组件清单**：

| 列 | 说明 |
|---|---|
| `ComponentName` | 注册名（`DynNInput` / `DynElInput` ...） |
| `ComponentType` | 1=表单项、2=容器、3=展示、4=通用 |
| `Category` | 左侧面板分类（表单/布局/展示/通用/组合） |
| `Label` / `Icon` | 面板显示名 / 图标 |
| `DefaultConfigJson` | 拖入画布时的初始节点 JSON |
| `DefaultOptionsJson` | 默认 comoptions JSON |
| `LoadUrl` | **组件加载地址**（Area 的 Razor View 路径，如 `/NutComponent/FormItem/Input`） |
| `UiLibrary` | `nutui` / `elementui` / `custom` |
| `PropertyConfigJson` | 右侧属性面板动态表单结构 |
| `IsComposite` / `CompositeConfigJson` | 是否为组合组件 + 组合配置（内部树 + 开放属性映射） |
| `CustomScriptJson` | 组件级自定义脚本 |

> 两表职责分工：`ComponentDefinitions` 提供"组件实现"（给 `ComponentController` 的 define API）；`ComponentMeta` 提供"设计器注册信息 + LoadUrl"（给 `LowCodeController` 的 components API 与 `nutLoadCom`）。

## 3. 后端 API

`Controllers/ComponentController.cs`（路由 `api/component`）：

| API | 说明 |
|---|---|
| `GET /api/component/list` | 已启用组件清单（启动时注册路由/全局组件用，不含正文） |
| `GET /api/component/define/{name}` | 单个组件完整定义（template+script+style） |
| `POST /api/component/defines` | 批量组件定义（body: `["A","B"]`） |
| `GET /api/component/pages` | 页面组件（`ComponentType==Page`），用于构建路由表 |

服务层 `Services/ComponentService.cs` 的加载优先级：

```text
ComponentService.GetDefineByNameAsync(name)
  ├─ 1. 查 ComponentDefinitions（IsEnabled）→ 命中直接返回
  ├─ 2. 未命中 → RazorComponentRenderer.RenderAsync(name)
  │        （渲染 Views/Shared/Components/{name}.cshtml → 三段式 DTO）
  └─ 3. 都未命中 → null（前端报"组件不存在"）
```

## 4. 前端运行时：nutLoadCom

`wwwroot/js/nut-runtime.js` 是动态加载的核心。

### 4.1 组件视图协议（comconfig 约定）

每个组件视图由「template + `<script tag='comconfig'>`」两部分组成。NutUI 组件通过 `_NutLayout.cshtml` / `_FormItemLayout.cshtml` 公共布局统一输出：

```cshtml
@{ Layout = "~/Areas/NutComponent/Views/_FormItemLayout.cshtml"; }

<nut-input v-model="modelinfo" v-bind="props.jsonconfig.options.comoptions"
           v-on="getComlisteners()"></nut-input>
```

`_FormItemLayout` 输出 `<template>` 包裹（label + content + error），`_NutLayout` 输出 `<script tag='comconfig'>` 注入公共 setup 逻辑。Element Plus 组件同理（`_ElementLayout.cshtml` / `_ElementFormItemLayout.cshtml`）。

### 4.2 fetchComponentFromRazor（拉取解析）

```js
async function fetchComponentFromRazor(url) {
    const resp = await fetch(url, { method: 'GET', headers: { 'Accept': 'text/html' } });
    const html = await resp.text();
    const doc = new DOMParser().parseFromString(html, 'text/html');

    // 1. 提取 <template> 作为组件模板
    const template = doc.querySelector('template').innerHTML;
    // 2. 提取 <script tag='comconfig'>，执行得到 comConfig
    const scriptText = doc.querySelector("script[tag='comconfig']").textContent;
    const comConfig = new Function(`${scriptText}; return typeof comConfig !== 'undefined' ? comConfig : null;`)();
    comConfig.template = template;   // 注入模板
    return comConfig;
}
```

### 4.3 nutLoadCom（异步组件 + 缓存 + 自定义脚本）

```js
function nutLoadCom(componentName, url) {
    const loader = async () => {
        if (componentCache.has(componentName)) return componentCache.get(componentName);
        if (loadingPromises.has(componentName)) return loadingPromises.get(componentName); // 防并发
        const promise = (async () => {
            let comp = await fetchComponentFromRazor(url);
            comp = applyCustomScript(comp, componentName);  // 注入 CustomScriptJson
            componentCache.set(componentName, comp);
            return comp;
        })();
        loadingPromises.set(componentName, promise);
        return promise;
    };
    return defineAsyncComponent({
        loader, delay: 200, timeout: 15000,
        loadingComponent: { /* 转圈占位 */ },
        errorComponent:   { /* 失败提示 */ }
    });
}
```

要点：**双缓存**（`componentCache` 完成缓存 + `loadingPromises` 进行中去重），避免重复请求。

### 4.4 applyCustomScript（组件级自定义脚本）

`ComponentMeta.CustomScriptJson` 支持 `{ methods, onMounted, watch }`，在加载时包装 setup：

```js
methods: { fnName: '字符串函数体' }  →  new Function('props','modelinfo','comInnerInfo','context', fnBody)
onMounted: '字符串函数体'           →  onMounted(() => new Function(...)())
watch: { modelinfo: '函数体' }      →  watch(() => props.modelinfo?.value, handler)
```

## 5. 设计器 / 预览如何消费

### 5.1 设计器注册（designer.js 启动）

```js
const resp = await fetch('/api/lowcode/components');     // ComponentMeta 全量
for (const meta of result.data) {
    app.component(meta.componentName, window.nutLoadCom(meta.componentName, meta.loadUrl));
}
```

组合组件同时解析 `compositeConfigJson` 保存到共享 map；`customScriptJson` 交给 `nutRegisterCustomScript` 注册。

### 5.2 预览运行时注册（preview.js）

与设计器同一套：`loadAndRegisterComponents(app)` → 遍历 `/api/lowcode/components` → `app.component(name, window.nutLoadCom(name, url))`。

画布渲染由 `NDynamicCom`（`Areas/NutComponent/Views/Com/DynamicCom.cshtml`）递归渲染组件树：读取节点 `component` 名 → 从 app 已注册组件动态 `<component :is>` 渲染，并把 `jsonconfig`、`parentmodelinfo`、`nodePath` 作为 props 传入，支持 `childrenctrls` 递归。

### 5.3 组件 props 协议（每个组件 setup 收到的）

| prop | 含义 |
|---|---|
| `jsonconfig` | 节点配置：`{ component, modelname, options:{comoptions,labeloptions,...}, validators, childrenctrls, slots, extendinfo }` |
| `parentmodelinfo` | 页面级数据模型（根对象，子组件按 modelname 路径绑定） |
| `nodePath` | 节点路径（设计器定位用） |

公共 setup 提供：`modelinfo`（相对路径 computed 双向绑定，支持 `a.b.c`/`a[0].b`）、`validate()/resetValidation()`（验证器系统 required/minLength/pattern/email/phone/url/number...）、`getComlisteners()`、`safeChildren`、`dragOptions`（设计器容器拖拽）、`provide('parentModelPrefix', ...)`（相对 modelname 前缀拼接）。

## 6. 组件命名规范（重要）

- 统一使用 `Dyn` 前缀 + UI 库标识：`DynN*`（NutUI 移动端）、`DynEl*`（Element Plus 桌面端）；
- 原因：避免与 UI 库全局注册组件（`el-input` / `nut-input`）冲突——历史上曾因 `ElInput` 裸名冲突导致"数据库和组件的都不对"；
- 容器组件白名单（可拖子组件）：`DynNForm`、`DynNCellGroup`、`DynNDivContainer`、`DynNGrid`、`DynElDivContainer`、`DynElCard`、`DynElRow`、`DynElCol`、`DynElTabs`。

## 7. 新增一个组件的完整步骤

1. **写组件视图**：在对应 Area 建 `.cshtml`（表单项用 `_FormItemLayout`，容器用 `_NutLayout`/`_ElementLayout`），只写交互主体 + `@section setupScripts{ DVALUE=...; }`；
2. **（可选）入库 ComponentDefinitions**：填 template/script 三段式，启用后生效；不入库则走 Razor View 回退；
3. **注册 ComponentMeta**：`INSERT` 一行，`LoadUrl` 指向视图地址，`UiLibrary` 标注 UI 库，`ComponentType/Category` 决定左侧面板分类；
4. **（可选）配 PropertyConfigJson**：右侧属性面板的动态表单结构；
5. **（可选）组合组件**：`IsComposite=1` + `CompositeConfigJson`（内部树 + `exposedProps` 开放属性映射 `{key,label,type,target,default}` + `openContainers` 开放容器）；
6. 刷新设计器 → 左侧出现 → 拖入画布 → 配置 → 保存 → 预览验证。

## 8. 关键文件清单

| 文件 | 职责 |
|---|---|
| `Models/ComponentDefinition.cs` | 组件实现实体（template/script/style） |
| `Models/LowCodeModels.cs` | `ComponentMeta`（设计器注册）+ `PageSetting`（页面配置） |
| `Services/ComponentService.cs` | 加载服务（DB 优先 → Razor 回退） |
| `Services/RazorComponentRenderer.cs` | Razor 组件渲染为三段式 DTO |
| `Controllers/ComponentController.cs` | `/api/component/*` 动态加载 API |
| `Controllers/LowCodeController.cs` | `/api/lowcode/components|page|pages` 设计器 API |
| `wwwroot/js/nut-runtime.js` | `nutLoadCom` / `fetchComponentFromRazor` / `applyCustomScript` |
| `wwwroot/js/dyn-lib.js` | 属性驱动动态引擎（`dyn-init`/`dyn-click-*`/`eventBus`/`mountCore`） |
| `Areas/NutComponent/Views/_NutLayout.cshtml` | NutUI 组件公共 setup 协议（comconfig） |
| `Areas/ElementComponent/Views/_ElementLayout.cshtml` | Element 组件公共 setup 协议 |
| `wwwroot/js/preview.js` | 预览运行时（注册组件 + 递归渲染 + postMessage 联动） |
