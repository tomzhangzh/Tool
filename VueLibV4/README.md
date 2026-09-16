# VueLibV4

企业级低代码 / 动态组件平台。基于 **ASP.NET Core 8 + SqlSugar ORM + SQL Server 双库**，前端为**原生 Vue 3.5 UMD（h() 渲染，无打包）**。

本版本融合了 V1（`E:\Tom\Tool\VueLib`）与 V3（`E:\Tom\Tool\VueLibV3`）的成熟实现：

- **组件定义机制沿用 V1**：组件在 **Razor View（`Views/Shared/Components/*.cshtml`）+ 数据库 ComponentMeta** 双定义源中定义，由 `_Layout.cshtml`（layout.cs）统一加载——**View 代码优先，DB 回退**。
- **桌面沿用 V1**：渐变背景 + Emoji 图标 + 任务栏 + 开始菜单。
- **运行时/设计器沿用 V3**：JSON 配置树 → VNode（`DynRender`）、悬浮 overlay 不包装 DOM 的设计器。
- **动作系统**：所有动作入库（DynActionHelper 表），前端 `/all` 动态注册，并补齐 V1 内置动作（postback/postdata/confirm-postdata/reload/open/close/updateel/evaljs/load/setVueModel/showmessage/redirect）。

> **两个前端核心文件以 V1 完整版为基底，merge V3 增量（方向 V1 为主）**：
> - `wwwroot/dyn/dyn-core.js` ≈ 52 KB：直接移植 V1 `wwwroot/js/dyn-core.js`（41.7 KB，含 mountCore / init / reload / postback / open / close / findAncestor / closestDynInit / getVueModel / setPathVal / `__expr` / eventBus / runJsonActions），尾部追加 V3 增量 `DynRender` 递归渲染 + `normalize/clean/isEmpty` 空值处理 + `getPath/setPath/evalValue/mount`。导出 `global.dynCore` / `global.dyn`（V1 执行引擎）与 `global.DynCore`（V3 渲染核）。
> - `wwwroot/dyn/dyn-actionhelper.js` ≈ 64 KB：直接移植 V1 `wwwroot/js/dyn-actionhelper.js`（58 KB，事件委托 ACTION_EVENTS 含 init、`dyn-{event}-{action}` 标记、完整 `$ctx` 上下文、全部内置动作、chain、setVueModel 用 lodash `_.set` + `el.__dynProxy`），尾部叠加 V4 增量：从 `/api/platform/dynactionhelper/all` 动态注册入库动作，并向 V3 运行时导出 `global.DynActionHelper.run(code,args,ctx)`（V1 内置优先、入库动作兜底）。
> - 装配顺序（`_Layout.cshtml`，同 V1）：`vue.global.prod.js → jquery.min.js → lodash → … → dyn-validator → dyn-core(dynCore) → dyn-actionhelper(注入动作) → dyn-template → dyn-com → dyn-designer`。V1 dyn-core 依赖 **jQuery**，已引入 `~/lib/jquery/dist/jquery.min.js`。

---

## 一、快速启动

```powershell
# 1. 还原并编译（0 错误）
cd E:\Tom\Tool\VueLibV4
dotnet build VueLibV4.sln

# 2. 启动（监听 http://localhost:5010）
cd src\VueLibV4.Web
dotnet run
```

启动后浏览器打开：<http://localhost:5010/> （根路由 302 跳转到 `/Platform/Page/Desktop`）。

> 端口在 `src/VueLibV4.Web/appsettings.json` 的 `Urls` 配置（默认 5010，避免与 V3 的 5000 冲突）。

## 二、数据库

连接串在 `src/VueLibV4.Web/appsettings.json`：

