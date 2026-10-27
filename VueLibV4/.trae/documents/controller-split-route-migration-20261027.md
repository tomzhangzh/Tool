# 控制器职责拆分与路由迁移方案（2026-10-27）

## Context

当前三个控制器承担了过多职责，且路由与职责不匹配：

- `PageController`（约 600 行）：页面入口、运行时装配、弹窗、Block 装载、参数面板、检查器、权限矩阵、图表数据全部混在一起。
- `DynWebPageController`（约 413 行）：元数据 CRUD、运行时渲染、参数 schema、模板导出、AI 骨架生成。
- `ToolController`（约 587 行）：组件自检、页面组成图、数据库 ER 图、Markdown 预览。

用户已确认：**彻底改 URL（不留旧别名）**、**按职责建子目录**、**工具视图一起移到 Views/Tools**。

目标：一控制器一职责、路由可读、目录可导航；同步更新仓库内全部引用、Seeder 种子与当前文档；存量数据库通过一次性迁移 SQL 修正菜单 URL。

---

## 1. 目标结构与路由映射

> 命名空间统一保持 `VueLibV4.Web.Areas.Platform.Controllers`（仅文件进子目录），避免改动类型引用。

### 1.1 PageController 拆分（`Areas/Platform/Controllers/Page/`）

| 新控制器 | 文件 | 承接方法 | 新路由 |
|---|---|---|---|
| `PageShellController` | `Page/PageShellController.cs` | Desktop、Designer、WebPageRender、DynWebPage | `/Platform/Shell/Desktop`、`/Platform/Shell/Designer`、`/Platform/Shell/PageRender`、`/Platform/Shell/Runtime` |
| `PageDetailController` | `Page/PageDetailController.cs` | DetailModal、DetailModalV2、DynDetail | `/Platform/Detail/Modal`、`/Platform/Detail/ModalV2`、`/Platform/Detail/Form` |
| `PageBlockController` | `Page/PageBlockController.cs` | LoadBlock(GET/POST)、ParamsPanel、SaveParams、InspectorData | `/Platform/Block/Load`、`/Platform/Block/ParamsPanel`、`/Platform/Block/SaveParams`、`/Platform/Block/Inspector` |
| `PagePermissionController` | `Page/PagePermissionController.cs` | PermissionGrid | `/Platform/Permission/Grid` |
| `PageChartController` | `Page/PageChartController.cs` | EChartData | `/Platform/Chart/Data` |

共享私有方法 `BuildEffectiveParams / HasValue / GetInt / TryParseDict / LoadSetting`（原 L498-600）抽到 `Core/PageRenderSupport.cs`（`internal static`，服务依赖作参数传入），三个新控制器共用，避免复制。

视图仍留在 `Areas/Platform/Views/Page/`（本次只要求移动工具视图），返回路径继续用绝对路径。

### 1.2 DynWebPageController 拆分（`Areas/Platform/Controllers/WebPage/`）

统一新前缀 `api/platform/dynpage`：

| 新控制器 | 文件 | 承接方法 | 新路由 |
|---|---|---|---|
| `DynPageController` | `WebPage/DynPageController.cs` | All、Get、Save、Delete | `/api/platform/dynpage/all|get|save|delete` |
| `DynPageRenderController` | `WebPage/DynPageRenderController.cs` | Render、ParamSchema + `BuildSlotValues/ParseJsonArray/LoadSettingConfig` | `/api/platform/dynpage/render`、`/api/platform/dynpage/paramschema` |
| `DynPageTemplateController` | `WebPage/DynPageTemplateController.cs` | ExportTemplate、ExtSkeleton + `BuildTemplate/AppendComOption` | `/api/platform/dynpage/export-template`、`/api/platform/dynpage/ext-skeleton` |

共享读取逻辑（`BuildSlotValues` 等）随 `Render/ParamSchema` 保留在渲染控制器；若 `ExportTemplate` 也调用，则抽 `Core/DynPageConfigSupport.cs`。

