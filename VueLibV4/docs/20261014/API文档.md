# VueLibV4 API 文档

> 文档版本：2026-10-14
> Base URL：`http://localhost:5010`
> 认证：API 请求需登录 Cookie，或请求头 `X-Api-Key`（appsettings.json 中配置）

---

## 目录

1. [数据 API（/api/dyndata）](#1-数据-apiapidyndata)
2. [通用 API（/api/dyncommon）](#2-通用-apiapidyncommon)
3. [组件 API（/api/component）](#3-组件-apiapicomponent)
4. [平台管理 API（/api/platform）](#4-平台管理-apiapiplatform)
5. [前端 JS API](#5-前端-js-api)

---

## 1. 数据 API（/api/dyndata）

免模型动态 CRUD，不需要为每张表建实体。

### 1.1 POST /api/dyndata/search

分页查询。

**请求体**：
```json
{
  "table": "Student",
  "project": "Business",
  "where": "Age > @age",
  "params": { "age": 18 },
  "order": "Id DESC",
  "pageIndex": 1,
  "pageSize": 20,
  "select": "Id, Name, Age"
}
```

**响应**：
```json
{
  "code": 0,
  "msg": "ok",
  "data": {
    "list": [{ "Id": 1, "Name": "张三", "Age": 20 }],
    "total": 12,
    "pageIndex": 1,
    "pageSize": 20
  }
}
```

### 1.2 GET /api/dyndata/get

按主键取单行。

| 参数 | 类型 | 说明 |
|------|------|------|
| table | string | 表名 |
| id | any | 主键值 |
| project | string | 项目名（可选） |

**响应**：`{ code: 0, data: { Id: 1, Name: "..." } }`

### 1.3 POST /api/dyndata/insert

新增。

```json
{ "table": "Student", "project": "Business", "data": { "Name": "李四", "Age": 22 } }
```

**响应**：`{ code: 0, data: { id: 13 } }`（返回新主键）

### 1.4 POST /api/dyndata/update

更新。

```json
{ "table": "Student", "project": "Business", "data": { "Id": 1, "Name": "张三改" } }
```

### 1.5 POST /api/dyndata/save

自动判断 insert / update（主键有值则 update，无值则 insert）。

```json
{ "table": "Student", "data": { "Id": 0, "Name": "王五" } }
```

### 1.6 POST /api/dyndata/delete

删除。

```json
{ "table": "Student", "id": 1, "keyField": "Id" }
```

### 1.7 GET /api/dyndata/tables

列出项目下所有表。

| 参数 | 说明 |
|------|------|
| project | 项目名（可选，默认当前） |

### 1.8 GET /api/dyndata/columns

列出表字段。

| 参数 | 说明 |
|------|------|
| table | 表名 |
| project | 项目名（可选） |

---

## 2. 通用 API（/api/dyncommon）

### 2.1 GET /api/dyncommon/emojis

返回 Emoji 列表（用于图标选择器）。

### 2.2 GET /api/dyncommon/options

通用下拉选项。

| 参数 | 说明 |
|------|------|
| projectId | 项目 ID |
| table | 表名 |
| valueField | 值字段（默认 Id） |
| textField | 文本字段（默认 Name） |
| parentField | 父字段（可选，联动） |
| parentValue | 父值（可选） |

### 2.3 GET /api/dyncommon/dicts

平台字典。

| 参数 | 说明 |
|------|------|
| type | 字典类型 |

### 2.4 GET /api/dyncommon/tables / tablelist

表列表（带元信息）。

### 2.5 POST /api/dyncommon/upload

文件上传。

| 参数 | 说明 |
|------|------|
| file | multipart/form-data 文件 |

---

## 3. 组件 API（/api/component）

### 3.1 GET /api/component/define/{name}

获取组件定义（Razor View 优先，DB 回退）。

**响应**：
```json
{
  "code": 0,
  "data": {
    "templateContent": "<el-input v-model='...' />",
    "scriptContent": "export default { ... }",
    "styleContent": ".dyn-el-input { ... }"
  }
}
```

---

## 4. 平台管理 API（/api/platform）

### 4.1 GET /api/platform/dynactionhelper/all

获取所有入库动作（前端启动时自动拉取注册）。

### 4.2 GET /api/platform/pagesetting/all?type=Detail

获取 PageSetting 列表（按类型筛选，用于下拉选择）。

| 参数 | 说明 |
|------|------|
| type | Filter / List / Detail |

### 4.3 其他管理 API

| 端点 | 说明 |
|------|------|
| `/api/platform/dynwebpage/*` | 页面实例 CRUD |
| `/api/platform/dyntemplate/*` | 模板 CRUD |
| `/api/platform/dynblock/*` | 积木 CRUD |
| `/api/platform/menu/*` | 菜单管理 |
| `/api/platform/dict/*` | 字典管理 |
| `/api/platform/permission/*` | 权限管理 |
| `/api/log/frontend-error` | 前端错误上报（POST） |

---

## 5. 前端 JS API

### 5.1 dyn 对象（global.dyn）

```js
// 挂载 App
dyn.mount(element) → Promise<App>

// 重新加载容器内容
dyn.reload(element, options)

// 局部更新（服务端返回 HTML 片段替换目标）
dyn.updateEl(targetEl, url, body)

// fetchPartial：拉取片段 HTML
dyn.fetchPartial(url, body, method) → Promise<string>

// HTML 注入并自动挂载
dyn.html(element, html, autoMount, bootInfo)

// DOM 查找
dyn.findAncestor(el, selector)
dyn.closestDynInit(el)
dyn.getApp(el)
dyn.getProxy(el)

// 模型操作
dyn.getByPath(obj, path)
dyn.setPathVal(obj, path, value)
dyn.getScopeModel(el)

// 消息
dyn.showMessage(msg, type)
dyn.confirmAsync(msg) → Promise<bool>
dyn.getLayer() → Promise<layui.layer>
```

### 5.2 DynEventBus

```js
// 注册模板根
DynEventBus.registerRoot(rootEl) → execId

// 发事件
DynEventBus.emit(execId, eventName, payload)

// 监听（返回解绑函数）
DynEventBus.on(execId, eventName, handler) → offFn

// 解绑
DynEventBus.off(execId, eventName, handler)

// 销毁
DynEventBus.destroy(execId)
```

### 5.3 DynCall

```js
DynCall.post(url, body, { scopeEl, silent }) → Promise
DynCall.get(url, params, { scopeEl, silent }) → Promise
```

### 5.4 DynBlocks

```js
// 构造保存请求
DynBlocks.saveRequest(url, table, form, project) → { url, body }

// 构造删除请求
DynBlocks.deleteRequest(url, table, id, keyField, project) → { url, body }

// 构造查询单行 URL
DynBlocks.getUrl(table, id, project) → url
```

### 5.5 DynLib.ready

```js
// DOM + 所有 dyn 模块加载完成后执行
DynLib.ready(function () {
    // 此时 Vue / ElementPlus / dyn-core / dyn-action 全部可用
    dyn.initActions("body");
});
```

### 5.6 DynAction（动作系统）

```js
// 直接调用动作
DynAction.run("Submit", { url: "/api/save" }, { element: el })

// 事件总线（动作内部用）
DynAction.bus.on("eventName", handler)
DynAction.bus.emit("eventName", payload)
```

---

## 6. 统一响应格式

```json
{
  "code": 0,
  "msg": "ok",
  "data": { ... }
}
```

- `code = 0`：成功
- `code != 0`：失败，`msg` 为错误信息
- 列表数据：`data = { list: [], total: n }`
