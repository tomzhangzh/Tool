# VueLib2 — .NET MVC + Razor + Vue3 动态组件低代码平台

全新重构版（VueLib 第二代）。核心思路：**组件 = Razor `<template>` + `<script tag='comconfig'>`，服务端渲染快照入库（SqlSugar + SQL Server），前端运行时/设计器共用同一渲染内核 `NDynamicCom`**。

## 快速开始

```bash
# 1. 前端第三方库（lodash / vue3 / element-plus / tailwind / axios / sortable）
cd frontend
npm install
npm run build:libs        # 拷贝 UMD 到 wwwroot/lib + 生成 wwwroot/css/tailwind.css

# 2. 后端（.NET 8 + SqlSugar + SQL Server，本地需有 SQL Server）
cd ..\src\VueLib.Web
dotnet run                # 首次启动自动建库（平台+业务）+ 组件快照入库 + demo 种子
```

打开 http://localhost:5198/ ：
- **🖥️ 桌面** `/desktop` —— 渐变桌面工作台（快捷方式图标、双击开窗口、任务栏、开始菜单按解决方案分组、窗口拖拽/缩放/记忆位置、右键菜单）
- **设计器** `/designer` —— left/center/right 三栏（组件库 / 画布 / 属性面板），画布复用运行时 NDynamicCom 内核，design 模式包选中/锁定/拖拽装饰
- **组件管理** `/component-manage` —— 23 个组件列表/搜索/分类筛选/快照查看/删除/一键刷新扫描 Razor
- **演示页面** `/#/page/demo-*` —— form/grid3/window/fragment/flex/composite/actions 等 8 个 demo

## 架构

```
src\VueLib.Web
├── Models\        PlatformModels(组件/页面)  BusinessModels(字典/客户/商品)  DesktopModels(桌面)
├── Areas\ElementComponent\Views\   21 个 Razor 组件（Common/FormItem/Container + _ElementLayout 公共内核）
├── Services\
│   ├── ViewRenderService.cs        Razor 视图 → 字符串（组件快照渲染）
│   ├── ComponentRegistryService.cs 内容比对 → 快照入库（版本号自增）；Resolve/GetMeta
│   ├── OptionsService.cs           下拉 static/dict/sql/ajax（SQL 白名单）
│   └── DemoPageFactory.cs          demo 页面 + 组合组件种子（程序化节点构建器）
├── Controllers\   LowCodeController(组件/页面/options)  DemoController(业务 demo)
│                  DesktopController(桌面+API)  ComponentManageController(组件管理)
├── TagHelpers\    <lc-page code=""/> / <lc-component name=""/> 嵌入任意 Razor 页面
└── wwwroot\
    ├── lib\       第三方 UMD（npm 拷贝）
    ├── css\       vue-lib.css（.lc-* 专用类）+ tailwind.css
    └── js\vuelib\ 前端模块（加载顺序即 _Layout 引用顺序）
        config.js              全部可调常量集中头部（组件加载模式/缓存/调试开关）
        core\utils.js          deepMerge/resolvePlaceholders/getByPath(lodash)
        core\eventbus.js       on/off/emit
        core\dyn-init.js       开放 model 属性 + 嵌套隔离（maskNested / dyn.mount）
        debug\debug.js         window.__DYN_DEBUG 开关 + [DynChain] 结构化日志 + dyn-debug 属性
        actions\registry.js    动作链解析（兼容裸数组与 {steps:[...]} 引用）
        actions\builtin.js     setVar/delay/notify/triggerEvent/collect/postback/updateEl/openwindow/closewindow/reload/evaljs
        services\options-service.js  下拉三源（dict/sql/ajax）解析
        services\component-service.js 异步组件注册 + resolveVersion + 组合组件构建
        services\page-service.js     页面加载/保存
        runtime\ndynamic-com.js 唯一渲染内核 NDynamicCom（LCNode/LcNode）
        runtime\nut-runtime.js 运行时入口（单根 App + bootPage/嵌入）
        designer\designer.js   三栏设计器（组件库/画布/属性面板/树/JSON/Model/组合封装）
        vue-lib.js             汇总加载与启动
```

## 数据库（双库，SqlSugar 双租户）

- **VueLib2_Platform**（平台库）：Components（组件快照+版本）、Pages（页面 JSON）、DesktopSolutions / DesktopShortcuts（桌面）
- **VueLib2_Business**（业务库）：DictTypes/DictItems（字典）、Products（商品）、Customers（客户）

连接串在 `appsettings.json`：`Server=localhost;Database=...;Trusted_Connection=True;TrustServerCertificate=True`，ConfigId `platform` / `business`。

## 关键机制

- **动态组件**：Razor `<template>` + `<script tag='comconfig'>` 服务端渲染快照入库；前端 defineAsyncComponent 懒加载 `?name=&version=`，DOMParser 取 template + script，new Function 注入 Vue/`_`/axios/VueLib/ElementPlus
- **运行时/设计器同内核**：NDynamicCom 单一渲染内核；design=true 时外层包 `.lc-node`（选中框/工具栏/拖拽/锁定/拖放区），不重写渲染逻辑
- **版本机制（静默）**：磁盘 Razor 变更自动升版本快照；UI 不再展示版本锁定（设计器/组件库隐藏版本号），组件管理页可查看快照历史
- **动作链**：`dyn-click-chain` / `dyn-load-chain` 声明式 JSON 步骤；`{path}` 占位符；`window.__DYN_DEBUG` 结构化日志
- **组合组件**：画布选择子树 → "封装组合组件" → 开放属性/开放插槽，DB 存 CompositeConfigJson
- **Flexbox 生成器**：容器组件属性面板可视化生成 Tailwind class
- **窗口组件**：Element Dialog + updateEl 注入 URL 内容（/api/demo/window-content）

## 注意事项

- 改前端 js/css 后：手动 bump `Views\Shared\_Layout.cshtml` 顶部 `var V`（静态资源版本号）
- 改组件 cshtml 后：`POST /api/lowcode/component/refresh` 升版本；或重启应用自动扫描
- Razor 中 `@click` 需写 `@@click`；C# raw string 与 JS `${}` 冲突时用 StringBuilder
- cshtml 一律 UTF-8 BOM 写入，避免中文乱码
- 组件管理 URL 走特性路由（`/component-manage` 小写），MVC 默认路由区分大小写
