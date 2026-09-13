# VueLib V3 系统设计

> 本文档描述 V3 的整体架构、双库设计、平台元数据表、前端 dyn 四层、动态 CRUD 与 Area 路由。

## 目录

- [1. 整体架构](#1-整体架构)
- [2. 双库设计](#2-双库设计)
- [3. 平台元数据表](#3-平台元数据表)
- [4. 前端 dyn 四层架构](#4-前端-dyn-四层架构)
- [5. 动态 CRUD 设计](#5-动态-crud-设计)
- [6. Area 路由设计](#6-area-路由设计)
- [7. 请求流转示例](#7-请求流转示例)

---

## 1. 整体架构

```
┌──────────────────────────────────────────────────────────────────────┐
│                              浏览器（无构建栈）                        │
│  Vue3 (vue.global.prod.js)  Element Plus  Lodash  Sortable           │
│  ┌────────────┬───────────────┬──────────────┬───────────────────┐  │
│  │ dyn-core.js │ dyn-actionhelper│ dyn-loader.js │    dyn-com.js     │  │
│  │ 核心工具     │ 动作系统(DB注册)│ 启动拉取/注册  │ 节点内核 clean/   │  │
│  │ model/fetch │ dyn-click-{code}│ /api/components│ normalize/create  │  │
│  │ message/... │ runChain        │ /api/action.. │ 节点JSON标准       │  │
│  └────────────┴───────────────┴──────────────┴───────────────────┘  │
└───────────────────────────────┬──────────────────────────────────────┘
                                │ fetch（JSON / cshtml 视图文本）
┌───────────────────────────────▼──────────────────────────────────────┐
│                     ASP.NET Core 7 MVC（Program.cs）                   │
│  ┌─────────────┐  ┌──────────────┐  ┌─────────────────────────────┐  │
│  │ Platform API │  │ Runtime CRUD │  │ Home（桌面/设计器/demo 视图）│  │
│  │ /api/*       │  │ /Runtime/... │  │ /  /designer  /demo/*       │  │
│  └──────┬───────┘  └──────┬───────┘  └─────────────────────────────┘  │
│         │ AppDbContext     │ BusinessDbFactory（按 Project 连接串建）    │
└─────────┼─────────────────┼───────────────────────────────────────────┘
          │                 │
┌─────────▼───────┐   ┌────▼────────────┐
│ VueLibV3_Platform│   │ VueLibV3_Business │
│ （元数据库）     │   │ （业务库）        │
│ 8 张平台表       │   │ Student 等业务表  │
└─────────────────┘   └──────────────────┘
```

关键设计原则：

- **无构建栈**：前端不打包，`_Layout.cshtml` 里按固定顺序 `<script>` 引入 4 个 vendor UMD + 4 个 dyn JS。改前端直接改文件、刷浏览器即可。
- **元数据驱动**：组件长什么样、有哪些动作、页面用什么模板，全部存在 Platform 库；前端启动时拉取并动态注册。
- **无需 model 的动态 CRUD**：业务表不用建 C# 实体，给定表名 + 参数化 SQL 即可增删改查。

## 2. 双库设计

| 库 | 连接串节点 | 用途 | 建库方式 |
|---|---|---|---|
| `VueLibV3_Platform` | `ConnectionStrings:Platform` | 存低代码平台自身的元数据：组件、动作、模板、页面、工程、桌面、词典 | 首启动 `db.DbMaintenance.CreateDatabase()` + CodeFirst `InitTables` |
| `VueLibV3_Business` | `ConnectionStrings:Business` | 存业务表（如 `Student`），运行时由 `DynCrudService` 动态操作 | 首启动自动建库；业务表由 DBA 建或种子建示例表 |

代码对应关系：

- `Data/AppDbContext.cs`
  - `AppDbContext`：Platform 库的 `ISqlSugarClient` 工厂，`EnsureInit()` 负责建库 + CodeFirst 建 8 张表。
  - `BusinessDbFactory`：业务库客户端工厂，`Create(connectionString)` 可传连接串，**不传则回落到配置里的 `Business` 连接串**。
- 多工程多库：`DynProject.ConnectionString` 字段存每个工程自己的业务库连接串。`DynCrudController.GetConn(projectId)` 按 `projectId` 查 `DynProject`，有自定义连接串就用它，否则用默认 Business 库。

```csharp
// GetConn(projectId) 逻辑（DynCrudController.cs）
if (projectId > 0) {
    var proj = db.Queryable<DynProject>().Where(p => p.Id == projectId).First();
    if (proj?.ConnectionString != null) return proj.ConnectionString;
}
return null; // BusinessDbFactory.Create(null) → 用配置默认 Business 连接串
```

## 3. 平台元数据表

首启动由 `AppDbContext.EnsureInit()` 注册并 CodeFirst 建表（共 8 张实体表，预留扩展位）：

| 表名 | 实体 | 作用 | 关键字段 |
|---|---|---|---|
| `ComponentMeta` | `ComponentMeta` | 组件注册表（dynCom 唯一内核的组件清单） | `ComponentName`(唯一)、`Label`、`Category`(data/container/wrapper/slot/composite/system)、`LoadUrl`、`DefaultConfigJson`、`PropertyConfigJson`、`IsContainer`、`IsComposite`、`CompositeConfigJson` |
| `DesktopSolution` | `DesktopSolution` | 桌面解决方案（1 个 Solution → 多 Project） | `Name`、`Icon`、`SortOrder` |
| `DesktopShortcut` | `DesktopShortcut` | 桌面快捷方式 | `Name`、`Icon`、`Url`、`OpenType`(iframe/newtab/window)、`SolutionId`、`PosX/PosY/Width/Height` |
| `DynActionHelper` | `DynActionHelper` | 动作脚本注册表（含 JS 函数体） | `Code`(=dyn-click-{code})、`Category`(flow/data/window/ui/system)、`ParamsSchema`、`ScriptContent`(`function(ctx){...}`)、`IsBuiltin` |
| `DynDict` | `DynDict` | 平台级数据词典 | `DictType`、`DictCode`、`DictText`、`DictValue`、`SortOrder` |
| `DynProject` | `DynProject` | 动态工程（挂在 Solution 下，对应一个业务库） | `SolutionId`、`Name`、`ConnectionString`、`DatabaseName`、`Type` |
| `DynTemplate` | `DynTemplate` | 页面模板（CRUD 列表/左树右列表/九宫格/自定义） | `Code`、`TemplateType`、`RenderView`、`ParamSchema`、`DefaultConfig` |
| `DynWebPage` | `DynWebPage` | 动态页面（选模板 + 填参数 = 真实页面） | `ProjectId`、`Route`、`TemplateId`、`Config`、`Params`、`LayoutJson`、`IsHome` |

关系：

```
DesktopSolution 1───* DynProject 1───* DynWebPage *───1 DynTemplate
       │
       └──* DesktopShortcut

ComponentMeta（全局组件注册表，独立于工程）
DynActionHelper（全局动作注册表，独立于工程）
DynDict（平台级词典；业务词典放业务库，表名约定 Dict_xxx）
```

## 4. 前端 dyn 四层架构

四个 JS 文件按 `_Layout.cshtml` 中的顺序加载，职责严格分层：

| 层 | 文件 | 职责 | 对外命名空间 |
|---|---|---|---|
| 核心 | `dyn-core.js` | 全局配置 `DYN_CFG`、i18n、`getByPath/setPathVal`、model 读写、`postJSON`、`message/confirmAsync`、`openModal/closeModal`、`reload/updateEl`、EventBus、`nextId` | `window.dyn` |
| 动作 | `dyn-actionhelper.js` | 从 `/api/actionhelpers` 拉脚本并 `eval` 注册；`dyn-click-{code}` 事件委托；`run/runChain` | `window.dynActionHelper` + `dyn._actionHelpers` |
| 加载器 | `dyn-loader.js` | 启动时 `bootstrap()` 并行拉组件 meta + 动作；按名称分组；按需拉组件 cshtml 视图 | `window.dynLoader` |
| 组件内核 | `dyn-com.js` | 节点 JSON 标准、`cleanNode`（保存清理）、`normalizeNode`（加载补默认值）、`createNode`、组合组件解包、容器判断 | `window.dynCom` |

启动时序：

```
浏览器加载 _Layout
  → vue.global.prod.js / lodash / element-plus / sortable（UMD）
  → dyn-core.js        （挂载 window.dyn，打印 "dyn-core v3 loaded"）
  → dyn-actionhelper.js（挂载 window.dynActionHelper，绑定 document click 委托）
  → dyn-loader.js      （挂载 window.dynLoader）
  → dyn-com.js         （挂载 window.dynCom）
  → 各页面 onMounted 里调用 dynLoader.bootstrap()
       → fetch /api/components      → _componentMetaMap
       → fetch /api/actionhelpers   → 每条 scriptContent eval('('+...+')') → dyn._actionHelpers[code]
```

### 4.1 节点 JSON 标准结构

`dyn-com.js` 文件头注释即权威定义：

```json
{
  "component": "DynNInput",
  "modelname": "UserName",
  "options": {
    "comoptions": { "placeholder": "请输入", "clearable": true },
    "comlisteners": { "change": { "action": "message", "args": { "type": "success", "text": "已改" } } },
    "labeloptions": { "label": "用户名", "required": true, "show": true },
    "itemoptions": { "style": { "width": "100%" }, "class": "" }
  },
  "validators": [ { "rule": "required", "message": "必填" } ],
  "childrenctrls": [],
  "slots": { "footer": [] },
  "extendinfo": {}
}
```

| 字段 | 含义 |
|---|---|
| `component` | 组件名，对应 `ComponentMeta.ComponentName`；缺省根节点为 `DynElForm` |
| `modelname` | 绑定到 Vue 响应式 model 的字段路径（支持 `a.b[0].c`） |
| `options.comoptions` | 组件自身属性（透传给 Element Plus 控件） |
| `options.comlisteners` | 事件 → 动作链映射 |
| `options.labeloptions` | 表单标签：`label` / `required` / `show` |
| `options.itemoptions` | 外壳样式：`style` / `class`（Grid 跨列写在这里） |
| `validators` | 校验规则数组 |
| `childrenctrls` | 容器子节点数组（递归同结构） |
| `slots` | 命名插槽内容 |
| `extendinfo` | 扩展预留 |

### 4.2 保存清理（cleanNode）与加载规范化（normalizeNode）

- **保存时**：`dynCom.cleanNode(node)` 深度遍历，值为 `null / undefined / "" / [] / {}` 的字段全部剔除，`childrenctrls` 过滤掉没有 `component` 的残节点。目的：库里只存有效值，布局 JSON 干净。
- **加载时**：`dynCom.normalizeNode(node)` 递归补默认值——`component` 缺省 `DynElForm`，`options.comoptions/comlisteners/labeloptions/itemoptions` 缺省空对象，`validators/childrenctrls/slots/extendinfo` 缺省空数组/对象，再递归规范化子节点。

```javascript
// 保存
const saved = JSON.stringify(dynCom.cleanNode(rootNode));
// 加载
const node = dynCom.normalizeNode(JSON.parse(json));
```

## 5. 动态 CRUD 设计

目标：**业务表不用建 C# Model**。前端只传表名 + 数据，后端用 ADO 参数化 SQL 操作。

服务：`Services/DynCrudService.cs`，入口：

| 接口 | 方法 | 请求体 / 参数 | 作用 |
|---|---|---|---|
| `/Runtime/DynCrud/Meta?table=Student&projectId=1` | GET | table, projectId | 返回主键列名 + 列信息（类型/可空/自增/长度/描述） |
| `/Runtime/DynCrud/Tables?projectId=1` | GET | projectId | 列出业务库所有表名（设计器选表） |
| `/Runtime/DynCrud/Query` | POST | `DynCrudQuery` | 分页 + 过滤 + 关键字模糊 + 排序 |
| `/Runtime/DynCrud/Save` | POST | `DynCrudSave` | 有主键值 → UPDATE；无主键值 → INSERT（`OUTPUT INSERTED.pk` 返回新 id） |
| `/Runtime/DynCrud/Delete` | POST | `DynCrudDelete` | 按主键删除 |
| `/Runtime/DynCrud/Get?table=&id=&projectId=` | GET | table,id | 按主键取单条 |

### 5.1 自动探测主键

`FindPk(db, tableName)` 通过 `db.DbMaintenance.GetColumnInfosByTableName(tableName)` 找 `IsPrimarykey == true` 的列；找不到就回退到 `"Id"`。前端传 `PrimaryKey` 可显式覆盖。

### 5.2 防 SQL 注入

所有表名/列名都过 `SafeName()` 正则白名单 `^[a-zA-Z_][a-zA-Z0-9_]*$`，不合法直接抛 `ArgumentException`；值一律走 `SugarParameter` 参数化。

```csharp
// WHERE 构造片段（DynCrudService.cs）
where.Add($"[{col}] = @{col}");
pars.Add(new SugarParameter($"@{col}", kv.Value));
// 分页用 ROW_NUMBER()
SELECT * FROM (
  SELECT ROW_NUMBER() OVER (ORDER BY [{orderBy}] {orderDir}) AS _rn, *
  FROM [{table}]{whereSql}
) t WHERE t._rn > @offset AND t._rn <= @offsetEnd
```

### 5.3 查询请求体示例

```json
{
  "tableName": "Student",
  "filter": { "Gender": "男" },
  "keyword": "张",
  "keywordColumns": ["Name", "ClassName"],
  "orderBy": "Id",
  "orderDir": "desc",
  "pageIndex": 1,
  "pageSize": 10
}
```

返回 `OpResult<PagedResult<Dictionary<string,object>>>`：

```json
{
  "success": true,
  "data": {
    "rows": [ { "Id": 1, "Name": "张三", "Age": 18, "...": "..." } ],
    "totalCount": 5, "pageIndex": 1, "pageSize": 10, "totalPages": 1
  }
}
```

## 6. Area 路由设计

`Program.cs` 注册了两段路由：

```csharp
app.MapControllerRoute(name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

| Area | 控制器 | 路由 | 职责 |
|---|---|---|---|
| `Platform` | `ComponentApiController` | `[Route("api")]` → `/api/components`、`/api/component-view/{name}`、`/api/actionhelpers`、`/api/dict/{type}`、`/api/projects`、`/api/templates` | 平台元数据读写（前端启动拉取） |
| `Runtime` | `DynCrudController` | 显式 `[Route("Runtime/DynCrud/...")]` | 动态 CRUD + 表元数据 |
| （无 Area） | `HomeController` | `/`、`/designer`、`/demo`、`/demo/actionhelper` 等 | 桌面、设计器、demo 页面视图 |

> `Areas/Components` 用于放组件 cshtml 视图（`/Areas/Components/Views/{Category}/{组件名}.cshtml`），由 `/api/component-view/{name}` 读取文件文本返回，前端 `dyn-loader.loadComponentView` 拉取。详见《COMPONENT-GUIDE.md》。

## 7. 请求流转示例

以“设计器拖拽一个输入框并保存”为例：

1. 进入 `/designer` → `HomeController.Designer()` → 渲染 `Designer.cshtml`。
2. 页面 `onMounted` 调 `fetch('/api/components')` → `ComponentApiController.Components()` → 读 `ComponentMeta` 表返回 21 个组件 meta。
3. 用户把 `DynNInput` 拖到画布 → 前端用 `defaultConfigJson` 创建节点。
4. 点“保存布局” → `JSON.stringify(rootNode)`（先过 `dynCom.cleanNode`）POST 出去。
5. 运行态页面加载时 → `dynLoader.bootstrap()` 拉组件 meta + 动作脚本 → 渲染节点树 → 绑定 `dyn-click-*` 事件。
6. 用户点击页面上的按钮 → 事件委托命中 `dyn-click-save` → `dynActionHelper.run(el,'save',args)` → 执行库里存的 `function(ctx){...}` → 调 `/Runtime/DynCrud/Save` → `DynCrudService.SaveAsync` 写业务库。
