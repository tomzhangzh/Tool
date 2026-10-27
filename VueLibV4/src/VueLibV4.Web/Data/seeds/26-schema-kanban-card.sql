-- ---------------- 26. 看板卡片（旧库幂等建表） ----------------

CREATE TABLE KanbanCard (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    BoardId     INTEGER NOT NULL DEFAULT 1,
    ListId      INTEGER NOT NULL,
    Title       TEXT NOT NULL,
    Description TEXT NULL,
    Assignee    TEXT NULL,
    Priority    TEXT NOT NULL DEFAULT 'normal',
    DueDate     TEXT NULL,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    IsActive    INTEGER NOT NULL DEFAULT 1,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE INDEX IF NOT EXISTS IX_KanbanCard_Board ON KanbanCard(BoardId);
CREATE INDEX IF NOT EXISTS IX_KanbanCard_List ON KanbanCard(ListId);