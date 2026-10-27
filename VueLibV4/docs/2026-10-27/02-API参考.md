# VueLibV4 API 参考

> 基线：2026-10-27 工作区代码。前端 API 以 `window.dyn` 等全局对象为准；后端端点以控制器 `[Route]` + `[HttpXxx]` 为准。

---

## 0. 全局约定

### 0.1 后端返回信封 `ApiResult`

```json
{ "code": 0, "msg": "ok", "data": {} }
```

- `code === 0` 表示成功；非 0 时前端 `DynCall` 统一抛出并交给业务方 `catch`。
- 后端使用 Newtonsoft.Json：**不转驼峰**，属性名原样输出；未知/空值字段省略。
- 所有 `/api/*` 受 `PermissionFilter` 保护；未登录返回 401。`login` 与 `log/frontend-error` 为匿名白名单。

### 0.2 数据域 `project`（全系统唯一数据入口约定）

| 取值 | 实际库 |
|---|---|
| 省略 / 空 | 默认业务库 `ConnectionStrings:BusinessDb` |
| `__platform__` | 平台元数据库 `ConnectionStrings:PlatformDb` |
| `DynProject.Code` 或 `Id` | 该项目的独立库（`ProjectDbResolver`） |

### 0.3 前端脚本与全局对象

| 全局 | 来源 | 说明 |
|---|---|---|
| `dyn` / `dynCore` / `DynCore` | dyn-core | 挂载、片段、元素、路径、消息 |
| `DynLib` | dyn-lib | `ready(fn)`、`load`、资产版本 |
| `DynKernel` | dyn-kernel | 插件注册/装配 |
| `DynCom` | dyn-com | 组件注册表 |
| `DynCall` | dyn-call | 请求封装（GET/POST/上传） |
| `DynParams` | dyn-params | 参数上下文 |
| `DynRpc` | dyn-rpc | RPC 能力桥（`call/eval`） |
| `DynBlocks` | dyn-blocks-common | Block 句柄与契约 |
| `DynLayouts` | dyn-layout-engine | 布局壳运行时 |
| `DynDsl` | dyn-dsl | DSL ⇄ JSON |
| `DynActionHelper` / `DynAction` | dyn-action | 动作管道 |
| `DynPermission` | dyn-permission | 前端权限渲染 |
| `DynDebug` | dyn-debug | 调试浮层 |
| `DynEventBus` | dyn-eventbus | 跨 App 事件 |

---

## 1. 前端 API

### 1.1 `dyn`（dyn-core）

| 分类 | API | 说明 |
|---|---|---|
| 常量 | `VERSION`、`CFG` | 4.0.0；全局配置（含 `disableEvalJs` 等） |
| 挂载 | `mount(el, cfg)` | 创建 Vue App 并挂载，返回 app 实例 |
| | `mountConfig(el)` | 读取元素上的 `dynconfig`/`dynmodel` 后挂载 |
| | `unmount(el)`、`render(el)` | 卸载 / 重新渲染 |
| | `getApp(el)`、`getClosestApp(el)` | 取所属 App 实例 |
| 作用域 | `getProxy(el)`、`getModel(el)`、`getScopeModel(el, name)` | 取响应式 model / 具名作用域 |
| | `resolve(target)`、`findAncestor(el, selector)`、`closestDynInit(el)` | 选择器/元素/`dyn-init` 祖先解析 |
| 片段 | `fetchPartial(url, data)` | 按内容类型返回 HTML / JSON / text |
| | `reload(el)` | 就地重载当前容器片段 |
| | `html(el, str)` | 安全写入 HTML 片段 |
| 表单 | `serializeForm(el)`、`collectParams(el)` | 表单/容器参数收集 |
| 参数 | `params(el)`、`attachParams(el, p)` | 上下文参数读写 |
| 路径 | `getByPath(obj, path)`、`setPathVal(obj, path, val)` | 点路径读写（优先 lodash） |
| UI | `showMessage(type, text)`、`confirmAsync(text, title)`、`getLayer()` | 统一消息/确认/layui layer |
| 模型 | `normalize(cfg)`、`clean(obj)`、`deepClone(v)`、`setDynCfg(cfg)` | 配置规范化/清理 |
| 扩展 | `installActionApi(api)` | 把动作实现平铺到 `dyn.*` |

### 1.2 `DynLib`（dyn-lib）

