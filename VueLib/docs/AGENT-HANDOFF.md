# AGENT-HANDOFF · VueLib 项目交接文档（给 AI 助理）

> **本文件是跨会话的唯一权威交接物**。新会话（尤其换新对话后）第一件事：完整读本文件 + `src\VueLib.Web\wwwroot\js\dyn-lib.md`，即可对齐项目全貌、约定、命令与当前状态，无需依赖旧会话记忆。
> 最后更新：2026-09-03

## 1. 项目是什么

**VueLib**：ASP.NET Core MVC（.NET 7）+ Vue 3（UMD 全量脚本，无构建步骤）+ SQL Server（SqlSugar ORM）的企业级低代码/动态 UI 平台。两条产品线：

```text
VueLib
├── 低代码设计器体系
│   ├── 可视化页面设计器（拖拽 → JSON 组件树 → 保存）
│   ├── 组件元数据 ComponentMeta + 组件实现（ComponentDefinitions / Razor View）
│   ├── 组件管理后台 ComponentManager（grid + filter + 分页）
│   ├── 桌面门户 Desktop（快捷方式/页面管理）
│   └── 预览 /designer/preview?code=xxx
└── 动态工程运行时体系（面向业务库的快速 CRUD）
    ├── DynProject（连接任意业务库）
    ├── DynPage（Summary 汇总屏 / Detail 细节屏）
    ├── DynTemplate（List / Home 等模板，参数可用设计器配置）
    ├── DynWebPage（route ↔ 模板）
    └── DynRunController（按定义动态查表渲染）
```

## 2. 关键路径（都在 `E:\Tom\Tool\VueLib` 下）

| 项 | 路径 |
|---|---|
| Web 项目 | `src\VueLib.Web`（net7.0，SqlSugarCore 5.1.4.217） |
| 核心运行时 | `src\VueLib.Web\wwwroot\js\dyn-lib.js`（**先读它旁的 dyn-lib.md**） |
| Demo 页 | `src\VueLib.Web\Views\DynDemo\Index.cshtml` + `Controllers\DynDemoController.cs` |
| 设计器 | `Views\Designer\` + `wwwroot\js\designer.js / designer.core.js / designer.utils.js / property-panel.js / html-code-generator.js` |
| 工程/模板 | `Controllers\DynProjectController.cs`、`Views\DynProject\`、`Views\DynRun\` |
| 组件 | `Controllers\ComponentController.cs`、`Areas\NutComponent`（移动）/ `ElementComponent`（桌面） |
| 文档 | 根 `README/QUICKSTART/DESIGN.md`、`docs\*.md`（SYSTEM-DESIGN / DYNAMIC-COMPONENT-LOADING / TEMPLATE-PARAMETRIC / USAGE） |

## 3. 构建 / 启动 / 重启（关键！）

```powershell
# 构建（服务运行时 exe 被锁，必须先停）
Get-Process -Name VueLib.Web -ErrorAction SilentlyContinue | Stop-Process -Force
cd E:\Tom\Tool\VueLib\src\VueLib.Web
dotnet build -v q --nologo

# 启动
Start-Process dotnet -ArgumentList "run --no-build --urls http://localhost:5000" -WindowStyle Hidden
# 或直接 dotnet run --urls http://localhost:5000

# 验证端口
netstat -ano | findstr :5000
```

- **改 cshtml / js 后必须重新 build + 重启**才生效（服务运行的是旧代码）。
- 浏览器验证入口：`http://localhost:5000/DynDemo?t=<随机数>`（t 仅缓存破坏）；设计器 `http://localhost:5000/designer`。
- csproj 已配置：`CopyRazorGenerateFilesToPublishDirectory=true` + `CopyComponentViews` Target（构建后复制 `Views\Shared\Components\**\*.cshtml` 到输出目录）——**.cshtml 不编进 dll**，靠运行时扫描。

## 4. 核心架构要点

- **无 Node 构建**：前端全走 UMD 全局变量，Razor 视图直接 `<script src>` 引用；改前端文件刷新即生效（配合 build 复制）。
- **dyn-lib.js**：属性驱动的声明式动作运行时（Vue3 UMD + jQuery + lodash + Element Plus），详见 `wwwroot\js\dyn-lib.md`。
- **设计器多 App**：toolbar / left / right / canvas / dialogs / breadcrumb 多个独立 Vue app，共享 `window.LCDesignerStore` + `eventBus` + `__lcApi`（跨 app 通信用事件总线，不用耦合调用）。

## 5. dyn-lib 动作系统（最近主线，务必对齐）

最新约定（**重写自 2026-09-03，dyn-lib.md 已同步**）：

