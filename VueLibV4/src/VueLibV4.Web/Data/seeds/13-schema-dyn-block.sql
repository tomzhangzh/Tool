-- ---------------- 13. DynBlock 建表（旧库迁移；新库 platform.sql 已含） ----------------

CREATE TABLE DynBlock (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    Code              TEXT NOT NULL UNIQUE,
    Name              TEXT NOT NULL,
    Category          TEXT NULL,
    ImplementsRole    TEXT NOT NULL DEFAULT '',
    ViewPath          TEXT NULL,
    HtmlCode          TEXT NULL,
    ScriptCode        TEXT NULL,
    ParamConfigJson   TEXT NULL,
    ParamDefaultJson  TEXT NULL,
    Commands          TEXT NULL,
    Events            TEXT NULL,
    Description       TEXT NULL,
    SortNo            INTEGER NOT NULL DEFAULT 0,
    IsActive          INTEGER NOT NULL DEFAULT 1,
    CreateTime        TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);