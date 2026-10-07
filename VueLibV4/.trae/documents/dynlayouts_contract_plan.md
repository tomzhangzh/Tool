# DynLayouts 强类型契约收敛 + 页面关系图 实施计划

## 一、调研结论（现状与问题）

### 1.1 契约三份事实源，互相漂移
| 事实源 | 位置 | 状态 |
|---|---|---|
| C# 强类型契约 | [BlockContracts.cs](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Platform/Models/BlockContracts.cs) + [BlockContractValidator.cs](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Platform/Services/BlockContractValidator.cs) | **完全悬空**：未注册 DI、无导出端点、保存时不校验 |
| DB 字符串列 | `DynBlock.Commands / Events`（[PlatformModels.cs:475-481](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Platform/Models/PlatformModels.cs#L475-L481)） | 仅文档用途，运行时无人读 |
| 前端字符串数组 | [dyn-blocks-common.js:24-29](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/wwwroot/dyn/blocks/dyn-blocks-common.js#L24-L29) `DynBlocks.CONTRACTS` | **运行时唯一生效** |

漂移实证：
- C# 声明 filter 有 `reset` 命令，但 [FilterApp.cshtml:101](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/DynBlocks/Apps/FilterApp.cshtml#L101) 只 `reg('submit')`，reset 是 Vue 内部方法。
- C# `FilterCondition{Field,Op,Value}` + `FilterConditionPayload{Filter}` 与真实载荷不符：FilterApp `emit('changed', buildSubmit())` 发出的是**裸对象** `{字段:{op,value}}`（[FilterApp.cshtml:72-89](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/DynBlocks/Apps/FilterApp.cshtml#L72-L89)）；包装成 `{filter:...}` 是壳 wire 的 `with:{filter:'$event'}` 做的（[ListMasterDetail.cshtml:173](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/DynLayouts/ListMasterDetail.cshtml#L173)）。
- C# 缺 tree（实际有 reload/nodeClick/addRoot/addChild）、tabs（openNew/openEdit/saved）。
- Validator 的 payload 类型兼容检查没有考虑 `with` 映射会变形载荷，逻辑上不成立。
- 环检测对默认线 `master→detail` + `detail.saved→master` 必报"潜在循环"，主从结构天生如此，只能是 warning。

### 1.2 壳运行时
- 引擎 [dyn-layout-engine.js](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/wwwroot/dyn/layouts/dyn-layout-engine.js)：壳注册表 + 挂 BlockApp + 绑 wire（端口缺失只 warning）+ 就绪握手。壳定义（slots/defaultWires/commands/onMount）硬编码在两个 cshtml 的 `register()`：
  - [ListMasterDetail.cshtml:170-249](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/DynLayouts/ListMasterDetail.cshtml#L170-L249)：5 条 defaultWires，shell.newChild 转发外键。
  - [TreeMasterDetail.cshtml:182-198](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/DynLayouts/TreeMasterDetail.cshtml#L182-L198)：4 条 defaultWires。
- 页面规格权威字段 `DynWebPage.SpecJson`（壳模型 v1.1），空则回退 ParamsJson（[DynPageViewHelper.cs:99-100](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Core/DynPageViewHelper.cs#L99-L100)）。
- SpecJson 保存路径：页面管理扩展 [page-mgmt.ext.cshtml](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/DynPages/Ext/page-mgmt.ext.cshtml) 文本框编辑，当前只做 JSON 合法性校验（`shellSync/specValid`），经通用 dyndata save 落库，没有专用保存端点。

### 1.3 四实体关系
```
DynWebPage ──TemplateId──▶ DynTemplate ──DynTemplateBlock(slot,BlockId)──▶ DynBlock(ImplementsRole,ViewPath)
   │ SpecJson.slots[槽].settingId ─────────────────────────────────────────▶ PageSetting(ConfigJson)
   │ SpecJson = {layout, layoutProps, provide, slots:{name:{block,settingId}}, wires[], replaceDefaultWires}
   └ ParamsJson.blocks[slot].settingId（旧扁平结构，回退兼容）
```

### 1.4 PageGraphDemo 现状
[PageController.cs:192-260](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Areas/Platform/Controllers/PageController.cs#L192-L260) 服务端拼 mermaid：页面 id 写死默认 34；只读旧 ParamsJson.blocks；消息边 `B0==>B1` 写死；无壳节点、无真实 wire、无 with 标注；[视图](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Web/Views/Platform/Page/PageGraphDemo.cshtml)仅渲染 mermaid 源码。本地已有 `wwwroot/lib/mermaid.min.js`。

### 1.5 各 Block 真实端口（从 Apps 源码核实）
| role | 命令（reg） | 事件（emit）与真实载荷 |
|---|---|---|
| filter | submit | changed: 裸 FilterBag `{字段:{op,value}}` |
| list | loadData(`{filter:FilterBag}`)、reload(无载荷) | add(`{}`)、edit(`{row}`)、addChild(`{row}`) |
| detail | newForm(`{preFill}`)、editForm(`{row}`) | saved(`{action:'add'/'edit'/'delete', row}`；delete 时 row 是主键标量)、cancel(`{}`) |
| tree | reload | nodeClick(`{row}`)、addRoot(`{}`)、addChild(`{preFill}`) |
| tabs | openNew(`{preFill?}`，转发内部 detail)、openEdit(`{row}`) | saved(透传 detail.saved 载荷) |

### 1.6 DI 机制
程序集扫描自动注册 IDependency 体系（[ServiceCollectionExtensions.cs](file:///e:/Tom/Tool/VueLibV4/src/VueLibV4.Services/Dependency/ServiceCollectionExtensions.cs)）。契约类需新增一个 `ISingletonDependency` 目录服务承接。

## 二、设计决策（已与用户确认）

1. **C# 契约为唯一权威**：修正/补齐后经 API 导出，前端启动拉取替换手写 CONTRACTS；DB 的 Commands/Events 列不删（避免迁移），加注释标明已废弃、仅展示。
2. **壳仍是真代码**：onMount/壳命令函数留在 cshtml；壳元数据（slots/defaultWires/命令名清单）抽到**共享 JS 单文件**，两个壳与关系图页面共用——避免 C# 再抄一份壳连线。
3. **PageGraphDemo 真实数据只读图**：mermaid 客户端渲染，可选页面；实体边来自服务端 DTO，wire 边来自壳元数据 defaultWires + SpecJson.wires（与引擎同合并规则）。
4. **契约覆盖**：filter/list/detail/tree/tabs 五个角色（不含 kanban）。
5. **保存强校验落点**：SpecJson 走通用 dyndata 保存、无专用端点，因此在 page-mgmt.ext 保存前调校验端点：**errors 阻断保存，warnings 放行**（等价于现有 specValid 拦截 UX），不在通用保存管道里埋钩子。

## 三、改动文件清单

### 后端（VueLibV4.Platform）

1. **`Models/BlockContracts.cs`（改写）**
   - 载荷模型对齐运行时：`FilterCondition{Op,Value}`（去掉 Field，字段名是字典键）；`FilterBag = Dictionary<string,FilterCondition>`（裸包，changed 直接发它）；`RowBag{Row}`、`PreFillBag{PreFill}`、`SavedBag{Action,Row}`。
   - 每个端口增加 `PayloadKind`（稳定字符串码：`"FilterBag"|"RowBag"|"PreFillBag"|"SavedBag"|null`），供导出 JSON 使用（Type 不跨语言）；`PayloadType` 保留供 C# 侧校验/反射。
   - 修正 filter 命令仅 `submit`（删 reset 误报）；list.loadData 载荷=`{filter:FilterBag}` 用新的 `ListLoadDataPayload{Filter}` 表达；reload 载荷 null。
   - 新增 `TreeBlockContract`（reload / nodeClick,addRoot,addChild）、`TabsBlockContract`（openNew,openEdit / saved）。
   - 新增导出 DTO：`BlockContractSetDto`（roles→命令/事件名+描述+payloadKind）、`PayloadShapeDto`（kind→字段说明，给图和调试浮层用）。

2. **`Services/BlockContractCatalog.cs`（新增）**：`ISingletonDependency`。显式持有 5 个契约实例；提供 `ByRole`、`All`、`BuildExportDto()`。

3. **`Services/BlockContractValidator.cs`（修正）**
   - 构造改为接 `BlockContractCatalog`（不再要求 DI 注册 `IEnumerable<IBlockContract>`）。
   - 提供 `ValidateSpec(PageLayoutSpec spec, IEnumerable<WireSpec>? defaultWires)`：按引擎同规则合并 default + page（ReplaceDefaultWires），端口校验。
   - payload 兼容检查改为 **with 感知**：wire 显式带 `with` 时不做载荷结构比对（载荷已被映射变形），仅当无 with 透传时比对 event/cmd 的 PayloadKind。
   - 环检测保留 warning 级，消息措辞改为"事件级环不一定致命（如 saved→reload 是请求/响应模式），请人工确认"。
   - `PageLayoutSpec/WireSpec/LayoutSlotSpec` 反序列化复用现有类（BlockContracts.cs 下半部）。

### 后端（VueLibV4.Web）

4. **`Areas/Platform/Controllers/BlockContractController.cs`（新增）**，路由 `api/platform/blockcontract`，仿 DynWebPageController 的 ApiResult 风格与现有鉴权约定：
   - `GET all` → 契约导出 DTO（含 roles + payload 形状）。
   - `POST validate`，body `{spec:{...PageLayoutSpec}, defaultWires:[...]}`（defaultWires 由前端按所选壳元数据传入，后端不持有壳连线）→ `{valid,errors[],warnings[]}`。
   - `GET page-graph?id=` → 关系图 DTO：`{page:{id,name,code}, template:{id,name,code,viewPath}?, layout, layoutProps, provideKeys, slots:[{slot, blockRole, blockName, blockId, settingId, settingName?}], wires:[{from,to,with,origin}], replaceDefaultWires}`。数据组装：PageController 已注入的 5 个服务照搬到本控制器（IDynWebPageService/IDynTemplateService/IDynTemplateBlockService/IDynBlockService/IPageSettingService）；槽位信息 SpecJson.slots 优先，缺槽回退 DynTemplateBlock→DynBlock；setting 名查 PageSetting。

### 前端

5. **`wwwroot/dyn/layouts/dyn-shell-meta.js`（新增）**：普通脚本（非 kernel 插件，无异步依赖），挂 `window.DynShells = { META: { 'list-master-detail': {label, slots:{filter:{expect,label},...}, defaultWires:[...原样...], commands:['newChild','log']}, 'tree-master-detail': {...} } }`。defaultWires 从两个 cshtml **平移**，保证唯一来源。

6. **`wwwroot/dyn/blocks/dyn-blocks-common.js`（改）**：
   - 内置最小 fallback CONTRACTS（拉取失败时保活，即现有 4 角色数组）。
   - 新增 `DynBlocks.loadContracts()`：GET `/api/platform/blockcontract/all`，结果规范化为 `{role:{commands:[name...],events:[name...], _rich:{...}}}` 写入 `DynBlocks.CONTRACTS`，缓存到 `DynBlocks._contractsPromise`（并发调用复用同一 Promise）；失败 catch 后保留 fallback。
   - 提供 `DynBlocks.contractsReady`（同一个 promise，永不 reject）。

7. **`wwwroot/dyn/layouts/dyn-layout-engine.js`（改）**：`mount` 在 `Promise.all(ready)` 链之前先 `await DynBlocks.contractsReady`（无 DynBlocks 时跳过）；`bindWires` 兼容富结构（端口名数组做 normalize）。其余行为不动。

8. **`Views/DynLayouts/ListMasterDetail.cshtml` / `TreeMasterDetail.cshtml`（改）**：
   - 多引一行 `<script src="~/dyn/layouts/dyn-shell-meta.js">`（在 engine 之前）。
   - `register(name, Object.assign({}, DynShells.META[name], { commands:{...函数}, onMount:function(){...} }))`：slots/defaultWires 改为引用 meta，命令函数与 onMount 原样保留。

9. **`Views/Platform/Page/PageGraphDemo.cshtml（重写）`**：
   - 顶部页面选择下拉（GET `/api/platform/dynwebpage/all` 填充，URL 参数 id 预选）。
   - 拉 `blockcontract/page-graph?id=` DTO，客户端拼 mermaid：
     - 节点：WP（蓝）→ T（橙，模板）；每个槽一个 Block 节点（图标按角色，标注 slot 名）；PageSetting 紫节点，虚线标注"配置"；壳节点（灰，layout 名）连到各 Block 表示槽位归属。
     - wire 边：`DynShells.META[layout].defaultWires`（origin=default，实线）+ DTO.wires（origin=page，粗实线/不同色）；边标签 `事件名 → 命令名`，有 with 时附小字 `with:{...}`；非法端口（DTO 校验或前端对 contracts 比对）边标红并在标签注明。
     - 引 `/dyn/layouts/dyn-shell-meta.js`；mermaid 仍用 `~/lib/mermaid.min.js`；保留"查看源码"details。
   - PageController.PageGraphDemo action 瘦身为仅返回视图（默认 id 保留）。

10. **`Views/DynPages/Ext/page-mgmt.ext.cshtml（改）**：`shellSync` JSON 合法后，调 `POST /api/platform/blockcontract/validate`（body 带 `DynShells.META[obj.layout].defaultWires`；需要在该 ext 页引 dyn-shell-meta.js，或在请求前判断壳存在）。errors 非空→置 specValid=false、specError=错误清单、阻断 onSave；warnings→console/ElMessage 提示但放行。沿用现有 DynCall/fetch 风格（实现时按该文件现有 HTTP 封装对齐）。

## 四、实施顺序（依赖序）

1. 后端契约模型改写 + Catalog + Validator 修正（1-3）。
2. 控制器 3 个端点（4）；`dotnet build` 0 错误。
3. 前端壳元数据抽离 + 两个壳切换引用（5、8）——纯重构，浏览器验证两个壳页面行为不变。
4. 前端契约拉取 + engine await（6、7）——浏览器验证无新增"未声明端口"告警。
5. PageGraphDemo 重写（9）——多页面抽查图形。
6. page-mgmt.ext 保存校验（10）——正反用例（故意写错端口被拦）。
7. 全量验证（见下）。

## 五、验证

- `dotnet build src\VueLibV4.Web\VueLibV4.Web.csproj -c Debug --nologo -v q` → 0 错误（警告维持既有，不新增）。
- PowerShell 登录（admin/admin123，cookie 维持）：
  - `GET /api/platform/blockcontract/all` 含 5 角色，filter.commands 无 reset，payloadKind 齐全。
  - 取所有含 SpecJson 的壳页面，逐页 POST validate：现有页面 **errors=0**（warnings 可接受）。
  - 构造反例（from:'filter.bogus'、未知 block、to 端口错）→ errors 命中。
- 浏览器（http://localhost:5010，运行时编译无需重启；改了后端 .cs 需确认站点重启）：
  - list-master-detail / tree-master-detail 实际页面：查询→列表加载、新增/编辑/"+子"、保存后刷新全部正常；console 只有与改造前相同的告警。
  - PageGraphDemo：切换页面，节点/虚线配置边/实线 wire 边与真实 defaultWires 一致；无 setting 的槽不出现紫节点。
  - 页面管理：SpecJson 写坏端口被拦截并提示；合法规格可正常保存。

## 六、风险与处理

- **契约接口异步化时序**：BlockApp 挂载不依赖契约，仅 wire 绑定需要；engine 在绑线前 await，挂载顺序不受影响。接口失败用内置 fallback，页面功能不瘫。
- **壳元数据平移误差**：平移时逐字符比对现有 defaultWires；验证环节用"页面行为不变 + 告警条数不变"兜底。
- **delete 载荷是标量主键**：在 SavedBag 描述/导出 shape 文档里注明 row 联合类型，不强行强类型（与前端真实行为一致）。
- **环检测误报**：保持 warning、措辞弱化；不阻断主从这种请求/响应式双向边。
- **Tabs 不在两个壳的槽位里**：契约只进权威表和导出，不影响现有壳；PageGraph 遇到 tabs 角色也能画。
- **mermaid.min.js**：工作区该文件有既存未提交改动，不触碰。
- **DB DynBlock.Commands/Events 列**：本轮不删不改（无迁移），仅在实体注释中标"已被 C# 契约取代"。
