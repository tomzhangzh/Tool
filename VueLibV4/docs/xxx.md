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