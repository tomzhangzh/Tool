-- ---------------- 27. 看板卡片 BoardId 索引（旧库补建） ----------------

CREATE INDEX IF NOT EXISTS IX_KanbanCard_Board ON KanbanCard(BoardId);