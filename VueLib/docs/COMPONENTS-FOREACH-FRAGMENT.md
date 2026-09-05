# ForEach 循环容器 & Fragment 透明容器组件

> 本文档说明 VueLib 动态组件体系中新增的两个容器组件：`ForEach`（按数据源循环渲染子组件）与 `Fragment`（无外层包装、直接透出子组件），以及为此引入的 `comExtra` 组件变量暴露机制。

## 1. 设计目标

### 1.1 ForEach（循环容器）
低代码页面中常见「列表/表格/卡片循环」场景：一组相同结构（如姓名+年龄）需要根据数组数据重复渲染。

- **配置**：`options.comoptions.dataSource` 指定数据源字段（相对当前 modelname 的路径，如 `users`），组件按该数组循环渲染其 `childrenctrls`；
- **子组件绑定**：每个循环项的子组件 `modelname` 相对于当前项解析（`name` → `users[0].name`）；
- **设计器**：保留容器 div 便于拖入/排序子组件；无数据时显示占位，保证子组件可编辑；
- **运行器**：无外层包装，直接按数组循环输出子组件。

### 1.2 Fragment（透明容器 / 无外部包装容器）
默认容器都会渲染一个外层 div。某些场景需要「逻辑分组但不产生额外 DOM 包装」：

- **运行模式**：完全透出子组件，不产生任何外层 div（无标签约束，直接平铺）；
- **设计模式**：保留容器 div，便于拖拽/选中子组件。

## 2. 组件注册清单

| 平台 | 组件名 | View（cshtml） | Controller Action |
|---|---|---|---|
| NutUI | `DynNForEach` | `Areas/NutComponent/Views/Container/ForEach.cshtml` | `NutComponent/Container/ForEach` |
| NutUI | `DynNFragment` | `Areas/NutComponent/Views/Container/Fragment.cshtml` | `NutComponent/Container/Fragment` |
| ElementPlus | `DynEForEach` | `Areas/ElementComponent/Views/Container/ForEach.cshtml` | `ElementComponent/Container/ForEach` |
| ElementPlus | `DynEFragment` | `Areas/ElementComponent/Views/Container/Fragment.cshtml` | `ElementComponent/Container/Fragment` |

Controller（`ContainerController`）需显式添加对应 Action：

```csharp
public IActionResult ForEach() => View();
public IActionResult Fragment() => View();
```

组件元数据插入 `[ComponentMeta]` 表（见下方 SQL），分类 `容器`，`canaccept=1` 表示可接受子组件。

### 2.1 数据库插入脚本（SQL Server）

```sql
-- Nut ForEach
INSERT INTO [ComponentMeta] (Name, Title, Category, Platform, Icon, CanAccept, Options, IsActive)
VALUES ('DynNForEach', '循环容器 ForEach', '容器', 'nut', 'N', 1, '{
  "comoptions": {
    "dataSource": { "label": "数据源字段", "type": "text", "default": "items", "tips": "相对当前 modelname 的数组字段路径，如 users" },
    "itemVar":   { "label": "循环变量", "type": "text", "default": "item" },
    "indexVar":  { "label": "索引变量", "type": "text", "default": "index" },
    "emptyText": { "label": "空数据提示", "type": "text", "default": "" }
  }
}', 1);

-- Nut Fragment
INSERT INTO [ComponentMeta] (Name, Title, Category, Platform, Icon, CanAccept, Options, IsActive)
VALUES ('DynNFragment', '透明容器 Fragment', '容器', 'nut', 'F', 1, '{}', 1);

-- Element 同构（platform 为 element，名称 DynEForEach / DynEFragment）
```

## 3. comExtra 机制（关键修复）

### 3.1 问题背景
组件 View 的 setup 逻辑由公共布局（`_NutLayout.cshtml` / `_ElementLayout.cshtml`）统一提供（`modelinfo`、`validate`、`getStyle` 等），并固定返回给模板。若某组件（如 ForEach）在 `@section setupScripts` 中定义了**组件特有变量**（`loopItems`、`displayItems`、`emptyText` 等），这些变量不在布局的 `return` 对象中，模板访问会得到 `undefined`，导致 `Cannot read properties of undefined (reading 'length')` 等渲染错误。

### 3.2 解决方案
布局的 setup `return` 增加 comExtra 展开：

