-- ---------------- 28. 看板评论（旧库幂等建表） ----------------

CREATE TABLE KanbanComment (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    CardId      INTEGER NOT NULL,
    Content     TEXT NULL,
    Author      TEXT NULL,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE INDEX IX_KanbanComment_Card ON KanbanComment(CardId);