# 动态页面积木架构说明（独立 BlockApp 版）

> 适用范围：动态页面体系（DynWebPage / PageSetting / DynTemplates / DynBlocks）。
> 本文描述当前实现的架构约定。三层运行时：ASP.NET Core MVC（Razor 装配）+ Vue 3（独立 app）+ Element Plus，弹窗统一 layui layer。

## 1. 四层模型

| 层 | 载体 | 职责 | 不做什么 |
|---|---|---|---|
| **WebPage**（DynWebPage） | 一条页面记录，`ConfigJson` 存实例参数 | Template 的实例：指定 TableName、各 URL、ProjectId、各 Block 用的 PageSettingId、ChildFkField 等 | 不写 UI、不写关系 |
| **PageSetting** | UI 元数据记录 | 组件的 `ConfigJson`（UI 树，由 dyn-dynamic-com 渲染）与 `DefaultJson`（表单骨架/初始值） | 与具体表、具体页面解耦，可跨页面复用 |
| **Block**（DynBlocks/Apps） | 一个 cshtml = 一个独立 VueApp | 功能自包含：自带 UI、状态、内部动作（查询/保存/删除/加载）；自读容器上的单个 JSON 配置 | 不找"根"、不知道宿主形态（页面/弹窗/片段）、不直接操作其他 Block |
| **Template**（DynTemplates） | 一个 cshtml | 摆放 Block 的 UI 布局；在 ready 脚本里用实例句柄把 Block 间关系**显式接起来** | 不写 Block 的内部逻辑；需要时自身也可以是一个只管自己 UI 的 app（与 Block 平级） |

一句话：**类型定义（PageSetting）→ 可复用单元（Block）→ 组装（Template）→ 实例化（WebPage）**。

## 2. Block 是独立 VueApp，不是页面内组件

平台需要支持 `open`/`updateEl` 随时把一段自带 app 的 HTML 注入页面任意位置（弹窗、片段、模板外）。因此 Block 必须能脱离任何父级独立存活：

- 每个 Block 容器带 `data-dyn-mode="createApp"`，挂载后是一个完整 Vue app；
- 全部输入来自容器自身的单个属性 `data-blk-config`（JSON，由 Razor 渲染时写入），同步可读，**无根查找、无轮询、无水合状态机、无双模式分支**；
- Block 可以放在模板卡片里、layui 弹窗片段里、或任何 updateEl 注入的容器里，代码零差别。

现有 Block：

| Block | role | 输入（data-blk-config 主要字段） | 接受命令 `send` | 发出事件 `emit` |
|---|---|---|---|---|
| FilterApp | `filter` | filterConfig, filterDefault | — | `changed {字段:{op,value}}` |
| ListApp | `list` | columns, table, keyField, project, loadUrl, deleteUrl | `loadData {filter}` / `reload` | `add {}` / `edit {row}` / `addChild {row}` |
| DetailApp | `detail` | detailConfig, detailDefault, table, keyField, project, addUrl/editUrl/deleteUrl, rowId?, preFill? | `newForm {preFill}` / `editForm {row}` | `saved {action,row}` / `cancel {}` |

### 内部动作 vs 对外事件（边界规则）

- **内部动作**（Block 自治）：查询自己的表单、加载自己的数据、保存、行删除确认。
- **对外只报告"发生了什么"**：DetailApp 保存成功只 `emit('saved')`，**不调 layer.close、不发 window 事件、没有 modal 标志**。三屏形态下模板选择不关窗只刷新列表；弹窗形态下模板选择关窗——行为差异全部在编排层。

判据：一段代码说不清属于哪一层（如 Block 里出现 layer.close、全局动作注册），就是越界。

## 3. 协作契约：实例句柄 `element.__dynBlock`

Block 挂载后在自己的容器上挂一个实例句柄（实现见 `wwwroot/dyn/blocks/dyn-blocks-common.js` 的 `createBlockHandle`）：

```js
element.__dynBlock = {
  send(cmd, payload),   // 编排方 → Block 下命令，返回 Promise；命令未注册/实例已销毁则 reject
  on(evt, fn) → off,    // 编排方订阅 Block 事件，返回注销函数
  emit(evt, payload),   // Block 内部使用
  reg(cmd, fn),         // Block 内部注册命令
  introspect(),         // 排查：{destroyed, commands:[], events:[]}
  destroy()             // beforeUnmount 调用；之后全部静默，防悬垂订阅/失效命令
}
```

时序契约：**Block 在 mounted 阶段只做自身初始化（首屏加载、rowId 回填），不得 emit 对外事件**；外事件只能由用户交互触发。模板 `await` 全部 Block mount 完成后再接线，故事件发生时接线必然就绪。

片段注入场景（`open`/`updateEl`）遵循"**谁注入，谁编排**"：注入方在片段挂载后用 `DynBlocks.scan(containerEl)` 按 role 取句柄接线，不使用任何 window 全局事件。

