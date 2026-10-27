-- 模板能力位：把「规格驱动（外壳视图 @model DynWebPage）」的历史模板显式写进 ConfigJson。
-- 背景：运行时装配器 PageRuntimeComposer 优先读取模板 ConfigJson 顶层 specDriven；
--       未声明时回退 TemplateCapabilities 里的内置基线（仅为兼容存量数据）。
--       执行本脚本后，新增模板只需在 ConfigJson 里写 "specDriven": true/false，无需改任何代码。
-- 幂等：json_set 覆盖同键；Code 命中才更新；skip 非 JSON 文本（json_valid 保护）。
-- 用法：sqlite3 Platform.db ".read migrate-template-capabilities-20261028.sql"

BEGIN;

UPDATE DynTemplate
SET ConfigJson = json_set(CASE WHEN json_valid(ConfigJson) THEN ConfigJson ELSE '{}' END, '$.specDriven', json('true'))
WHERE Code IN (
    'triscreen-blocks',
    'filterlist-crud',
    'tree-detail',
    'tree-detail-v2',
    'tree-master-detail',
    'list-master-detail',
    'filter-list-open-window',
    'filter-list-open-windowV2',
    'filter-list-drawer-leftV2',
    'detail-modal',
    'detail-modal-v2',
    'filter-list-tabbed',
    'tab-host',
    'tab-container',
    'tabs-basic'
);

-- 共享模型模板（外壳视图 @model DynSharedModel）显式声明 false，避免将来被基线误判
UPDATE DynTemplate
SET ConfigJson = json_set(CASE WHEN json_valid(ConfigJson) THEN ConfigJson ELSE '{}' END, '$.specDriven', json('false'))
WHERE Code IN ('crud-basic', 'echart-demo', 'master-detail-basic', 'tree-basic', 'view-switcher-demo');

COMMIT;
