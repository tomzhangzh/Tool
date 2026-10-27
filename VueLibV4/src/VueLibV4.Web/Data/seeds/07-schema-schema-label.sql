-- ---------------- 7. 表结构显示名字典（PageGen 中文名来源；旧库迁移） ----------------

CREATE TABLE DynSchemaLabel (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    ProjectId   INTEGER NOT NULL DEFAULT 0,
    TableName   TEXT NOT NULL DEFAULT '',
    ColumnName  TEXT NOT NULL DEFAULT '',
    Label       TEXT NOT NULL,
    CreateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime')),
    UpdateTime  TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);
CREATE UNIQUE INDEX UX_DynSchemaLabel_Key ON DynSchemaLabel(ProjectId, TableName, ColumnName);