## 4. Template：纯布局 + 薄接线

模板本身默认不是 app，只做两件事：输出纯 HTML 布局容器，和一段 ready 接线脚本。关系集中、肉眼可见：

```js
Promise.all([dyn.mount(fEl), dyn.mount(lEl), dyn.mount(dEl)]).then(function () {
  var filter = fEl.__dynBlock, list = lEl.__dynBlock, detail = dEl.__dynBlock;
  filter.on('changed', p => list.send('loadData', { filter: p }));
  list.on('add',      () => detail.send('newForm', { preFill: {} }));
  list.on('edit',     p  => detail.send('editForm', { row: p.row }));
  detail.on('saved',  () => list.send('reload'));
});
```

弹窗形态（FilterListCrud + DetailModal 片段）：DetailModal 是**零 JS 片段**，只输出 DetailApp 容器（rowId/preFill 都在配置里）；宿主负责拉片段→注入 layer→挂载→接线，`layer.end` 是唯一刷新通道（保存自动关窗、X、取消都经 end 刷新）。

### 模板自身需要 app 时（TreeDetail 模式）

当模板区域自身有动态 UI（如左树：组树、搜索、拖拽排序、分隔条），该区域可以是一个 app，但必须满足：

1. 它与各 BlockApp **平级**，不是 Block 的"根"或配置袋；同样通过自己容器上的 `data-blk-role` 句柄协作（树 app：事件 `nodeClick/addRoot/addChild`，命令 `reload`）；
2. **app 的 DOM 子树互不相交**：Block 容器不能放在某个 app 的受管模板内部（否则其 `v-*`/`{{}}` 会被外层 app 先编译）。栏布局用普通 div，Block 作为其子节点但不属于任何 app 模板；
3. app 之外的纯布局元素交互（如分隔条双击）用 mounted 里原生 `addEventListener` 绑定，不写 Vue 指令。

## 5. 配置与请求

- **配置流**：Razor 读 WebPage.ConfigJson（ClayHelper）+ PageSetting（ConfigJson/DefaultJson，经 `DynPageViewHelper`）→ 拼成一个 JSON → 写入 Block 容器 `data-blk-config`。Block 内 `DynBlocks.parseAttr` 读取。
- **请求契约收口在 `DynBlocks`**：按 URL 前缀 `/api/platform/` 区分平台库与业务库；`search / getUrl / saveUrl / savePayload / deleteRequest` 一处构造两种风格的 URL 与 body，Block 内不写分支。DynCall 统一解包信封，resolve 的值即业务 data。

## 6. 目录结构

```
Views/
  DynBlocks/
    Apps/
      FilterApp.cshtml      # 独立 BlockApp：筛选
      ListApp.cshtml        # 独立 BlockApp：列表
      DetailApp.cshtml      # 独立 BlockApp：明细
  DynTemplates/
    FilterListCrud.cshtml   # 筛选+列表+弹窗（模板非 app）
    TriScreenBlocks.cshtml  # 筛选+列表+明细三屏（模板非 app）
    TreeDetail.cshtml       # 左树 app + 右 DetailApp（模板自身是平级 app）
    DetailModal.cshtml      # 弹窗片段：零 JS，仅 DetailApp 容器
    Partials/DynPageHeader.cshtml
wwwroot/dyn/blocks/
  dyn-blocks-common.js      # parseAttr / API 契约 / createBlockHandle / scan
Core/
  DynPageViewHelper.cs      # Razor 侧配置拼装：URL 缺省、PageSetting 应用、SafeJson
```

## 7. 新增一个 Block / 一个 Template 的步骤

**新 Block**：
1. 在 `Views/DynBlocks/Apps/` 新建 cshtml：容器 `data-dyn-mode="createApp"` + `data-blk-role` + `data-blk-config`；
2. 文件顶部注释写清契约：输入字段、`send` 命令列表、`emit` 事件列表；
3. mounted 中 `DynBlocks.createBlockHandle(element)`，`reg` 命令、交互处 `emit` 事件；beforeUnmount 调 `destroy`；
4. mounted 只做自身初始化，不发外事件；内部按钮用原生 `@@click`，不注册全局动作。

**新 Template**：
1. Razor 拼各 Block 的配置 JSON，输出纯 HTML 布局 + Block partial；
2. Scripts 段 `Promise.all` 挂载后，用句柄把关系一次接完；
3. 模板自身有动态 UI 才建 app，且遵守平级、DOM 子树不相交两条约束。

## 8. 已明确废弃的旧模式

隐藏根 app（只当配置袋/总线宿主的 display:none app）、message-center 全局消息总线、`data-root-ext-id` 50ms 轮询与水合状态机、组合/独立双模式、window 全局事件（`dyndata.saved`）、DetailBlock 的 modal 越界标志、Block 向全局动作表注册内部按钮动作——均已删除，新代码不得重新引入。
