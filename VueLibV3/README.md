# VueLibV3 — 企业级低代码平台（UMD · 免模型 · 动态组件）

基于旧版 `E:\Tom\Tool\VueLib` 从零重建的 V3 版本。**C# 后端（ASP.NET Core 8 + SqlSugar）+ 原生 JS 前端（Vue3 UMD，无需打包工具）**，双数据库（PlatformDb / BusinessDb）。字段类型严格化：**字符串列全部 `NVARCHAR`，非字符串列保留原生类型**（Id=BIGINT IDENTITY、IsActive=BIT、SortNo=INT、CreateTime=DATETIME2、Age=INT、Score/Credit=DECIMAL(18,2)）。所有页面由 **Razor View（.cshtml）经服务器返回**，不使用 wwwroot 静态 HTML；图标全部使用 **Emoji**。

## 快速启动

```bash
# 1. 编译（已通过：0 警告 0 错误）
dotnet build VueLibV3.sln

# 2. 运行（自动建库 + 灌种子，监听 http://localhost:5000）
dotnet run --project src/VueLibV3.Web/VueLibV3.Web.csproj
```

打开 `http://localhost:5000/` 即跳转桌面（双击图标进入各页面）。

> 数据库：SQL Server 本机 `localhost`，Windows 认证（`TrustServerCertificate=True`）。
> 启动时 `Seeder.Run()` 会自动创建 PlatformDb / BusinessDb、执行 `SeedData/*.sql`、导入全部 `*.seed.json` 种子。
> 页面均由 `Areas/Platform/Controllers/PageController.cs` 经 Razor View 返回（非静态 HTML）。

## 页面入口（Razor View 路由）

| 页面 | 地址 | 说明 |
|---|---|---|
| 桌面 | `/Platform/Page/Desktop` | 读取 DesktopSolution + DesktopShortcut，双击图标打开应用 |
| 页面设计器 | `/Platform/Page/Designer` | 拖拽设计 + 属性面板 + 结构树 + JSON + 模板导入导出 |
| 动态页面 | `/Platform/Page/WebPageRender?code=student-manage` | 运行时渲染 DynWebPage（模板即页面） |
| 动作助手 Demo | `/Platform/Page/Demo/ActionHelper` | 数据库中的 10 个动作执行/动作链/生成注册脚本 |
| 组合组件 Demo | `/Platform/Page/Demo/Combination` | 开放属性/容器/插槽的组合组件 |
| 栅格布局 Demo | `/Platform/Page/Demo/Grid` | `grid-template-columns: 1fr 1fr` + `col-span-2` |
| 免模型 CRUD Demo | `/Platform/Page/Demo/Crud` | 仅凭表名增删改查/分页/排序 |
| 模板导入 Demo | `/Platform/Page/Demo/TemplateImport` | ElementPlus Vue Template ⇄ DynCom JSON |
| JSON 编辑器 Demo | `/Platform/Page/Demo/CodeMirror` | DynCodeMirror 多模式编辑 |

## 核心 API（均位于各自 Area）

- 平台（`Areas/Platform`，`/api/platform/*`）：desktopsolution / desktopshortcut / dynproject / dyndict / componentmeta / dynactionhelper / dyntemplate / dynwebpage / dyncom
- 业务（`Areas/Business`，`/api/business/*`）：dyndata（免模型数据）/ dict（业务词典）
  - `GET /api/business/dyndata/page?table=Student&project=Business-School&page=1&size=20&filter=...`
  - `POST /api/business/dyndata/save` / `delete` / `search`
  - `GET /api/business/dyndata/meta?table=Student` — 表主键/字段信息（免模型基础）

## 前端模块（wwwroot/dyn，均由 dyn-lib.js 按序加载）

