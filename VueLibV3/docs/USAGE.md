# VueLib V3 使用说明

> 本文档面向业务实施人员：如何新建一个 CRUD 页面、如何用设计器拖拽、如何保存/预览布局、如何配置 Grid 跨列。

## 目录

- [1. 新建一个 CRUD 页面](#1-新建一个-crud-页面)
- [2. 用设计器拖拽组件](#2-用设计器拖拽组件)
- [3. 保存 / 预览布局](#3-保存--预览布局)
- [4. 配置 Grid 跨列布局](#4-配置-grid-跨列布局)
- [5. 动态 CRUD 接口速查](#5-动态-crud-接口速查)

---

## 1. 新建一个 CRUD 页面

V3 提供 4 个内置模板（存在 `DynTemplate` 表），新建页面 = **选模板 → 填参数 → 选表 → 配置列**。

### 1.1 内置模板清单

| 模板 Code | 名称 | 类型 | 渲染视图 | 用途 |
|---|---|---|---|---|
| `crud-list` | CRUD 列表模板 | CrudList | `/Areas/Runtime/Views/Templates/CrudList.cshtml` | 查询条件 + 数据表格 + 分页 + 新增/编辑/删除 |
| `tree-right-list` | 左树右列表 | TreeList | `/Areas/Runtime/Views/Templates/TreeRightList.cshtml` | 左侧分类树 + 右侧数据列表 |
| `home-grid` | 主页九宫格 | HomeGrid | `/Areas/Runtime/Views/Templates/HomeGrid.cshtml` | 主页快捷入口九宫格 |
| `custom-layout` | 自定义布局 | Custom | `/Areas/Runtime/Views/Templates/CustomLayout.cshtml` | 用设计器画出的自由布局 |

### 1.2 操作步骤（以 crud-list 为例）

**第 1 步：准备业务表**

在业务库（默认 `VueLibV3_Business`）建好表，例如：

```sql
CREATE TABLE [Product](
  [Id] int IDENTITY(1,1) PRIMARY KEY,
  [Name] nvarchar(100) NOT NULL,
  [Price] decimal(10,2) NULL,
  [Category] nvarchar(50) NULL,
  [IsActive] bit NOT NULL DEFAULT(1),
  [CreatedAt] datetime NOT NULL DEFAULT(GETDATE())
)
```

> 表名/列名只允许字母数字下划线（`SafeName` 白名单），否则接口会抛“非法标识符”。

**第 2 步：选模板**

新建一条 `DynWebPage` 记录，关联模板：

```json
{
  "projectId": 1,
  "route": "/product-list",
  "name": "产品列表",
  "title": "产品管理",
  "templateId": 1,
  "params": "{...见第3步...}"
}
```

**第 3 步：填模板参数**

`crud-list` 模板的 `ParamSchema` 定义了以下参数（设计器按 Schema 自动生成表单）：

| key | label | 类型 | 必填 | 默认 | 说明 |
|---|---|---|---|---|---|
| `tableName` | 业务表名 | input | ✅ | - | 如 `Product` |
| `title` | 页面标题 | input | - | - | 显示在表格卡片头部 |
| `pageSize` | 每页条数 | number | - | 10 | 分页大小 |
| `orderBy` | 默认排序列 | input | - | 主键 | 如 `CreatedAt` |
| `orderDir` | 排序方向 | select | - | desc | desc / asc |

`params` 填值示例：

```json
{
  "tableName": "Product",
  "title": "产品管理",
  "pageSize": 10,
  "orderBy": "CreatedAt",
  "orderDir": "desc"
}
```

**第 4 步：选表 / 配置列**

- 设计器“选表”时调 `/Runtime/DynCrud/Tables?projectId=1` 列出业务库所有表；
- 选中 `Product` 后调 `/Runtime/DynCrud/Meta?table=Product&projectId=1` 拿列元数据：

```json
{
  "success": true,
  "data": {
    "tableName": "Product",
    "primaryKey": "Id",
    "columns": [
      { "columnName": "Id", "dataType": "int", "isPrimary": true, "isIdentity": true },
      { "columnName": "Name", "dataType": "nvarchar", "maxLength": 100, "isNullable": false },
      { "columnName": "Price", "dataType": "decimal", "isNullable": true }
    ]
  }
}
```

- 配置列：在模板列配置里勾选要显示的列、设置列宽/排序/对齐；查询条件列配置为可筛选（对应请求体 `filter` 字典）；模糊搜索列配置为 `keywordColumns`。

参考实现见 `Views/Home/DemoWebPage.cshtml`（Student 表完整 CRUD），其查询请求体长这样：

```json
POST /Runtime/DynCrud/Query?projectId=1
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

## 2. 用设计器拖拽组件

打开 `/designer`，界面三栏：

```
┌─────────────────────────────────────────────────────────┐
│ 顶部工具栏：🔍搜索  💾保存布局  👁️预览  🗑️清空  🏠      │
├──────────┬──────────────────────────────┬───────────────┤
│ 组件面板  │        设计画布               │   属性面板     │
│ (240px)  │      (拖拽落点)              │   (320px)     │
│ data/    │                              │ 组件名/model   │
│ container│   从左侧拖组件到这里          │ comoptions表单 │
│ wrapper/ │                              │ JSON 源码编辑  │
│ slot/... │                              │               │
└──────────┴──────────────────────────────┴───────────────┘
```

操作：

1. **拖入组件**：在左侧面板按分类浏览组件（分类来自 `ComponentMeta.Category`：data / container / wrapper / slot / composite / system），用 HTML5 Drag & Drop 把组件拖到中间画布。
2. **搜索组件**：顶部搜索框按 `label` 或 `componentName` 过滤。
3. **选中编辑**：单击画布中的组件，右侧属性面板显示：
   - `component`（只读）；
   - `modelname`（输入框，绑定 model 字段路径）；
   - `options.comoptions`（按组件默认配置自动生成键值输入）；
   - **JSON 源码**：直接编辑节点 JSON，改完点空白处自动 `JSON.parse` 回写。
4. **嵌套**：容器类组件（`isContainer=true`，如 `DynElForm` / `DynElCard` / `DynElDiv` / `DynWrapper`）可以继续往里面拖子组件。
5. **清空**：顶部“🗑️ 清空”把根节点 `childrenctrls` 置空。

> 设计器画布根节点固定为 `DynElForm`（见 `dyn-com.js` 的 `ROOT_COMPONENT`）。

## 3. 保存 / 预览布局

**保存布局**：点顶部“💾 保存布局”。前端逻辑（`Designer.cshtml`）：

```javascript
function saveLayout() {
    const json = JSON.stringify(rootNode);   // 实际项目里先过 dynCom.cleanNode(rootNode)
    fetch('/api/components', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: json
    });
    ElementPlus.ElMessage.success('布局已保存（' + new Blob([json]).size + ' bytes）');
}
```

- 保存前务必对根节点执行 `dynCom.cleanNode(rootNode)`：移除 `null / "" / [] / {}` 字段，避免脏数据入库；
- 布局 JSON 最终存到 `DynWebPage.LayoutJson`（自定义布局模板）或对应页面配置。

**预览**：点“👁️ 预览”在新窗口打开运行态，按 `normalizeNode` 补默认值后渲染节点树。

**加载还原**：从库里读出 `LayoutJson` 后：

```javascript
const node = dynCom.normalizeNode(JSON.parse(layoutJson));
// node.options.itemoptions.style / childrenctrls 等已全部补好默认值
```

## 4. 配置 Grid 跨列布局

V3 的容器布局用 **CSS Grid**（不是 Element 的 24 栅格）。父容器在 `itemoptions.style` 里写 grid 模板，子节点在 `itemoptions` 里用 `grid-column: span N` 跨列。

### 4.1 父容器（DynElDiv）设置两列网格

组件 `DynElDiv` 的默认配置（种子数据里就是）：

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
  "childrenctrls": [ ... ]
}
```

设计器里等价操作：选中 `DynElDiv`，在右侧 `itemoptions.style` 填：

```
display = grid
gridTemplateColumns = 1fr 1fr
gap = 16px 24px
```

三列就写 `gridTemplateColumns = 1fr 1fr 1fr`。

### 4.2 子节点跨两列

方式 A（直接写 style）——在子节点的 `itemoptions.style` 里加：

```json
{
  "component": "DynNInput",
  "modelname": "Remark",
  "options": {
    "itemoptions": {
      "style": { "gridColumn": "span 2" }
    }
  }
}
```

方式 B（用 class）——在子节点 `itemoptions.class` 写 `col-span-2`，并在页面 CSS 里定义：

```css
.col-span-2 { grid-column: span 2; }
.col-span-3 { grid-column: span 3; }
```

### 4.3 布局示例（两列，第 3 项跨满）

```
┌─────────────┬─────────────┐
│  Name       │  Age        │
├─────────────┴─────────────┤
│  Address（span 2）         │
├─────────────┬─────────────┤
│  Phone      │  Email      │
└─────────────┴─────────────┘
```

对应节点 JSON：

```json
{
  "component": "DynElDiv",
  "options": {
    "itemoptions": { "style": { "display": "grid", "gridTemplateColumns": "1fr 1fr", "gap": "16px 24px" } }
  },
  "childrenctrls": [
    { "component": "DynNInput", "modelname": "Name", "options": { "labeloptions": { "label": "姓名" } } },
    { "component": "DynNInputNumber", "modelname": "Age", "options": { "labeloptions": { "label": "年龄" } } },
    { "component": "DynNInput", "modelname": "Address",
      "options": { "labeloptions": { "label": "地址" },
                   "itemoptions": { "style": { "gridColumn": "span 2" } } } },
    { "component": "DynNInput", "modelname": "Phone", "options": { "labeloptions": { "label": "电话" } } },
    { "component": "DynNInput", "modelname": "Email", "options": { "labeloptions": { "label": "邮箱" } } }
  ]
}
```

> 实时效果可访问 `/demo/composite`，页面里有现成的 Grid 跨列示例卡片。

## 5. 动态 CRUD 接口速查

| 操作 | Method | URL | 关键入参 |
|---|---|---|---|
| 列表 | POST | `/Runtime/DynCrud/Query?projectId=1` | `{tableName, filter, keyword, keywordColumns, orderBy, orderDir, pageIndex, pageSize}` |
| 详情 | GET | `/Runtime/DynCrud/Get?table=Student&id=1&projectId=1` | table, id |
| 新增/更新 | POST | `/Runtime/DynCrud/Save?projectId=1` | `{tableName, data:{...}}`（data 含主键值则 UPDATE，否则 INSERT） |
| 删除 | POST | `/Runtime/DynCrud/Delete?projectId=1` | `{tableName, id}` |
| 表元数据 | GET | `/Runtime/DynCrud/Meta?table=Student&projectId=1` | 返回主键 + 列信息 |
| 表清单 | GET | `/Runtime/DynCrud/Tables?projectId=1` | 返回业务库所有表名 |

> `projectId` 可选：传了就用 `DynProject.ConnectionString` 指向的业务库；不传用配置里默认的 `Business` 连接串。
