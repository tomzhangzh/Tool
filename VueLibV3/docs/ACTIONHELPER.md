# ActionHelper 动作清单与扩展

> 动作（ActionHelper）是 V3 的“无代码业务逻辑单元”。所有动作（含内置）都存 `DynActionHelper` 表，前端启动时拉取并 `eval` 注册，通过 HTML 属性 `dyn-click-{code}` 触发。

## 目录

- [1. 工作原理](#1-工作原理)
- [2. 14 个内置动作清单](#2-14-个内置动作清单)
- [3. 在 DB 新增自定义动作](#3-在-db-新增自定义动作)
- [4. Chain Editor 用法](#4-chain-editor-用法)
- [5. 在 HTML / 节点中使用动作](#5-在-html--节点中使用动作)

---

## 1. 工作原理

1. 页面加载 `dyn-actionhelper.js` → 绑定 `document` 的 click **捕获阶段委托**。
2. 启动时 `dyn-loader.bootstrap()` 调 `/api/actionhelpers` 拉全部启用动作，每条 `scriptContent` 形如：
   ```js
   function(ctx){ ctx.dyn.message('success','保存成功'); }
   ```
   前端执行 `eval('(' + scriptContent + ')')` 得到函数，注册到 `dyn._actionHelpers[code]`。
3. 用户点击元素时，事件委托从 `e.target` 向上找带 `dyn-click-{code}` 属性的元素：
   - 属性值按 JSON 解析为 `args`（解析失败则 `{raw: 值}`）；
   - 构造 `ctx = { el, args, dyn, getModel, setModel }`；
   - 调 `fn(ctx)`，异常被捕获并弹错误消息。

`ctx` 对象可用能力：

| 字段 | 说明 |
|---|---|
| `ctx.el` | 触发动作的 DOM 元素 |
| `ctx.args` | 动作参数（来自 HTML 属性 JSON） |
| `ctx.dyn` | 全局 `dyn` 命名空间（`message/postJSON/setVueModel/runChain/reload/...`） |
| `ctx.getModel()` | 取当前 app 的 Vue model |
| `ctx.setModel(path, val)` | 写 Vue model（等价 `dyn.setVueModel`） |

## 2. 14 个内置动作清单

种子数据（`SeedService.SeedActionHelpers`）预置 14 个，全部 `IsBuiltin=1`：

| # | Code | 名称 | 分类 | 用途 |
|---|---|---|---|---|
| 1 | `post` | Post 提交 | data | 把当前 model POST 到 url，返回分部视图或 JSON |
| 2 | `get` | Get 请求 | data | GET url 并把结果合并到 model |
| 3 | `open` | 打开模态窗口 | window | 打开模态框并加载分部视图 |
| 4 | `close` | 关闭模态窗口 | window | 关闭最近的模态框 |
| 5 | `reload` | 刷新容器 | ui | 重新加载 `data-dyn-url` 容器 |
| 6 | `updateEl` | 更新元素 | ui | 更新指定 DOM 元素的 HTML |
| 7 | `setVueModel` | 设置 Vue Model | data | 设置 model 某路径值 |
| 8 | `evalJS` | 执行 JS | system | 执行一段 JS 代码 |
| 9 | `confirm` | 确认对话框 | ui | 确认后执行后续动作链 |
| 10 | `message` | 消息提示 | ui | Element Plus 消息提示 |
| 11 | `chain` | 动作链 | flow | 顺序执行多个动作 |
| 12 | `navigate` | 路由跳转 | window | 跳转到指定路由 |
| 13 | `save` | 保存数据 | data | 动态 insert/update 到业务表 |
| 14 | `delete` | 删除数据 | data | 动态删除业务表记录 |

### 2.1 post

- **用途**：把当前 app 的 model POST 到 url。
- **参数 Schema**：

| key | label | type | required |
|---|---|---|---|
| `url` | 请求地址 | input | ✅ |
| `confirm` | 确认提示 | input | - |

- **脚本**：`function(ctx){ ctx.dyn.postback(ctx.el, ctx.args.url, ctx.args); }`
- **HTML 用法**：
  ```html
  <button dyn-click-post='{"url":"/Runtime/DynCrud/Save"}'>提交</button>
  ```

### 2.2 get

- **用途**：GET url 并把结果合并到 model。
- **参数**：`url`(input, 必填)。
- **脚本**：`function(ctx){ ctx.dyn.doGet(ctx.el, ctx.args.url); }`

### 2.3 open

- **用途**：打开模态框并加载分部视图。
- **参数**：

| key | label | type | required |
|---|---|---|---|
| `url` | 视图地址 | input | ✅ |
| `title` | 标题 | input | - |
| `width` | 宽度 | input | - |

- **脚本**：`function(ctx){ ctx.dyn.openModal(ctx.args.url, { title: ctx.args.title, width: ctx.args.width, params: ctx.args.params }); }`
- **用法**：
  ```html
  <button dyn-click-open='{"url":"/demo/webpage","title":"学生详情","width":"800px"}'>打开</button>
  ```

### 2.4 close

- **用途**：关闭最近的模态框。无参数。
- **脚本**：`function(ctx){ ctx.dyn.closeModal(ctx.el); }`

### 2.5 reload

- **用途**：重新加载带 `data-dyn-url` 属性的容器。
- **参数**：`selector`(input)。
- **脚本**：`function(ctx){ ctx.dyn.reload(ctx.el, ctx.args.selector); }`
- **用法**：
  ```html
  <button dyn-click-reload='{"selector":"#list-container"}'>刷新列表</button>
  ```

### 2.6 updateEl

- **用途**：更新指定 DOM 元素的 innerHTML。
- **参数**：`selector`(必填)、`html`。
- **脚本**：`function(ctx){ ctx.dyn.updateEl(ctx.args.selector, ctx.args.html); }`

### 2.7 setVueModel

- **用途**：设置 Vue 响应式 model 的某路径值（支持 `a.b[0].c`）。
- **参数**：`path`(必填)、`value`。
- **脚本**：`function(ctx){ ctx.dyn.setVueModel(ctx.el, ctx.args.path, ctx.args.value); }`
- **用法**：
  ```html
  <button dyn-click-setVueModel='{"path":"filter.Status","value":1}'>只看启用</button>
  ```

### 2.8 evalJS

- **用途**：执行一段 JS 代码（沙箱外，谨慎使用）。
- **参数**：`code`(textarea, 必填)。
- **脚本**：
  ```js
  function(ctx){
    try { new Function('ctx', ctx.args.code)(ctx); }
    catch(e) { ctx.dyn.message('error', '执行异常: ' + e.message); }
  }
  ```
- **用法**：
  ```html
  <button dyn-click-evalJS='{"code":"ctx.dyn.message(\"info\", \"count=\"+ctx.getModel().count)"}'>跑一段JS</button>
  ```

### 2.9 confirm

- **用途**：先弹确认框，确认后执行后续动作链。
- **参数**：`message`(必填)、`then`(json，确认后动作链)。
- **脚本**：
  ```js
  function(ctx){
    ctx.dyn.confirmAsync(ctx.args.message).then(function(){
      if(ctx.args.then) ctx.dyn.runChain(ctx.el, ctx.args.then);
    });
  }
  ```

### 2.10 message

- **用途**：Element Plus 消息提示。
- **参数**：`type`(select: success/error/warning/info)、`text`(必填)。
- **脚本**：`function(ctx){ ctx.dyn.message(ctx.args.type || 'info', ctx.args.text); }`
- **用法**：
  ```html
  <button dyn-click-message='{"type":"success","text":"操作成功"}'>提示</button>
  ```

### 2.11 chain

- **用途**：顺序执行一组动作（Chain Editor 产出的 steps）。
- **参数**：`steps`(json, 必填)，形如 `[{"action":"message","args":{...}}, ...]`。
- **脚本**：`function(ctx){ ctx.dyn.runChain(ctx.el, ctx.args.steps); }`

### 2.12 navigate

- **用途**：整页跳转。
- **参数**：`url`(必填)。
- **脚本**：`function(ctx){ window.location.href = ctx.args.url; }`
- **用法**：
  ```html
  <button dyn-click-navigate='{"url":"/demo/webpage"}'>去 CRUD 页</button>
  ```

### 2.13 save

- **用途**：把当前 model 动态保存（insert/update）到业务表。
- **参数**：

| key | label | type | required | default |
|---|---|---|---|---|
| `table` | 表名 | input | ✅ | - |
| `url` | 保存接口 | input | - | `/Runtime/DynCrud/Save` |

- **脚本**：
  ```js
  function(ctx){
    var data = ctx.dyn.getModel(ctx.el);
    var url = ctx.args.url || '/Runtime/DynCrud/Save';
    ctx.dyn.postJSON(url, { tableName: ctx.args.table, data: data })
      .then(function(res){
        if(res.success) ctx.dyn.message('success','保存成功');
        else ctx.dyn.message('error', res.message);
      });
  }
  ```

### 2.14 delete

- **用途**：确认后动态删除业务表记录。
- **参数**：`table`(必填)、`id`(必填)。
- **脚本**：
  ```js
  function(ctx){
    ctx.dyn.confirmAsync('确认删除？').then(function(){
      ctx.dyn.postJSON('/Runtime/DynCrud/Delete', { tableName: ctx.args.table, id: ctx.args.id })
        .then(function(res){
          if(res.success){ ctx.dyn.message('success','删除成功'); ctx.dyn.reload(ctx.el); }
          else ctx.dyn.message('error', res.message);
        });
    });
  }
  ```

## 3. 在 DB 新增自定义动作

不用改前端代码、不用重启后端。直接往 `DynActionHelper` 表插一行，刷新浏览器即可。

### 3.1 表字段

| 字段 | 说明 |
|---|---|
| `Name` | 显示名（设计器动作面板上的标签） |
| `Code` | 动作编码 = `dyn-click-{Code}`，全局唯一 |
| `Category` | flow / data / window / ui / system（仅分组用） |
| `Description` | 一句话说明 |
| `ParamsSchema` | 参数表单 Schema JSON，数组 |
| `ScriptContent` | JS 函数体，必须是 `function(ctx){ ... }` |
| `IsBuiltin` | 0=自定义（可编辑），1=内置（只读目录） |
| `IsEnabled` | 1=启用 |
| `SortOrder` | 排序 |

### 3.2 ParamsSchema 格式

```json
[
  { "key": "url", "label": "请求地址", "type": "input", "required": true },
  { "key": "type", "label": "类型", "type": "select",
    "options": [ {"label":"成功","value":"success"}, {"label":"错误","value":"error"} ] }
]
```

`type` 支持：`input` / `textarea` / `number` / `switch` / `select` / `json`。

### 3.3 示例：新增一个“打印 model”动作

```sql
INSERT INTO DynActionHelper
  (Name, Code, Category, Description, ParamsSchema, ScriptContent, IsBuiltin, IsEnabled, SortOrder, CreatedAt, UpdatedAt)
VALUES
  ('打印Model', 'dumpModel', 'system', '把当前 model 打到 console',
   '[{"key":"label","label":"标题","type":"input"}]',
   N'function(ctx){ console.log(ctx.args.label || "model:", ctx.getModel()); ctx.dyn.message("info","已打印到控制台"); }',
   0, 1, 100, GETUTCDATE(), GETUTCDATE());
```

刷新浏览器后即可用：

```html
<button dyn-click-dumpModel='{"label":"当前表单"}'>打印 model</button>
```

### 3.4 注册机制确认

- 前端 `dyn-actionhelper.js` 的 `loadFromServer()` 启动时拉 `/api/actionhelpers`；
- 每条 `scriptContent` 执行 `eval('(' + a.scriptContent + ')')` 注册到 `dyn._actionHelpers[a.code]`；
- 注册失败只打 console 错误，不影响其它动作；
- **改完 DB 强刷浏览器**即可生效，无需重启后端。

> 安全提醒：`ScriptContent` 在浏览器端 `eval` 执行，等于在客户端跑任意 JS。不要在脚本里写敏感密钥；生产环境应限制谁能写 `DynActionHelper` 表。

## 4. Chain Editor 用法

打开 `/demo/chain-editor`：

```
┌────────────┬─────────────────────────────┬──────────────┐
│ 可用动作    │  动作链（按顺序执行）         │ 生成的 JSON   │
│ (拖拽)     │  1. message   dyn-click-msg │ [            │
│ ⚡ Post    │     参数: {"type":"success"}│   {          │
│ ⚡ Get     │  2. reload    dyn-click-reload│    "action": │
│ ⚡ open    │     参数: {"selector":"#x"} │    "message",│
│ ...        │  [↑][↓][✕]                 │    "args":{} │
└────────────┴─────────────────────────────┴──────────────┘
```

操作：

1. 左侧动作列表来自 `/api/actionhelpers`（含内置 + 你新增的自定义动作）；
2. 把动作拖到中间链区，自动追加为一个 step；
3. 每个 step 在输入框里填参数 JSON，如 `{"type":"success","text":"保存成功"}`；
4. `↑ / ↓` 调整顺序，`✕` 删除；
5. 右侧实时生成链 JSON，点“📋 复制 JSON”。

产出的 JSON 结构：

```json
[
  { "action": "message", "args": { "type": "success", "text": "保存成功" } },
  { "action": "reload", "args": { "selector": "#list-container" } },
  { "action": "navigate", "args": { "url": "/demo/webpage" } }
]
```

把这段 JSON 喂给 `chain` 动作的 `steps` 参数，或在节点 `options.comlisteners` 里：

```json
{
  "change": { "action": "chain", "args": { "steps": [ ...上面 JSON... ] } }
}
```

## 5. 在 HTML / 节点中使用动作

**纯 HTML**（事件委托自动接管）：

```html
<button dyn-click-message='{"type":"success","text":"保存成功"}'>保存</button>
<button dyn-click-confirm='{"message":"确认删除？","then":[{"action":"delete","args":{"table":"Student","id":5}}]}'>删除</button>
```

**节点 JSON 的事件绑定**（`options.comlisteners`）：

```json
{
  "component": "DynNButton",
  "options": {
    "comoptions": { "label": "保存" },
    "comlisteners": {
      "click": { "action": "save", "args": { "table": "Student" } }
    }
  }
}
```

> 注意：动作在客户端执行；涉及写库时脚本内部调 `/Runtime/DynCrud/*`，后端仍做参数化 SQL 与列名白名单校验。
