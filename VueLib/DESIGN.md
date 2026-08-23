# VueLib 系统设计文档

## 1. 系统概述

VueLib 是一个基于 ASP.NET Core MVC + Vue 3 UMD 的动态组件加载低代码平台，支持可视化拖拽设计、多 UI 组件库（NutUI 移动端 + ElementUI PC端）、Windows 风格桌面多窗口管理。

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────┐
│                      浏览器前端                            │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │
│  │ 设计器    │  │  预览页   │  │  Windows 桌面        │  │
│  │ designer  │  │ preview  │  │  Desktop             │  │
│  └────┬─────┘  └────┬─────┘  └──────────┬───────────┘  │
│       │               │                     │              │
│  ┌────▼───────────────▼─────────────────────▼───────────┐ │
│  │              Vue 3 UMD 运行时                          │ │
│  │  ┌─────────────┐  ┌──────────────┐  ┌─────────────┐ │ │
│  │  │ nut-runtime │  │  NDynamicCom │  │  nutLoadCom │ │ │
│  │  │  核心运行时  │  │  递归渲染器   │  │  异步加载   │ │ │
│  │  └─────────────┘  └──────────────┘  └─────────────┘ │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────┬───────────────────────────────┘
                              │ HTTP / fetch
┌─────────────────────────────▼───────────────────────────────┐
│                   ASP.NET Core MVC 后端                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │  LowCode     │  │  Component   │  │  Desktop         │  │
│  │  Controller  │  │  Manager     │  │  Controller      │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         │                   │                     │            │
│  ┌──────▼───────────────────▼─────────────────────▼─────────┐│
│  │              Razor View 引擎 (.cshtml)                     ││
│  │  Areas/NutComponent/    Areas/ElementComponent/            ││
│  └──────────────────────────────┬─────────────────────────────┘│
└─────────────────────────────────┼──────────────────────────────┘
                                  │
                         ┌────────▼────────┐
                         │   SQL Server     │
                         │   VueLib 数据库   │
                         └──────────────────┘
```

### 2.2 核心模块

| 模块 | 文件 | 职责 |
|------|------|------|
| 组件加载器 | `wwwroot/js/nut-runtime.js` | `nutLoadCom` 异步加载 Razor View，缓存组件定义 |
| 递归渲染器 | `designer.js` / `preview.js` 中的 `NDynamicCom` | 根据 jsonconfig 递归渲染组件树 |
| 设计器 | `Views/Designer/Index.cshtml` + `wwwroot/js/designer.js` | 三栏可视化设计器（组件面板/画布/属性面板） |
| 预览 | `Views/Designer/Preview.cshtml` + `wwwroot/js/preview.js` | 独立页面预览，支持设计/预览模式切换 |
| 桌面 | `Views/Desktop/Index.cshtml` | Windows 风格多窗口桌面 |
| NutUI 布局 | `Areas/NutComponent/Views/_NutLayout.cshtml` | NutUI 组件公共 setup 逻辑 |
| ElementUI 布局 | `Areas/ElementComponent/Views/_ElementLayout.cshtml` | ElementUI 组件公共 setup 逻辑 |

## 3. 组件加载机制

### 3.1 nutLoadCom 流程

```
nutLoadCom(name, url)
    │
    ▼
defineAsyncComponent({ loader, delay, timeout, loadingComponent })
    │
    ▼ 首次渲染时调用 loader
loader()
    ├─ componentCache.has(name)? → 返回缓存组件
    ├─ loadingPromises.has(name)? → 返回加载中的 Promise
    └─ 否则 → fetch(url) → 解析 template + script → 缓存 → 返回
```

### 3.2 Razor View 组件格式

每个组件是一个 `.cshtml` 文件，输出：
```html
<template>...Vue 模板...</template>
<script tag='comconfig'>
var comConfig = {
    props: { jsonconfig: { type: Object, required: true }, ... },
    setup(props, context) { ... }
};
</script>
```

`fetchComponentFromRazor` 用 DOMParser 解析 HTML，提取 template 和 comConfig script，通过 `new Function()` 执行 script 获取组件配置对象。

## 4. NDynamicCom 递归渲染

### 4.1 渲染流程

```
NDynamicCom (jsonconfig)
    │
    ├─ validConfig 检查（null/无 component → 错误提示）
    ├─ depth 检查（>20层 → 深度超限提示）
    │
    ├─ hasWrapper?
    │   └─ <component :is="wrapperComponent">
    │       ├─ isComposite? → <n-dynamic-com :jsonconfig="compositeTree">
    │       └─ else → <component :is="jsonconfig.component">
    │
    └─ else
        ├─ isComposite? → <n-dynamic-com :jsonconfig="compositeTree">
        └─ else → <component :is="jsonconfig.component">
