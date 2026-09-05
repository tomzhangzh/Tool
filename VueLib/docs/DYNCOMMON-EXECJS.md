# DynCommon 公共 Area & ExecJS 动作机制

> 本文档说明 VueLib 的三块新能力：
> 1. **DynCommon Area**：公共动态 CRUD 接口（Detail/Delete/List/Filter/Pagination，View + JSON 双返回），不再依赖具体业务 Model；
> 2. **ExecJS 机制**：Controller 中 `this.ExecJS(...)` 注册 dyn 动作 → 布局输出隐藏 div（`dyn-init-*`）→ dyn-lib 页面初始化时自动执行；
> 3. **JSON 携带 Actions**（Shapeless 模式）：后端返回 JSON 时顶层带 `actions` 数组，dyn-lib 收到后自动逐个执行。

## 1. DynCommon Area（公共 CRUD）

### 1.1 解决的问题
原来公共的动态 CRUD（`/DynRun/Data/List`、`/DynRun/Save`、`/DynRun/Delete` 等）与模板路由渲染（`/DynRun/Route/*`）混在 `DynRunController` 中。本次把**不依赖业务 Model、只按"工程 + 页面定义"通用读写**的操作独立到 `Areas/DynCommon`。

### 1.2 路由表

| 动作 | 路由 | 返回 | 说明 |
|---|---|---|---|
| List | `GET /DynCommon/DynCommon/List?projectId=&pageId=` | View（_Summary） | 组合查询屏：查询条件 + 汇总表格 + 操作列 + 分页 |
| Filter | `POST /DynCommon/DynCommon/Filter` | View（_Filter） | 只读查询屏（无操作列） |
| Summary | `POST /DynCommon/DynCommon/Summary` | View（_Summary） | 汇总屏（表格 + 操作列 + 分页） |
| Detail | `GET /DynCommon/DynCommon/Detail?projectId=&pageId=&id=` | View（_Detail） | 详情/编辑屏（id=0 为新增） |
| Paged | `POST /DynCommon/Data/Paged` | JSON | 分页数据 `{success, rows, pageInfo, detailPageId}` |
| Get | `GET /DynCommon/Data/Get` | JSON | 单行详情 `{success, data}` |
| Save | `POST /DynCommon/Data/Save` | JSON | 新增/更新，返回 **actions**（自动提示 + 关窗） |
| Delete | `POST /DynCommon/Data/Delete` | JSON | 删除，返回 **actions**（自动提示 + reload） |
| Options | `POST/GET /DynCommon/Project/Options` | JSON | 执行 SQL 返回下拉选项 |

### 1.3 说明
- 全部接口通过 `projectId/pageId` 定位工程数据库与页面定义（`ColumnDefs`），无需专用实体；
- 与旧接口兼容：`DynRunController` 保留原有 `/DynRun/*`（模板路由系统仍依赖），新开发统一走 DynCommon；
- 视图基于 ElementUI（el-table/el-form）+ dyn-lib 动态挂载。

## 2. ExecJS 机制（服务端注册 dyn 动作）

### 2.1 思想来源
参考 `backend/TUI.Core/Models/BaseJavaScript.cs`（AlertMessageJavaScript / FlashMessageJavaScript 等返回 Script 字符串），但**输出物从"JS 代码"改为"dyn 属性 div"**——复用 dyn-lib 的动作体系，无需拼 JS 字符串。

### 2.2 使用方式

```csharp
// Controller 中（需 using VueLib.Web.Infrastructure）
public IActionResult Save()
{
    // ...业务逻辑...
    this.ExecJS(
        new FlashMessageJavaScript { Message = "保存成功", Type = "success", Title = "提示" },
        new SetWindowJavaScript { Title = "新标题", Width = 1000, Height = 700, Fullscreen = true },
        new ReloadJavaScript { Selector = "#dynHost" }
    );
    return View();
}
```

视图（布局或视图末尾，任意位置）：

```razor
@Html.RenderDynActions()
```

渲染输出（dyn-lib 初始化时自动执行）：

```html
<div style="display:none" dyn-init-showmessage='{"message":"保存成功","type":"success","title":"提示"}'></div>
<div style="display:none" dyn-init-setwindow='{"title":"新标题","width":1000,"height":700,"fullscreen":true}'></div>
<div style="display:none" dyn-init-reload='{"selector":"#dynHost"}'></div>
```

### 2.3 内置动作类（DynJavaScript 派生）

| 类 | Action | Options 示例 | 说明 |
|---|---|---|---|
| `FlashMessageJavaScript` | showmessage | `{message,type,title}` | 消息提示（自动探测 ElementPlus/NutUI/LayUI） |
| `AlertJavaScript` | alert | `{message,title,type}` | 弹窗警示（ElMessageBox / NutUI Dialog） |
| `SetWindowJavaScript` | setwindow | `{title,width,height,fullscreen,minimize,close}` | 设置所在窗口标题/尺寸/全屏/关闭 |
| `CloseDialogJavaScript` | close | `{}` | 关闭最近模态框 |
| `ReloadJavaScript` | reload | `{selector}` | 刷新容器（不传 selector 刷新最近 dyn-init 容器） |
| `RedirectJavaScript` | redirect | `{url}` | 页面跳转 |
| `EvalJavaScript` | evaljs | `{script}` | 直接执行 JS |
| `UpdateElJavaScript` | updateel | `{url,params}` | 更新指定容器（沿祖先找 data-url） |

自定义：继承 `DynJavaScript` 实现 `Action`/`Options` 即可（事件可覆盖 `Event` 改为 click 等）。

