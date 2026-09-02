# VueLib 快速上手指南

> 面向第一次接触 VueLib 的开发者的最短路径：环境 → 启动 → 数据库 → 访问 → 加一个组件。
> 详细架构见 `docs/SYSTEM-DESIGN.md`，动态组件加载机制见 `docs/DYNAMIC-COMPONENT-LOADING.md`。

## 1. VueLib 是什么

VueLib 是一套 **ASP.NET Core MVC + Vue3(UMD) + SQL Server** 的低代码平台，核心能力：

- **可视化页面设计器**：拖拽组件搭页面，配置绑定字段/标签/验证器/布局，保存为 JSON 组件树；
- **动态组件加载系统**：组件以"模板 + 脚本"形式存于数据库（或 Razor View），前端运行时按需异步加载渲染；
- **双 UI 库**：移动端 NutUI（`DynN*` 前缀）、桌面端 Element Plus（`DynEl*` 前缀）；
- **动态工程运行时**：连接任意业务库，用"汇总屏/细节屏/模板"动态生成 CRUD 页面与路由；
- **桌面门户**：快捷方式/解决方案管理，窗口化打开页面。

## 2. 环境要求

| 依赖 | 版本 | 说明 |
|---|---|---|
| .NET SDK | 7.0+ | 后端运行时 |
| SQL Server | 2016+ | 数据库（本地实例 `Server=.`） |
| 浏览器 | 现代浏览器 | 设计器需要 ES2020+ 语法支持 |

无需 Node.js / npm——前端全部使用 UMD 全局脚本（Vue 3 运行时 + CDN 式本地库）。

## 3. 启动步骤

```powershell
# 1) 进入 Web 项目
cd E:\Tom\Tool\VueLib\src\VueLib.Web

# 2) 编译并运行
dotnet run
```

访问入口：

| 地址 | 说明 |
|---|---|
| `http://localhost:5000/` | 首页（桌面门户） |
| `http://localhost:5000/designer` | 低代码页面设计器 |
| `http://localhost:5000/designer/preview?code=xxx` | 页面独立预览（code 为页面编码） |
| `http://localhost:5000/ComponentManager` | 组件管理后台（grid + filter + 分页） |
| `http://localhost:5000/Desktop` | 桌面门户/快捷方式 |
| `http://localhost:5000/DynProject` | 动态工程管理（模板/路由页面） |

> 改 js/cshtml 后：停止进程 → `dotnet build -v q --nologo` → `dotnet run --no-build`。
> 停止进程：`Get-Process -Name VueLib.Web | Stop-Process -Force`。

## 4. 数据库初始化

连接串在 `src\VueLib.Web\appsettings.json`：

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=VueLib;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

首次搭建需按顺序执行 `src\VueLib.Web\sql\` 下的脚本（PowerShell + `sqlcmd` 或 SSMS 均可）：

```powershell
$db = "VueLib"
# 按编号顺序执行（01 → 41），例如：
Get-ChildItem "E:\Tom\Tool\VueLib\src\VueLib.Web\sql\*.sql" |
  Sort-Object { [int]($_.BaseName -split '_')[0] } |
  ForEach-Object { sqlcmd -S . -d $db -E -i $_.FullName }