```js
// _NutLayout.cshtml（_ElementLayout 同理）
return {
    modelinfo, comInnerInfo, validate, resetValidation,
    getStyle, getClass, getComlisteners, safeChildren,
    props, context, designShared, parentConfig, parentCom,
    DVALUE, fullModelName, dragOptions, dragGroup,
    onDragEnd, onDragAdd,
    // 组件可通过 setupScripts 定义 comExtra 暴露额外变量给模板
    ...(typeof comExtra !== 'undefined' ? comExtra : {})
};
```

组件侧在 `@section setupScripts` 末尾定义 `comExtra`：

```js
@section setupScripts{
    // ...组件特有逻辑（const / function / computed）...
    // 暴露组件特有变量给模板
    const comExtra = { isDesignFE, dataSource, itemVar, indexVar, emptyText, loopItems, displayItems, itemPrefix, feBase };
}
```

**规则**：任何组件若需在模板中使用 setupScripts 中自定义的变量/方法，必须通过 `comExtra` 暴露。

## 4. ForEach 用法

### 4.1 配置项（options.comoptions）

| 字段 | 类型 | 默认 | 说明 |
|---|---|---|---|
| `dataSource` | string | `items` | 数据源数组字段（相对当前 modelname 的路径） |
| `itemVar` | string | `item` | 循环项变量名（预留） |
| `indexVar` | string | `index` | 循环索引变量名（预留） |
| `emptyText` | string | `''` | 空数据时提示文案 |

### 4.2 模型绑定规则
- 子组件 `modelname='name'` → 实际绑定 `{dataSource}[{i}].name`（如 `users[0].name`）；
- 子组件 `modelname='~abs.path'` → 绝对路径；
- ForEach 自身 `modelname` 若非空，则作为数据源前缀（如 `modelname='form'` + `dataSource='items'` → `form.items[0].name`）。

### 4.3 示例配置（JSON）

```json
{
  "component": "DynNForEach",
  "modelname": "",
  "options": {
    "comoptions": {
      "dataSource": "users",
      "itemVar": "item",
      "indexVar": "index",
      "emptyText": "暂无用户数据"
    }
  },
  "childrenctrls": [
    { "component": "DynNInput", "modelname": "name", "options": { "comoptions": { "placeholder": "姓名" } } },
    { "component": "DynNInput", "modelname": "age",  "options": { "comoptions": { "placeholder": "年龄", "type": "number" } } }
  ]
}
```

配合模型：

```json
{ "users": [ { "name": "张三", "age": 18 }, { "name": "李四", "age": 20 }, { "name": "王五", "age": 22 } ] }
```

渲染结果：3 组「姓名+年龄」输入框，值分别绑定 `张三/18`、`李四/20`、`王五/22`。

## 5. Fragment 用法

Fragment 不需要额外配置（`comoptions` 为空）。把任意子组件拖入即可；运行模式子组件直接平铺透出，无外层 div。

```json
{
  "component": "DynNFragment",
  "modelname": "",
  "options": { "comoptions": {} },
  "childrenctrls": [
    { "component": "DynNButton", "modelname": "", "options": { "comoptions": { "text": "按钮A", "type": "primary" } } },
    { "component": "DynNButton", "modelname": "", "options": { "comoptions": { "text": "按钮B" } } }
  ]
}
```

渲染：`<button>A</button><button>B</button>` 平铺，无包裹 div。

## 6. 验证 Demo

- **页面**：`http://localhost:5000/designer/preview?code=foreach-demo`
- **结构**：`DynNDivContainer` → `[DynNForEach(dataSource=users) → DynNInput×2, DynNFragment → DynNButton]`
- **预期**：ForEach 渲染 3 组输入框（张三/李四/王五 正确绑定）；Fragment 按钮无外层 div 直接透出；页面无 Vue 渲染错误。

## 7. 验证结果（2026-09-04）

| 检查项 | 结果 |
|---|---|
| `/NutComponent/Container/ForEach`、`/Fragment` | 200，模板 + comconfig 完整 |
| `/ElementComponent/Container/ForEach`、`/Fragment` | 200，comExtra 已注入 |
| `foreach-demo` 页面渲染 | 通过：3 组输入框 + Fragment 按钮透出，无外层 div |
| 输入框值绑定 | `张三/18、李四/20、王五/22` 正确 |
| Vue 渲染错误 | 无 |

## 8. 备注

- 修改组件 cshtml（含布局）后需 **重新 build**（`dotnet build`）再重启，因为本项目 Razor View 编译进 DLL；
- 若新增其他需要自定义模板变量的组件，沿用 `comExtra` 模式即可。
