# VueLib - ASP.NET Core MVC + Vue3 UMD 动态组件加载系统

基于 .NET 8.0 MVC + Vue 3 UMD + SqlSugar 的动态组件加载框架。组件定义存储在 SQL Server 数据库中，前端通过 `vueLoadCom()` 动态加载并转换为 Vue 3 异步组件；同时支持以 Razor View (.cshtml) 作为组件定义载体，数据库不存在时自动回退到 Razor。

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端 | ASP.NET Core 8.0 MVC |
| ORM | SqlSugarCore 5.x |
| 数据库 | SQL Server |
| 前端 | Vue 3 UMD 全局构建 + Vue Router 4 |
| 组件载体 | SQL Server 数据库 + Razor View (.cshtml) |

## 项目结构

```
VueLib/
├── VueLib.sln
└── src/VueLib.Web/
    ├── Program.cs                    # 启动配置（依赖注入、中间件）
    ├── appsettings.json              # 连接字符串等配置
    ├── Controllers/
    │   ├── HomeController.cs         # SPA 入口页面
    │   └── ComponentController.cs    # 组件加载 API (/api/component/*)
    ├── Models/
    │   ├── ComponentDefinition.cs    # 数据库实体（SqlSugar）
    │   ├── ComponentType.cs          # 枚举：Common=1, Page=2
    │   └── ComponentViewModel.cs     # Razor 组件视图模型
    ├── Data/
    │   └── AppDbContext.cs           # SqlSugar 上下文
    ├── Services/
    │   ├── ComponentService.cs       # 组件服务（DB优先 + Razor回退）
    │   └── RazorComponentRenderer.cs # Razor 组件渲染解析器
    ├── Dtos/
    │   └── ComponentDtos.cs          # API 数据传输对象
    ├── Views/
    │   ├── Shared/
    │   │   ├── _Layout.cshtml        # SPA 布局（引入 Vue + 挂载点）
    │   │   └── Components/           # Razor 组件定义载体
    │   │       └── RazorDemo.cshtml  # 示例：纯Razor定义的组件
    │   └── Home/Index.cshtml         # SPA 入口
    ├── wwwroot/
    │   ├── js/
    │   │   ├── vue.global.prod.js    # Vue 3 UMD
    │   │   ├── vue-router.global.prod.js  # Vue Router 4 UMD
    │   │   ├── vue-loader.js         # 核心：vueLoadCom 动态加载器
    │   │   └── app.js                # 应用入口（注册组件+构建路由）
    │   └── css/app.css
    └── sql/
        ├── 01_InitDatabase.sql       # 建库建表脚本
        └── 02_SeedData.sql           # 模拟数据（3公共 + 3页面组件）
```

## 快速开始

### 1. 初始化数据库

在 SQL Server 中依次执行：

```sql
-- 建库建表
sqlcmd -S . -i sql/01_InitDatabase.sql
-- 插入模拟数据
sqlcmd -S . -i sql/02_SeedData.sql
```

或在 SSMS 中打开执行。

### 2. 配置连接字符串

修改 `appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VueLib;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. 运行

```bash
cd src/VueLib.Web
dotnet run
```

浏览器访问 `https://localhost:5001`（或控制台输出的端口）。

## 核心机制

### 组件加载流程

```
浏览器请求页面
    │
    ▼
Layout 加载 Vue3 + VueRouter + vue-loader.js + app.js
    │
    ▼
app.js 启动 → GET /api/component/list 获取组件清单
    │
    ├─ 公共组件(ComponentType=1) → app.component(name, vueLoadCom(name)) 全局注册
    └─ 页面组件(ComponentType=2) → 注册到 Vue Router routes
    │
    ▼
用户导航到路由 / 组件首次渲染
    │
    ▼
vueLoadCom(name) 触发 → GET /api/component/define/{name}
    │
    ├─ 数据库存在 → 返回 DB 中的 template/script/style
    └─ 数据库不存在 → 回退渲染 Views/Shared/Components/{name}.cshtml
    │
    ▼
解析 script (export default {...}) → 组件选项对象 + 注入 template + 动态注入 style
    │
    ▼
Vue 渲染组件
```

