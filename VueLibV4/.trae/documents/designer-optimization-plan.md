# 页面设计器优化实施计划

## Context

设计器功能已完整（四分区共享 scope、组件树、组合库、JSON 模式、标尺、embed 协议），但存在会造成**数据丢失或不可恢复**的问题：

- 属性面板所有编辑不入撤销栈，且无快捷键、无未保存提示；
- slots 子组件在组件树中不可见，删除/移动 slots 节点会误删 `childrenctrls` 末项；
- 切换控件类型整体替换 `options`，已填配置丢失；
- 设计器不能打开已有页面，保存前不校验配置；
- 选中框不随属性变化更新，工具条符号按钮过多，组件库无搜索；
- 画布同时挂了原生 HTML5 拖放与 Sortable 两套通道，前者基本不触发；
- `dyn-designer-ops.js`（669 行）、`Canvas.cshtml`（640 行）承载过多职责。

目标：先让设计器**不丢数据、可撤销、能拦住脏配置**，再顺体验与收技术债。范围按用户确认的「全部按优先级推进」。

## 实施步骤

### 1. 撤销 / 脏状态 / 快捷键（P0）

- 新建 `src/VueLibV4.Web/wwwroot/dyn/dyn-designer-history.js`（stage=designer，`requires:['DynDesignerOps']`）：
  - `init(scope, vm)`：`vm.$watch(() => scope.pageJson, …, {deep:true})`，600ms 防抖合并连续输入；首次记录基线。
  - `snapshot(scope)`：先提交待合并编辑，再把「操作前状态」压栈（替代现有 `saveSnapshot` 语义）。
  - `undo/redo`：入栈前先提交待合并编辑；`applySnapshot` 后重置基线并重算 dirty。
  - `markSaved(scope)`：记录 `__savedJson` 并清 dirty；`reset(scope)`：载入页面/JSON 应用后清空栈与基线。
  - 兼容导出：写回 `DynDesignerOps.saveSnapshot/undoAction/redoAction`，现有调用点不改名。
- 修改 `dyn-designer-ops.js`：删除旧快照栈实现（约 40 行），内部 9 处 `saveSnapshot(scope)` 改调 `global.DynDesignerHistory.snapshot(scope)`；导出列表移除三个旧函数。
- 修改 `TopBar.cshtml`：
  - 页面名旁显示「● 未保存」，保存成功后清除；
  - `mounted` 注册快捷键：Ctrl+Z / Ctrl+Shift+Z、Ctrl+Y、Ctrl+S、Delete、Esc（输入框/对话框内不拦截）；
  - 独立页注册 `beforeunload`，dirty 时拦截；`onNew` 有未保存改动先确认。
- `Canvas.cshtml` 的 JSON 应用、`Designer.cshtml` 的 embed init 调 `DynDesignerHistory.reset`。

### 2. slots 修正（P0）

修改 `dyn-designer-ops.js`：

- 新增 `arrayOfHit(hit)`：按 `hit.slot` 取 `childrenctrls` 或 `slots[slot]`。
- `moveSelected / duplicateSelected / removeSelected / onDrop / dropToRoot / treeReorder` 统一走 `arrayOfHit`，杜绝 `splice(-1,1)` 误删。
- `isDescendantOf`、`getPathList` 补 slots 递归。
- `buildTreeData` 的 slots 分支：生成 `cfg:null` 的分组节点，并把 `slots[k]` 的每个子节点 `walk` 成可选中节点。

### 3. 切换控件类型不丢配置（P0）

修改 `RightPanel.cshtml` 的 `onComponentTypeChange`：

- 变更前调用快照；
- 以新组件默认 `options` 为底，按字段级合并：`itemoptions` 整体保留；`labeloptions` 保留 label/show/required/help/labelposition/labelwidth；`comoptions` 仅保留新默认中存在的同名键 + 通用 `size`；
- 保留 `modelname/validators/bindings/evalcondition/modelexpr/modelsetexpr/slots/extendinfo`；非容器才清空 `childrenctrls`。

### 4. 载入已有页面 + 保存前体检（P0）

- `PageShellController.Designer` 增加 `int? id, string code`，注入 `IDynWebPageService`，按 id/code 取 `DynWebPage`；
- 传给视图：`ViewBag.InitialPageJson`（`JObject.Parse(...).ToString(Formatting.None)`，无值用空白栅格默认）、`PageId/PageCode/PageName`；
- `Designer.cshtml` 的 `data-dyn-scope-json` 内嵌这些值（`&`→`&amp;`、`'`→`&#39;` 后再输出，保证单引号属性安全）；
- 新建 `src/VueLibV4.Web/wwwroot/dyn/dyn-designer-lint.js`（stage=designer，`requires:['DynDesignerOps']`），`DynDesignerOps.lintPageJson(scope)` 返回 `{issues, errors}`：
  - 错误：根/节点缺 component、未知组件（元数据已加载时）、非容器带子节点、modelname 重复、`evalcondition` 语法错误；
  - 警告：容器为空、必填规则无 modelname、`{{ }}` 不配对；
