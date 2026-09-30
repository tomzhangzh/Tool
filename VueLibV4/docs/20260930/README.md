# VueLibV4 文档（2026-09-30 版）

本目录是按 **2026-09-30 代码库实际状态** 重新梳理的一套文档，替代上一版散落在 `docs/` 根目录的同名文档。

## 文档清单

| 文档 | 内容 | 读者 |
|------|------|------|
| [快速开始.md](./快速开始.md) | 环境要求、启动、地址速查、设计器/CRUD 上手、组件开发入门、FAQ | 所有人，先读这篇 |
| [设计方案.md](./设计方案.md) | 三层项目分层、启动管线、双库 11 表、组件双定义源、前端无打包架构、三屏四层模型、拖拽内核、已知取舍 | 架构理解 / 二次开发 |
| [使用说明.md](./使用说明.md) | API 手册（两套 dyndata、平台 CRUD、通用辅助接口）、页面操作、组件规范、动作系统、校验器、前端配置、运维速查 | 日常开发查阅 |
| [项目亮点与改进建议.md](./项目亮点与改进建议.md) | 8 大亮点与独特设计、7 项不足、7 个方向（A~G）的改进创意、按 ROI 排序的三梯队优先级、综合评分 | 产品规划 / 技术决策 |
| [头脑风暴.md](./头脑风暴.md) | 创意讨论记录：能力桥（RPC 网关 / 枚举绑定 / source:// 统一数据源）+ 20 个天马行空产品创意（近期/中期/远期分级） | 灵感参考，非实施方案 |

## 本版相对旧文档的主要更新

1. **项目结构**：补充三个项目（Web / Platform / Services）的分层职责。
2. **数据库**：从单库更新为**双 SQLite**（Platform.db + Business.db）；平台表从 9 张更新为 **11 张**（新增 `PageSetting` 三屏配置、`SysMenu` 系统菜单）。
3. **组件命名**：组件体系已换代为 `DynElXxx`（Element UI）/ `DynXxx`（公共）规范，旧文档中的 `ComInput` 等示例同步更新；组件目录改为 `Areas/Component/Views/`。
4. **三屏积木体系**：新增 PageSetting / DynTemplate / DynWebPage / Block 四层模型、页面生成向导（PageGen）、模板引擎运行时入口说明。
5. **Razor 运行时编译**：记录"视图不编译进 dll"的当前约定（RuntimeCompilation 包 + 三个 RazorCompile 开关）。
6. **设计器拖拽**：记录嵌套容器拖拽修复（直接子元素隔离、透明包装层不重复初始化）与红绿放置反馈、空容器占位符改进。
7. **API 更新**：补充 `/api/platform/dyndata`（模板引擎用）与 `/api/business/dyndata`（完整版）两套接口、`/api/dyncommon/*`（emoji/options/dicts/tablelist/upload）、统一信封的 `dyn-actions` 服务端动作链。
8. **新增页面**：PageGen 向导、DSL 编辑器、MetaDsl、Mac 桌面 Demo、SysMenu 等管理页地址速查。
9. **缓存约定**：dyn 模块 JS 版本号机制与 dyn.css 的 `asp-append-version` 哈希机制。

## 仓库内仍有参考价值的旧文档

- `docs/动态页面与三屏积木体系规范.md` —— 三屏体系最详尽的规范（字段、dynmodel 机制、挂载顺序、踩坑），本版文档引用它，不重复展开。
- `docs/PageSetting设计文档.md` —— PageSetting 的设计推演记录。
- `docs/M1-M2验收测试报告.md` / `docs/M3-M4验收测试报告.md` —— 阶段性验收留档。
- `docs/VueLibV4 架构设计与完整亮点总结.md`、`docs/VueLibV4核心开发备忘录.md`、`docs/V1对比与待实现清单.md`、`docs/实施规划-对齐核心原则.md` —— 历史设计与规划记录，部分内容已被本版文档取代，阅读时请注意时效。