### vueLoadCom API

```javascript
// 基础用法：返回 Vue 3 异步组件定义
const MyComp = vueLoadCom('MyComponent');

// 用于全局注册
app.component('MyComponent', vueLoadCom('MyComponent'));

// 用于路由
const routes = [{ path: '/my', component: vueLoadCom('MyPage') }];

// 自定义加载/错误组件
vueLoadCom('MyComponent', {
    delay: 300,
    timeout: 15000,
    loadingComponent: { template: '<div>加载中...</div>' },
    errorComponent: { template: '<div>加载失败</div>' }
});

// 预加载（后台静默加载）
vueLoadCom.preload(['CompA', 'CompB']);

// 清除缓存（热更新场景）
vueLoadCom.clearCache('MyComponent');  // 单个
vueLoadCom.clearCache();               // 全部
```

### 数据库表结构

**ComponentDefinitions**

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY | 主键 |
| ComponentName | NVARCHAR(100) | 组件名（唯一） |
| ComponentType | TINYINT | 1=公共组件, 2=页面组件 |
| RoutePath | NVARCHAR(200) | 页面组件路由路径 |
| TemplateContent | NVARCHAR(MAX) | Vue template HTML |
| ScriptContent | NVARCHAR(MAX) | Vue script (export default {...}) |
| StyleContent | NVARCHAR(MAX) | 组件 CSS（可选） |
| Description | NVARCHAR(500) | 描述 |
| IsEnabled | BIT | 是否启用 |
| SortOrder | INT | 排序 |
| CreatedAt / UpdatedAt | DATETIME2 | 时间戳 |

### 组件定义格式（ScriptContent）

数据库中 `ScriptContent` 存储标准的 Vue 组件选项：

```javascript
export default {
    name: "MyComponent",
    props: {
        title: { type: String, default: "Hello" }
    },
    data() {
        return { count: 0 };
    },
    methods: {
        increment() { this.count++; }
    },
    computed: { /* ... */ },
    mounted() { /* ... */ }
};
```

> 注意：使用 Options API，不支持 `<script setup>` 语法（运行时编译限制）。

### Razor View 组件定义格式

在 `Views/Shared/Components/{ComponentName}.cshtml` 中定义：

```cshtml
@* ComponentType: Common *@
@* RoutePath: /optional *@
@* Description: 组件描述 *@
@model VueLib.Web.Models.ComponentViewModel
@{ Layout = null; }

<!--COMPONENT_TYPE:1-->

<!--TEMPLATE_START-->
<div>
    <h3>{{ title }}</h3>
    <button @@click="count++">{{ count }}</button>
</div>
<!--TEMPLATE_END-->

<!--SCRIPT_START-->
export default {
    name: "MyComponent",
    data() { return { count: 0 }; }
};
<!--SCRIPT_END-->

<!--STYLE_START-->
h3 { color: red; }
<!--STYLE_END-->
```

> Razor 中 `@click` 需转义为 `@@click`。

