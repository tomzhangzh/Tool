# VueLib 快速上手指南

## 环境要求

- .NET 7.0 SDK
- SQL Server（本地或远程）
- 现代浏览器（Chrome / Edge / Firefox）

## 5 分钟快速启动

### 1. 克隆项目

```bash
git clone <repository-url>
cd VueLib
```

### 2. 创建数据库

在 SQL Server 中创建数据库 `VueLib`，然后依次执行 SQL 脚本：

```bash
# 使用 sqlcmd 命令行
sqlcmd -S . -i src/VueLib.Web/sql/01_InitDatabase.sql
sqlcmd -S . -i src/VueLib.Web/sql/02_SeedData.sql
sqlcmd -S . -i src/VueLib.Web/sql/03_LowCodeTables.sql
sqlcmd -S . -i src/VueLib.Web/sql/04_LowCodeSeedData.sql
sqlcmd -S . -i src/VueLib.Web/sql/11_ExtendedFieldsMigration.sql
sqlcmd -S . -i src/VueLib.Web/sql/13_DesktopTables.sql
sqlcmd -S . -i src/VueLib.Web/sql/14_ElementUIComponents_Full.sql
sqlcmd -S . -i src/VueLib.Web/sql/14b_ElementUIComponents_Part2.sql
```

或在 SSMS 中依次打开执行。

### 3. 配置连接字符串

编辑 `src/VueLib.Web/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VueLib;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### 4. 运行项目

```bash
cd src/VueLib.Web
dotnet run
```

控制台输出监听地址（如 `https://localhost:5001`）。

### 5. 访问页面

| 页面 | 地址 | 说明 |
|------|------|------|
| 桌面 | `https://localhost:5001/` | Windows 风格桌面主页 |
| 设计器 | `https://localhost:5001/designer` | 低代码可视化设计器 |
| 组件管理 | `https://localhost:5001/ComponentManager` | 组件元数据管理 |
| 页面预览 | `https://localhost:5001/designer/preview?code=user-register` | 独立预览页 |

## 设计器使用速查

### 基本操作

1. **选择页面**：顶部下拉选择已有页面，或点击"新建"
2. **添加组件**：左侧面板点击组件名，添加到当前选中容器
3. **选中组件**：点击画布中的组件（或组件树中的节点）
4. **编辑属性**：右侧属性面板修改配置
5. **保存**：顶部"保存"按钮
6. **预览**：顶部"预览"按钮在新窗口打开

### UI 库切换

左侧组件面板顶部有 UI 库筛选下拉：
- **全部**：显示 NutUI + ElementUI 所有组件
- **NutUI**：仅显示移动端组件（N 前缀）
- **ElementUI**：仅显示 PC 端组件（El 前缀）

### 组件配置核心字段

| 字段 | 说明 | 示例 |
|------|------|------|
| `component` | 组件注册名 | `DynElInput` / `DynNInput` |
| `modelname` | 数据绑定路径 | `user.name`（支持嵌套） |
| `options.comoptions` | 组件 props | `{ "placeholder": "请输入" }` |
| `options.labeloptions` | 标签配置 | `{ "label": "用户名", "required": true }` |
| `validators` | 验证规则 | `[{ "type": "required", "message": "必填" }]` |
| `childrenctrls` | 子组件数组 | 容器组件递归渲染 |

## 常见问题

### Q: 运行时报数据库连接错误？
A: 确认 SQL Server 已启动，连接字符串中的 Server 地址正确，Windows 身份验证可用。

### Q: 设计器中组件列表为空？
A: 检查浏览器控制台是否有 `/api/lowcode/components` 请求失败，确认 ComponentMeta 表有数据。

### Q: 组件显示 404 错误？
A: 确认对应 Controller Action 和 View 文件存在。NutUI 在 `Areas/NutComponent/`，ElementUI 在 `Areas/ElementComponent/`。

### Q: Maximum call stack size exceeded？
A: 检查页面配置 JSON 是否有循环引用。NDynamicCom 已添加 20 层深度限制，超过会显示错误提示。

### Q: 编译时文件被锁定？
A: 停止正在运行的 `dotnet run` 进程（Ctrl+C），再重新编译。

### Q: Razor 中 @ 符号报错？
A: Razor 中 `@` 是特殊字符，需要转义为 `@@`（如 `@@click`、`@@keyframes`、`@@media`）。

## 项目结构速览

```
VueLib/
├── src/VueLib.Web/
│   ├── Areas/
│   │   ├── NutComponent/          # NutUI 移动端组件
│   │   │   ├── Controllers/
│   │   │   └── Views/
│   │   │       ├── _NutLayout.cshtml
│   │   │       ├── FormItem/
│   │   │       └── ...
│   │   └── ElementComponent/      # ElementUI PC端组件
│   │       ├── Controllers/
│   │       │   ├── FormItemController.cs
│   │       │   ├── CommonController.cs
│   │       │   └── ContainerController.cs
│   │       └── Views/
│   │           ├── _ElementLayout.cshtml
│   │           ├── FormItem/
│   │           ├── Common/
│   │           └── Container/
│   ├── Controllers/
│   │   ├── LowCodeController.cs       # 低代码 API
│   │   ├── ComponentManagerController.cs
│   │   └── DesktopController.cs
│   ├── Views/
│   │   ├── Designer/Index.cshtml      # 设计器
│   │   ├── Desktop/Index.cshtml       # 桌面
│   │   └── ComponentManager/
│   ├── wwwroot/
│   │   ├── js/
│   │   │   ├── nut-runtime.js         # 核心运行时
│   │   │   ├── designer.js            # 设计器逻辑
│   │   │   └── preview.js             # 预览逻辑
│   │   └── lib/                       # 第三方库（Vue, NutUI, ElementUI, lodash）
│   ├── Models/
│   │   ├── LowCodeModels.cs
│   │   ├── DesktopModels.cs
│   │   └── ComponentDefinition.cs
│   └── sql/                           # 数据库脚本
├── README.md
├── DESIGN.md
└── QUICKSTART.md
```

## 下一步

- 阅读 [README.md](./README.md) 了解完整功能
- 阅读 [DESIGN.md](./DESIGN.md) 了解系统架构和设计决策
- 访问 `/ComponentManager` 管理组件元数据
- 访问 `/designer` 开始设计页面