| API | 说明 |
|---|---|
| `DynLib.ready(fn)` | 内核与全部模块就绪后执行（等价 `DOMContentLoaded` 之后的安全入口） |
| `DynLib.load()` | 手动触发脚本链加载（一般无需调用） |
| `DynLib.version` | 资产版本（4.4.4） |

### 1.3 `DynCom`（dyn-com）

| API | 签名 | 说明 |
|---|---|---|
| `register(name, comp)` | — | 注册组件 |
| `get(name)` / `meta(name)` | — | 取组件定义 / 元信息 |
| `loadMeta()` | — | 拉取 `/api/platform/componentmeta/all` |
| `setupApp(app)` | — | 安装 Element Plus、Icons、全局组件 |
| `ensureRegistered(app)` | — | 按 `/api/component/list` 为每个 App 注册组件（含 `DynDynamicCom`） |
| `registerComponents(app, list)` | — | 批量注册（含 `DynLoadCom` 懒加载包装） |
| `compositeComponents` | 数组 | 组合组件清单 |
| `applyCompositeProps` | — | 组合组件默认属性合并 |

### 1.4 `DynCall`（dyn-call）

| API | 说明 |
|---|---|
| `DynCall.get(url, params)` | GET，返回 `ApiResult.data`；`code!==0` 抛错 |
| `DynCall.post(url, body)` | POST JSON |
| `DynCall.upload(url, file, extra)` | 上传（FormData） |
| `DynCall.raw(method, url, body)` | 返回完整信封，不自动解包 |

### 1.5 `DynParams`（dyn-params）

| API | 说明 |
|---|---|
| `ensure(el, parentEl)` | 建立/复用上下文（挂载点维度） |
| `attach(el)` | 把片段节点接入最近上下文 |
| `fromEl(el)` | 取元素所属上下文 |
| `byId(id)` | 取游离容器上下文（`data-dyn-ctx`） |
| `root()` | 根上下文 |
| `set(ctx, key, val)` / `get(ctx, key)` | 写入 / 读取（写入触发 `watch`） |
| `commit(ctx, obj)` | 批量提交 |
| `fork(parentEl, el)` | 复制父上下文（弹窗/抽屉隔离） |
| `watch(ctx, key, fn)` | 订阅键变化，返回取消函数 |
| `view(ctx)` | 只读快照 |
| `resolveValue(v, ctx)` | 解析 `{{$params.x}}` / `{{$model.x}}` / `{{$result.x}}` |
| `getPath(obj, path)` / `parseJson(str)` | 工具 |
| `trace()` / `onTrace(fn)` | 变更轨迹（最近 100 条）/ 订阅 |

### 1.6 `DynBlocks`（dyn-blocks-common）

| API / 常量 | 说明 |
|---|---|
| `DynBlocks.URLS` | `search/get/save/delete` 等统一端点常量 |
| `DynBlocks.PLATFORM_PROJECT` | `'__platform__'` |
| `DynBlocks.CONTRACTS` | 四类角色的入命令/出事件契约 |
| `DynBlocks.createHandle(el)` | 创建 Block 句柄 |
| `DynBlocks.fetchConfig(el)` | 读取并解析 `data-blk-config` |
| 句柄 | `reg(cmd,fn)`、`onReady(fn)`、`ready()`、`send(cmd,payload)`、`on(evt,fn)`、`emit(evt,payload)`、`introspect()`、`destroy()` |

### 1.7 `DynLayouts`（dyn-layout-engine）

| API | 说明 |
|---|---|
| `register(name, shell)` | 注册壳（`name/layout/slots/defaultWires/commands/onMount`） |
| `get(name)` / `list()` | 取壳 / 列壳 |
| `mount(rootEl)` | 挂载壳内全部 Block，建立 wire，`ready` 握手；返回 `destroy()` |
| `validate(spec)` | 静态校验 spec |

### 1.8 `DynDsl`（dyn-dsl）

| API | 说明 |
|---|---|
| `parseDsl(text)` | DSL 文本 → 组件树 |
| `toDsl(tree)` | 组件树 → DSL 文本 |
| `ALIASES` | 标签别名表 |
| `loadMeta()` / `getHintIndex()` / `buildHintIndex(meta)` | 补全元数据 |
| `extractProps(meta)` | 提取组件可配属性（参数面板用） |
| `attachDslHints(el)` | 绑定 CodeMirror 补全 |
| `markDslIssues(cm, issues)` | 标记 lint 问题 |
| `defineDslMode(...)` | 注册 CodeMirror 模式 |

