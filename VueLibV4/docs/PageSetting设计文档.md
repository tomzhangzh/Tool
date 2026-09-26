VueLib V4 低代码平台设计文档
## 目录
1. 核心概念定义
2. 页面渲染完整流程
3. 数据库表设计（Platform 元数据库）
4. 前端核心伪代码
5. 前后端交互约定
6. 组件规范（dyn-com）
7. Action 作用域规范
8. 待办开发清单
9. 风险与注意事项

---

## 1. 核心概念定义
### 1.1 PageSetting
PageSetting 是**最小可独立渲染单元**。
- 实例化时通过 `createApp` 创建独立 Vue3 小App；
- 可挂载到任意DOM元素，支持单独局部刷新、单独销毁；
- 两种使用形态：
  1. 独立完整页面：直接打开使用；
  2. 嵌入区块：作为 Filter / Grid / Detail 嵌入外层布局壳；
- 存储在 Platform 数据库 `PageSetting` 表，包含组件树、内部Action、私有初始Model、ComponentMeta引用。

> 示例：订单筛选区块、订单表格区块、订单详情区块，各自都是一条独立 PageSetting 记录。

### 1.2 外层布局壳（Shell）
Shell本身**不是PageSetting**，仅为布局容器，职责：
1. 定义DOM布局结构（三屏、左右分栏、多区块布局）；
2. 创建共享model与`DynEventBus`事件总线；
3. 根据配置加载多个PageSettingId，逐个实例化PageSetting；
4. 将 `sharedModel`、事件总线注入每一个PageSetting实例；
5. 统一管理实例生命周期：页面销毁时，逐个unmount子PageSetting，清理Action订阅、事件监听；
6. 定义**跨PageSetting全局Action**，实现多区块联动（筛选→表格加载、表格选中→详情刷新）。

### 1.3 Model 隔离域
1. **私有 Model（PageSetting内部）**
    每个PageSetting实例独有，实例之间完全隔离；仅当前PageSetting内部组件、内部Action可访问。
    从 `InitModelJson` 初始化。
2. **共享 Model（Shell持有）**
    不属于任何PageSetting，由外层Shell创建，用于多个PageSetting之间互通数据。
    > 典型数据：筛选条件、表格选中行数据。
    > PageSetting内部Action可读、可修改sharedModel，但不拥有该对象。

### 1.4 Action 作用域划分
- **PageSetting内Action**：作用域为本PageSetting实例，优先访问私有model，支持读写注入的sharedModel；
- **Shell全局Action**：属于外层布局壳，用于跨PageSetting调度联动，依靠DynEventBus驱动。

> 全部Action统一使用 ActionHelper，尽量不手写原生JS，符合V4核心原则。

---

## 2. 页面渲染完整流程
1. 前端传入：Shell 配置 + PageSettingId 列表 + 外部传入参数
2. Shell 初始化 sharedModel、DynEventBus 事件总线
3. 循环遍历 PageSettingId 列表：
3.1 请求后端读取 PageSetting 完整配置（组件树、Action、InitModel）
3.2 dyn-core 执行 createApp 创建独立 Vue 小 App 实例
3.3 挂载到指定 DOM 元素
3.4 注入：私有 model、sharedModel、DynEventBus
4. 事件联动示例（三屏场景）
Filter 区块执行【查询 Action】→ 修改 sharedModel.filter 条件 → 事件总线发布变更
Grid 区块监听 sharedModel 变更事件 → 执行加载数据 Action，局部刷新 Grid 区块
Grid 行选中 → Action 写入 sharedModel.selectedRow → Detail 监听，重载详情页面到对应 DOM
5. 局部刷新：dyn-click-updateEl 动作，只重载目标 PageSetting 到指定 DOM，不影响其他区块
6. 页面销毁：Shell 遍历所有 PageSetting 实例，逐个 unmount，清理 Action 订阅、定时器、事件监听，防止内存泄漏

---

## 3. 数据库表设计（Platform元数据库）
> Platform库：存放低代码元数据；Business库：业务数据表，SqlSugar支持无Model增删改查、分页排序，自动读取表主键、字段信息。

