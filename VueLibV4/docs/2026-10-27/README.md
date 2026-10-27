# VueLibV4 框架文档（2026-10-27）

> 基于当前工作区代码的**全量走查**产出：框架设计、API 参考、使用手册、框架评估（含评分）。
> 所有结论均以 2026-10-27 的代码为准，未实现的演进方向只出现在「评估/建议」章节。

## 文档清单

| 文档 | 内容 | 适合谁 |
|---|---|---|
| [01-框架设计](./01-框架设计.md) | 工程分层、DynKernel 插件内核、组件体系、Block/布局壳、参数上下文、动作管道、数据面、权限、完整数据流 | 架构师、框架维护者 |
| [02-API参考](./02-API参考.md) | 前端 `dyn/DynCom/DynCall/DynParams/DynBlocks/DynLayouts/DynDsl/DynActionHelper` API；配置契约；后端全部端点；配置项；实体 | 前后端开发者 |
| [03-使用手册](./03-使用手册.md) | 启动方式、第一个页面、组件配置、动作管道、Block 搭页、PageGen/AI、设计器、权限、扩展点、12 个常见坑、调试入口 | 业务开发者、配置人员 |
| [04-框架评估](./04-框架评估.md) | 10 维度打分（权重+证据）、总分、风险清单、TOP5 改进与验收指标 | 技术负责人、评审 |

## 30 秒结论

- **架构**：`Web → Platform → Services` 单向分层；前端由 **DynKernel** 以 9 个阶段装配 18 个模块，具备热装配与失败隔离。
- **核心能力**：配置树渲染（`_BaseLayout` 统一契约）、Block + Shell + Wire 编排、统一数据面 `/api/dyndata/*`、规则/AI 双通道页面生成、DB 动作与动作助手。
- **总分：81.2 / 100（B+）**
  - 最强：可扩展性 8.5、架构分层 8.0。
  - 最弱：测试覆盖 2.5、可维护性 6.0、规范一致性 6.5。
- **最高优先级**：① 建最小测试体系；② 安全基线（凭据出库、代码执行收敛）；③ 配置契约唯一化；④ 首屏内核拆分；⑤ 部署初始化闭环。

## 关键度量

| 指标 | 数值 |
|---|---|
| 后端项目 | 3（Web / Platform / Services），.NET 8 |
| 控制器 | 29 个，端点约 150 个 |
| 前端内核模块 | 18 个（`wwwroot/dyn`） |
| 内置动作 | 33 个（可扩展 DB 动作） |
| 布局壳 | 5 个（ListMasterDetail、TreeMasterDetail、FilterListOpenWindow、FilterListDrawer、FilterListDrawerLeft） |
| Block 应用 | 7 个（Filter/List/Detail/Tree/Tabs/Kanban/Widget）+ AppsV2 |
| 模板视图 | 20+（`Views/DynTemplates`） |
| 自动化测试项目 | 0 |

## 版本与变更

- 文档版本：v1（2026-10-27）。
- 后续代码变更若涉及配置键、端点、Block 契约，请同步更新 02 与 03 两份文档，并在本文件记录变更日期。