- **唯一注册方式**：`window.dyn.actionHelper.xxx = function(ctx){...}`，挂方法即动作。**没有** `registerAction` / `registerInitAction` / `_init` 标记（已全部删除，别再用）。
- **事件由属性名决定**：`dyn-{event}-{action}`，如 `dyn-click-postdata`、`dyn-mouseover-updateel`。没有对应函数时委托静默忽略。
- **初始化由属性决定**：`dyn-init-{action}`（或旧式 `dyn-{action}-init`）在 init/initAll/innerHTML 更新后自动执行。
- **可选白名单**：`fn._events = ['click']` 只限事件委托；`fn._skip = true` 跳过辅助函数。
- **运行时新增动作**后调 `window.dyn.rebind()` 重新生成委托选择器。
- **内置动作**（全部在 actionHelper）：`postback / postdata / confirm-postdata / reload / open / close / updateel / evaljs / load / setVueModel`。
- **setVueModel**：`modelName`（点路径/数组下标 `items[0].name`）、`model`、`TargetEl`（指定容器）、`settimeout`；写入用 lodash `_.set`；Vue 3.5.41 用 `el.__dynProxy` 取 model（`app._instance` 是 false）。
- **evaljs**：裸 JS 字符串（`ElementPlus.ElMessageBox.alert("xxx")`），事件 + init 双用。
- **祖先工具**：`dyn.findAncestor / closestDynInit / closestDataUrl`。
- **ctx**：`{ element, el, event, $event, targetInfo, action, options, params, model, vm, url }`。

### 关键坑：属性名大小写
HTML 属性名被浏览器强制小写化（`dyn-click-setVueModel` → DOM `dyn-click-setvuemodel`），且部分环境 `querySelectorAll`/`hasAttribute` 大小写敏感。dyn-lib 已用 `name.toLowerCase()` 生成选择器 + `resolveAction` 大小写不敏感匹配处理。**新增动作/改 demo 时，HTML 属性名要与"动作名小写"一致**（如 `helloInit` → 属性写 `dyn-init-helloinit`，注意不是 `helloinite`）。

## 6. 历史功能现状（已实现/已撤销，改动以代码为准）

- **设计器**：组件树、拖拽（SortableJS 自研实现，非 vue-draggable）、遮罩层、标尺/缩放、电脑/手机模式、标签统一、组合组件（可开放容器/属性，开放属性用 `childrenctrls[i].xxx` 路径）、撤销重做、左侧/右侧折叠面板。
- **模板管理**：DynTemplate 支持定义模板参数（filter/grid/detail url 等，JSON 存），参数面板可用设计器配置；页面管理可选模板并配置参数；均含 grid + filter + 分页（Element UI table）。
- **已撤销**：Element 表格组件拖列、vue-draggable-plus 方案（改用 SortableJS）。
- 细节以 `docs\*.md` 和代码为准；不确定就问用户。

## 7. 数据库关键表（SQL Server，SqlSugar）

`ComponentDefinitions` / `ComponentMeta` / `PageSetting` / `DesktopShortcut` / `DesktopSolution` / `DynProject` / `DynPage` / `DynTemplate` / `DynWebPage`。icon 等中文/emoji 字段注意字符集（历史有过 `??` 显示问题）。

## 8. 用户偏好（本项目强相关，违背前先确认）

- **动作系统参考 common.js 的「挂方法即动作」**：极简、零注册零标记，事件/初始化全属性驱动；**拒绝** registerAction 式的显式注册和防御性过度设计。
- 保留 `dyn` 前缀；evalJS 用裸 JS 字符串；setVueModel 用 lodash 不自研。
- 每次改动要**生成 demo 验证**（DynDemo 页有 ①-⑧ 卡片演示，改动作系统后要回归）。
- 工程能力要求高：要可落地代码 + 注释，不要纯理论；全栈覆盖 .NET/C#、SQL Server、Vue、Element Plus / NutUI。
- 重视文档沉淀（本文件 + docs\* + dyn-lib.md 就是为此）；长会话后我会迟钝，靠文档重新对齐。

## 9. 当前状态 / 待办

- ✅ 已完：dyn-lib 动作系统全面重构为 actionHelper 约定式（2026-09-03），dyn-lib.md 已同步；DynDemo ⑦ setVueModel / ⑧ 约定式动作已验证。
- ⚠️ 待确认/未做：内置动作是否还要迁移更多到 actionHelper（当前已全迁）；设计器/模板管理最新细节以代码为准；如需继续迭代设计器功能，先读 `docs\DESIGN.md` / `docs\TEMPLATE-PARAMETRIC.md`。

## 10. 验证方式（改完必做）

1. 停进程 → `dotnet build -v q --nologo`（0 错误）→ 启动 → `netstat` 确认 5000 监听。
2. 浏览器打开 `http://localhost:5000/DynDemo?t=新随机数`，用 seed_browser_use 检查：
   - 事件动作：点击/触发 `dyn-click-xxx` 看 `.el-message` 或后端回显；
   - 初始化动作：看 `dyn-init-xxx` 元素文本/状态；
   - 回归：hello / helloInit / badge / setVueModel / dyn-init-load / reload / updateEl / evaljs。
3. 动态元素委托：动态插入带 `dyn-*` 属性的节点后触发，验证委托仍生效。