### 1.3 ToolController 拆分（`Areas/Platform/Controllers/Tools/`）

| 新控制器 | 文件 | 承接方法 | 新路由 |
|---|---|---|---|
| `ComponentCheckController` | `Tools/ComponentCheckController.cs` | ComponentCheck | `/Platform/Tools/ComponentCheck` |
| `PageGraphController` | `Tools/PageGraphController.cs` | PageGraphDemo、PageGraphData + 页面图构建私有方法 | `/Platform/Tools/PageGraph`、`/Platform/Tools/PageGraphData` |
| `DbErGraphController` | `Tools/DbErGraphController.cs` | DbErGraph + ER 图私有方法 | `/Platform/Tools/DbErGraph` |
| `MarkdownViewerController` | `Tools/MarkdownViewerController.cs` | MdViewer | `/Platform/Tools/MdViewer` |

若 `Nid/Seq` 同时被页面图与 ER 图使用，抽到 `Core/GraphNodeId.cs`；否则随页面图控制器保留。

### 1.4 工具视图迁移

`Views/Page/{ComponentCheck,PageGraphDemo,DbErGraph,MdViewer}.cshtml` → `Areas/Platform/Views/Tools/`，控制器返回路径改为 `~/Areas/Platform/Views/Tools/Xxx.cshtml`。
`PageGraphDemo.cshtml` 内跳转/请求地址同步改（PageGraphData、DetailModalV2）。

---

## 2. 引用更新清单（必须全部处理）

按“新路由”全文替换，涉及约 50 个文件：

1. **页面入口**：`Program.cs`（`/` 重定向）、`_LayoutMac.cshtml`、`_LayoutCarDark.cshtml`、`Login.cshtml`（默认回跳）、`Desktop.cshtml`、所有 Demo 页返回桌面按钮、`DynTemplates/*`、`DynPages/Ext/*`、`dyn-action.js` 示例、`dyn-designer-dialog.js`。
2. **运行时装配**：`DynTemplates/FilterListOpenWindow|FilterListDrawerLeft|FilterListTabbedWindow|TabHost|CrudBasic|MasterDetailBasic|FilterListCrud`、`DynLayouts/*`（DynDetail/LoadBlock）、`DynBlocks/AppsV2/ListViewApp`。
3. **Block/参数**：`DynTemplates/Partials/InspectorPanel*.cshtml`、`DynPages/Ext/_WebPageParamsPanel.cshtml`、`ParamsPanelStandalone.cshtml`。
4. **DynWebPage API**：`Mgmt/DynWebPageMgmt.cshtml`、`Page/Designer/TopBar.cshtml`、`Page/WebPageRender.cshtml`、`DynPages/Ext/dynwebpage.ext.cshtml`、`ViewsAutoGenerate/Platform/DynWebPage/Detail_118.cshtml`。
5. **工具**：`SysMenuMgmt.cshtml`（OpenWindow 页面组成图）、`ViewsAutoGenerate/.../Detail_118.cshtml`、`PageGraphDemo.cshtml` 自身。
6. **注释/文档字符串**：`DynSharedModel.cs`、`DynPageExtService.cs`、`Seeder.cs` 注释、`docs/2026-10-27/02-API参考.md`、`03-使用手册.md`、根 `README.md`。

**不动**：历史文档（`docs/2026-10-14*`、`docs/20261014*` 等）、`docs/db-export/Platform.db.sql`（历史快照）。

---

## 3. 种子与存量数据迁移

1. 更新 `Core/Seeder.cs` 中所有 `/Platform/Page/...` 种子 URL（Designer、WebPageRender、DynWebPage、SysMenu 关联页）。
2. 新增 `src/VueLibV4.Web/Data/migrate-routes-20261027.sql`：
   - `UPDATE SysMenu SET Url=replace(...)` 修正 Designer/WebPageRender/DynWebPage/MdViewer/PageGraphDemo；
   - `UPDATE DynWebPage SET Url=replace(...)` 修正实例运行地址；
   - 幂等（仅替换旧前缀）。