## API 接口

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/component/list` | 获取所有已启用组件清单 |
| GET | `/api/component/define/{name}` | 获取单个组件完整定义 |
| POST | `/api/component/defines` | 批量获取组件定义（Body: string[]） |
| GET | `/api/component/pages` | 获取所有页面组件（路由表） |

响应格式：

```json
{
    "success": true,
    "message": "共 6 个组件",
    "data": { /* ... */ }
}
```

## 模拟数据说明

执行 `02_SeedData.sql` 后包含 6 个组件：

### 公共组件 (3个)
| 名称 | 说明 |
|------|------|
| HelloWorld | 基础问候组件，演示 props 与事件 |
| Counter | 计数器，支持步长和 change 事件 |
| UserCard | 用户卡片，展示 computed 与角色样式 |

### 页面组件 (3个)
| 名称 | 路由 | 说明 |
|------|------|------|
| HomePage | `/` | 首页，组合演示所有公共组件 |
| AboutPage | `/about` | 关于页，介绍系统架构 |
| UserListPage | `/users` | 用户列表，演示搜索过滤 |

### Razor 回退组件 (1个)
| 名称 | 说明 |
|------|------|
| RazorDemo | 纯 .cshtml 定义，数据库中不存在，演示回退加载 |

## 扩展开发

### 新增数据库组件

1. 向 `ComponentDefinitions` 表插入记录（TemplateContent + ScriptContent）
2. 无需重新编译，刷新页面即可加载

### 新增 Razor 组件

1. 在 `Views/Shared/Components/` 下创建 `{Name}.cshtml`
2. 按约定格式编写 template/script/style
3. 系统自动扫描并注册，数据库中同名组件会覆盖 Razor 定义

### 新增 API 组件来源

扩展 `ComponentService.GetDefineByNameAsync`，在数据库和 Razor 之后添加其他来源（如 Redis、远程配置中心等）。

---

# 低代码动态页面平台（NutUI）

在动态组件加载基础上，扩展了完整的低代码页面设计器，支持可视化拖拽配置、手机模拟器预览、表单验证器。

## 技术栈扩展

| 层级 | 技术 |
|------|------|
| 设计器 UI | Element Plus（PC端三栏布局） |
| 移动端组件 | NutUI 3.x（京东移动端组件库） |
| 组件定义载体 | Razor View (.cshtml) + 数据库元数据 |
| 页面配置存储 | SQL Server (PageSetting 表) |

## 新增数据库表

执行 `sql/03_LowCodeTables.sql` 和 `sql/04_LowCodeSeedData.sql`：

- **ComponentMeta** — 组件元数据（注册名、分类、默认配置、加载地址）
- **PageSetting** — 页面配置（组件树 JSON + 默认数据模型）

## 访问地址

| 页面 | 地址 | 说明 |
|------|------|------|
| 设计器 | `/designer` | Element Plus 三栏设计器，手机模拟器预览 |
| 页面预览 | `/designer/preview?code=user-register` | 独立手机端预览（NutUI 渲染） |

## 设计器功能

### 三栏布局

```
┌─────────────────────────────────────────────────────┐
│  工具栏：页面选择 / 保存 / 验证 / JSON / 新窗口预览    │
├──────────┬──────────────────────┬───────────────────┤
│ 组件面板  │   手机模拟器画布       │  属性配置面板      │
│ (左240px) │   (中 375×667 iPhone) │  (右320px)        │
│          │                      │                   │
│ - 表单   │  📱 iPhone 外壳       │  绑定字段         │
│ - 布局   │  ┌──────────────┐    │  标签文字         │
│ - 展示   │  │  NutUI 渲染  │    │  必填/验证器      │
│ - 通用   │  │  实时预览     │    │  占位符/选项值    │
│          │  └──────────────┘    │  上移/下移/删除   │
└──────────┴──────────────────────┴───────────────────┘
```

### 操作方式

1. **添加组件**：左侧面板点击组件，添加到当前选中容器（未选中则添加到根容器）
2. **选中组件**：在属性面板编辑配置（画布中暂不支持直接点击选中，通过组件树定位）
3. **编辑属性**：右侧面板修改绑定字段、标签、验证规则等
4. **JSON 编辑**：顶部切换 JSON 视图，直接编辑完整配置
5. **保存**：保存到数据库 PageSetting 表
6. **验证**：点击"验证表单"触发所有字段验证

## 组件配置 Schema (jsonconfig)

```json
{
  "component": "NInput",
  "modelname": "user.name",
  "options": {
    "comoptions": { "placeholder": "请输入", "clearable": true },
    "comlisteners": { "blur": "handleBlur" },
    "labeloptions": { "label": "用户名", "required": true, "show": true },
    "itemoptions": { "style": {}, "class": "" },
    "optionValues": "选项1,选项2,选项3"
  },
  "validators": [
    { "type": "required", "message": "必填" },
    { "type": "minLength", "value": 3, "message": "最少3个字符" },
    { "type": "pattern", "value": "^[a-zA-Z]+$", "message": "只能是字母" }
  ],
  "childrenctrls": [],
  "slots": {},
  "extendinfo": {}
}
```

### 字段说明

| 字段 | 说明 |
|------|------|
| `component` | 组件注册名（如 NInput、NSelect） |
| `modelname` | 数据绑定路径，支持嵌套（a.b.c），从 parentmodelinfo 取值/赋值 |
| `options.comoptions` | 传递给 NutUI 组件的 props |
| `options.comlisteners` | 事件监听器（函数名字符串） |
| `options.labeloptions` | 标签配置（label/required/show） |
| `options.optionValues` | 选择器选项（逗号分隔字符串） |
| `validators` | 验证规则数组 |
| `childrenctrls` | 子组件配置数组（容器组件递归渲染） |
| `slots` | 插槽配置 |
| `extendinfo` | 扩展信息 |

## 验证器系统

### 内置验证器

| 类型 | 说明 | value 参数 |
|------|------|-----------|
| `required` | 必填（非空/非空数组） | - |
| `requiredTrue` | 必须为 true（同意协议） | - |
| `minLength` | 最小字符串长度 | 数字 |
| `maxLength` | 最大字符串长度 | 数字 |
| `min` | 最小值 | 数字 |
| `max` | 最大值 | 数字 |
| `pattern` | 正则匹配 | 正则字符串 |
| `email` | 邮箱格式 | - |
| `phone` | 手机号格式 | - |
| `url` | URL 格式 | - |
| `number` | 数字格式 | - |

### 验证时机

- **blur 时**：输入框/文本域失焦后自动验证
- **change 时**：单选/多选/评分变化后自动验证
- **提交时**：点击"验证表单"或独立预览页"提交"按钮递归验证所有字段

### 验证结果

```javascript
// 返回结构
{
  valid: false,
  errors: {
    "username": ["用户名至少3个字符"],
    "phone": ["手机号格式不正确"]
  }
}
```

## 已实现的 NutUI 组件

### 表单项（11个）
`NInput` `NTextarea` `NSwitch` `NRadio` `NCheckbox` `NStepper` `NRate` `NSlider` `NPicker` `NDatePicker` `NUploader`

### 容器（5个）
`NForm` `NCellGroup` `NDivContainer` `NDivider` `NGrid`

### 展示（4个）
`NTag` `NText` `NNoticeBar` `NProgress`

### 通用（2个）
`NButton` `NImage`

## 新增 NutUI 组件步骤

1. 在 `Areas/NutComponent/Controllers/FormItemController.cs`（或对应分类 Controller）添加 Action
2. 在 `Areas/NutComponent/Views/FormItem/` 下创建同名 `.cshtml`
3. 表单项组件使用 `_FormItemLayout.cshtml`，其他使用默认 `_NutLayout.cshtml`
4. 在 `ComponentMeta` 表插入元数据记录（或修改 `sql/04_LowCodeSeedData.sql`）

组件 View 示例：
```cshtml
@{
    Layout = "~/Areas/NutComponent/Views/_FormItemLayout.cshtml";
}
<nut-input v-model="modelinfo"
           v-bind="props.jsonconfig.options.comoptions"
           v-on="getComlisteners()"
           @@blur="comInnerInfo.validation.touched = true; validate()">
