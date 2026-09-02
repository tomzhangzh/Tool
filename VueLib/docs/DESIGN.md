# VueLib 低代码设计器 —— 多 App 拆分设计文档

> 版本：v2.0（多 App 架构） · 适用目录：`src/VueLib.Web/Views/Designer` + `src/VueLib.Web/wwwroot/js`

## 1. 背景与目标

原 `designer.js` 是一个 1500+ 行的巨型 IIFE + 单 `createApp`，左侧组件库、中间画布、右侧属性面板、顶部工具栏、面包屑、弹窗全部耦合在一个 Vue 实例内，闭包依赖深、难以维护。

本次重构目标（用户确认方案）：
- 将设计器按 **区域拆分为多个独立 Vue app**（`dyn-init` 容器），各自 partial 自带 `<script tag="dynconfig">`；
- 各 app 通过 **全局共享 store**（`window.LCDesignerStore`）+ **公共 API**（`window.__lcApi`）+ **eventBus**（`dyn.eventBus`）解耦通信；
- 复用并扩展 `dyn-lib.js` 公共方法，不引入额外框架。

## 2. 架构总览

```
┌─────────────────────────────── .lc-shell (flex column, 100vh) ───────────────────────────────┐
│  #lc-toolbar    ← 独立 app（dyn-init）工具栏：页面/新建/保存/开放配置/平台/缩放/预览/JSON      │
│  ┌────────────────────────── .lc-main (flex row, flex:1) ──────────────────────────────────┐ │
│  │  #lc-left    ← 独立 app：组件库（分类+拖拽）/ 组件树 / 折叠浮动菜单(palette-float)        │ │
│  │  #designer-app ← 主 app：画布（NDynamicCom 递归渲染）+ 内核业务逻辑                        │ │
│  │  #lc-right   ← 独立 app：属性配置(DynamicPropertyPanel)/开放项/验证器/操作                  │ │
│  └────────────────────────────────────────────────────────────────────────────────────────┘ │
│  #lc-dialogs   ← 独立 app：JSON 编辑 / 新建页面 / Model 查看 / 存组合 四个弹窗                 │
│  #lc-breadcrumb← 独立 app：路径面包屑                                                         │
└──────────────────────────────────────────────────────────────────────────────────────────────┘

共享层（按加载顺序）：
  dyn-lib.js        → window.dyn（eventBus / getByPath / isContainerComp / mountCore 支持 dynconfig 的 components/directives/plugins）
  designer.core.js  → window.LCDesignerStore（全局共享 reactive store）+ window.__lcApi（公共 API 容器）
  designer.utils.js → window.LCDesignerUtils（deepClone / dragState / 拖拽 ghost / drop 占位符）
  designer.js       → 主 app（画布 + 内核方法），mount 后调用 dyn.initAll() 挂载各独立面板 app
```

## 3. 文件职责

### 3.1 核心 JS

| 文件 | 全局命名 | 职责 |
|---|---|---|
| `wwwroot/js/dyn-lib.js` | `window.dyn` (V1.1.0) | dyn-init 框架本体；扩展了 `eventBus`(on/off/emit/clear)、`getByPath`、`setPathVal`、`isContainerComp`、`nextId`；`mountCore` 支持 dynconfig 声明 `components/directives/plugins`（独立 app 注册全局组件的通道） |
| `wwwroot/js/designer.core.js` | `window.LCDesignerStore` / `window.__lcApi` | 全局共享 store（ref/reactive/computed 风格）+ 公共 API 容器。**拆分的基石** |
| `wwwroot/js/designer.utils.js` | `window.LCDesignerUtils` | 工具集：deepClone/getByPath/setPathVal/isContainerComp/applyCompositeProps/ghostShape/createPaletteGhost/removePaletteGhost/computeInsertIndex/updateDropPlaceholder/clearDropPlaceholder + `dragState`（含 sessionId/currentMenuCom/draggingFromPalette/dropTargetInfo） |
| `wwwroot/js/designer.js` | 主 app | 画布渲染 + 内核业务（loadPage/savePage/setCurrentCom/onPaletteDrop/组合组件注册/验证器），setup 末尾 `Object.assign(window.__lcApi, {...})` 导出公共方法 |
| `wwwroot/js/property-panel.js` | `window.DynamicPropertyPanel` / `window.ppSetByPath` | 动态属性面板组件（由 right app 注册） |
| `wwwroot/js/preview.js` | preview 页面 | 预览页逻辑 |