```json
"ConnectionStrings": {
  "PlatformDb": "Server=localhost;Database=PlatformDb;Trusted_Connection=True;TrustServerCertificate=True;",
  "BusinessDb":  "Server=localhost;Database=BusinessDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

- 首次启动自动建库 + 建表 + 种子（`Core/Seeder.cs` 执行 `SeedData/*.sql` 与 `*.seed.json`）。
- **平台表主键统一 `Id INT IDENTITY(1,1)` 自增**；字符串列统一 `NVARCHAR`（非字符串列如 `IsActive BIT`、`SortNo INT`、`CreateTime DATETIME2` 保留原生类型）。
- 业务库 3 张表：`Student`（12 行种子）、`Course`、`BusinessDict`。
- 若连不上 SQL Server，请改 `appsettings.json` 连接串后重启。

> 说明：V4 复用了与 V3 同名的 `PlatformDb` / `BusinessDb`。若这些库已由 V3 建过（旧主键为 BIGINT），Seeder 会**幂等补齐新增列**（ComponentMeta 的 TemplateContent/ScriptContent/StyleContent/PropertyConfigJson/DefaultConfigJson），不会清空数据；全新建库时主键即为 INT。

## 三、页面清单（已逐页浏览器实测，0 console 错误）

| 路由 | 说明 |
|------|------|
| `/Platform/Page/Desktop` | 渐变桌面 + Emoji 图标 + 任务栏 + 开始菜单 |
| `/Platform/Page/Designer` | 可视化设计器（组件库 / 画布 / 属性面板，左右折叠） |
| `/Platform/Page/WebPageRender?code=page-1789482473167` | 动态页面运行时（学生管理 CRUD，12 行） |
| `/Platform/Page/Demo/ActionHelper` | 动作助手 Demo（10 个入库动作） |
| `/Platform/Page/Demo/Combination` | 组合组件 Demo |
| `/Platform/Page/Demo/Grid` | 栅格布局 Demo |
| `/Platform/Page/Demo/Crud` | 免模型 CRUD Demo |
| `/Platform/Page/Demo/TemplateImport` | 模板导入 Demo |
| `/Platform/Page/Demo/CodeMirror` | CodeMirror 代码编辑 Demo |

## 四、组件双定义源（本版本核心）

详见 [docs/设计方案.md](docs/设计方案.md)。要点：

1. `_Layout.cshtml` 中 `@await Component.InvokeAsync("DynComRegistry")` 把 DB 启用组件清单序列化为 `<script type="application/json" id="dyncom-registry">`。
2. `Views/Shared/Components/{组件名}.cshtml` 用 `<!--TEMPLATE_START-->...<!--TEMPLATE_END-->`、`<!--SCRIPT_START-->...<!--SCRIPT_END-->`、`<!--STYLE_START-->...<!--STYLE_END-->` 三段标记定义组件；文件头用 `@* ComponentType: Page *@`、`@* RoutePath: /xxx *@`、`@* Description: ... *@` 声明元数据。
3. `RazorComponentRenderer`（IRazorViewEngine 渲染 .cshtml → 正则提取三段）+ `ComponentService.GetDefineByName`：**Razor View 代码优先，DB ComponentMeta 回退**；列表合并 = DB 行 + 扫描 `Views/Shared/Components/*.cshtml` 补全。
4. `GET /api/component/define/{name}` 返回 `{templateContent, scriptContent, styleContent}`；前端 `vue-loader.js` 的 `vueLoadCom` 用 `new Function` 解析 `export default`、注入 template/style、注册进 `DynCom`。
5. 样例组件：`Views/Shared/Components/RazorDemo.cshtml`（纯 Razor 定义，DB 中不存在，演示 View 回退）。

## 五、目录结构

```
E:\Tom\Tool\VueLibV4
├── VueLibV4.sln
├── README.md
├── docs\设计方案.md
└── src\VueLibV4.Web
    ├── Program.cs                     # 根路由 302、DI、启动种子
    ├── Core\                         # DbFactory / DynamicCrudService / Seeder / ApiResult
    ├── Controllers\ComponentController.cs   # GET /api/component/define/{name}
    ├── ViewComponents\DynComRegistryViewComponent.cs
    ├── Services\RazorComponentRenderer.cs / ComponentService.cs
    ├── Infrastructure\DynJavaScript.cs      # ExecJS / RenderDynActions
    ├── Dtos\ComponentDtos.cs
    ├── Areas\Platform\Controllers\    # 9 个平台控制器
    ├── Areas\Business\Controllers\    # DynData / BusinessDict
    ├── Views\
    │   ├── Shared\_Layout.cshtml            # 注册表 + RenderDynActions + 脚本链
    │   ├── Shared\Components\DynComRegistry\Default.cshtml
    │   ├── Shared\Components\RazorDemo.cshtml
    │   └── Platform\Page\*.cshtml / Platform\Demo\*.cshtml
    ├── wwwroot\dyn\                 # vue-loader / dyn-core / dyn-actionhelper / dyn-com / dyn-designer ...
    └── SeedData\                    # 01_PlatformDb.sql / 02_BusinessDb.sql / *.seed.json
```