</nut-input>

@section setupScripts{
    DVALUE = '';  // 默认值
}
```

## 核心运行时 (nut-runtime.js)

| 函数 | 说明 |
|------|------|
| `nutLoadCom(name, url)` | 从 Razor View 加载组件，返回 Vue 异步组件 |
| `nutInit(options)` | 初始化 NutUI 应用（注册组件+路由） |
| `nutValidate(config, model)` | 递归验证整个表单配置 |
| `nutValidateField(value, rules)` | 验证单个字段 |
| `nutRenderPage(config, model, container)` | 渲染页面配置到容器 |

## 示例页面

种子数据包含 2 个示例页面：

1. **用户注册表单** (`user-register`) — 用户名/手机号/性别/同意协议，含完整验证
2. **商品评价** (`product-review`) — 评分/评价内容/图片上传

访问 `/designer/preview?code=user-register` 查看手机端效果。

---

# ElementUI PC端组件库

在 NutUI 移动端组件基础上，新增了 24 个 Element Plus PC 端组件，支持在设计器中混合使用两套组件库。

## 已实现的 ElementUI 组件（24个）

### 表单项（11个）
`ElInput` `ElInputNumber` `ElSelect` `ElSwitch` `ElRadio` `ElCheckbox` `ElDatePicker` `ElTimePicker` `ElSlider` `ElRate` `ElColorPicker`

### 通用/展示（8个）
`ElButton` `ElTag` `ElBadge` `ElAvatar` `ElProgress` `ElAlert` `ElDivider` `ElImage`

### 布局容器（5个）
`ElDivContainer` `ElCard` `ElRow` `ElCol` `ElTabs`

## 组件目录结构

```
Areas/ElementComponent/
├── Controllers/
│   ├── FormItemController.cs    # 11个表单项 Action
│   ├── CommonController.cs       # 8个通用/展示 Action
│   └── ContainerController.cs    # 5个布局容器 Action
└── Views/
    ├── _ElementLayout.cshtml     # 公共布局（setup 逻辑、默认配置合并）
    ├── FormItem/                  # 11个表单项视图
    ├── Common/                    # 8个通用/展示视图
    └── Container/                 # 5个布局容器视图