### 2.4 触发时机
- 默认 `Event = "init"`：dyn-lib `initActions` 扫描 `dyn-init-{action}` 在**页面/Vue 初始化**时执行；
- 页面需调用 `dyn.initAll()`（dyn-lib 是工具库，不自动启动）；
- `_Layout.cshtml` 与 DynCommon 分部视图均已挂 `@Html.RenderDynActions()`。

## 3. JSON 携带 Actions（Shapeless 模式）

### 3.1 约定
后端返回 JSON 时，可在顶层携带 `actions` 数组，dyn-lib 的 `handleResponse`（postback/postdata 处理响应）自动逐个执行：

```json
{
  "success": true,
  "message": "保存成功",
  "actions": [
    { "action": "showmessage", "options": { "message": "保存成功", "type": "success" } },
    { "action": "setwindow",   "options": { "title": "已保存", "close": false } },
    { "action": "reload",      "options": { } }
  ]
}
```

元素格式：
- `{ action: '<已注册动作名>', options: {...} }` → 调用 dyn-lib 动作（任意 `actionHelper` 上的动作均可）；
- `{ action: 'chain', options: { steps: [...] } }` → 动作链；
- `{ script: '<JS>' }` → 直接执行 JS。

### 3.2 后端辅助
在 C# 中直接返回匿名对象即可（如 DynCommonController.Save）：

```csharp
return Json(new
{
    success = true,
    message = "保存成功",
    actions = new object[]
    {
        new { action = "showmessage", options = new { message = "保存成功", type = "success" } },
        new { action = "setwindow",   options = new { close = true } }
    }
});
```

### 3.3 与 Shapeless 的关系（评估结论）
`https://github.com/monksoul/Shapeless` 是**动态 JSON 操作库**（类似 JS 的 JSON 增删改查 + Linq 查询），**不是**"服务端返回动作指令"框架，对本需求无直接帮助，**未引入**。你提出的"JSON 携带 actions 由 dyn-lib 执行"方案本身就是更贴合的设计，已按上述约定实现。

## 4. SQL 下拉接口（Project/Options）

```text
GET  /DynCommon/Project/Options?projectId=1&sql=SELECT Id, Name FROM [Customer]&valueField=Id&textField=Name
POST /DynCommon/Project/Options   body: { projectId, sql, valueField, textField }
```

- 执行工程连接上的 SQL，返回 `{ success, data: [{ value, text }] }`；
- 未指定 valueField/textField 时取结果集第 1、2 列；value 自动去重；
- 供配置好的下拉数据源使用（SQL 来自配置，需注意只读查询）。

## 5. dyn-lib 新增动作

| 动作 | 触发属性 | 说明 |
|---|---|---|
| `showmessage` | `dyn-click-showmessage='{"message":"..","type":"success"}'` | 消息提示 |
| `redirect` | `dyn-click-redirect='{"url":"/Home/Index"}'` | 页面跳转 |

## 6. Demo 与验证

### Demo 页面
`http://localhost:5000/DynDemo/ExecJsDemo`

包含三块演示：
1. **ExecJS 自动执行**：页面加载后自动弹出消息 + 设置窗口（由 `DynDemoController.ExecJsDemo` 的 ExecJS 注册）；
2. **SQL 下拉**：输入 SQL → 加载到 el-select（调 `/DynCommon/Project/Options`）；
3. **JSON actions**：点击"保存"→ postdata 到 `/DynDemo/SaveWithActions` → 返回 `actions` → 自动弹消息 + 改窗口标题。

### 验证结果（2026-09-05）

| 检查项 | 结果 |
|---|---|
| `DynCommon/DynCommon/List`（projectId=1, pageId=3） | 200，_Summary 视图 |
| `DynCommon/DynCommon/Detail`（id=1） | 200，_Detail 视图 |
| `DynCommon/Data/Paged` | JSON 正常（Customer 分页数据） |
| `DynCommon/Project/Options`（SQL=SELECT Id,Name FROM [Customer]） | `success:true, data: 5 项` |
| ExecJS div 渲染 | `dyn-init-showmessage` + `dyn-init-setwindow` 正确输出 |
| ExecJS 自动执行 | 页面加载后 el-message 自动出现，`__dynInitDone=true` |
| JSON actions 执行 | POST 后 el-message 出现（showmessage 生效），无控制台错误 |
| `dotnet build` | 0 错误 |

## 7. 文件清单

| 文件 | 说明 |
|---|---|
| `Infrastructure/DynJavaScript.cs` | 动作类族 + `ExecJS` / `RenderDynActions` 扩展 |
| `Areas/DynCommon/Controllers/DynCommonController.cs` | 公共 CRUD（View + JSON 双版本） |
| `Areas/DynCommon/Controllers/ProjectController.cs` | SQL 下拉接口 |
| `Areas/DynCommon/Views/DynCommon/_Summary.cshtml` 等 | 复用 DynRun 分部视图，URL 指向 DynCommon |
| `wwwroot/js/dyn-lib.js` | `runJsonActions` + `showmessage`/`redirect` 动作 |
| `Views/DynDemo/ExecJsDemo.cshtml` | 三块能力演示页 |
| `Views/Shared/_Layout.cshtml` | 挂 `@Html.RenderDynActions()` |

## 8. 注意

- 本项目 Razor View 编译进 DLL：改 cshtml/控制器后需 `dotnet build` 并重启（静态 js 无需）；
- 改 `wwwroot/js/dyn-lib.js` 后浏览器强刷（`Ctrl+F5`）避免缓存；
- ExecJS 动作在页面初始化时执行，适合"操作后反馈"；如需点击触发请用 `dyn-click-*` 直接写属性。
