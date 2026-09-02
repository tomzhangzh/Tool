# VueLib 系统设计文档

> 总体架构、模块划分、数据库设计、运行时机制与关键设计决策。配套：`QUICKSTART.md`（上手）、`DYNAMIC-COMPONENT-LOADING.md`（动态组件）、`DESIGN.md`（设计器多 App 拆分）。

## 1. 系统定位

VueLib 是一套面向企业内部的**低代码/动态 UI 平台**，技术栈 **ASP.NET Core MVC（.NET 7）+ Vue 3（UMD 全量脚本）+ SQL Server（SqlSugar ORM）**。

两条核心产品线：

```text
VueLib 系统
├── 低代码设计器体系（设计师/配置者使用）
│   ├── 可视化页面设计器（拖拽组件 → JSON 组件树 → 保存）
│   ├── 组件元数据（ComponentMeta）+ 组件实现（ComponentDefinitions / Razor View）
│   ├── 组件管理后台（ComponentManager：grid+filter+分页）
│   ├── 页面管理/快捷方式（Desktop：桌面门户）
│   └── 预览运行时（/designer/preview?code=xxx）
└── 动态工程运行时体系（面向业务库的快速 CRUD 平台）
    ├── 工程（DynProject：连接任意业务库）
    ├── 页面（DynPage：汇总屏 Summary / 细节屏 Detail）
    ├── 模板（DynTemplate：List / Home 组合）
    ├── 路由页面（DynWebPage：route ↔ 模板）
    └── 运行时渲染（DynRunController：按定义动态查表渲染）
```

## 2. 总体架构

```text
┌────────────────────────── 浏览器 ──────────────────────────┐
│  Vue3 UMD 全局脚本栈（无构建步骤）                          │
│   Vue / Element Plus / NutUI / ECharts / jQuery / lodash   │
│   dyn-lib（动态引擎） + nut-runtime（组件加载）              │
│   设计器多 App：toolbar / left / right / canvas / dialogs / breadcrumb
│   （共享 window.LCDesignerStore + eventBus + __lcApi）      │
└───────────────▲──────────────────────────┬────────────────┘
                │ fetch / JSON              │ 动态加载组件定义
┌───────────────┴──────────────────────────▼────────────────┐
│               ASP.NET Core MVC（.NET 7）                   │
│  Controllers: LowCode / Component / Designer / ComponentManager
│               Desktop / DynProject / DynRun / Home         │
│  Services:    ComponentService / ComponentMetaService / PageSettingService
│               DynCrudService / DynProjectService / ConfigMigrator
│  Areas:       NutComponent（移动组件） / ElementComponent（桌面组件）│
│  Views:       Designer / ComponentManager / Desktop / DynProject / DynRun│
└───────────────▲───────────────────────────────────────────┘
                │ SqlSugar（DbType=SqlServer）
┌───────────────┴───────────────────────────────────────────┐
│  SQL Server（VueLib 库 + 各 DynProject 连接的业务库）        │
│  低代码表：ComponentDefinitions / ComponentMeta / PageSetting│
│  桌面表：  DesktopShortcut / DesktopSolution                │
│  工程表：  DynProject / DynPage / DynTemplate / DynWebPage  │
└────────────────────────────────────────────────────────────┘
```

**关键决策：无 Node 构建**。前端全部走 UMD 全局变量，Razor 视图直接输出 `<script src>` 引用，配合 `dyn-init` 属性驱动的多 App 挂载，实现"改前端代码 = 改文件刷新即生效"，非常适合无前端工程化的企业内部工具。

## 3. 模块清单

### 3.1 后端