### 3.1 PageSetting 表
|字段|类型|说明|
| ---- | ---- | ---- |
|Id|bigint / Guid|主键|
|Name|nvarchar(200)|名称，如：订单筛选区块、订单表格区块|
|Code|nvarchar(100)|唯一编码，业务引用标识|
|ConfigJson|nvarchar(max)|组件树配置，dyn组件、属性、comextra属性穿透配置|
|ActionJson|nvarchar(max)|PageSetting内部ActionHelper动作列表JSON|
|InitModelJson|nvarchar(max)|私有model初始值JSON|
|ComponentMetaIds|nvarchar(500)|逗号分隔，引用的ComponentMeta元数据ID|
|Remark|nvarchar(500)|备注|
|CreateTime|datetime|创建时间|
|UpdateTime|datetime|更新时间|

### 3.2 ComponentMeta 表（保留原有表）
存储动态组件元数据，DynNInput、DynSelect、DynDate等，`[Category]=常用`
|字段|类型|说明|
| ---- | ---- | ---- |
|Id|bigint / Guid|主键|
|ComCode|nvarchar(100)|组件编码 DynNInput / DynSelect|
|Category|nvarchar(100)|分类：常用/高级等|
|ComExtra|nvarchar(max)|comextra配置，支持comPassThrough属性穿透|
|ConfigJson|nvarchar(max)|组件默认属性|
|Remark|nvarchar(500)|备注|

> comPassThrough 示例配置：`comOptions.size,labelOptions.css,,labelOptions.style`，支持逗号分隔白名单，允许空项。

---

## 4. 前端核心伪代码
```js
// ===== 外层Shell布局壳 =====
const shell = {
  sharedModel: {
    filterParams: {},
    selectedRow: null
  },
  eventBus: new DynCore.DynEventBus(),
  // 配置多个PageSetting区块
  blockList: [
    { el: "#filter-wrap", pageSettingId: "ps_order_filter" },
    { el: "#grid-wrap", pageSettingId: "ps_order_grid" },
    { el: "#detail-wrap", pageSettingId: "ps_order_detail" }
  ],
  instances: [], // 保存所有PageSetting实例，用于统一销毁
  async init() {
    for(let block of this.blockList) {
      const psInst = await DynCore.createPageSetting(block.el, block.pageSettingId, {
        sharedModel: this.sharedModel,
        eventBus: this.eventBus
      });
      this.instances.push(psInst);
    }
  },
  destroy() {
    // 统一销毁所有PageSetting，释放资源
    this.instances.forEach(inst=>inst.unmount());
    this.instances = [];
  }
}

// ===== dyn-core 创建PageSetting实例方法 =====
// DynCore.createPageSetting(el, pageSettingId, injectOptions)
// injectOptions: { sharedModel, eventBus }
const psInst = await DynCore.createPageSetting("#filter-wrap", "ps_order_filter", {
  sharedModel: shell.sharedModel,
  eventBus: shell.eventBus
});

// 局部刷新PageSetting（对应 dyn-click-updateEl）
psInst.reload();
## 5. 前后端交互约定

1. 后端：.NET MVC，SqlSugar ORM；区分 Platform 库（元数据） / Business 库（业务数据）；无需提前定义 Model，自动读取表结构、主键、字段，支持增删改查、分页排序。
2. 前端提交：**通过 dyn-xx 指令提交数据给 Controller，后端返回 View 片段，渲染到指定 DOM 元素**，不单纯依靠 AJAX 直接保存 /filter。
3. 前端打包：UMD 打包，`dyn-lib.js`拆分为 `dyn-core.js` + `dyn-actionhelper.js`；默认加载 lodash、ElementPlus、TailwindCSS、LayUI、vue-validator。
4. 动态组件 dyn-com：组件定义后端生成；comextra.comPassThrough 属性穿透，支持逗号分隔白名单、空项。

## 6. 组件规范（dyn-com）

- 表单组件：DynNInput、DynSelect、DynDate 等；
- 属性穿透：`comextra.comPassThrough:"comOptions.size,labelOptions.css,,labelOptions.style"`
- 待实现：开放属性、开放插槽、开放容器。

## 7. Action 作用域规范

1. 区块内 Action：作用域绑定当前 PageSetting 实例，访问私有 model，可读写 sharedModel；
2. Shell 全局 Action：跨 PageSetting 联动，由 DynEventBus 驱动；
3. 全部动作优先使用 ActionHelper，减少手写 JS。