-- ---------------- 14. DynTemplateBlock 建表（旧库迁移；新库 platform.sql 已含） ----------------

CREATE TABLE DynTemplateBlock (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TemplateId  INTEGER NOT NULL,
    Slot        TEXT NOT NULL,
    BlockId     INTEGER NOT NULL,
    Required    INTEGER NOT NULL DEFAULT 0,
    SortNo      INTEGER NOT NULL DEFAULT 0,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_DynTemplateBlock_Template_Slot ON DynTemplateBlock(TemplateId, Slot);