| 控制器 | 路由前缀 | 职责 |
|---|---|---|
| `LowCodeController` | `/api/lowcode` | 组件元数据列表、页面 CRUD、分页查询、组件管理 API |
| `ComponentController` | `/api/component` | 组件定义动态加载（list/define/defines/pages） |
| `DesignerController` | `/Designer` | 设计器主页面 + 预览页 |
| `ComponentManagerController` | `/ComponentManager` | 组件管理后台（grid+filter+分页） |
| `DesktopController` | `/Desktop` + `/api/desktop` | 桌面门户、快捷方式/解决方案、窗口大小 |
| `DynProjectController` | `/DynProject` + `/api/dynproject` | 工程/页面/模板/路由页面 CRUD、动态 SQL |
| `DynRunController` | `/DynRun` | 汇总屏/细节屏分部视图、路由渲染、数据接口 |
| `HomeController` | `/` | 首页 |

| 服务 | 职责 |
|---|---|
| `ComponentService` | 组件定义加载（DB 优先 → Razor 回退） |
| `ComponentMetaService` / `PageSettingService` | 设计器组件元数据 / 页面配置 CRUD |
| `RazorComponentRenderer` | 把 `.cshtml` 组件渲染为三段式 DTO |
| `DynCrudService` / `DynProjectService` | 动态工程的数据访问（动态建客户端/查询/增删改）与定义解析 |
| `ConfigMigrator` / `PropertyConfigValidator` | 配置迁移 / 属性面板配置校验 |
| `DynViewGenerator` | 视图生成（动态工程辅助） |

数据访问统一经 `Data/AppDbContext.cs`：每次 `Create()` 返回新的 `SqlSugarClient`（`IsAutoCloseConnection=true`），全局约定可空 string 映射为可空列。

### 3.2 前端运行时

| js | 职责 |
|---|---|
| `dyn-lib.js` | 属性驱动动态引擎：`dyn-init`（容器→Vue app）、`dyn-click-postback`/`dyn-click-open`/`dyn-click-close`/`dyn-click-reload`、`eventBus`、`getByPath/setPathVal`、`isContainerComp`、`nextId`、`mountCore`（支持 dynconfig 的 components/directives/plugins） |
| `nut-runtime.js` | `nutLoadCom`（异步加载+双缓存）、`fetchComponentFromRazor`、`applyCustomScript` |
| `mobile-setup.js` | NutUI 移动端全局设置 |
| `property-panel.js` | `DynamicPropertyPanel` 动态属性面板组件 + `ppSetByPath` |
| `preview.js` | 预览运行时：注册组件、递归渲染、postMessage 与设计器联动 |
| `designer.core.js` | 共享 store `window.LCDesignerStore` + 公共 API 容器 `window.__lcApi` |
| `designer.utils.js` | 工具 + `dragState`（拖拽状态机） |
| `designer.js` | 设计器主 app 内核（画布渲染/选择/拖拽/保存/组合） |

### 3.3 设计器多 App 架构

设计器页 `Views/Designer/Index.cshtml` 拆为 6 个独立 Vue app（dyn-init 容器）：

```text
#designer-app   主画布 app（渲染组件树、选择、拖拽、保存、组合组件业务）
#lc-toolbar     顶部工具栏 app
#lc-left        左侧面板 app（组件库/组件树/折叠浮动菜单）
#lc-right       右侧面板 app（属性配置/开放属性标记）
#lc-dialogs     弹窗组 app（JSON 编辑器/新建页面/Model 查看/存组合）
#lc-breadcrumb  底部面包屑 app
```

**通信四层**（详见 `DESIGN.md`）：
1. **共享 store** `window.LCDesignerStore`：`currentCom/currentPath/breadcrumbList/componentMetaList/pageList/configObj/modelObj/...`（ref/reactive/computed）；
2. **公共 API** `window.__lcApi`：主 app 导出的业务方法（`setCurrent`/`openCompositeDialog`/`showJsonEditor`/...），面板按值引用调用；
3. **事件总线** `dyn.eventBus`：`setcurrent`/`dropin`/`saved`/`loaded` 等跨 app 广播；
4. **拖拽状态** `dragState`：left 拖出 → 画布 drop 的共享状态通道。