### 1.9 `DynActionHelper`（dyn-action）

```js
DynActionHelper.run(code, options, ctx)   // 执行单个动作
DynActionHelper.actionList()              // [{name,label,group,doc,options,example,source,...}]
DynActionHelper.getMeta(code)             // 动作元信息
DynActionHelper.defineAction(code, fn, meta)  // 注册自定义动作
```

`ctx` 字段：`element`、`model`、`params`、`options`、`$event`、`$result`、`$callStack`。

**33 个内置动作**（文档按用途归类）：

| 归类 | 动作名 |
|---|---|
| 应用 | `createapp`、`setdyncom` |
| 请求/数据 | `postback`(=postdata)、`reload`、`grid`、`load`、`updateel`(=submit、reloadtarget) |
| 弹窗/窗口 | `open`(=openwindow)、`close`(=closewindow)、`setwin`(=setwindow) |
| 消息/确认 | `toast`(=showmessage)、`notify`、`confirm` |
| 模型/变量 | `setVueModel`、`setvar`、`calc`、`setparams`、`setattr` |
| 流程 | `chain`、`delay`、`switch`、`triggerevent`、`redirect` |
| 工具 | `copy`、`download`、`evaljs` |

> 别名通过 `ACTION_DOCS.aliasOf` 归一，`ActionHelper.Xxx` 与 `Xxx` 等价。

**数据库动作**：`GET /api/platform/dynactionhelper/all` → `registerDbAction`；`ActionType` 支持 `api | chain | script | url`；注册后带 `source:'db'`。

### 1.10 `DynRpc`（dyn-rpc）

| API | 说明 |
|---|---|
| `DynRpc.services()` | 服务目录（`RpcServiceCatalog`） |
| `DynRpc.call(service, method, args)` | `POST /api/rpc/invoke/{service}/{method}` |
| `DynRpc.eval(expr, params)` | `POST /api/rpc/eval`（DynamicExpresso；受 `Dyn:Rpc:EvalEnabled` 控制） |

### 1.11 `DynPermission`（dyn-permission）

| API | 说明 |
|---|---|
| `load()` | `GET /api/platform/permission/me` 缓存权限集 |
| `can(key)` / `hasField(table, field, op)` | 判定 |
| `apply(rootEl)` | 扫描 `data-permission-op`/`data-permission-field`，默认 disable，`data-permission-mode="hide"` 隐藏 |

---

## 2. 配置契约

### 2.1 组件节点（PageSetting `ConfigJson` / 设计器 / DSL 共用）

```json
{
  "component": "DynElInput",
  "modelname": "UserName",
  "options": {
    "comoptions": { "placeholder": "请输入", "sourceType": "static", "optionValuesText": "1,启用\n0,停用" },
    "comlisteners": {},
    "labeloptions": { "label": "用户名", "required": true, "show": true },
    "itemoptions": { "style": {}, "class": "" },
    "wrapperoptions": {},
    "compassthrough": "comoptions.size,labeloptions.labelwidth",
    "uiGlobalProps": { "size": null, "disabled": null },
    "defaultLabelOptions": { "labelwidth": "auto", "requiredmark": true },
    "evalCondition": ""
  },
  "validators": [
    { "type": "required", "message": "必填" },
    { "type": "length", "min": 2, "max": 20 },
    { "type": "custom", "expr": "value.length>2", "enableWhen": "Type==='A'" }
  ],
  "childrenctrls": [],
  "slots": {},
  "extendinfo": {}
}
```

| 节点字段 | 必填 | 说明 |
|---|---|---|
| `component` | 是 | 组件注册名（`Dyn*`）或 Element 标签（`el-input`） |
| `modelname` | 否 | 绑定路径；容器内自动拼 `parentModelPrefix` |
| `options.comoptions` | 否 | 组件属性（含 `sourceType`/`optionValuesText`/`ajaxUrl` 等） |
| `options.labeloptions` | 否 | 标签属性（`label/required/show/labelwidth/labelposition`） |
| `options.compassthrough` | 否 | 父级属性继承白名单（逗号分隔 `comoptions.x`） |
| `options.evalCondition` | 否 | 条件脚本，返回 `{shell,labeloptions,comoptions}` 补丁 |
| `validators` | 否 | 校验器数组，`enableWhen` 条件启用 |
| `childrenctrls` | 否 | 子组件树（容器类组件） |
| `slots` | 否 | 具名插槽 → 节点数组 |
| `extendinfo` | 否 | 业务扩展信息（设计器/PageGen 用） |

