# VueLib V3 快速上手

> 企业级低代码平台 V3：.NET 7 MVC + SqlSugar + SQL Server，前端 UMD 无构建栈（Vue3 + Element Plus + Lodash + Sortable，直接 `<script>` 标签加载）。

## 目录

- [1. 环境要求](#1-环境要求)
- [2. 获取代码](#2-获取代码)
- [3. 修改数据库连接串](#3-修改数据库连接串)
- [4. 启动项目](#4-启动项目)
- [5. 首启动自动建库建表 + 种子数据](#5-首启动自动建库建表--种子数据)
- [6. 浏览器访问地址清单](#6-浏览器访问地址清单)
- [7. 常见问题](#7-常见问题)

---

## 1. 环境要求

| 软件 | 版本要求 | 说明 |
|---|---|---|
| .NET SDK | **.NET 7 SDK** | `dotnet --version` 应输出 7.0.x |
| SQL Server | 2016 及以上（含 LocalDB / Express） | 需开启混合或 Windows 身份验证 |
| Node.js | **不需要** | 前端全部 UMD 本地文件，无 npm / webpack / vite |
| 浏览器 | Chrome / Edge 最新版 | 设计器拖拽依赖 HTML5 Drag & Drop |

验证 .NET 7：

```powershell
dotnet --version
# 期望输出示例：7.0.40x
```

## 2. 获取代码

```powershell
# 方式一：git clone
git clone <你的仓库地址> E:\Tom\Tool\VueLibV3
cd E:\Tom\Tool\VueLibV3\src\VueLib.Web

# 方式二：解压发布包到 E:\Tom\Tool\VueLibV3，进入 Web 项目目录
cd E:\Tom\Tool\VueLibV3\src\VueLib.Web
```

目录结构（关键部分）：

```
VueLibV3/
├─ docs/                        # 本文档目录
└─ src/VueLib.Web/
   ├─ Program.cs                # 入口：服务注册 + 首启动建库 + 路由
   ├─ appsettings.json          # ★ 连接串在这里改
   ├─ Data/AppDbContext.cs      # Platform 上下文 + BusinessDbFactory
   ├─ Models/                   # 8 张平台表实体
   ├─ Services/                 # SeedService（种子）+ DynCrudService（动态CRUD）
   ├─ Areas/
   │  ├─ Platform/              # 平台 API（组件/动作/字典/模板）
   │  └─ Runtime/               # 运行时动态 CRUD
   ├─ Controllers/HomeController.cs
   ├─ Views/Home/               # 桌面/设计器/demo 页面
   └─ wwwroot/
      ├─ lib/                   # vue / element-plus / lodash / sortable（UMD）
      ├─ js/                    # dyn-core / dyn-actionhelper / dyn-loader / dyn-com
      └─ css/tailwind.css
```

## 3. 修改数据库连接串

编辑 `src/VueLib.Web/appsettings.json`：

```json
{
  "Urls": "http://localhost:5001",
  "ConnectionStrings": {
    "Platform": "Server=.;Database=VueLibV3_Platform;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    "Business": "Server=.;Database=VueLibV3_Business;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

按本机 SQL Server 实际情况修改：

| 场景 | 修改点 |
|---|---|
| 默认实例（本机 Windows 认证） | 保持 `Server=.` 或 `Server=localhost` |
| 命名实例，如 `SQLEXPRESS` | 改为 `Server=.\SQLEXPRESS` |
| SQL 账号密码 | 改为 `Server=你的IP;User Id=sa;Password=xxx;Database=...;TrustServerCertificate=True` |
| 改库名 | 把 `Database=VueLibV3_Platform` / `Database=VueLibV3_Business` 改成你想要的库名即可，**库不存在会自动创建** |

> 注意：`Platform` 库存元数据（组件、动作、模板、页面、桌面），`Business` 库存业务表（如种子里的 `Student`）。两库必须分开。

## 4. 启动项目

```powershell
cd E:\Tom\Tool\VueLibV3\src\VueLib.Web
dotnet run --urls http://localhost:5001
```

也可以直接改 `appsettings.json` 里的 `"Urls": "http://localhost:5001"`，之后只跑 `dotnet run`。

启动成功后控制台会输出类似：

```
[dyn-v3] dyn-core v3 loaded
[dyn-loader] bootstrap done: 21 components, 14 actions
Now listening on: http://localhost:5001
```

## 5. 首启动自动建库建表 + 种子数据

**无需手动执行任何 SQL。** 首启动时 `Program.cs` 会自动：

1. `AppDbContext.EnsureInit()`：
   - 调用 `db.DbMaintenance.CreateDatabase()` 自动创建 `VueLibV3_Platform` 库；
   - CodeFirst 自动创建 8 张平台表（`ComponentMeta` / `DesktopSolution` / `DesktopShortcut` / `DynActionHelper` / `DynDict` / `DynProject` / `DynTemplate` / `DynWebPage`）。
2. `SeedService.Seed()` 在库为空时灌入：
   - **21 个组件元数据**（8 数据组件 + 6 容器 + 1 布局壳 + 1 插槽 + 1 组合组件 + 4 系统组件）；
   - **14 个内置 ActionHelper 动作脚本**；
   - **4 个页面模板**（crud-list / tree-right-list / home-grid / custom-layout）；
   - 平台词典（YesNo / Enabled）；
   - 1 个默认解决方案 + 1 个示例工程（DemoProject，业务库 `VueLibV3_Business`）+ 3 个桌面快捷方式；
   - 在业务库 `VueLibV3_Business` 自动建 `Student` 表演示数据（张三/李四/王五/赵六/钱七）。

> 种子是“空库才灌”：`if (db.Queryable<T>().Any()) return;`。想重置种子，直接删掉两个库再启动即可。

## 6. 浏览器访问地址清单

启动后打开 `http://localhost:5001`，完整入口列表：

| URL | 页面 | 说明 |
|---|---|---|
| `/` | 桌面 | 解决方案 / 快捷方式入口（Index.cshtml） |
| `/designer` | 设计器 | 拖拽组件画布 + 右侧属性面板 |
| `/demo` | Demo 汇总 | 4 个 Demo 卡片导航 |
| `/demo/actionhelper` | 动作测试 | 验证 14 个内置动作 + Student 动态 CRUD |
| `/demo/chain-editor` | 动作链编辑器 | 拖拽动作编排，生成链 JSON |
| `/demo/composite` | 组合组件 | 开放属性/容器/slot + CSS Grid 跨列示例 |
| `/demo/webpage` | CRUD 页面 | Student 表查询/分页/新增/编辑/删除完整示例 |
| `/api/components` | JSON | 全部启用的组件元数据（前端启动时拉取） |
| `/api/actionhelpers` | JSON | 全部启用的动作脚本（前端启动时 eval 注册） |
| `/api/templates` | JSON | 全部模板 |
| `/api/dict/YesNo` | JSON | 指定词典 |
| `/Runtime/DynCrud/Tables?projectId=1` | JSON | 业务库所有表名（设计器选表用） |

## 7. 常见问题

**Q1：启动报 "未配置连接字符串 ConnectionStrings:Platform"**
检查 `appsettings.json` 是否被改坏，JSON 是否合法，`ConnectionStrings:Platform` 节点是否存在。

**Q2：SQL Server 连接失败（Login failed / network-related）**
- 确认 SQL Server 服务已启动（`services.msc` 里看 SQL Server 代理/服务）；
- 确认账号密码与 `appsettings.json` 一致；
- 本机默认实例用 `Server=.`，命名实例用 `Server=.\SQLEXPRESS`；
- 加 `TrustServerCertificate=True` 避免证书错误。

**Q3：改了 `DynActionHelper` / `ComponentMeta` 表但前端没生效**
动作与组件元数据在前端启动时一次性拉取并 `eval` 注册（见 `dyn-loader.js` 的 `bootstrap()`）。改库后**强刷浏览器**（Ctrl+F5）即可，无需重启后端。

**Q4：想清空重来**
在 SSMS 里删掉 `VueLibV3_Platform` 和 `VueLibV3_Business` 两个库，再 `dotnet run`，会自动重建并重新种子。

**Q5：端口被占用**
换端口：`dotnet run --urls http://localhost:5002`，或改 `appsettings.json` 的 `Urls`。