```

## 公共布局核心机制 (`_ElementLayout.cshtml`)

- **默认配置合并**：setup 开头通过 `deepMerge(props.jsonconfig, defaults)` 直接填充默认值（不通过 reactive 代理，避免响应式循环）
- **本地兜底对象**：`props.jsonconfig` 为 undefined 时用本地 `jc` 对象兜底
- **modelinfo computed**：支持嵌套路径（a.b.c）的双向绑定，通过 lodash `_.get/_.set` 操作 `parentmodelinfo`
- **父子 modelname 相对路径**：支持 `~` 绝对路径、`[` 直接拼接、空 modelname 继承父前缀
- **验证器系统**：required/min/max/pattern/email/number 等内置验证器
- **safeChildren computed**：过滤 `childrenctrls` 中的 null/undefined 元素，防止递归渲染崩溃
- **事件监听器**：`getComlisteners()` 支持函数引用和字符串函数名（从 context.attrs 查找）

## 新增 ElementUI 组件步骤

1. 在对应分类 Controller（FormItem/Common/Container）添加 Action
2. 在对应 Views 子目录创建同名 `.cshtml`，Layout 设为 `_ElementLayout.cshtml`
3. 模板中使用 `jc.options.comoptions` 传递 props，`v-on="getComlisteners()"` 绑定事件
4. 在 `ComponentMeta` 表插入元数据记录（UiLibrary = 'ElementUI'）

组件 View 示例：
```cshtml
@{
    Layout = "~/Areas/ElementComponent/Views/_ElementLayout.cshtml";
}
<template>
    <el-button v-bind="jc.options.comoptions" v-on="getComlisteners()">
        {{ jc.options.comoptions.text || '按钮' }}
    </el-button>
</template>
@section setupScripts{
    DVALUE = null;
}
```

---

# Windows 风格桌面系统

新增 Windows 风格桌面，支持多窗口管理、快捷方式、解决方案管理。

## 访问地址

| 页面 | 地址 | 说明 |
|------|------|------|
| 桌面 | `/` 或 `/Desktop` | Windows 风格桌面主页 |
| 快捷方式管理 | `/Desktop/ShortcutManage` | 桌面快捷方式 CRUD |
| 解决方案管理 | `/Desktop/SolutionManage` | 解决方案（应用集合）管理 |
| 页面管理 | `/Desktop/PageManage` | 低代码页面管理 |

## 核心功能

- **多窗口管理**：可同时打开多个设计器/预览窗口，支持最小化/最大化/关闭/拖拽
- **快捷方式**：桌面图标双击打开对应应用（设计器/预览/自定义页面）
- **解决方案**：将多个页面组合为一个解决方案，一键打开
- **任务栏**：底部任务栏显示已打开窗口，点击切换/最小化

## 数据库表

执行 `sql/13_DesktopTables.sql`：

- **DesktopShortcut** — 桌面快捷方式（名称、图标、URL、排序）
- **DesktopSolution** — 解决方案（名称、图标、页面列表 JSON）

---

# 设计器使用指南

## 快速开始

1. 访问 `/designer` 打开设计器
2. 顶部下拉选择页面（或新建页面）
3. 左侧组件面板选择 UI 库（NutUI 移动端 / ElementUI PC端）
4. 点击组件添加到画布，或拖拽到容器中
5. 右侧属性面板编辑组件配置（字段绑定、标签、验证规则等）
6. 顶部"保存"按钮保存到数据库
7. 顶部"预览"按钮在新窗口打开独立预览

## UI 库切换

设计器顶部左侧组件面板支持 UI 库筛选下拉：
- **全部**：显示所有组件
- **NutUI**：仅显示移动端组件（N 前缀）
- **ElementUI**：仅显示 PC 端组件（El 前缀）

同一页面可混合使用两套组件库，NDynamicCom 根据 `jsonconfig.component` 自动路由到对应组件。

## 组件配置 Schema

详见上方"组件配置 Schema (jsonconfig)"章节。两套组件库共用同一套配置结构，仅 `component` 字段不同。

## 常见问题

### Q: 设计器中组件拖不动？
A: 确保组件在容器内（NForm/NCellGroup/ElDivContainer 等），容器组件支持拖拽排序。

### Q: 预览页面组件不显示？
A: 检查浏览器控制台是否有组件加载失败（404），确认 Controller Action 和 View 文件存在。

### Q: Maximum call stack size exceeded？
A: 检查页面配置 JSON 是否有循环引用（容器的 childrenctrls 中包含自身）。NDynamicCom 已添加深度限制（20层）和 null 检查。

---

# 技术架构说明

## 前后端分离模式

- **后端**：ASP.NET Core MVC 提供 Razor View 渲染（组件定义载体）和 REST API（组件元数据、页面配置）
- **前端**：Vue 3 UMD 全局构建，运行时编译模板，无需 Node.js 构建流程
- **组件加载**：`nutLoadCom(name, url)` 返回 `defineAsyncComponent`，首次渲染时 fetch Razor View，解析 template + script，缓存到 `componentCache`

## 响应式设计注意事项

- **不要用 `reactive(props.jsonconfig)`**：会创建响应式代理，修改时触发父组件重渲染 → 子组件重新 setup → 再次修改 → 无限循环
- **直接修改 props 对象属性是安全的**：`props.jsonconfig` 只是普通对象引用，不是响应式代理
- **模板中避免每次渲染创建新对象**：`v-bind`/`v-on` 绑定新对象引用会触发重渲染，用 computed 缓存
- **NDynamicCom 递归防护**：null 检查 + 深度限制（20层）+ safeChildren 过滤空洞元素

## 数据库连接

```
Server=.;Database=VueLib;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

SqlSugar 注入方式：项目用 `AppDbContext.Create()` 工厂方式，**不直接注册 ISqlSugarClient 到 DI**。

## Area 路由

Area 路由必须在 default 路由前面注册（见 `Program.cs`）：
```csharp
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```