脚本加载顺序（Index.cshtml，jQuery 必须在 dyn-lib 之前）：vue → lodash → element-plus(+icons) → nutui → echarts → nut-runtime → mobile-setup → property-panel → sortable/ruler/vue-draggable-plus → **jquery → dyn-lib → designer.core → designer.utils → designer.js**。

## 4. 数据库设计

连接库 `VueLib`（SQL Server，连接串见 appsettings）。脚本按序在 `sql/` 目录。

### 4.1 低代码表

**ComponentDefinitions**（组件实现，可回退 Razor）：
`Id, ComponentName(唯一), ComponentType(Common/Page), RoutePath, TemplateContent, ScriptContent, StyleContent, PropertyConfigJson, DefaultConfigJson, Description, IsEnabled, SortOrder, CreatedAt, UpdatedAt`

**ComponentMeta**（设计器注册清单）：
`Id, ComponentName(唯一), ComponentType(1表单/2容器/3展示/4通用), Category, Label, Icon, DefaultConfigJson, DefaultOptionsJson, PropertyConfigJson, IsComposite, CompositeConfigJson, UiLibrary, CustomScriptJson, LoadUrl, Description, IsEnabled, SortOrder, CreatedAt`

**PageSetting**（低代码页面配置）：
`Id, PageName, PageCode(唯一), Category, Icon, ConfigJson(nvarchar max 组件树), DefaultModelJson, ApiBaseUrl, Description, Platform(mobile/desktop), CustomScriptJson, IsEnabled, SortOrder, CreatedAt, UpdatedAt`

### 4.2 桌面表

**DesktopShortcut**（快捷方式）：`Id, Name, Icon, Url, OpenType(iframe/newtab/window), SolutionId, PosX, PosY, Width, Height, SortOrder, IsEnabled`
**DesktopSolution**（解决方案/分组）：`Id, Name, Icon, Description, SortOrder, IsEnabled`

### 4.3 动态工程表

| 表 | 关键字段 | 说明 |
|---|---|---|
| `DynProject` | `Name, ConnectionString, DatabaseName, Type(Web/Phone/PC)` | 一工程一业务库 |
| `DynPage` | `ProjectId, Name, PageType(Summary/Detail), TableName, DataSource(Dynamic/View), ViewName, ColumnDefs(JSON), DetailPageId` | 汇总屏/细节屏定义 |
| `DynTemplate` | `ProjectId, Name, Code, TemplateType(List/Home/Custom), FilterPageId, SummaryPageId, DetailPageId, Config(JSON)` | 三屏组装模板 |
| `DynWebPage` | `ProjectId, Route, Name, TemplateId, Config(JSON), IsHome` | 路由 ↔ 模板 |

`ColumnDefs`（DynPageDefinition）结构：`{ PrimaryKey, IsIdentity, PageSize, OrderBy, OrderDir, Columns:[{Name,Label,DbType,SqlType,IsFilter,FilterOp,IsGrid,IsForm,Control,Options,Required,Width,IsReadOnly,Order}], Navs:[{NavKey,Relation,TargetTable,FkColumn,DisplayColumns}] }`——一个 JSON 同时驱动筛选、表格、表单控件、外键导航。

## 5. 核心流程

### 5.1 页面设计与预览

```text
[设计器] 拖组件 → 写 configObj(组件树 JSON) → 保存 POST /api/lowcode/page → PageSetting.ConfigJson
   │
   ▼
[预览] /designer/preview?code=xxx → fetch /api/lowcode/page/{code} → JSON.parse(configJson)
   │
   ▼
注册组件（/api/lowcode/components → nutLoadCom）→ NDynamicCom 递归渲染组件树
   │  每个节点：<component :is="node.component"> + props(jsonconfig, parentmodelinfo, nodePath)
   ▼
模型绑定 modelinfo：相对 modelname 路径（a.b.c / a[0].b）→ lodash get/set → 页面数据模型
```

### 5.2 动态工程运行时（DynRun）