**列定义**（`SettingType=List` 的 `ConfigJson.columns[]`）：

```json
{ "prop": "Name", "label": "姓名", "width": 160, "align": "left", "sortable": true,
  "formatter": "", "show": true, "fixed": "", "component": "DynElTag" }
```

### 2.2 页面实例参数 `ParamsJson`

```json
{
  "blocks": {
    "filter": { "settingId": 12, "model": {} },
    "list":   { "settingId": 13, "model": { "loadUrl": "/api/dyndata/search" } },
    "detail": { "settingId": 14, "model": {} }
  }
}
```

兼容旧扁平键：`FilterPageSettingId` / `ListPageSettingId` / `DetailPageSettingId`、`ListUrl`（由 `DynPageViewHelper.SlotSettingId/SlotValue` 回退）。

### 2.3 布局壳规格 `SpecJson`（v1.1，唯一权威）

```json
{
  "layout": "filter-list-open-window",
  "layoutProps": { "split": 320 },
  "provide": { "project": "demo" },
  "slots": {
    "filter": { "block": "filter", "settingId": 12, "model": {} },
    "master": { "block": "list",   "settingId": 13, "model": {} },
    "detail": { "block": "detail", "settingId": 14, "model": {} }
  },
  "wires": [
    { "from": "filter.changed", "to": "master.loadData", "with": "$event" },
    { "from": "master.edit",    "to": "detail.editForm", "with": "$event" },
    { "from": "detail.saved",   "to": "master.reload" },
    { "from": "detail.saved",   "to": "shell.closeDetail" }
  ],
  "replaceDefaultWires": false
}
```

| 字段 | 说明 |
|---|---|
| `layout` | 已注册壳名（见 §1.7 / 视图 `DynLayouts`） |
| `slots[name].block` | 期望角色：`filter/list/detail/tree` 等 |
| `slots[name].settingId` | 该槽位 Block 的 PageSetting Id |
| `slots[name].model` | Block 运行参数（`loadUrl`、`saveUrl` 等，空则走缺省） |
| `wires[].from` | `节点.事件` |
| `wires[].to` | `节点.命令` 或字符串数组 |
| `wires[].with` | 载荷模板：`$event`、`$params.x`、`{ ... }` |
| `replaceDefaultWires` | `true` 时丢弃壳默认连线 |

---

## 3. 后端端点

### 3.1 页面渲染（Platform，返回 HTML/Partial）

> 页面入口已按职责拆分到 `PageShellController` / `PageDetailController` / `PageBlockController` / `PagePermissionController` / `PageChartController`。

| 方法 | 路由 | 入参 | 说明 |
|---|---|---|---|
| GET | `/Platform/Shell/Desktop` | — | 桌面（默认首页） |
| GET | `/Platform/Shell/Designer` | `embed` | 可视化设计器 |
| GET | `/Platform/Shell/PageRender` | `code`/`id` | 页面实例渲染（壳 + Block） |
| GET | `/Platform/Shell/Runtime` | `id` | 兼容入口 |
| GET | `/Platform/Detail/Modal` | `id,rowId,project,prefill` | 详情弹窗（v1） |
| GET | `/Platform/Detail/ModalV2` | 同上 | 详情弹窗（v2） |
| GET/POST | `/Platform/Block/Load` | `blockName,webpageId,...` | 加载 Block 片段（可带实例参数） |
| GET | `/Platform/Block/Inspector` | `webpageId` | 运行时诊断数据 |
| POST | `/Platform/Block/SaveParams` | `SaveParamsRequest` | 保存页面实例参数 |
| GET | `/Platform/Block/ParamsPanel` | `templateId,pageId,raw` | 参数面板片段 |
| GET | `/Platform/Chart/Data` | — | 图表示例数据 |
| GET | `/Platform/Detail/Form` | `settingId,bizId,table` | 通用详情页 |
| GET | `/Platform/Permission/Grid` | `roleId,project,execId` | 权限矩阵 |
| GET | `/Platform/Tools/ComponentCheck` | — | 组件注册自检 |
| GET | `/Platform/Tools/PageGraph` / `/Platform/Tools/PageGraphData` | `id` | 页面结构图 |
| GET | `/Platform/Tools/DbErGraph` | — | 数据库 ER 图 |
| GET | `/Platform/Tools/MdViewer` | `path` | Markdown 预览 |
| GET | `/Platform/Page/PageGen` / `Dsl` / `MetaDsl` | `componentId` | 生成向导 / DSL 工作台 |
| GET | `/Platform/Page/Login` / `ChangePassword` | — | 登录 / 改密页（匿名） |