### 3.2 视图（Razor Partial）

| 文件 | 容器 | app 类型 |
|---|---|---|
| `Index.cshtml` | 骨架 + `.lc-shell/.lc-main` 布局 + 全部脚本引用 | — |
| `_TopToolbar.cshtml` | `#lc-toolbar` | 独立 dyn-init app |
| `_LeftPanel.cshtml` | `#lc-left` | 独立 dyn-init app |
| `_CenterCanvas.cshtml` | 主 app 内 | 主 app 模板片段 |
| `_RightPanel.cshtml` | `#lc-right` | 独立 dyn-init app |
| `_Dialogs.cshtml` | `#lc-dialogs` | 独立 dyn-init app |
| `_Breadcrumb.cshtml` | `#lc-breadcrumb` | 独立 dyn-init app |
| `Preview.cshtml` | — | 预览页 |

### 3.3 样式

`wwwroot/css/designer.css` 新增 `.lc-shell`（flex column 100vh）与 `.lc-main`（flex row flex:1 overflow:hidden）；`#designer-app` 由 `height:100vh` 改为 `flex:1; min-width:0`。**面板移出主 app 后必须依赖这套外壳，否则布局错乱。**

## 4. 脚本加载顺序（Index.cshtml，不可随意调整）

```
vue.global.prod.js → lodash.min.js → element-plus.full.min.js → element-plus-icons.min.js
→ nutui.umd.min.js → echarts.min.js → nut-runtime.js → mobile-setup.js → property-panel.js
→ lib/sortable/sortable.min.js → lib/ruler/ruler.min.js → vue-draggable-plus.umd.min.js
→ lib/jquery/dist/jquery.min.js   ← 必须（dyn-lib 的 $ 依赖）
→ dyn-lib.js → designer.core.js → designer.utils.js → designer.js
```

> ⚠️ **jQuery 必须在 dyn-lib.js 之前**，否则 dyn-init 挂载静默失败（所有面板显示原始 `{{ }}` 模板）。

## 5. 通信机制

### 5.1 共享 store（`window.LCDesignerStore`）
所有面板读写同一份页面数据。核心字段（ref 风格，模板绑定不变）：

```
componentMetaList / pageList / currentPageCode / currentPageId / saving
designMode / canvasPlatform / canvasZoom / showRuler / showJson
canvasWidth / canvasHeight
currentCom / currentContainer / currentPath / breadcrumbList / treeVersion
openConfigMode / configObj(reactive) / modelObj(reactive)
showNewPage / showModelModal / showCompositeDialog / configJsonText
newPageForm / compositeForm / modelJsonText(computed)
```

### 5.2 公共 API（`window.__lcApi`）
主 app 内核在 setup 末尾导出，各面板通过**值引用**调用（挂载时取函数，点击时动态解析）：

```
loadComponentMeta / loadPageList / loadPage / savePage / newPage / confirmNewPage
showModelData / openCompositeDialog / onCompositeSourceChange / saveAsComposite
openPreview / applyJson / setCurrentCom / showJsonEditor
deleteCurrent / moveUp / moveDown / copyCurrent / zoomIn / zoomOut / zoomReset
```

### 5.3 eventBus（`dyn.eventBus`）
广播状态变化事件，监听方各自处理。当前事件：
- `setcurrent` — setCurrentCom 内触发，payload: 组件配置
- `dropin` — onPaletteDrop 成功后触发，payload: `{parent, config, index}`
- `saved` — savePage 成功
- `loaded` — loadPage 成功