```
dyn-lib.js          依赖加载器：vue/element-plus/lodash/layui/tailwind/echarts/
                    codemirror/sortable + dyn 模块链，DynLib.ready() 就绪回调
dyn-validator.js    校验规则：required/email/phone/pattern/min/max/length/custom
dyn-actionhelper.js 动作助手：script/url/api/chain，全部动作入库、动态注册
dyn-core.js         渲染核心：DynRender 递归 VNode（设计时/运行时同一套）+ normalize/clean
dyn-template.js     模板导入(DOMParser→JSON) / 导出(JSON→Vue Template)
dyn-com.js          组件库：Dyn* 组件（容器/数据/展示）+ ElementPlus 全量支持
dyn-designer.js     设计器：组件面板/悬浮选中层/拖拽准入/属性面板/导入导出
```

## 第三方库（wwwroot/lib，均已内置）

`vue.global.prod.js` · `element-plus`（css/js/icons）· `lodash` · `layui` · `tailwind.css` · `sortable` · `echarts` · `codemirror` · `vue-draggable-plus`

## 设计要点

1. **设计器不包装 DOM**：选中/悬停/拖放事件直接落在组件根元素（`data-dyn-uid`），选中框为**绝对定位悬浮层**（`getBoundingClientRect` + `ResizeObserver` 定位），grid/flex 布局设计与运行时完全一致。所有 Dyn 组件 `inheritAttrs: true`，`data-dyn-uid` 透传到根元素；选中/拖拽统一使用 **reactive proxy 对象**（`push` 后从数组取回再 `select`），保证 uid 与渲染一致。**点击交互**：第一次点击选中组件，再点一次选中其**父容器**（`parentOf` + 悬浮层点击），结构树在**左侧**实时高亮联动（`el-tree` `highlight-current`）。
2. **Vue3 h() 事件键**：手写渲染函数的事件监听必须是 `onXxx`（click→`onClick`、update:modelValue→`onUpdate:modelValue`），`dyn-core.js` 已统一处理——comlisteners 事件与设计模式交互均正确绑定。
3. **标签宽度**：`labeloptions.labelWidth` 直传 `el-form-item` 的 `label-width`，在任何容器内（含 DynWrapper/GridContainer）均生效；属性面板基础组可编辑（如 `150px` / `30%`）。
4. **免 model CRUD**：`DynamicCrudService` 用 SqlSugar `DbMaintenance` 读取表主键与字段，`InsertableByObject/UpdateableByObject` 动态操作；**值按列 DataType 转类型**（字符串 NVARCHAR、整数/小数/日期/位分别转换），查询过滤同样按列类型转值。
5. **所有动作入库**：DynActionHelper 的脚本/url/api/chain 均存 DB，前端 `/all` 动态注册，`/script` 可生成注册脚本。
6. **模板即页面**：DynWebPage 选择 DynTemplate（存模板 **Code**）+ ConfigJson 即可运行（PageJson 为空时回退模板 JSON）。
7. **空值处理**：保存时空值（null/""/[]/{}）剔除（`DynCore.clean`），加载时补默认（`DynCore.normalize` + PropsMeta.default）。
8. **静态资源防缓存**：`_Layout.cshtml` 对 6 个 `dyn-*.js` 加 `asp-append-version="true"` 自动指纹，修改后刷新即生效。

## 已验证（浏览器逐页实测）

- 9 个 Razor View 页面全部零 console 错误：Desktop（10 个 Emoji 快捷方式）、Designer（组件库 46 组件 / 拖拽添加 / 属性面板 / 悬浮选中框 / 模板加载）、WebPageRender（学生 12 行 9 列）、Demo 6 页。
- 设计器交互链路：拖拽 ElInput → 画布渲染 → 点击选中 → 属性面板 14 项属性 + 悬浮框覆盖（不破坏 flex/grid 布局）→ 从模板创建 StudentManage 渲染成功。
- 数据库：PlatformDb 9 表 / BusinessDb 3 表，字段类型严格化（Id BIGINT IDENTITY、IsActive BIT、SortNo INT、CreateTime DATETIME2、Age INT、Score DECIMAL）。

详见 [docs/设计方案.md](docs/设计方案.md)。