Demo 页（`DemoController`，全部 GET）：`/Platform/Page/Demo/{ActionHelper|Combination|Grid|Crud|Gallery|TemplateImport|CodeMirror|Rpc|TabsParams|CascadeSelect}`、`/Platform/Page/{TabsBlockDemo|KanbanDemo|WidgetDemo|ListViewDemo|MacDesktopDemo}`。

### 3.2 页面生成（Business）

| 方法 | 路由 | 入参 | 说明 |
|---|---|---|---|
| POST | `/api/business/pagegen/generate` | `{table,project?,fields?,...}` | 规则生成三屏 PageSetting（+ 可选 WebPage/壳） |
| GET | `/api/business/pagegen/fields` | `table,project?` | 字段元数据 |
| POST | `/api/business/pagegen/preview` | 同 generate | 只返回配置不落库 |
| GET | `/api/business/pagegen/templates` | — | 可用模板清单 |
| POST | `/api/business/pagegen/ai` | `{prompt,tableName?,project?}` | AI 生成，返回 `{filter,list,detail,reasoning}` |

### 3.3 数据面（根 Area）

| 方法 | 路由 | 入参 | 说明 |
|---|---|---|---|
| GET | `/api/dyndata/tables` | `project` | 表清单 |
| GET | `/api/dyndata/columns` | `table,project` | 列清单 |
| GET | `/api/dyndata/meta` | `table,project` | 表+列+主键元数据 |
| POST | `/api/dyndata/search` | `{table,page,size,filter,sort?,project?}` | 分页查询 |
| GET | `/api/dyndata/get` | `table,id,project` | 单条 |
| POST | `/api/dyndata/insert` / `update` / `save` | `{table,data,project?}` | 增 / 改 / 自动判增改 |
| POST | `/api/dyndata/saverelations` | `{table,data,relations,...}` | 主子表联动保存 |
| GET | `/api/dyndata/getrelations` | `table,id,relations,project` | 读取关联 |
| POST | `/api/dyndata/delete` | `{table,keys,project?}` | 删除 |
| POST | `/api/dyndata/batch` | `{table,rows,action,project?}` | 批量操作 |

### 3.4 平台元数据（`/api/platform/*`）

| 控制器 | 路由前缀 | 主要端点 |
|---|---|---|
| Permission | `permission` | `GET me`、`POST login`(匿名)、`logout`、`changepassword`、`roles`、`role/save`、`role/delete`、`resources`、`resource/save`、`resource/delete`、`resource/scan`、`granted?roleId`、`grant`、`users`、`user/save`、`user/delete`、`userroles`、`userrole/add`、`userrole/remove` |
| PageSetting | `pagesetting` | `GET all?type&projectId`、`get?id`、`POST save`、`delete`、`generateview` |
| DynPage | `dynpage` | `GET all?projectId`、`get?id`、`POST save`、`delete` |
| DynPageRender | `dynpage` | `GET render?id&code`、`paramschema?templateId&pageId` |
| DynPageTemplate | `dynpage` | `POST export-template`、`ext-skeleton` |
| DynTemplate | `dyntemplate` | `GET all?category`、`get?id`、`config?id`、`POST save`、`delete` |
| DynBlock | `dynblock` | `GET all?role`、`get?id`、`POST save`、`delete` |
| DynCom | `dyncom` | `GET all?category`、`get?id`、`POST save`、`delete` |
| ComponentMeta | `componentmeta` | `GET all?category`、`grouped`、`get?id`、`byName?name`、`getmeta?id`、`POST save`、`delete`、`savemeta` |
| DynActionHelper | `dynactionhelper` | `GET all`、`get?id`、`script`、`POST save`、`delete` |
| DynProject | `dynproject` | `GET all?solutionId`、`get?id`、`POST test`、`save`、`delete` |
| DynDict | `dyndict` | `GET all?dictType`、`types`、`POST save`、`delete` |
| SysMenu | `sysmenu` | `GET tree`、`get?id`、`desktoplist`、`POST save`、`delete` |
| DesktopSolution | `desktopsolution` | `GET all`、`get?id`、`POST save`、`delete` |
| DesktopShortcut | `desktopshortcut` | `GET all?solutionId`、`get?id`、`POST save`、`delete` |
| DbMeta | `dbmeta` | `GET tables?conn`、`er?conn&table`、`POST label` |