- `TopBar.cshtml` 增加体检对话框（问题列表点击定位到节点），错误阻止保存、仅警告可「仍然保存」；页面编码加 `^[A-Za-z0-9][A-Za-z0-9_-]{1,63}$` 校验；
- 保存成功调 `markSaved`。

### 5. 画布体验（P1）

- `Canvas.cshtml`：Watcher `selectedUid/canvasVersion` → 用 `ResizeObserver` 监听选中节点，尺寸变化即 `updateOverlay`；`pointerover` 委托加 `.dyn-hover-node` 描边（`display:contents` 时落到首个子元素）；
- 浮层工具条：基础操作保留 ↑ ↓ ⧉ ✕，子项管理 6 个收进「⋯」菜单；所有按钮保留 `title`；
- 样式补到 `Designer.cshtml` 的 style 块（`.dyn-hover-node`、`.dyn-overlay-menu`）。

### 6. 组件库搜索 + 拖拽通道收敛（P1）

- `LeftPanel.cshtml`：新增关键词输入（ComponentName/Label/分类），与分类 chips 同时生效；
- 先用浏览器确认画布拖拽由 Sortable 驱动、节点无 `draggable` 属性，再删除失效的原生通道：
  - `dyn-node-config.js` 的 `dragstart/dragover/drop/dragleave` 接线；
  - `Canvas.cshtml` 的根 `dragover/drop` 绑定与 `dropIndicator` 模板；
  - `dyn-designer-ops.js` 的 `onDragStartCfg/onDragOver/onDragLeave/onDrop/dropToRoot`；
  - 保留 `onDragStartMeta/clearDrag` 供 Sortable 复用；若验证不成立则保留原状并在计划中回退。

### 7. 属性面板整理（P1）

- 新增 `comPropPaths`（comProps 子节点 modelname 集合），`v-if` 隐藏重复项：标签文本/帮助说明/显示标签/Label对齐/Label宽度/子控件尺寸；
- 样式 JSON 改成单一事实源：`data.styleText` + `@focus/@blur` + 深度 watch，外部变化时仅在未聚焦时回写，修复「输入被重渲染冲掉」；
- 必填仍以 validators 为唯一可编辑源，`labeloptions.required` 只做派生写入。

### 8. 文件与职责（P2，配合上面一起做）

- 抽出 `dyn-designer-ruler.js`（纯函数：刻度计算 + `draw(ctx, opts)`，双导出供 node 测试）；`Canvas.cshtml` 只保留 `attach/detach` 调用，减少约 90 行；
- 上述 history/lint 拆出后，`dyn-designer-ops.js` 预计 ≤ 620 行（基线 669，只减不增）；
- `Canvas.cshtml` 目标 ≤ 590；`RightPanel.cshtml` 控制在 ≤ 495。

### 9. 测试

- `tests/dyn/run.js` 的 `loadEngine` 追加加载 `dyn-designer-ops.js`、`dyn-designer-history.js`、`dyn-designer-lint.js`、`dyn-designer-ruler.js`（designer 阶段随 boot 执行）；
- 新增用例（约 8 条）：
  - slots 子节点出现在树数据中；
  - 删除 slots 节点只删 slots 数组；
  - `lintPageJson` 能报重复 modelname / 未知组件 / 非容器带子节点；
  - 标尺刻度换算（zoom、偏移）；
  - 历史合并：相同 JSON 不重复入栈（用假 vm 直接调 `snapshot`）。

## 验证

1. `dotnet build VueLibV4.sln` → 0 error。
2. `node tests/dyn/run.js` → 全部通过（数量增加到约 58）。
3. `powershell -File tools/check-file-size.ps1` → 0 违规。
4. `dotnet run --project tools/VueLibV4.ArchCheck` → 0 违规。
5. 重启 Web（后端改动）：`dotnet run --project src/VueLibV4.Web`，冒烟 `/Platform/Shell/Designer`、`/Platform/Shell/Designer?id=<已有页面>`、`/api/platform/dynpage/get?id=<code>`。
6. 浏览器手测（admin/admin123）：改属性后 Ctrl+Z 可回退；脏标记与关闭提示；切换控件类型保留配置；保存非法配置被拦截并可定位；组件树可见 slots 子项；组件库搜索；画布拖拽仍正常；选中框随属性高度变化贴合。
