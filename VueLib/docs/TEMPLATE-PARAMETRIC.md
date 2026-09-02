# VueLib 模板体系（参数化）设计文档

> 版本：2026-09-02 · 覆盖本次 DynTemplate 参数化重构（42 系列脚本）

## 一、设计目标

把「模板」从「固定三屏（Filter/Summary/Detail）+ 写死类型（List/Home）」重构为**参数化模板**：

- 可定义**很多种模板**（3 屏 List、手机 Home 九宫格、Custom 看板……），每种模板的参数完全不同；
- 每种模板有一个**统一页面**（真实 View 或动态渲染 View），由 `RenderView` 指定；
- 模板需要一个**参数设置**入口，参数定义（ParamSchema）本身通过管理界面的**可视化设计器**配置（key/label/控件类型/页面类型/子字段……）；
- **页面实例** = 选择模板 + 按该模板的 ParamSchema 填参 → 保存 → 真正运行。

## 二、数据模型

### DynTemplate（模板定义）

| 列 | 说明 |
|---|---|
| Id / ProjectId / Name / Code | 基础信息 |
| TemplateType | List（3 屏）/ Home（主页九宫格）/ Custom（自定义） |
| **RenderView**（新增） | 模板统一渲染视图名，如 `RouteList` / `RouteHome` / `RouteCustom`。运行时按此分派。兼容旧数据：未配置时按 TemplateType 推导 |
| **ParamSchema**（参数定义 JSON，重构为正式字段） | `DynTemplateParam[]`，描述模板需要哪些参数、每个参数用什么控件、是否必填、默认值、子字段 |
| Config | 模板默认配置 JSON（DynTemplateConfig：DetailOpenPath 等） |
| IsEnabled / SortOrder / Remark / CreatedAt / UpdatedAt | 常规 |

> 旧的 `FilterPageId / SummaryPageId / DetailPageId` 三列已弃用（保留以兼容历史数据），三屏 Id 改由页面实例的 Params 表达。

### ParamSchema 结构（DynTemplateParam）

```json
[
  { "key": "filterPageId",  "label": "筛选屏页面", "type": "pagePicker", "pageType": "Filter",  "required": false },
  { "key": "summaryPageId", "label": "汇总屏页面", "type": "pagePicker", "pageType": "Summary", "required": true  },
  { "key": "detailPageId",  "label": "细节屏页面", "type": "pagePicker", "pageType": "Detail",  "required": false }
]
```

| 字段 | 说明 |
|---|---|
| key | 参数键，存到页面实例 Params 的字段名 |
| label | 参数标签（参数表单/运行时展示用） |
| type | 控件类型：`pagePicker`（页面选择）/ `input` / `textarea` / `number` / `switch` / `select`（需 options）/ `gridItems`（九宫格数组，需 fields）/ `json` |
| pageType | 仅 `pagePicker` 用：按页面类型（Filter/Summary/Detail）过滤可选页面 |
| required / default | 必填 / 默认值 |
| options | 仅 `select`：`[{label,value}]` |
| fields | 仅 `gridItems`：`[{key,label}]` 子字段定义 |

### DynWebPage（页面实例 = 选择模板 + 填参数）

| 列 | 说明 |
|---|---|
| Id / ProjectId / Route / Name / Title | 基础信息 |
| TemplateId | 所选模板 |
| **Params**（新增） | 按模板 ParamSchema 填写的参数值 JSON，如 `{"summaryPageId":6,"filterPageId":17,"detailPageId":5}` |
| Config | 页面级覆盖配置（DynWebPageConfig） |
| IsHome / IsEnabled / SortOrder | 常规 |

## 三、运行时链路

`GET /DynRun/Route?projectId={id}&route={route}`

1. `FindWebPage(projectId, route)` 取页面实例 → `GetTemplate(TemplateId)` 取模板；
2. `EffectivePageIds(wp, template)`：**优先解析页面实例 Params 的 filterPageId/summaryPageId/detailPageId**，未配置时回退页面 Config 覆盖 / 模板旧三字段（兼容历史）；
3. 按 `RenderView` 分派：
   - `RouteHome` → `RouteHome.cshtml`：从 Params 读 `banner` + `gridItems`（九宫格入口）渲染；
   - `RouteList` → `RouteList.cshtml`：三屏组合（Filter 屏定义筛选、Summary 屏表格、Detail 屏弹出）；
   - 其它 → `RouteCustom.cshtml`（通用）：按 Params + ParamSchema 动态渲染参数卡片/九宫格/JSON，`pagePicker` 值自动转页面名。

## 四、管理 UI（DynProject/Index.cshtml）

- **模板管理 tab**：grid + filter（名称/编码关键字 + 类型下拉 + 查询/清除）+ 分页；编辑对话框内含**参数定义设计器**（可视化行编辑 key/label/控件类型/页面类型/必填 + 可切 JSON 模式）。
- **路由页面管理 tab**：grid + filter（路由/名称关键字 + 模板下拉 + 查询/清除）+ 分页 + 运行；编辑对话框内选模板后**按 ParamSchema 动态生成参数表单**（pagePicker=页面下拉、gridItems=可编辑表格、switch/number/select 等），保存为 Params JSON。

## 五、API

| 端点 | 说明 |
|---|---|
| `GET /api/dynproject/{id}/templates` | 模板列表 |
| `POST /api/dynproject/template/save` | 保存模板（含 RenderView/ParamSchema） |
| `DELETE /api/dynproject/template/{id}` | 删除模板 |
| `GET /api/dynproject/{id}/webpages` | 路由页面列表 |
| `POST /api/dynproject/webpage/save` | 保存页面实例（含 Params） |
| `DELETE /api/dynproject/webpage/{id}` | 删除页面实例 |
| `GET /api/dynproject/{id}/pages` | 工程页面列表（pagePicker 下拉数据源） |

## 六、Demo 数据（工程 2：CAD 图纸系统）

| 模板 | 类型 | RenderView | 参数 |
|---|---|---|---|
| drawing_list 图纸列表 | List | RouteList | filter/summary/detail 三个 pagePicker |
| component_list 部件列表 | List | RouteList | 同上 |
| reviewlog_list 审图记录 | List | RouteList | 同上 |
| home 主页 | Home | RouteHome | banner（input）+ gridItems（九宫格） |
| dashboard_demo 看板 | Custom | RouteCustom | title/refreshSeconds/showChart/color/linkPageId |

| 页面实例 | 模板 | Params 要点 |
|---|---|---|
| /drawings | drawing_list | 17/6/5 三屏 |
| /components | component_list | 20/10/9 |
| /reviewlogs | reviewlog_list | 8/7 |
| /home | home | banner + 九宫格 3 项 |
| /dashboard | dashboard_demo | 看板参数组合 |

## 七、数据库脚本

- `sql/42_TemplateParametric.sql`：加列（DynTemplate.RenderView / DynWebPage.Params）、填现有模板 ParamSchema、迁移三屏 Id 到 Params。
- `sql/42b_FixHomeEmoji.sql`：用 NCHAR 代理对修复 home 九宫格 emoji 图标（规避 GBK 编码）。
- `sql/42c_SeedCustomDemo.sql`：新增 Custom 看板模板 + /dashboard 页面实例 demo。

> 注意：本 SQL Server 不支持 `JSON_QUERY/JSON_VALUE`，JSON 解析全部在后端（Newtonsoft）完成。
