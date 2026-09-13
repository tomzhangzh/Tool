# 新增组件指南

> 本文档面向平台开发者：如何在 V3 中新增一个 dynCom 组件，包括组件分类、节点 JSON 标准、在 `ComponentMeta` 表注册、写 cshtml 视图、组合组件机制、保存清理与加载规范化。

## 目录

- [1. 组件分类](#1-组件分类)
- [2. 节点 JSON 标准结构](#2-节点-json-标准结构)
- [3. 在 ComponentMeta 表注册新组件](#3-在-componentmeta-表注册新组件)
- [4. 写 cshtml 视图](#4-写-cshtml-视图)
- [5. 组合组件：开放属性 / 容器 / slot](#5-组合组件开放属性--容器--slot)
- [6. 保存清理 cleanNode](#6-保存清理-cleanNode)
- [7. 加载规范化 normalizeNode](#7-加载规范化-normalizeNode)
- [8. 容器 Grid 布局配置示例](#8-容器-grid-布局配置示例)
- [9. 内置组件速查表](#9-内置组件速查表)

---

## 1. 组件分类

分类存在 `ComponentMeta.Category` 字段，设计器左侧面板按它分组：

| Category | 含义 | 是否容器 | 示例 |
|---|---|---|---|
| `data` | 数据录入组件（绑定 modelname） | 否 | DynNInput / DynNSelect / DynNDatePicker / DynNInputNumber / DynNSwitch / DynNRadio / DynNCheckbox / DynNTextarea |
| `container` | 容器组件（可拖子组件） | 是 | DynElForm / DynElCard / DynElDiv / DynElRow / DynElCol / DynElTabs |
| `wrapper` | 纯布局壳（无业务逻辑，只包一层样式） | 是 | DynWrapper |
| `slot` | 插槽（把外部内容投射到指定位置） | 是 | DynSlot |
| `composite` | 组合组件（内置组件的包装，开放属性/容器/slot） | 是 | DynCompositeDemo |
| `system` / `property` | 系统组件（设计器自身用） | 视情况 | DynPropPanel / DynCodeMirror / DynOpenWindow / DynDesktop |

`dynCom.isContainerComp(meta)` 判断是否可拖子组件：`isContainer=true` 或 `category ∈ {container, wrapper}`。

## 2. 节点 JSON 标准结构

`dyn-com.js` 文件头是权威定义：

```json
{
  "component": "DynNInput",
  "modelname": "UserName",
  "options": {
    "comoptions":     { "placeholder": "请输入", "clearable": true },
    "comlisteners":   { "change": { "action": "message", "args": { "type": "success", "text": "已改" } } },
    "labeloptions":   { "label": "用户名", "required": true, "show": true },
    "itemoptions":    { "style": { "width": "100%" }, "class": "" }
  },
  "validators":  [ { "rule": "required", "message": "必填" } ],
  "childrenctrls": [],
  "slots":       { "footer": [] },
  "extendinfo":  {}
}
```

| 字段 | 层级 | 含义 |
|---|---|---|
| `component` | 根 | 组件名，对应 `ComponentMeta.ComponentName`；根节点缺省 `DynElForm` |
| `modelname` | 根 | 绑定 model 路径，支持 `user.name` / `list[0].id`（见 `dyn.getByPath`） |
| `options.comoptions` | 子 | 组件自身属性，透传给底层控件（Element Plus props） |
| `options.comlisteners` | 子 | 事件名 → `{action, args}`，命中动作系统 |
| `options.labeloptions` | 子 | `label` 文本 / `required` 是否必填 / `show` 是否显示标签 |
| `options.itemoptions` | 子 | 外壳 `style` / `class`（Grid 跨列写这里） |
| `validators` | 根 | 校验规则数组 |
| `childrenctrls` | 根 | 容器子节点（递归同结构） |
| `slots` | 根 | 命名插槽内容 `{slotName: [node...]}` |
| `extendinfo` | 根 | 扩展预留 |

## 3. 在 ComponentMeta 表注册新组件

新增组件 = 插一行 `ComponentMeta` + 放一个 cshtml 视图。前端启动时 `dyn-loader` 会把新组件带到设计器面板。

### 3.1 字段说明

| 字段 | 类型 | 说明 |
|---|---|---|
| `ComponentName` | string(100) | **全局唯一**，如 `DynColorPicker` |
| `Label` | string(100) | 设计器面板显示名，如“颜色选择” |
| `Category` | string(50) | `data` / `container` / `wrapper` / `slot` / `composite` / `system` |
| `UiLibrary` | string(50) | `element` / `native` / `custom` / `layui` |
| `Icon` | string(200) | 面板图标（emoji 或 class） |
| `LoadUrl` | string(300) | cshtml 视图路径，如 `/Areas/Components/Views/Data/DynColorPicker.cshtml` |
| `DefaultConfigJson` | nvarchar(max) | 新建实例时的初始 options |
| `PropertyConfigJson` | nvarchar(max) | 属性面板可编辑字段名数组（见下） |
| `IsContainer` | bit | 是否可拖子组件 |
| `IsComposite` | bit | 是否组合组件 |
| `CompositeConfigJson` | nvarchar(max) | 组合组件配置（开放属性/容器/slot） |
| `SortOrder` | int | 面板排序 |
| `IsEnabled` | bit | 是否出现在面板 |

### 3.2 PropertyConfigJson

数组，列出右侧属性面板要暴露的可编辑字段名：

```json
["placeholder", "clearable", "disabled", "type", "maxlength", "showwordlimit", "label", "required", "modelname"]
```

### 3.3 注册 SQL 示例（新增一个颜色选择器）

```sql
INSERT INTO ComponentMeta
  (ComponentName, Label, Category, UiLibrary, LoadUrl,
   DefaultConfigJson, PropertyConfigJson,
   IsContainer, IsComposite, SortOrder, IsEnabled, CreatedAt, UpdatedAt)
VALUES
  ('DynColorPicker', N'颜色选择', 'data', 'element',
   '/Areas/Components/Views/Data/DynColorPicker.cshtml',
   N'{"comoptions":{"predefine":true,"showAlpha":false},"labeloptions":{"label":"颜色","required":false,"show":true}}',
   N'["predefine","showAlpha","disabled","label","required","modelname"]',
   0, 0, 10, 1, GETUTCDATE(), GETUTCDATE());
```

> 参照种子代码 `SeedService.SeedComponentMeta()` 的 `NewComp(...)` 写法：`defaultOptions` 里的 `comoptions / labeloptions / itemoptions` 会被设计器 `createNode()` 读出来作为新节点初始值。

## 4. 写 cshtml 视图

### 4.1 路径约定

放在 `Areas/Components/Views/{Category}/` 下，文件名 = 组件名：

```
src/VueLib.Web/Areas/Components/
└─ Views/
   ├─ Data/       DynNInput.cshtml DynNSelect.cshtml ... DynColorPicker.cshtml
   ├─ Container/  DynElForm.cshtml DynElCard.cshtml DynElDiv.cshtml ...
   ├─ Wrapper/    DynWrapper.cshtml
   ├─ Slot/       DynSlot.cshtml
   ├─ Composite/  DynCompositeDemo.cshtml
   └─ System/     DynPropPanel.cshtml DynCodeMirror.cshtml DynOpenWindow.cshtml DynDesktop.cshtml
```

### 4.2 视图如何被加载

`GET /api/component-view/{name}`（`ComponentApiController.ComponentView`）：

1. 先查 `ComponentMeta.LoadUrl`；
2. `LoadUrl` 为空时按约定拼 `/Areas/Components/Views/{Category}/{name}.cshtml`；
3. 从磁盘读文件文本，以 `text/plain` 返回；
4. 前端 `dyn-loader.loadComponentView(name)` 拉到后注册为 Vue 组件 template。

### 4.3 cshtml 视图模板

视图内容就是一段 Vue 模板字符串（服务端不 Razor 渲染动态部分，文件作为文本下发）。以 `DynColorPicker` 为例：

```html
<!-- Areas/Components/Views/Data/DynColorPicker.cshtml -->
<el-color-picker
    :model-value="model[node.modelname]"
    @update:model-value="v => model[node.modelname] = v"
    :disabled="node.options.comoptions.disabled"
    :show-alpha="node.options.comoptions.showAlpha"
    :predefine="node.options.comoptions.predefine">
</el-color-picker>
```

约定：

- 组件运行态拿到的是 `node`（节点 JSON）和 `model`（Vue 响应式数据）；
- 绑定值用 `node.modelname` 路径读写；
- 组件自身属性从 `node.options.comoptions` 取；
- 标签从 `node.options.labeloptions.label` 取；
- 外壳样式从 `node.options.itemoptions.style` / `class` 取（设计器画布已在外层包裹，运行态由内核统一应用）。

> 目前仓库里 `Areas/Components/` 目录尚未随种子创建，首次新增组件时先建目录结构；`/api/component-view/{name}` 会同时尝试 `Areas` 根与 `wwwroot` 两个物理路径，放哪都能读到。

## 5. 组合组件：开放属性 / 容器 / slot

组合组件（`IsComposite=1`）= 把多个内置组件包成一个“业务组件”，同时把内部某些属性、容器、slot **开放**给外部配置。配置存在 `CompositeConfigJson`。

### 5.1 CompositeConfigJson 结构

```json
{
  "openProps": [
    "childrenctrls[0].options.comoptions.placeholder",
    "childrenctrls[0].options.labeloptions.label"
  ],
  "openContainers": [ "childrenctrls[1]" ],
  "openSlots": [ "footer" ]
}
```

| 字段 | 含义 |
|---|---|
| `openProps` | 字符串路径数组：外部可直接编辑的内部子组件属性路径（支持 `childrenctrls[i]...` 下标语法） |
| `openContainers` | 容器路径数组：外部可往这些内部容器拖子组件 |
| `openSlots` | slot 名数组：外部可往里塞内容 |

### 5.2 种子里的 DynCompositeDemo

```csharp
NewComp("DynCompositeDemo", "组合组件示例", "composite", "element",
    "/Areas/Components/Views/Composite/DynCompositeDemo.cshtml",
    new { comoptions = new { title = "组合组件" } },
    isContainer: true, isComposite: true,
    composite: new {
        openProps       = new[] { "childrenctrls[0].options.comoptions.placeholder",
                                  "childrenctrls[0].options.labeloptions.label" },
        openContainers  = new[] { "childrenctrls[1]" },
        openSlots       = new[] { "footer" }
    });
```

含义：第 0 个子节点（输入框）的 placeholder / label 开放给外部；第 1 个子节点（按钮区）作为开放容器；`footer` slot 开放。运行时 `dynCom.applyCompositeProps(node, compositeConfig)` 把外部传入的值映射到内部路径。

> 实时示例见 `/demo/composite`。

## 6. 保存清理 cleanNode

保存布局到 `DynWebPage.LayoutJson` 之前，先过 `dynCom.cleanNode(node)`：

```javascript
const saved = JSON.stringify(dynCom.cleanNode(rootNode));
```

清理规则（`dyn-com.js` 的 `EMPTY_VALUES = [null, undefined, '', [], {}]`）：

- 值为 `null / undefined / "" / [] / {}` 的字段直接剔除，不落库；
- `options` 对象递归清掉空值子键；
- `childrenctrls` 递归清理，并过滤掉没有 `component` 的残节点；
- `slots` 只保留非空的 slot；
- `validators` 为空数组则整体剔除。

目的：库里只存有效值，布局 JSON 干净、可读、diff 友好。

## 7. 加载规范化 normalizeNode

从库里读出 JSON 后，先过 `dynCom.normalizeNode(node)`：

```javascript
const node = dynCom.normalizeNode(JSON.parse(layoutJson));
```

补默认值（递归子节点）：

```javascript
node.component   = node.component   || 'DynElForm';
node.modelname   = node.modelname   || '';
node.options.comoptions    = node.options.comoptions    || {};
node.options.comlisteners  = node.options.comlisteners  || {};
node.options.labeloptions   = node.options.labeloptions  || { label: node.component, required: false, show: true };
node.options.itemoptions   = node.options.itemoptions   || { style: {}, class: '' };
node.validators     = node.validators     || [];
node.childrenctrls  = node.childrenctrls  || [];
node.slots          = node.slots          || {};
node.extendinfo     = node.extendinfo     || {};
```

新建节点用 `dynCom.createNode(componentName, label)`，它内部就是 `normalizeNode({...})`，保证任何节点从出生起结构完整。

## 8. 容器 Grid 布局配置示例

容器组件（如 `DynElDiv`）在 `itemoptions.style` 写 CSS Grid：

```json
{
  "component": "DynElDiv",
  "options": {
    "itemoptions": {
      "style": {
        "display": "grid",
        "gridTemplateColumns": "1fr 1fr",
        "gap": "16px 24px"
      }
    }
  },
  "childrenctrls": [
    { "component": "DynNInput", "modelname": "Name",
      "options": { "labeloptions": { "label": "姓名" } } },
    { "component": "DynNInputNumber", "modelname": "Age",
      "options": { "labeloptions": { "label": "年龄" } } },
    { "component": "DynNInput", "modelname": "Address",
      "options": { "labeloptions": { "label": "地址" },
                   "itemoptions": { "style": { "gridColumn": "span 2" } } } }
  ]
}
```

要点：

- 父容器：`display:grid` + `gridTemplateColumns:"1fr 1fr"` + `gap:"16px 24px"`；
- 子节点跨列：在它自己的 `itemoptions.style.gridColumn` 写 `"span 2"`；
- 或在 `itemoptions.class` 写 `col-span-2`，CSS 里定义 `.col-span-2{grid-column:span 2;}`；
- 设计器右侧属性面板直接编辑这些键即可。

## 9. 内置组件速查表

首启动种子 21 个组件：

| 组件名 | 显示名 | 分类 | UI 库 | 容器 |
|---|---|---|---|---|
| DynNInput | 输入框 | data | element | 否 |
| DynNSelect | 下拉选择 | data | element | 否 |
| DynNDatePicker | 日期选择 | data | element | 否 |
| DynNInputNumber | 数字输入 | data | element | 否 |
| DynNSwitch | 开关 | data | element | 否 |
| DynNRadio | 单选框 | data | element | 否 |
| DynNCheckbox | 多选框 | data | element | 否 |
| DynNTextarea | 多行文本 | data | element | 否 |
| DynElForm | 表单容器 | container | element | ✅ |
| DynElCard | 卡片容器 | container | element | ✅ |
| DynElDiv | 通用div容器 | container | native | ✅ |
| DynElRow | 栅格行 | container | element | ✅ |
| DynElCol | 栅格列 | container | element | ✅ |
| DynElTabs | 标签页 | container | element | ✅ |
| DynWrapper | 布局壳 | wrapper | native | ✅ |
| DynSlot | 插槽 | slot | native | ✅ |
| DynCompositeDemo | 组合组件示例 | composite | element | ✅（组合） |
| DynPropPanel | 属性面板 | property | element | 否 |
| DynCodeMirror | 代码编辑器 | system | custom | 否 |
| DynOpenWindow | 打开窗口组件 | system | custom | 否 |
| DynDesktop | 桌面组件 | system | custom | 否 |