3. 执行说明写入迁移脚本头注释：存量库必须执行一次，否则菜单 404。

---

## 4. 执行顺序

1. 抽 `Core/PageRenderSupport.cs`，迁移共享方法（不改行为）。
2. 建 `Page/` 下 5 个控制器，逐个搬方法并改路由；删除原 `PageController.cs`。
3. 建 `WebPage/` 下 3 个控制器；删除原 `DynWebPageController.cs`；按职责裁剪构造函数依赖。
4. 建 `Tools/` 下 4 个控制器；搬方法 + 移视图；删除原 `ToolController.cs`。
5. 全局替换引用（按第 2 节清单，逐文件 Edit）。
6. 更新 Seeder + 新增迁移 SQL。
7. 更新 `docs/2026-10-27/02`、`03` 与根 `README.md` 路由表。
8. 编译与冒烟验证。

> 建议 3 个 general-purpose 子代理并行完成第 2-4 步（互不重叠文件），第 5 步由单独一轮统一做，避免并发改同一视图。若采用串行方式，按 2→3→4→5 执行。

---

## 5. 验证

1. `dotnet build` 零错误（PowerShell 5，不用 `&&`）。
2. `dotnet run --project src/VueLibV4.Web/VueLibV4.Web.csproj`，逐项 `Invoke-WebRequest`：
   - `/Platform/Shell/Desktop`、`/Platform/Shell/Designer`、`/Platform/Shell/PageRender?code=student-manage`、`/Platform/Shell/Runtime?id=1`
   - `/Platform/Detail/ModalV2?id=118`、`/Platform/Detail/Form?settingId=..&table=..`
   - `/Platform/Block/Load?blockName=filter&webpageId=22`、`/Platform/Block/Inspector?webpageId=1`、`/Platform/Block/ParamsPanel?templateId=21&pageId=6`
   - `/Platform/Permission/Grid`、`/Platform/Chart/Data`
   - `/api/platform/dynpage/all`、`/api/platform/dynpage/render?id=1`、`/api/platform/dynpage/paramschema?templateId=21&pageId=6`
   - `/Platform/Tools/ComponentCheck`、`/Platform/Tools/PageGraph?id=34`、`/Platform/Tools/PageGraphData?id=34`、`/Platform/Tools/DbErGraph`、`/Platform/Tools/MdViewer?path=README.md`
3. 全局 grep 校验：`/Platform/Page/(Desktop|Designer|WebPageRender|DynWebPage|DetailModal|LoadBlock|InspectorData|SaveParams|ParamsPanel|EChartData|DynDetail|PermissionGrid|ComponentCheck|PageGraph|DbErGraph|MdViewer)` 在 `src` 中应为 0 命中（注释中的历史说明除外）。
4. 浏览器抽查：桌面 → 设计器 → 运行一个页面实例 → 打开 Block 弹窗 → 打开工具页。

---

## 6. 风险与对策

| 风险 | 对策 |
|---|---|
| 拆分时漏搬私有方法导致编译错误 | 先抽公共 helper，再搬 public action；每完成一个控制器立即 `dotnet build` |
| 视图绝对路径写错导致 500 | 工具视图迁移后统一用 `~/Areas/Platform/Views/Tools/...`；构建后逐页验证 |
| 存量菜单 404（已决定不留别名） | 提供幂等迁移 SQL，并在计划/文档里标注必须执行 |
| 多子代理并发改同一视图 | 引用替换集中到单独一轮；子代理只负责控制器与视图迁移 |
| 依赖裁剪导致 DI 缺失 | 每个新控制器只保留实际使用的服务，编译 + 冒烟双重校验 |