### 3.5 通用能力（根 Area）

| 控制器 | 路由 | 端点 |
|---|---|---|
| Component | `/api/component` | `GET list`、`GET define/{componentName}`、`POST defines`（批量） |
| DynCommon | `/api/dyncommon` | `GET emojis?q`、`options?project&table&valueField&textField`、`dicts?type`、`sqloptions?project&sql`、`tables?project`、`tablelist?project&table`、`POST upload` |
| Rpc | `/api/rpc` | `GET services`、`POST invoke/{service}/{method}`、`POST eval` |
| AiChat | `/api/ai` | `POST chat/stream`（SSE 流式） |
| Log | `/api/log` | `POST frontend-error`（匿名，IP 限流） |

### 3.6 模板运行（Business）

| 方法 | 路由 | 说明 |
|---|---|---|
| GET | `/Business/Templates/Run` | 按模板 `code` 渲染运行页 |
| GET | `/Business/Templates/Detail` | 详情（`code,id`） |
| POST | `/Business/Templates/Save` | 模板数据保存 |

---

## 4. 配置项（appsettings.json）

| 键 | 说明 |
|---|---|
| `ConnectionStrings:PlatformDb` | 平台元数据库（Sqlite） |
| `ConnectionStrings:BusinessDb` | 默认业务库 |
| `Db:Provider` | `Sqlite`（SqlSugar 方言） |
| `Dyn:ShowRuntimeDesignEntry` | 运行时是否显示设计入口 |
| `Dyn:Auth:Enabled/AllowLocal/ApiKey/CookieSecret` | API Key 与签名 Cookie |
| `Dyn:Rpc:Enabled/EvalEnabled/MaxRows/Timeout` | RPC 开关、表达式求值、行数上限、超时 |
| `Urls` | 监听地址（当前 `http://localhost:5010`） |
| `Ai:BaseUrl/Model/TimeoutSeconds/ApiKey` | 豆包 OpenAI 兼容端点；`ApiKey` 必须留空提交 |

---

## 5. 数据实体（Platform）

| 实体 | 关键字段 | 用途 |
|---|---|---|
| `PageSetting` | `Code,Name,SettingType,ProjectId,TableName,ConfigJson,DefaultJson,RenderMode,PartialPath,GeneratedViewPath,SortNo,IsActive` | 三屏 UI 配置 |
| `DynWebPage` | `Code,Name,ProjectId,TemplateId,PageJson,ParamsJson,SpecJson,Url,ResourceKey,ExtViewPath` | 页面实例 |
| `DynTemplate` | `Code,Name,Category,ConfigJson,ParamsSchemaJson,ViewPath` | 页面模板 |
| `DynBlock` | `Code,Name,Role,AppPath,ConfigJson,SortNo` | 积木定义 |
| `DynTemplateBlock` | `TemplateId,BlockCode,Slot,ModelJson,SortNo` | 模板-积木槽位 |
| `ComponentMeta` | `ComponentName,Title,Category,PropsMeta,AllowDrop,SlotsDefine,IsContainer` | 组件元信息 |
| `DynCom` | `Name,Title,Category,LoadUrl,IsComposite,CompositeConfigJson` | 组件注册行 |
| `DynActionHelper` | `Code,Name,ActionType,ParmsJson,ScriptCode,IsEnabled` | DB 动作 |
| `DynDict` | `DictType,Code,Label,Value,SortNo` | 字典 |
| `DynProject` | `Code,Name,ConnString,DbType` | 项目数据域 |
| `DynSchemaLabel` | `Conn,TableName,ColumnName,Label` | 库表中文名 |
| `SysUser/SysRole/SysResource/SysResourcePermission/SysMenu` | 用户/角色/资源/授权/菜单 | 权限体系 |
