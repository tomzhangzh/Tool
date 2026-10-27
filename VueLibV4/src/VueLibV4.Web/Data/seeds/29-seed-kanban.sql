-- ---------------- 29. 看板初始种子（幂等：3 列 + 若干示例卡片，仅当 KanbanList 为空时插入） ----------------

INSERT INTO KanbanList (Id, BoardId, Title, Color, SortNo) VALUES
(1, 1, '待办',   '#909399', 1),
(2, 1, '进行中', '#409eff', 2),
(3, 1, '已完成', '#67c23a', 3);
INSERT INTO KanbanCard (ListId, Title, Description, Assignee, Priority, DueDate, SortNo) VALUES
(1, '字段变更标记 diff marker', 'FormItem label 红点，hover 看原值新值', 'Tom', 'high',  NULL, 1),
(1, 'Block 契约与 C# 强类型',   'BlockContracts.cs + Validator',      'Tom', 'normal',NULL, 2),
(2, 'Kanban 看板 Block 自举',   '用本平台做 Trello 看板管理自身任务',  'Tom', 'high',  NULL, 1),
(3, '组件 PropertyConfig gap',  '58 个组件最外层容器统一 gap=4px',      'Tom', 'low',   NULL, 1);