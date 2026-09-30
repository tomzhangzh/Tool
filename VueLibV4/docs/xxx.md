# VueLibV4 的亮点与独特之处
不打分了，专门说说 真正有辨识度、同类项目里少见的地方 ：

## 1. 组件定义落在 Razor View，而不是数据库字符串
绝大多数低代码平台的组件是"存进数据库的一段 HTML/JS 字符串"——难版本控制、没 IDE 高亮、无法调试。

VueLibV4 走的是 代码优先双源 ：

- 组件是真实的`.cshtml` 文件，走 Razor 引擎渲染，能享受服务端能力（注入配置、渲染分部视图、循环生成）；
- 数据库`ComponentMeta` 只存"在设计器里怎么展示和配置"的元数据；
- 解析优先级：View 有就用 View，DB 兜底。
这意味着组件 天生可 git 管理、可断点调试、可复用 Razor 生态 ，改完刷新就生效——这个开发体验在低代码圈里是少见的。

## 2. 前后端共享同一条"热重载链路"
后端启用`AddRazorRuntimeCompilation()` + csproj 关闭`RazorCompileOnBuild/Publish` （视图不进 dll），前端又是 UMD 无构建。

结果就是： 改组件`.cshtml` → 刷新浏览器，前后端同时更新 ，没有构建步骤、没有 npm、没有文件监听。对于一个"组件即核心"的平台，这条链路把开发摩擦降到了极低。

## 3. 后端下发前端动作链（dyn-actions）
普通做法是：前端调保存接口 → 拿到结果 → 写 JS 判断成功了要刷新列表、关弹窗、toast 提示。

VueLibV4 的做法是：

C#

1
2
3
4

`return ApiResult.Ok(data).WithActions(
    DynJavaScript.Toast("保存成功"),
    DynJavaScript.ReloadBlock("list"),
    DynJavaScript.CloseLayer());`

后端直接把"前端要做什么"以动作数组的形式塞进响应 ，前端统一执行器按序跑。Controller 零 JS，保存类接口的模板化程度很高。这个设计在中小型项目里非常实用。

## 4. "知道表名就能跑"的闭环
免模型 CRUD 不算独有，但 VueLibV4 把它做成了一条真正自动化的链路：

1
2
3

`选表 → PageGen 读 schema → 自动生成 Filter/List/Detail 三份 PageSetting
     → 自动建 DynWebPage 绑定三屏 → TableName 自动推导 dyndata 各接口 URL
     → 打开就是完整的查询/新增/编辑/删除页面`

TableName 留空时，`ListUrl/AddUrl/EditUrl/DeleteUrl` 由控制器`BuildEffectiveParams` 自动补齐，连接口地址都不用填。 从"一张表"到"一个可用 CRUD 页面"是全自动的 ，不需要手写一行配置。

## 5. 三屏四层的积木化抽象
很多低代码把"页面"当成一棵组件树从头搭到尾，结果每个页面都是孤立的。

VueLibV4 拆成四层：

- PageSetting ：区块配置（筛选/列表/详情各自独立存）
- DynTemplate ：外壳，决定区块怎么流转
- DynWebPage ：实例，只填参数（表名、URL、引用哪个 PageSetting）
- Block ：最小复用单元，自带 createApp
好处是： 同样的"筛选+列表+详情"骨架，换一张表就是换参数 ，页面之间可以共享区块配置。PageSetting 还支持`RenderMode=Back` 用 Razor 局部视图渲染——前端控件和后端局部视图可以混搭，灵活度很高。

## 6. 多 Vue 实例共享同一个 Reactive Model
设计器左栏/画布/右栏是 三个独立的 Vue 应用 ，但通过：

HTML

1
2
3
4
5

`<div data-dyn-shared-scope="designer" data-dyn-scope-json='{...}'>
  <div data-dyn-use-scope="designer" id="left">...</div>
  <div data-dyn-use-scope="designer" id="canvas">...</div>
  <div data-dyn-use-scope="designer" id="right">...</div>
</div>`

共享 同一份 reactive model ，`window.dyn.getScopeModel(el)` 可反查。多 App 之间既隔离（各自独立挂载/销毁）又共享状态（设计时数据联动），这个 scope 机制在无构建体系里是比较优雅的解法。

## 7. 一套组件，多套桌面壳
组件体系（DynElXxx）是统一的，但外壳可以切换：

- Windows 工作台风格（渐变背景 + 任务栏 + 窗口）
- macOS 拟物风格（`_LayoutMac` + DynMacSidebar/DynMacWindow）
- 车机暗黑大屏（`_LayoutCarDark` ）
同一批业务组件可以套进不同的桌面环境， 壳和内容分离 做对了。

