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





# VueLib V3 低代码平台设计文档

> 
> 项目：VueLib V3（基于原有 E:\Tom\Tool\VueLib 重构）
> 构建模式：UMD，不使用 Vite
> 技术栈：Vue3 + TailwindCSS (UMD) + ElementPlus + LayUI + Lodash + Vue-Validator + SqlSugar ORM
> 数据库：SQL Server（双库：Platform 平台库 / Business 业务库；备选 SQLite）
> 核心模块：dyn-lib 拆分为 `dyn-core`、`dyn-actionhelper`
> 文档版本：V1.0
> 目标：零代码标签驱动，DOM 指令`dyn-init`自动初始化 Vue 应用；dyn-xx 动作提交数据给后端 Controller 返回 View 局部渲染

## 目录

1. [项目概述](#1-%E9%A1%B9%E7%9B%AE%E6%A6%82%E8%BF%B0)
2. [核心需求清单](#2-%E6%A0%B8%E5%BF%83%E9%9C%80%E6%B1%82%E6%B8%85%E5%8D%95)
3. [已实现能力](#3-%E5%B7%B2%E5%AE%9E%E7%8E%B0%E8%83%BD%E5%8A%9B)
4. [待实现能力](#4-%E5%BE%85%E5%AE%9E%E7%8E%B0%E8%83%BD%E5%8A%9B)
5. [架构设计](#5-%E6%9E%B6%E6%9E%84%E8%AE%BE%E8%AE%A1)
6. [核心对象定义](#6-%E6%A0%B8%E5%BF%83%E5%AF%B9%E8%B1%A1%E5%AE%9A%E4%B9%89)
7. [工具方法与代码示例](#7-%E5%B7%A5%E5%85%B7%E6%96%B9%E6%B3%95%E4%B8%8E%E4%BB%A3%E7%A0%81%E7%A4%BA%E4%BE%8B)
8. [数据库设计](#8-%E6%95%B0%E6%8D%AE%E5%BA%93%E8%AE%BE%E8%AE%A1)
9. [目录结构约定](#9-%E7%9B%AE%E5%BD%95%E7%BB%93%E6%9E%84%E7%BA%A6%E5%AE%9A)
10. [风险与注意事项](#10-%E9%A3%8E%E9%99%A9%E4%B8%8E%E6%B3%A8%E6%84%8F%E4%BA%8B%E9%A1%B9)

## 1. 项目概述

基于旧版 VueLib 重构 V3，**UMD 打包**，可以直接浏览器引入 JS/CSS，不需要构建工具。
核心特性：

- 在普通 HTML 写指令 `dyn-init="{xx:1,b:2}"`，DOM 自动初始化 DynamicCommon 根容器
- 自定义动作指令，例如 `dyn-click-updateEl='{}'`，点击后加载页面并渲染到对应元素
- 后端使用 SqlSugar，**无需预先定义 Model**，自动读取表结构、主键、字段，支持 CRUD、分页排序
- 组件元数据 `ComponentMeta` 存储在数据库，前端读取渲染动态组件（DynNInput、DynSelect、DynDate 等）
- 表单校验使用 Vue-validator；组件分类 `[Category]=常用`

> 
> 通信约定：不使用 AJAX 单纯保存 /filter；通过`dyn-xx`动作提交数据到后端 Controller，后端返回 View 片段，渲染到指定 DOM 位置。

## 2. 核心需求清单

1. UMD 打包，Tailwind、ElementPlus、LayUI、lodash 全部以 UMD 方式引入
2. dyn-lib 拆分：dyn-core（底层核心、指令、查找工具）、dyn-actionhelper（动作处理器）
3. SqlSugar ORM，SQL Server，Platform/Business 双库；支持无 Model CRUD、分页、排序
4. 保留`ComponentMeta`，组件元数据来自数据库，表单组件分类 Category = 常用
5. 指令系统：`dyn-init` 初始化根容器；`dyn-click-updateEl` 点击局部加载页面
6. DOM 反向查找 API：
   - 根据 DOM 元素，查找**最近 DynamicCommon**（dyn-init 根容器实例）
   - 根据 DOM 元素，查找**顶层 DynamicCommon**
   - 根据 DOM 元素，查找**最近 dyn-com 动态组件实例 (DynNInput/DynSelect/DynDate)**
7. DynamicCommon 实例支持修改`config`、`model`，并且支持`forceRefresh()`强制刷新渲染
8. 动态组件挂载自身实例到 DOM，支持销毁清理，避免内存泄漏
9. Action 动作系统：dyn-actionhelper 处理点击等动作，拿到组件实例、上下文执行业务逻辑

## 3. 已实现能力

> 
> 指方案已经设计完成，代码骨架已经编写完成，可以直接落地开发

1. ✅ `dyn-init` 自定义指令：挂载时创建 DynamicCommon 实例，将实例挂载到 DOM 属性 `el.$dynCommon`
2. ✅ DynamicCommon 类设计：包含`config`、`model`、`_renderKey`，提供`updateModel` / `updateConfig` / `forceRefresh()`
3. ✅ DOM 向上查找工具函数
   - `findNearestDynamicCommon(el)`：查找最近 dyn-init 根容器实例
   - `findRootDynamicCommon(el)`：查找页面最顶层 dyn-init 根容器实例
4. ✅ 动态组件（DynNInput/DynSelect/DynDate）mounted 时挂载实例到 DOM `el.$dynComponent`，同时可选挂载`$dynComponentMeta`
5. ✅ `findNearestDynComponent(el)`：DOM 向上查找最近的 dyn-com 动态组件实例
6. ✅ VueCall 类集成上述全部查找 API，actionhelper 中可以直接调用
7. ✅ 实例销毁清理方案：onUnmounted 清空 DOM 上挂载实例引用，防止内存泄漏
8. ✅ 数据库方案：SqlSugar 自动读取表元信息，不需要实体 Model，自动识别主键，支持 CRUD 分页排序

## 4. 待实现能力

> 
> 设计完成，尚未编写代码，需要后续开发

1. ⏳ dyn-actionhelper 内置动作注册：`dyn-click-updateEl` 动作完整实现（加载页面，渲染到目标元素）
2. ⏳ 后端 Controller 接收 dyn 动作提交数据，返回 View 片段
3. ⏳ 数据库表：`ComponentMeta` 表结构、初始化脚本（Category 字段，常用组件分类）
4. ⏳ 渲染器完整实现：DynamicCommon.forceRefresh () 联动渲染器重新渲染 dyn 组件树
5. ⏳ UMD 打包配置（Rollup），打包 dyn-lib.js，包含 dyn-core + dyn-actionhelper
6. ⏳ 依赖的 UMD 资源引入封装：Tailwind、ElementPlus、LayUI、lodash、vue-validator
7. ⏳ 嵌套 dyn-init 边界处理：多层 dyn 容器隔离上下文
8. ⏳ 组件元数据加载：前端从 Platform 数据库读取 ComponentMeta，注册 dyn-com 组件
9. ⏳ SqlSugar 无 Model 封装层：统一封装 CRUD、分页、排序接口
10. ⏳ 表单校验集成 Vue-validator，绑定 dyn 表单组件

## 5. 架构设计

```
前端（UMD）
├─ dyn-lib.js
│  ├─ dyn-core
│  │   ├─ 自定义指令 dyn-init
│  │   ├─ DynamicCommon 根容器类
│  │   ├─ DOM查找工具函数
│  │   ├─ VueCall 对外入口类
│  │   └─ dyn-com 动态组件（DynNInput/DynSelect/DynDate）
│  └─ dyn-actionhelper
│      ├─ 动作管理器
│      ├─ dyn-click-updateEl 动作处理器
│      └─ 其它dyn-xx动作
└─ 第三方UMD依赖：Vue3、TailwindCSS、ElementPlus、LayUI、lodash、vue-validator

后端 .NET + SqlSugar
├─ Platform库：组件元数据 ComponentMeta、平台配置
└─ Business库：业务数据表，无实体Model，自动读取表结构
```

**数据流**

1. HTML 写`dyn-init`，dyn-core 指令初始化 DynamicCommon
2. DynamicCommon 读取 ComponentMeta，渲染 dyn-com 动态组件
3. 用户触发 dyn-click 动作，进入 dyn-actionhelper
4. Action 内部通过`VueCall`查找 DOM 对应的 DynamicCommon / DynComponent 实例
5. Action 提交数据给后端 Controller
6. 后端使用 SqlSugar 无 Model 操作业务表，返回 View 片段
7. 前端将返回 View 渲染到指定 DOM 元素

## 6. 核心对象定义

### 6.1 DynamicCommon（dyn-init 根容器实例）

```
class DynamicCommon {
  constructor(initConfig, rootEl) {
    this.el = rootEl;
    this.config = structuredClone(initConfig);
    this.model = {};
    this._renderKey = 0; // 强制刷新标记
  }

  /** 更新model，合并字段，自动刷新 */
  updateModel(newModel) {
    this.model = Object.assign({}, this.model, newModel);
    this.forceRefresh();
  }

  /** 更新config，合并配置，自动刷新 */
  updateConfig(newConfig) {
    this.config = Object.assign({}, this.config, newConfig);
    this.forceRefresh();
  }

  /** 只修改，不自动刷新 */
  setModel(newModel) {
    this.model = Object.assign({}, this.model, newModel);
  }
  setConfig(newConfig) {
    this.config = Object.assign({}, this.config, newConfig);
  }

  /** 强制触发渲染刷新 */
  forceRefresh() {
    this._renderKey += 1;
    this.renderer?.render(this.config, this.model, this._renderKey);
  }
}
```

### 6.2 VueCall 入口类（对外暴露所有工具）

```
class VueCall {
  // 查找最近DynamicCommon
  findNearestDynamicCommon(el) {
    return findDynamicCommon(el, false);
  }
  // 查找顶层DynamicCommon
  findRootDynamicCommon(el) {
    return findDynamicCommon(el, true);
  }
  // 查找最近dyn-com动态组件实例
  findNearestDynComponent(el) {
    if (!el) return null;
    let cur = el;
    while (cur) {
      if (cur.$dynComponent) {
        return cur.$dynComponent;
      }
      cur = cur.parentElement;
    }
    return null;
  }
}

/**
 * 底层查找DynamicCommon
 * @param {HTMLElement} el DOM元素
 * @param {boolean} findRoot true=找最顶层，false=最近
 * @returns {DynamicCommon|null}
 */
function findDynamicCommon(el, findRoot = false) {
  if (!el) return null;
  let cur = el;
  let rootCommon = null;
  while (cur) {
    if (cur.$dynCommon) {
      const common = cur.$dynCommon;
      if (!findRoot) {
        return common;
      }
      rootCommon = common;
    }
    cur = cur.parentElement;
  }
  return rootCommon;
}
```

### 6.3 dyn-init 指令

```
directives: {
  "dyn-init": {
    mounted(el, binding) {
      const common = new DynamicCommon(binding.value, el);
      el.$dynCommon = common;
    },
    unmounted(el) {
      el.$dynCommon = null;
    }
  }
}
```

### 6.4 dyn-com 动态组件（DynNInput/DynSelect/DynDate）挂载实例

```
// 在单个dyn-com组件的<script setup>
import { getCurrentInstance, onMounted, onUnmounted, ref } from "vue";
const rootEl = ref(null);
const props = defineProps(["meta"]);

onMounted(() => {
  const inst = getCurrentInstance();
  rootEl.value.$dynComponent = inst;
  rootEl.value.$dynComponentMeta = props.meta;
});

onUnmounted(() => {
  if (rootEl.value) {
    rootEl.value.$dynComponent = null;
    rootEl.value.$dynComponentMeta = null;
  }
});
```

## 7. 业务代码示例

### 7.1 HTML 使用示例

```
<!-- dyn-init 初始化根容器 -->
<div dyn-init="{xx:1,b:2}">
  <DynNInput></DynNInput>
  <button dyn-click-updateEl="{}">点击加载页面并渲染</button>
</div>
```

### 7.2 dyn-actionhelper 动作示例

```
// dyn-click动作处理器
function dynClickUpdateEl(event) {
  const targetEl = event.target;
  // 1. 获取当前动态组件实例（DynNInput等）
  const dynComp = vueCall.findNearestDynComponent(targetEl);
  // 2. 获取外层DynamicCommon根容器
  const common = vueCall.findNearestDynamicCommon(targetEl);
  if (!common) {
    console.warn("找不到dyn根容器");
    return;
  }

  // 修改model，自动刷新
  common.updateModel({
    name: "更新后的内容",
    timestamp: new Date()
  });

  // 提交数据给后端controller，获取View片段
  // const html = await fetchPost("/Controller/Action", common.model);
  // 将返回html渲染到目标元素
}
```

## 8. 数据库设计

### 8.1 数据库划分

- Platform（平台库）：存储 ComponentMeta 组件元数据、平台配置
- Business（业务库）：业务数据表，**不需要提前定义实体 Model**

> 
> SqlSugar 能力：自动查询表名、主键、字段列表，动态生成 CRUD、分页排序逻辑

### 8.2 ComponentMeta 表（Platform 库）

表格

| 字段 | 说明 |
| --- | --- |
| Id | 主键 |
| ComponentKey | 组件标识 DynNInput / DynSelect / DynDate |
| Category | 分类，常用组件值 = 常用 |
| ComponentName | 组件显示名称 |
| PropsMeta | JSON，组件属性元信息 |
| RenderTemplate | 渲染模板信息 |

> 
> 前端读取本表，动态注册 dyn-com 组件。

## 9. 目录结构约定

```
E:\Tom\Tool\VueLibV3
├─ src/
│  ├─ dyn-core/
│  │  ├─ directives/
│  │  │   └─ dyn-init.js
│  │  ├─ DynamicCommon.js
│  │  ├─ VueCall.js
│  │  └─ utils/
│  │      └─ domFinder.js // DOM查找工具
│  ├─ dyn-actionhelper/
│  │  ├─ actionRegister.js
│  │  └─ actions/
│  │      └─ dyn-click-updateEl.js
│  └─ components/
│     └─ dyn-com/
│        ├─ wrapper/
│        ├─ slot/
│        ├─ combine/
│        ├─ data/
│        ├─ form/ // DynNInput、DynSelect、DynDate
│        └─ editor/
├─ rollup.config.js // UMD打包配置
└─ index.html // 测试页面
```

## 10. 风险与注意事项

1. ❗禁止依赖 Vue 内部私有属性`__v`，全部使用自定义 DOM 属性`$dynCommon` / `$dynComponent`，UMD 生产环境稳定可控
2. ❗组件销毁必须清空 DOM 挂载实例引用，防止内存泄漏
3. ❗多层 dyn-init 嵌套时，`findNearest`返回最近容器；`findRoot`返回最顶层容器，上下文隔离
4. ❗`structuredClone`处理对象拷贝，避免外部直接修改内部 config/model 引用，产生副作用
5. ❗UMD 打包：所有第三方依赖采用 UMD 版本，不打包进 dyn-lib.js，外部 CDN 引入
6. ❗SqlSugar 无 Model 模式，注意 SQL 注入防护，必须参数化查询