```

脚本按模块分块：

- `01-04`：基础表 + 低代码核心表（`ComponentDefinitions` / `PageSetting` / `ComponentMeta`）
- `05-12`：移动端组件、页面种子数据、属性配置、组合组件、扩展字段
- `13-19`：桌面表、Element UI 组件（`DynEl*` 前缀）、Label 属性
- `20+`：动态工程（`DynProject`/`DynPage`/`DynTemplate`/`DynWebPage`）、设计扩展、Demo 页面

> 已有库时只需执行缺失的增量脚本；脚本大多可重复执行（`IF OBJECT_ID ... DROP` 重建）。

## 5. 目录结构

```
src\VueLib.Web\
├── Program.cs                    # 服务注册 + 中间件管道 + 路由
├── appsettings.json              # 连接串 / 日志
├── Controllers/
│   ├── LowCodeController.cs      # 设计器 API：组件元数据 + 页面 CRUD + 分页
│   ├── ComponentController.cs    # 组件定义动态加载 API（list/define/defines/pages）
│   ├── DesignerController.cs     # 设计器 / 预览页面
│   ├── ComponentManagerController.cs  # 组件管理后台
│   ├── DesktopController.cs      # 桌面快捷方式 / 解决方案 API
│   ├── DynProjectController.cs   # 动态工程 / 模板 / 路由页面 API
│   └── DynRunController.cs       # 动态运行时：汇总屏/细节屏/路由渲染
├── Models/                       # SqlSugar 实体（PageSetting/ComponentMeta/Desktop*/Dyn*）
├── Dtos/                         # API 传输对象
├── Services/                     # 业务服务（ComponentService/LowCodeServices/Dyn*）
├── Data/AppDbContext.cs          # SqlSugar 客户端工厂（每次请求新实例）
├── Areas/
│   ├── NutComponent/             # NutUI 移动组件视图（FormItem/Container/Display/Common/Mobile）
│   └── ElementComponent/         # Element Plus 桌面组件视图
├── Views/
│   ├── Designer/                 # 设计器（Index + 6 个 partial）+ Preview 预览
│   ├── ComponentManager/         # 组件管理
│   ├── Desktop/                  # 桌面门户 + 快捷方式/页面/方案管理
│   ├── DynProject/ DynRun/       # 工程管理与动态运行时视图
│   └── Shared/Components/        # Razor 组件回退目录（RazorDemo 等）
├── wwwroot/
│   ├── js/                       # vue.global / nutui / element-plus / dyn-lib / designer.* / preview.js 等
│   ├── css/                      # 样式
│   └── lib/                      # sortable / ruler / jquery 等
├── sql/                          # 数据库脚本（01-41）
└── docs/                         # 本目录：设计/使用/系统文档
```

## 6. 加一个组件（最快路径）

以移动端 NutUI 组件为例：

**① 新建组件视图** `Areas\NutComponent\Views\FormItem\MyInput.cshtml`：

```cshtml
@{
    Layout = "~/Areas/NutComponent/Views/_FormItemLayout.cshtml";
}

<nut-input v-model="modelinfo"
           v-bind="props.jsonconfig.options.comoptions"
           v-on="getComlisteners()">
</nut-input>
```

`_FormItemLayout` 会自动注入：`jsonconfig`（节点配置）、`modelinfo`（模型绑定）、`validate()`（验证器）、`getComlisteners()`（事件）。容器组件用 `_NutLayout.cshtml` 布局。

**② 注册到 `ComponentMeta` 表**（设计器可拖拽）：

```sql
INSERT INTO ComponentMeta (ComponentName, ComponentType, Category, Label, Icon,
                           DefaultConfigJson, LoadUrl, UiLibrary, IsEnabled, SortOrder)
VALUES ('DynNMyInput', 1, '表单', '我的输入框', '🔤',
        '{"component":"DynNMyInput","modelname":"myValue"}',
        '/NutComponent/FormItem/MyInput', 'nutui', 1, 999);
```

`ComponentType`：1=表单项、2=容器、3=展示、4=通用。`LoadUrl` 指向组件视图地址，前端 `nutLoadCom` 异步拉取。

**③ 重启 / 刷新**：设计器左侧「表单」分类即出现新组件，可拖入画布、配置、保存、预览。

## 7. 加一个页面（最快路径）

1. 打开 `/designer`，顶部工具栏点「新建」，填页面名称/编码（如 `demo-page`）；
2. 从左侧拖组件到画布容器（表单/栅格/单元格组可嵌套），点选组件在右侧配属性；
3. 点「保存」（POST `/api/lowcode/page`）；
4. 访问 `http://localhost:5000/designer/preview?code=demo-page` 预览。

## 8. 常见问题

| 现象 | 处理 |
|---|---|
| 全页 `{{ xxx }}` 不解析 | 查看浏览器 console；确认 `dyn-lib.js` 在 `jquery.min.js` 之后加载；`designer.js` 是否抛错（`window.__mountErr`） |
| 组件加载失败 | 确认 `ComponentMeta.LoadUrl` 路径正确、Area 路由可达（`/NutComponent/FormItem/xxx`） |
| 组件名冲突 | 统一用 `DynN*`（NutUI）/ `DynEl*`（Element）前缀，避免与 UI 库全局组件冲突 |
| 设计器面板不联动 | 确认 `designer.core.js` 的共享 store（`window.LCDesignerStore`）正常挂载 |
| 页面保存后 JSON 错 | 画布中检查组件树节点是否含 `component` 字段；`childrenctrls` 是否顺序正确 |

## 9. 下一步

- 理解组件如何被动态加载 → `docs/DYNAMIC-COMPONENT-LOADING.md`
- 理解整体架构、数据库、运行时 → `docs/SYSTEM-DESIGN.md`
- 理解设计器多 App 拆分与通信 → `docs/DESIGN.md`