## 8. 设计器的"悬浮选中层不包 DOM"
选中态没有用包一层`<div class="selected-wrapper">` 的做法（那会破坏组件布局和事件），而是用一个 独立的绝对定位 overlay 层 （`pointer-events:none` ），只在顶部工具条处开放交互。组件本身的 DOM 结构完全不受选中态影响——这个细节在拖拽型设计器里很关键，少走了很多弯路。




### 模板不能"一刀切"——有三个还在挂线上功能
A. 可立即清理（无任何页面引用，删文件 + 停用 DB 模板行即可）：

- TreeBasic.cshtml （DynTemplate Id=3）
- MasterDetailBasic.cshtml （Id=4）
- EChartDemo.cshtml （Id=5）
- ViewSwitcherDemo.cshtml （Id=6）
- CarDashboardDemo.cshtml （Id=7）
- 清理前只需核对一下"示例 Demo"菜单（SysMenu Id=34/35/37 等）和桌面快捷方式的 Url 是否还指向它们的演示路由。
B. 有硬依赖，必须先迁移再删：

- CrudBasic.cshtml （Id=2）—— 平台自己的 5 个管理页正在用 （PageSetting 管理、DynTemplate 管理、DynWebPage 列表等），且是 PageController.cs#L258 的兜底 ViewPath， Seeder.cs#L245 也在种它。需要先把这 5 个页迁到 filterlist-crud，再改种子和兜底路径。
- DetailTemplate.cshtml ——没有模板行，但`/Platform/Page/DynDetail` 片段端点（ PageController.cs#L370 ）硬编码渲染它，需先确认有没有列表 open 动作在调这个地址。
- MacDesktopDemo.cshtml ——有独立硬编码路由 PageController.cs#L164-L169 。
- Partials/ListBack.cshtml ——还被 PageSetting 种子`setting-list-basic` （RenderMode=Back）引用，保留。
## 二、建议新增的 DynBlocks（按优先级）
P0 — 先把现有大模板拆出积木，复用度最高：

1. TreeBlock ：把 TreeDetail 里的树（加载/增删改/拖拽/搜索过滤）抽成独立积木。抽完后 TreeDetail 退化为"TreeBlock + DetailBlock 的组合壳"，并能解锁树+列表、树+图表等形态。
2. ChildTableBlock（子表/明细表格） ：可编辑表格（行内新增/删除），主子表保存，主从页面必备。
3. ToolbarBlock（操作工具栏） ：新增/批量删除/导出/导入/列设置按钮位，被各模板复用。
P1 — 仪表盘与展示类：

4. StatCardBlock（KPI 指标卡） ：数字 + 同比 + 图标，支持从接口取值。
5. ChartBlock（ECharts 图表） ：配置驱动（柱/折/饼/漏斗），经消息中心接收筛选条件，替代 EChartDemo 那种整页 Demo。
6. CardListBlock（卡片网格） ：图片/标题/标签/操作，替代 ViewSwitcherDemo。
7. DescriptionBlock（只读详情） ：el-descriptions 只读展示。
P2 — 容器与高级场景：

8. TabsBlock（标签页容器） ：每页签内挂子积木（多列表/列表+图表混排）。
9. SplitLayoutBlock（分栏容器） ：可拖拽分割线，组合任意左右积木。
10. WizardBlock（分步表单） 、 ImportBlock（Excel 导入/导出） 、 AttachmentBlock（附件上传） 、 KanbanBlock / CalendarBlock（看板/日历） 。
## 三、建议新增的 Templates（全部用积木拼装，不再写整页逻辑）
P0 常用后台形态：

1. tree-list（左树+右列表） ——分类树导航 + 右列表弹窗 CRUD（组织机构-员工、商品分类-商品、菜单-权限），使用频率最高，抽完 TreeBlock 即可拼。
2. master-detail（主从表） ——上主表下子表（订单+明细、单据+分录），替代旧的 MasterDetailBasic。
3. tree-table（树形表格） ——el-tree-v2 同体树表，自引用表内联展开编辑，是 tree-detail 的"表格版"补充。
P1：

4. dashboard（数据仪表盘） ——StatCard + Chart + Filter 组合。
5. list-drawer（列表+右侧抽屉详情） ——比弹窗更适合宽表单/长时间停留编辑，是 filterlist-crud 的抽屉版。
6. permission-allot（权限分配） ——穿梭框/树勾选（角色→菜单授权、用户→角色），后台系统几乎必做。
P2：

7. tab-multi（多标签多列表） 、8. wizard-form（分步表单页） 、9. kanban / calendar（任务看板、日程） 、10. workbench（工作台门户：快捷入口+待办+图表） 。
建议落地顺序： 先清理 A 组旧模板 → 抽 TreeBlock（顺带给 TreeDetail 瘦身）→ 出 tree-list 和 master-detail 两个模板 ，这三步收益最大、风险最小。