### 5.4 拖拽跨 app 链路（关键）
左侧（left app）与画布（主 app）通过**共享 `dragState`**（designer.utils.js）通信：
1. left app `onPaletteDragStart`：写 `dragState.draggingFromPalette/sessionId/currentMenuCom` + `createPaletteGhost` 设置 dragImage；
2. 画布容器 `dragover/drop`：主 app 读 `dragState` 判定来源，`onPaletteDrop` 插入并 `dragState.sessionId++`；
3. left app `onPaletteDragEnd`：清理 ghost/占位符/`currentMenuCom`。

画布**内部**排序/嵌套仍走 Sortable.js（`v-draggable` 指令，主 app 注册）。

## 6. dynconfig 规范（新增/修改面板必读）

每个独立面板 partial 结构：

```html
<div id="lc-xxx" dyn-init="{}">
    ... 模板（Vue 语法，事件用 @@click 让 Razor 转义为 @click）...
    <script tag="dynconfig">
        var dynConfig = {
            setup: function () {
                var Vue = window.Vue;
                var S = window.LCDesignerStore;
                var api = window.__lcApi;
                // ...
                return { /* 模板绑定数据/方法 */ };
            },
            components: { MyComp: window.MyComp },   // 可选：注册全局组件
            directives: {}, plugins: []               // 可选
        };
    </script>
</div>
```

要点：
- **面板私有状态**（如 leftTab/rightPanel/categories）留在各自 setup 内，不污染共享；
- **跨面板共享状态**（如 leftPanel 被 right 模板的 palette-float 引用）必须提升到 `LCDesignerStore`；
- **组合组件配置** `compositeComponents` 挂在 `window.LCDesignerCore.compositeComponents`（right app 与主 app 共享）；
- **业务重的方法**（存组合/应用 JSON/新建页面）留主 app 内核，面板通过 `__lcApi` 调用，避免在多个 app 重复实现。

## 7. 已踩过的坑（务必规避）

1. **`window.LCDesignerCore` 初始化顺序**：compositeComponents 共享时曾在文件顶部（第 23 行）直接 `window.LCDesignerCore.compositeComponents = ...`，但 LCDesignerCore 在其后（PaletteContent 导出处）才初始化，导致整个 IIFE 崩溃、所有 app 静默不挂载（表现为全页 `{{ }}` 未解析）。**必须先 `if (!window.LCDesignerCore) window.LCDesignerCore = {};` 再赋值。**
2. **jQuery 缺失**：dyn-init 挂载依赖 `$`，需在 dyn-lib.js 之前引入 `~/lib/jquery/dist/jquery.min.js`。
3. **面板移出 #designer-app 后布局错乱**：原 `#designer-app { height:100vh }` 会把散在 body 的面板盖掉。必须用 `.lc-shell/.lc-main` flex 外壳重新布局。
4. **Razor 事件转义**：partial 内 Vue 事件必须写 `@@click`（Razor 转义为 `@click`）。
5. **PowerShell 无 `&&`**、内联 `node -e` 含中文/引号必失败：批处理脚本一律写临时 `_*.js` 文件执行，用后即删。

## 8. 验证清单（每次改动后）

1. `dotnet build` 0 错误；
2. 浏览器打开 `http://localhost:5000/designer`：
   - 所有 `#lc-*` 容器与 `#designer-app` 均挂载（检查 `__vue_app__` 存在、`script[tag=dynconfig]` 已被消费移除）；
   - 页面无 `{{ }}` 未解析、无 JS 错误；
3. 点画布组件 → right 属性面板 + breadcrumb 同步更新（跨 app 联动）；
4. 左侧拖组件 → 画布插入（dragState 跨 app）；
5. toolbar「存组合」→ 组合弹窗打开（toolbar → __lcApi → dialogs app）。
