# VueLib（V4）整体规划回顾

## 定位

自研低代码渲染引擎，基于 **Vue3 + ElementPlus**，后端是.NET + SqlSugar，**无 jQuery**，使用 Axios UMD。
核心目标：让开发人员几乎不用手写前端 JS，通过 JSON 配置渲染页面、表单、容器，配套可视化设计器。

## 核心架构约定（重点）

1. **组件配置结构**

- `__uid`：唯一标识
- `component`：真实组件名称（数据库存储原生组件名）
- `itemOptions`：**子项布局配置**（Grid/Flex 容器的子元素，如`grid-column:span 2`，仅作用于容器内子项布局）

> 
> 👉 容器层（DynGridContainer/DynElContainer）在`safeChildren`预处理：预览模式把`itemOptions`合并进`options.itemOptions`；设计模式直接读取，**不再额外增加 wrapper DOM，避免破坏 Grid 布局**

- `options.itemOptions`：**组件本体根元素样式 class/style**
- `comInnerInfo`：属性穿透配置，向下递归透传公共属性，例如`size:small`、`labelOptions`，子组件自动继承（表单 label 左右布局就是靠这个）
- `options`：组件自身业务参数（输入框 placeholder、下拉选项等）
- `modelInfo`：绑定模型路径，做数据绑定
- `comProps`：**动态属性面板配置**（右侧属性面板只这块是动态渲染，其余是固定基础面板）

2. **渲染核心：DynDynamicCom**

- 不再添加额外`<span>` wrapper，消除多余 DOM 破坏 Grid 布局
- 设计模式：直接渲染组件，`itemOptions`交给父容器预处理；靠 Overlay 浮层做选中高亮，SortableJS 直接操作真实 DOM 实现拖拽
- 预览模式：父容器`safeChildren`提前合并`itemOptions`到`options.itemOptions`，布局效果和设计器保持一致

3. **容器体系**
统一规范：`DynGridContainer`、`DynElContainer`、DynForm 容器，全部复用`safeChildren`逻辑，处理子项`itemOptions`布局合并。

- DynGridContainer：网格布局，自动加`dyn-is-grid`标记
- DynElContainer：弹性 flex 布局，加`dyn-is-flex`标记

## 设计器（右侧属性面板）规划

1. 整体尺寸：全局`size=small`，formItem 采用**label 左、控件右**水平布局
2. 面板分组：
   - 基础属性（折叠面板）：顶部【控件类型下拉】可切换组件，下方嵌入`comProps`动态渲染组件专属配置
   - 布局样式（itemOptions）：新增`DynCodeMirror`，编辑 JSON 字符串，自动转成 js 对象写入`itemOptions.style`，附带 JSON 语法校验
   - 动态表达式 evalCondition
   - 校验规则
3. 数据库：`PropertyConfigJson`存储各组件对应的属性表单 JSON，空的需要补齐；`model.current.options.labeloptions.required`控制必填红色星号展示。

## Action 动作系统（ACHM）

短语法，无旧语法，一套统一规则；支持组合动作（confirm→postback，openWindow 后回调 reload/updateElement），支持`calc`计算动作，通过`ctx`上下文传递参数。

## 数据与元数据

- SqlSugar 读取数据库表结构，自动生成页面 JSON（汇总屏、筛选屏、明细屏多屏模板）
- 支持模板机制：预定义三屏模板，用户只需要选择表、配置 URL，快速生成页面，后续可在设计器修改组件、替换控件
- 多组件库兼容：ElementPlus、AntdVue、NutUI，支持自定义组件

## 关键约束

1. lodash 强制引入，不做降级容错
2. 组件注册统一在 dyn-core，`ensureRegistered`幂等注册，多 createApp 实例（主页面、弹窗、右侧属性面板）都能拿到注册的组件
3. 弹窗逻辑：创建空 div，调用`dyn.mount()`挂载组件 json，不再单独维护一套渲染引擎，和页面渲染逻辑统一