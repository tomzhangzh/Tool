-- ---------------- 25. 看板列（旧库幂等建表） ----------------

CREATE TABLE KanbanList (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    BoardId     INTEGER NOT NULL DEFAULT 1,
    Title       TEXT NOT NULL,
    Color       TEXT NULL,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);