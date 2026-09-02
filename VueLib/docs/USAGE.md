# VueLib 低代码设计器 —— 使用说明

> 面向后续维护者/开发者的操作指南。运行与开发环境：Windows + .NET 7 + Vue 3（UMD）。

## 1. 快速开始

```powershell
cd E:\Tom\Tool\VueLib\src\VueLib.Web
dotnet run          # 或 dotnet run --no-build（改 js/cshtml 后）
```

浏览器访问：`http://localhost:5000/designer`（设计器主页）、`http://localhost:5000/designer/preview?code=xxx`（预览页）。

**改代码后重启**：改 js/cshtml → 停 `VueLib.Web` 进程 → `dotnet build` → `dotnet run --no-build`。

```powershell
Get-Process -Name VueLib.Web | Stop-Process -Force
cd E:\Tom\Tool\VueLib\src\VueLib.Web
dotnet build -v q --nologo
dotnet run --no-build
```

## 2. 设计器界面分区

| 区域 | 容器/App | 功能 |
|---|---|---|
| 顶部工具栏 | `#lc-toolbar` | 选择页面、新建、保存、开放配置开关、存组合、手机/电脑切换、画布宽高(电脑)、标尺、缩放、设计/预览、JSON、查看数据、新窗口预览 |
| 左侧面板 | `#lc-left` | 「组件库」tab：按分类(表单/布局/展示/通用/组合)拖拽组件；「组件树」tab：树状结构、过滤、点击选中；可折叠(docked)/关闭 |
| 中间画布 | 主 app `#designer-app` | 手机/电脑外观渲染、标尺、缩放；点击选中、内部拖拽排序/嵌套、从左侧拖入 |
| 右侧面板 | `#lc-right` | 选中组件属性配置(DynamicPropertyPanel)、开放属性/容器标记、验证器、上移/下移/复制/删除；可折叠/关闭 |
| 底部面包屑 | `#lc-breadcrumb` | 当前选中组件路径链 |
| 弹窗组 | `#lc-dialogs` | 页面 JSON 编辑、新建页面、Model 数据查看、保存为组合组件 |

## 3. 核心操作流程

### 3.1 设计一个页面
1. 顶部选择已有页面，或「新建」输入页面名称/编码；
2. 从左侧「组件库」拖组件到画布容器（表单/栅格/单元格组等可嵌套）；
3. 点选画布组件，在右侧属性面板配置绑定字段、标签、占位符、验证器；
4. 点「保存」持久化（POST `/api/lowcode/page`）；
5. 「新窗口预览」查看运行效果。

### 3.2 组合组件（复用/封装）
1. 顶部开「开放配置」开关；
2. 画布中选中要封装的子树；
3. 属性面板勾选需要外部开放的属性（标签/绑定字段/placeholder 等），对容器勾选「开放此容器」；
4. 点「存组合」→ 弹窗中确认开放属性 key/显示名、开放容器插槽 key →「保存组合组件」；
5. 保存后出现在左侧「组合」分类，可拖入新页面；**组合组件只暴露开放项，内部节点锁定不可选**。

### 3.3 面板折叠/浮动
- 左侧/右侧面板头部 `◀/▶` 吸附成窄条，窄条上仍有 📦/⚙️ 入口；`✕` 完全关闭，页面边缘留恢复按钮；
- 左侧折叠后出现「🧩」悬浮按钮，点击弹出浮动组件库，可直接拖组件到画布。

## 4. 代码结构速查

```
src/VueLib.Web/
├── Views/Designer/
│   ├── Index.cshtml            # 骨架：lc-shell 布局 + 6 个 partial + 脚本引用
│   ├── _TopToolbar.cshtml      # #lc-toolbar 独立 app
│   ├── _LeftPanel.cshtml       # #lc-left 独立 app（组件库/树/浮动菜单）
│   ├── _CenterCanvas.cshtml    # 主 app 内画布模板
│   ├── _RightPanel.cshtml      # #lc-right 独立 app
│   ├── _Dialogs.cshtml         # #lc-dialogs 独立 app
│   ├── _Breadcrumb.cshtml      # #lc-breadcrumb 独立 app
│   └── Preview.cshtml          # 预览页
├── wwwroot/js/
│   ├── dyn-lib.js              # dyn-init 框架（eventBus/工具/mountCore）
│   ├── designer.core.js        # 共享 store + __lcApi
│   ├── designer.utils.js       # 工具 + dragState
│   ├── designer.js             # 主 app 内核（画布 + 业务）
│   ├── property-panel.js       # 动态属性面板组件
│   └── preview.js              # 预览逻辑
├── wwwroot/css/designer.css    # .lc-shell/.lc-main 布局等
└── docs/                       # 本目录：DESIGN.md（架构）/ USAGE.md（本说明）
```

## 5. 如何新增/修改面板

### 5.1 新增一个独立面板 app
1. 新建 `_MyPanel.cshtml`，最外层容器加 `dyn-init="{}"`（如 `<div id="lc-mypanel" dyn-init="{}">`）；
2. 模板里写 Vue 语法，**事件用 `@@click`**（Razor 转义）；
3. 容器末尾写 `<script tag="dynconfig"> var dynConfig = { setup(){ return {...} }, components: {...} }; </script>`；
4. 在 `Index.cshtml` 的 `.lc-shell` 内合适位置 `@await Html.PartialAsync("_MyPanel")`；
5. 刷新页面即自动被 `dyn.initAll()` 挂载。

### 5.2 新增共享状态
在 `designer.core.js` 的 `LCDesignerStore` 对象中加字段（`Vue.ref/reactive/computed`）。主 app 与各面板用 `const S = window.LCDesignerStore` 后 `S.xxx` 访问。

### 5.3 新增公共方法
在主 app `designer.js` setup 末尾的 `Object.assign(window.__lcApi, {...})` 中导出；面板通过 `window.__lcApi.xxx` 值引用调用。

### 5.4 监听事件
`dyn.eventBus.on('setcurrent', cb)` / `off` / `emit`。事件名见 DESIGN.md §5.3。

## 6. 常见问题排查（FAQ）

| 现象 | 原因 / 处理 |
|---|---|
| 全页显示 `{{ xxx }}` 未解析、所有面板空白 | dyn-init 未挂载。检查：①jQuery 是否在 dyn-lib.js 之前加载；②`designer.js` mount 是否抛错（看 console 或 `window.__mountErr`）；③`window.LCDesignerCore` 是否在使用前已初始化 |
| 只有某面板是 `{{ }}` | 该 partial 的 dynconfig script 语法错误或 setup 抛错，逐个 app 排查 console |
| 点组件右侧面板/面包屑不联动 | `currentCom` 是否写入 store；面板是否仍引用旧的本地状态 |
| 从左侧拖不动/拖了不插入 | dragState 链路：left `onPaletteDragStart` 是否写 `dragState`；画布 drop 是否读到 |
| 面板布局错乱/被遮挡 | 检查是否仍依赖旧 `#designer-app` 100vh 布局；确认 `.lc-shell/.lc-main` 存在 |
| 拖动后组件树顺序不对 | 画布内部排序由 Sortable `onEnd` 同步 `childrenctrls` 数组；看 console `[Drag] onEnd` 日志 |
| Razor 编译报错 `@click` | partial 里必须写 `@@click`（Razor 转义） |

## 7. 验证建议

每次改动后按 DESIGN.md §8 清单验证：build 通过 → 浏览器无 JS 错误 → 跨 app 联动（选中/拖拽/弹窗）正常。
