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