```text
GET /DynRun/Route?projectId=1&route=/drawings
  ├─ DynProject → FindWebPage(route) → DynTemplate
  ├─ List 模板：EffectivePageIds → (FilterPage, SummaryPage, DetailPage)
  ├─ 初始数据：无自定义 DataUrl → 按 SummaryDef + FilterDef 动态查询（BuildQueryDef → ListPaged）
  ├─ 加载外键导航 LoadNavs（多对一 object / 一对多 array 注入）
  └─ 渲染 RouteList（dyn-lib 属性驱动 UI：dyn-init + dyn-click-* 局部刷新）

POST /DynRun/Route/List → 分页筛选数据（JSON）
POST /DynRun/Detail   → 细节屏表单（按 ColumnDefs 生成控件）
POST /DynRun/Save     → 主键>0 更新 / 否则新增（动态 Insert/Update）
POST /DynRun/Delete   → 动态删除
```

## 6. API 汇总

| API | 说明 |
|---|---|
| `GET /api/lowcode/components?type=` | 设计器组件元数据 |
| `GET /api/lowcode/pages` · `GET /api/lowcode/pages/paged` | 页面列表 / 分页+筛选 |
| `GET /api/lowcode/page/{code}` · `POST /api/lowcode/page` · `DELETE /api/lowcode/page/{id}` | 页面读取/保存/删除 |
| `GET /api/lowcode/components/all` · `POST /api/lowcode/component` · `DELETE .../component/{id}` | 组件管理 |
| `GET /api/component/list` · `GET /api/component/define/{name}` · `POST /api/component/defines` · `GET /api/component/pages` | 组件定义动态加载 |
| `GET /api/desktop/shortcuts` · `POST /api/desktop/shortcut` · `POST /api/desktop/shortcut/{id}/window` · `GET /api/desktop/solutions` · `POST /api/desktop/solution` | 桌面快捷方式/方案 |
| `GET/POST /api/dynproject/*` | 工程/页面/模板/路由页面/动态 SQL |

响应统一包装：`{ success, message, data }`（`ApiResponse<T>`）。

## 7. 关键设计决策与命名规范

1. **Dyn 前缀命名**：`DynN*`（NutUI）/ `DynEl*`（Element Plus），避免与 UI 库全局组件冲突（历史教训：`ElInput` 裸名冲突曾导致组件与数据库定义错乱）；
2. **属性驱动 UI**：Razor 输出 `dyn-init` 容器 + 服务端分部视图局部刷新，取代传统 JS 框架的前后端数据交互样板代码；
3. **组件即数据**：组件定义入库/入 Razor View，运行时不构建；容器组件白名单（Form/CellGroup/DivContainer/Grid/ElCard/ElRow/ElCol/ElTabs）可嵌套拖拽；
4. **组合组件**：`CompositeConfigJson` 描述内部树 + `exposedProps`（开放属性，`target` 指向内部节点路径如 `childrenctrls[0].options.labeloptions.label`）+ `openContainers`（开放容器插槽），实现"封装 + 局部开放"；
5. **相对 modelname 绑定**：子组件按父前缀拼接路径（`~` 绝对路径 / `[0]` 数组下标），支持嵌套数组与对象；
6. **动态工程按库隔离**：`DynProject.ConnectionString` 运行时创建独立 SqlSugarClient，主库只存"定义"，业务数据在业务库；
7. **可空映射**：SqlSugar 全局 EntityService 把可空 string 引用映射为可空列，代码优先与脚本建表一致。

## 8. 演进方向（建议）

- 组件属性面板（PropertyConfigJson）从"手工 JSON"演进为可视化配置器（当前已支持在属性面板勾选开放属性）；
- 模板参数（`DynTemplateConfig` 的 `DataUrl/DetailOpenPath/AddParams/DeleteUrl`）用 `DynamicCom` 可视化配置；
- 设计器标尺/缩放/手机电脑双画布、组合组件目录分类等已规划能力持续完善；
- 路由页面管理/页面管理的 grid+filter+分页（Element Plus table）落地为通用管理模板。
