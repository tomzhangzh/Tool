-- ---------------- 23. 内置模板图标自愈：早期 class 名图标修正为 Emoji（仅更新仍是旧 class 值的行） ----------------

UPDATE DynTemplate SET Icon='🧩' WHERE Code='triscreen-blocks' AND Icon='puzzle';
UPDATE DynTemplate SET Icon='🔍' WHERE Code='filterlist-crud' AND Icon='icon-table';
UPDATE DynTemplate SET Icon='📝' WHERE Code='detail-modal' AND Icon='icon-edit';
UPDATE DynTemplate SET Icon='🌳' WHERE Code='tree-detail' AND Icon='icon-tree';