```

### 4.2 安全防护

- **null 检查**：`validConfig` computed 验证 jsonconfig 存在且有 component 字段
- **深度限制**：`depth` computed 统计 nodePath 中 `.childrenctrls[` 出现次数，超过 20 层停止递归
- **safeChildren**：ElementUI 布局中过滤 childrenctrls 中的 null/undefined 元素
- **组件缓存**：nutLoadCom 的 componentCache 避免重复加载

## 5. 组件配置 Schema (jsonconfig)

```json
{
  "component": "ElInput",
  "modelname": "user.name",
  "options": {
    "comoptions": { "placeholder": "请输入" },
    "comlisteners": { "blur": "handleBlur" },
    "labeloptions": { "label": "用户名", "required": true },
    "itemoptions": { "style": {}, "class": "" },
    "wrapperoptions": { "component": "ElCard" },
    "optionValues": "选项1,选项2"
  },
  "validators": [{ "type": "required", "message": "必填" }],
  "childrenctrls": [],
  "slots": {},
  "extendinfo": {}
}
```

### 5.1 modelname 相对路径规则

| 写法 | 说明 | 示例 |
|------|------|------|
| 空字符串 | 继承父组件前缀 | 父: `form` → 子: `form` |
| `~` 开头 | 绝对路径，去掉 `~` | `~user.name` → `user.name` |
| `[` 开头 | 直接拼接，不加 `.` | 父: `list` → 子: `list[0]` |
| 其他 | 拼接到父前缀后 | 父: `form` → 子: `form.name` |

## 6. 响应式设计关键约束

### 6.1 禁止事项

1. **禁止 `reactive(props.jsonconfig)`**：创建响应式代理后修改会触发父组件重渲染 → 子组件重新 setup → 再次修改 → 无限循环（Maximum call stack size exceeded）
2. **禁止直接给 `props.jsonconfig` 赋值**：Vue 3 props 只读
3. **禁止模板中每次渲染创建新对象**：`v-bind`/`v-on` 绑定新对象引用会触发重渲染，用 computed 缓存
4. **禁止组件树循环引用**：容器的 childrenctrls 中包含自身会导致无限递归

### 6.2 推荐做法

1. **直接修改 props 对象属性**：`deepMerge(props.jsonconfig, defaults)` 是安全的，因为 props.jsonconfig 只是普通对象引用
2. **用 computed 缓存派生数据**：comListeners、safeChildren 等
3. **NDynamicCom 添加防护**：null 检查 + 深度限制 + safeChildren 过滤
4. **本地兜底对象**：props.jsonconfig 为 undefined 时用本地对象兜底

## 7. 数据库设计

### 7.1 核心表

| 表名 | 说明 |
|------|------|
| ComponentDefinitions | 组件定义（template/script/style） |
| ComponentMeta | 组件元数据（注册名、分类、UI库、加载地址、默认配置） |
| PageSetting | 页面配置（组件树 JSON + 默认数据模型） |
| DesktopShortcut | 桌面快捷方式 |
| DesktopSolution | 桌面解决方案 |

### 7.2 SqlSugar 注入

项目使用 `AppDbContext.Create()` 工厂方式获取 SqlSugar 客户端，**不直接注册 ISqlSugarClient 到 DI**。

## 8. Area 路由

Area 路由必须在 default 路由前面注册：
```csharp
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
```

## 9. 扩展开发指南

### 9.1 新增 NutUI 组件

1. `Areas/NutComponent/Controllers/` 添加 Action
2. `Areas/NutComponent/Views/` 创建 .cshtml
3. ComponentMeta 表插入记录（UiLibrary = 'NutUI'）

### 9.2 新增 ElementUI 组件

1. `Areas/ElementComponent/Controllers/` 对应分类 Controller 添加 Action
2. `Areas/ElementComponent/Views/` 对应分类目录创建 .cshtml，Layout = `_ElementLayout.cshtml`
3. ComponentMeta 表插入记录（UiLibrary = 'ElementUI'）

### 9.3 新增桌面应用

1. DesktopShortcut 表插入快捷方式记录
2. 或创建解决方案（DesktopSolution），包含多个页面
